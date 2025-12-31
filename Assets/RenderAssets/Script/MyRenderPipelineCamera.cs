using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(Camera))]
public class MyRenderPipelineCamera : MonoBehaviour
{
    [SerializeField] CameraSettings settings = default;

    public CameraSettings Settings => settings == null ? settings = new CameraSettings() : settings;
}
