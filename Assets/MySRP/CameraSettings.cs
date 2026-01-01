using System;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable] public class CameraSettings
{
    [Serializable] public struct FinalBlendMode
    {
        public BlendMode source, destination;
    }

    public enum RenderScaleMode { Inherit, Multiply, Override }

    public bool copyColor = true, copyDepth = true;
    [RenderingLayerMaskField] public int renderingLayerMask = -1;
    public bool maskLights = false;
    public RenderScaleMode renderScaleMode = RenderScaleMode.Inherit;
    [Range(CameraRenderer.renderScaleMin, CameraRenderer.renderScaleMax)] public float renderScale = 1f;
    public bool overridePostFX = false;
    public PostFXSettings postFXSettings = default;
    public FinalBlendMode finalBlendMode = new FinalBlendMode { source = BlendMode.One, destination = BlendMode.Zero };

    public float GetRenderScale(float scale)
    {
        return renderScaleMode == RenderScaleMode.Inherit ? scale : (renderScaleMode == RenderScaleMode.Override ? renderScale : scale * renderScale);
    }
}
