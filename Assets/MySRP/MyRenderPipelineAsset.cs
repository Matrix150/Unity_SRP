using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/MyRenderPipeline")]

public class MyRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField] bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true, useLightsPerObject = true; // Batch settings
    [SerializeField] ShadowSettings shadow = default;

    protected override RenderPipeline CreatePipeline()
    {
        return new MyRenderPipeline(useDynamicBatching, useGPUInstancing, useSRPBatcher, useLightsPerObject, shadow);
    }
}
