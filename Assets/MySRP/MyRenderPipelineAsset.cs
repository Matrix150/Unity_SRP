using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/MyRenderPipeline")]

public class MyRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField] bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true, useLightsPerObject = true; // Batch settings
    [SerializeField] ShadowSettings shadows = default;
    [SerializeField] PostFXSettings postFXSettings = default;

    protected override RenderPipeline CreatePipeline()
    {
        return new MyRenderPipeline(useDynamicBatching, useGPUInstancing, useSRPBatcher, useLightsPerObject, shadows, postFXSettings);
    }
}
