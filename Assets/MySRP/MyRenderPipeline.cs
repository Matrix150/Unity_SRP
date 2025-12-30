using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class MyRenderPipeline : RenderPipeline
{
    CameraRender renderer = new CameraRender();
    bool useDynamicBatching, useGPUInstancing, useLightsPerObject;
    ShadowSettings shadowSettings;
    PostFXSettings postFXSettings;
    bool allowHDR;
    int colorLUTResolution;

    // Constructor
    public MyRenderPipeline(bool allowHDR, bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher, bool useLightsPerObject, ShadowSettings shadowSettings, PostFXSettings postFXSettings, int colorLUTResolution)
    {
        this.allowHDR = allowHDR;
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        this.useLightsPerObject = useLightsPerObject;
        this.shadowSettings = shadowSettings;
        this.postFXSettings = postFXSettings;
        this.colorLUTResolution = colorLUTResolution;
        GraphicsSettings.lightsUseLinearIntensity = true;

        InitializeForEditor();
        this.colorLUTResolution = colorLUTResolution;
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        var cameraList = new List<Camera>(cameras);
        Render(context, cameraList);
    }

    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        // Call CameraRenderer
        for (int i = 0; i < cameras.Count; i++)
        {
            renderer.Render(context, cameras[i], allowHDR, useDynamicBatching, useGPUInstancing, useLightsPerObject, shadowSettings, postFXSettings, colorLUTResolution);
        }
    }
}
