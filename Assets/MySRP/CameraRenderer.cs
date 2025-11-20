using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRender
{
    ScriptableRenderContext context;
    Camera camera;
    const string bufferName = "Render Camera";
    CommandBuffer buffer = new CommandBuffer { name = bufferName };
    CullingResults cullingResults;
    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

    public void Render(ScriptableRenderContext context, Camera camera)
    {
        this.context = context;
        this.camera = camera;

        PrepareBuffer();
        PrepareForSceneWindow();
        if (!Cull())
            return;

        Setup();
        DrawVisibleGeometry();
        DrawGizmos();
        DrawUnsupportedShaders();
        Submit();
    }

    void Setup()
    {
        context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;
        buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags <= CameraClearFlags.Color, (flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear));
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
    }

    void DrawVisibleGeometry()
    {
        // Opaque
        var sortSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortSettings);
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        // Skybox
        context.DrawSkybox(camera);

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

    bool Cull()
    {
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            cullingResults = context.Cull(ref p);
            return true; 
        }
        return false;
    }
}
