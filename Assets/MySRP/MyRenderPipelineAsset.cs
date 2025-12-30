using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/MyRenderPipeline")]

public class MyRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField] bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true, useLightsPerObject = true; // Batch settings
    [SerializeField] ShadowSettings shadows = default;
    [SerializeField] PostFXSettings postFXSettings = default;
    [SerializeField] bool allowHDR = true;
    public enum ColorLUTResolution { _16 = 16, _32 = 32, _64 = 64 }
    [SerializeField] ColorLUTResolution colorLUTResolution = ColorLUTResolution._32;

    protected override RenderPipeline CreatePipeline()
    {
        return new MyRenderPipeline(allowHDR, useDynamicBatching, useGPUInstancing, useSRPBatcher, useLightsPerObject, shadows, postFXSettings, (int)colorLUTResolution);
    }
}
