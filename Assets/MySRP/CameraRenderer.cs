using UnityEngine;
using UnityEngine.Rendering;
using static MyRenderPipelineAsset;

public partial class CameraRenderer
{
    ScriptableRenderContext context;
    Camera camera;
    const string bufferName = "Render Camera";
    CommandBuffer buffer = new CommandBuffer { name = bufferName };
    CullingResults cullingResults;
    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"),
                       litShaderTagId = new ShaderTagId("CustomLit");
    Lighting lighting = new Lighting();
    PostFXStack postFXStack = new PostFXStack();
    static int bufferSizeId = Shader.PropertyToID("_CameraBufferSize"),
               colorAttachmentId = Shader.PropertyToID("_CameraColorAttachment"),
               depthAttachmentId = Shader.PropertyToID("_CameraDepthAttachment"),
               colorTextureId = Shader.PropertyToID("_CameraColorTexture"),
               depthTextureId = Shader.PropertyToID("_CameraDepthTexture"),
               sourceTextureId = Shader.PropertyToID("_SourceTexture"),
               srcBlendId = Shader.PropertyToID("_CameraSrcBlend"),
               dstBlendId = Shader.PropertyToID("_CameraDstBlend");
    bool useHDR, useScaledRendering;
    static CameraSettings defaultCameraSettings = new CameraSettings();
    bool useColorTexture, useDepthTexture, useIntermediateBuffer;
    Material material;
    Texture2D missingTexture;
    static bool copyTextureSupported = SystemInfo.copyTextureSupport > CopyTextureSupport.None;
    static Rect fullViewRect = new Rect(0f, 0f, 1f, 1f);
    Vector2Int bufferSize;
    public const float renderScaleMin = 0.1f, renderScaleMax = 2f;

    public void Render(ScriptableRenderContext context, Camera camera, CameraBufferSettings bufferSettings, bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject, ShadowSettings shadowSettings, PostFXSettings postFXSettings, int colorLUTResolution)
    {
        this.context = context;
        this.camera = camera;
        var crpCamera = camera.GetComponent<MyRenderPipelineCamera>();
        CameraSettings cameraSettings = crpCamera ? crpCamera.Settings : defaultCameraSettings;
        //useDepthTexture = true;
        if (camera.cameraType == CameraType.Reflection)
        {
            useColorTexture = bufferSettings.copyColorReflection;
            useDepthTexture = bufferSettings.copyDepthReflection;
        }
        else 
        {
            useColorTexture = bufferSettings.copyColor && cameraSettings.copyColor;
            useDepthTexture = bufferSettings.copyDepth && cameraSettings.copyDepth;
        }
        if (cameraSettings.overridePostFX)
            postFXSettings = cameraSettings.postFXSettings;

        float renderScale = cameraSettings.GetRenderScale(bufferSettings.renderScale);
        useScaledRendering = renderScale < 0.99f || renderScale > 1.01f;        // Give it a variance
        PrepareBuffer();
        PrepareForSceneWindow();
        if (!Cull(shadowSettings.maxDistance))
            return;

        useHDR = bufferSettings.allowHDR && camera.allowHDR;
        if (useScaledRendering)
        {
            renderScale = Mathf.Clamp(renderScale, renderScaleMin, renderScaleMax);
            bufferSize.x = (int)(camera.pixelWidth * renderScale);
            bufferSize.y = (int)(camera.pixelHeight * renderScale);
        }
        else
        { 
            bufferSize.x = camera.pixelWidth;
            bufferSize.y = camera.pixelHeight;
        }

        buffer.BeginSample(SampleName);
        buffer.SetGlobalVector(bufferSizeId, new Vector4(1f / bufferSize.x, 1f / bufferSize.y, bufferSize.x, bufferSize.y));
        ExecuteBuffer();
        lighting.Setup(context, cullingResults, shadowSettings, useLightsPerObject, (cameraSettings.maskLights ? cameraSettings.renderingLayerMask : -1));
        bufferSettings.fXAA.enabled &= cameraSettings.allowFXAA;
        postFXStack.Setup(context, camera, bufferSize, postFXSettings, cameraSettings.keepAlpha, useHDR, colorLUTResolution, cameraSettings.finalBlendMode, bufferSettings.bicubicRescaling, bufferSettings.fXAA);
        buffer.EndSample(SampleName);
        Setup();
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing, useLightsPerObject, cameraSettings.renderingLayerMask);  // Pass batch setting
        //DrawGizmos();
        DrawGizmosBeforeFX();
        DrawUnsupportedShaders();
        if (postFXStack.isActive)
        {
            postFXStack.Render(colorAttachmentId);
        }
        else if (useIntermediateBuffer)
        {
            DrawFinal(cameraSettings.finalBlendMode);
            ExecuteBuffer();
        }
        DrawGizmosAfterFX();
        Cleanup();
        Submit();
    }

    void Setup()
    {
        context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;
        useIntermediateBuffer = useScaledRendering || useColorTexture || useDepthTexture || postFXStack.isActive;
        if (useIntermediateBuffer)
        {
            if (flags > CameraClearFlags.Color)
            { 
                flags = CameraClearFlags.Color;
            }
            buffer.GetTemporaryRT(colorAttachmentId, bufferSize.x, bufferSize.y, 0, FilterMode.Bilinear, (useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default));
            buffer.GetTemporaryRT(depthAttachmentId, bufferSize.x, bufferSize.y, 32, FilterMode.Point, RenderTextureFormat.Depth);
            buffer.SetRenderTarget(colorAttachmentId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, depthAttachmentId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        }
        buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags <= CameraClearFlags.Color, (flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear));
        buffer.BeginSample(SampleName);
        buffer.SetGlobalTexture(colorTextureId, missingTexture);
        buffer.SetGlobalTexture(depthTextureId, missingTexture);
        ExecuteBuffer();
    }

    void Cleanup()
    { 
        lighting.Cleanup();
        if (useIntermediateBuffer)
        { 
            buffer.ReleaseTemporaryRT(colorAttachmentId);
            buffer.ReleaseTemporaryRT(depthAttachmentId);
            if (useColorTexture)
            {
                buffer.ReleaseTemporaryRT(colorTextureId);
            }
            if (useDepthTexture)
            {
                buffer.ReleaseTemporaryRT(depthTextureId);
            }
        }
    }

    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject, int renderingLayerMask)
    {
        PerObjectData lightsPerObjectFlags = useLightsPerObject ? (PerObjectData.LightData | PerObjectData.LightIndices) : PerObjectData.None;

        // Opaque
        var sortSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortSettings) { enableDynamicBatching = useDynamicBatching, enableInstancing = useGPUInstancing, 
                                                                                    perObjectData = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.ShadowMask| PerObjectData.LightProbe | PerObjectData.OcclusionProbe| PerObjectData.LightProbeProxyVolume | PerObjectData.OcclusionProbeProxyVolume | lightsPerObjectFlags };
        drawingSettings.SetShaderPassName(1, litShaderTagId);
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque, renderingLayerMask: (uint)renderingLayerMask);
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        // Skybox
        context.DrawSkybox(camera);

        // Depth Texture for other use (PostFX, Soft Particles etc.) Or Color Texture
        if (useColorTexture || useDepthTexture)
        { 
            CopyAttachments();
        }

        // Transparent
        sortSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    void Submit()
    {
        buffer.EndSample(SampleName);
        ExecuteBuffer();
        context.Submit();
    }

    void ExecuteBuffer()
    { 
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    bool Cull(float maxShadowDistance)
    {
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            cullingResults = context.Cull(ref p);
            return true; 
        }
        return false;
    }

    void CopyAttachments()
    {
        if (useColorTexture)
        { 
            buffer.GetTemporaryRT(colorTextureId, bufferSize.x, bufferSize.y, 0, FilterMode.Bilinear, (useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default));
            if (copyTextureSupported)
            {
                buffer.CopyTexture(colorAttachmentId, colorTextureId);
            }
            else
            {
                Draw(colorAttachmentId, colorTextureId);
            }
        }
        if (useDepthTexture)
        {
            buffer.GetTemporaryRT(depthTextureId, bufferSize.x, bufferSize.y, 32, FilterMode.Point, RenderTextureFormat.Depth);
            if (copyTextureSupported)
            {
                buffer.CopyTexture(depthAttachmentId, depthTextureId);
            }
            else
            {
                Draw(depthAttachmentId, depthTextureId, true);
            }
        }
        if (!copyTextureSupported) 
        {
            buffer.SetRenderTarget(colorAttachmentId, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, depthAttachmentId, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        }
        ExecuteBuffer();
    }

    public CameraRenderer(Shader shader)
    { 
        material = CoreUtils.CreateEngineMaterial(shader);
        missingTexture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave, name = "Missing" };
        missingTexture.SetPixel(0, 0, Color.white * 0.5f);
        missingTexture.Apply(true, true);
    }

    public void Dispose()
    { 
        CoreUtils.Destroy(material);
        CoreUtils.Destroy(missingTexture);
    }

    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, bool isDepth = false)
    {
        buffer.SetGlobalTexture(sourceTextureId, from);
        buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity, material, isDepth ? 1 : 0, MeshTopology.Triangles, 3);
    }

    void DrawFinal(CameraSettings.FinalBlendMode finalBlendMode)
    {
        buffer.SetGlobalFloat(srcBlendId, (float)finalBlendMode.source);
        buffer.SetGlobalFloat(dstBlendId, (float)finalBlendMode.destination);
        buffer.SetGlobalTexture(sourceTextureId, colorAttachmentId);
        buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, finalBlendMode.destination == BlendMode.Zero && camera.rect == fullViewRect ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
        buffer.SetViewport(camera.pixelRect);
        buffer.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3);
        buffer.SetGlobalFloat(srcBlendId, 1f);
        buffer.SetGlobalFloat(dstBlendId, 0f);
    }
}
