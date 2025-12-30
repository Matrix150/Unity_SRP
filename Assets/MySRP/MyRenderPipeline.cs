using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class MyRenderPipeline : RenderPipeline
{
    CameraRender renderer = new CameraRender();
    bool useDynamicBatching, useGPUInstancing, useLightsPerObject;
    ShadowSettings shadowSettings;
    PostFXSettings postFXSettings;

    // Constructor
    public MyRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher, bool useLightsPerObject, ShadowSettings shadowSettings, PostFXSettings postFXSettings)
    {
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        this.useLightsPerObject = useLightsPerObject;
        this.shadowSettings = shadowSettings;
        this.postFXSettings = postFXSettings;
        GraphicsSettings.lightsUseLinearIntensity = true;

        InitializeForEditor();
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
            renderer.Render(context, cameras[i], useDynamicBatching, useGPUInstancing, useLightsPerObject, shadowSettings, postFXSettings);
        }
    }
}
