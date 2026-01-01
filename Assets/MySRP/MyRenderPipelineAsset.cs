using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/MyRenderPipeline")]

public partial class MyRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField] bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true, useLightsPerObject = true; // Batch settings
    [SerializeField] ShadowSettings shadows = default;
    [SerializeField] PostFXSettings postFXSettings = default;
    //[SerializeField] bool allowHDR = true;
    public enum ColorLUTResolution { _16 = 16, _32 = 32, _64 = 64 }
    [SerializeField] ColorLUTResolution colorLUTResolution = ColorLUTResolution._32;
    [SerializeField] Shader cameraRendererShader = default;

    [SerializeField] CameraBufferSettings cameraBuffer = new CameraBufferSettings { allowHDR = true, renderScale = 1f };

    protected override RenderPipeline CreatePipeline()
    {
        return new MyRenderPipeline(cameraBuffer, useDynamicBatching, useGPUInstancing, useSRPBatcher, useLightsPerObject, shadows, postFXSettings, (int)colorLUTResolution, cameraRendererShader);
    }
}
