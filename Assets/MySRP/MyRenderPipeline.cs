using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MyRenderPipeline : RenderPipeline
{
    CameraRender renderer = new CameraRender();
    bool useDynamicBatching, useGPUInstancing;
    ShadowSettings shadowSetting;

    // Constructor
    public MyRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher, ShadowSettings shadowSetting)
    {
        this.shadowSetting = shadowSetting;
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
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
            renderer.Render(context, cameras[i], useDynamicBatching, useGPUInstancing, shadowSetting);
        }
    }
}
