using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class MyRenderPipeline : RenderPipeline
{
    CameraRender renderer = new CameraRender();
    bool useDynamicBatching, useGPUInstancing, useLightsPerObject;
    ShadowSettings shadowSetting;

    // Constructor
    public MyRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher, bool useLightsPerObject, ShadowSettings shadowSetting)
    {
        this.shadowSetting = shadowSetting;
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        this.useLightsPerObject = useLightsPerObject;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
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
            renderer.Render(context, cameras[i], useDynamicBatching, useGPUInstancing, useLightsPerObject, shadowSetting);
        }
    }
}
