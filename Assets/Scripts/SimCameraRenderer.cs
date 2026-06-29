using UnityEngine;

/// <summary>
/// Periodically renders the SimPackCamera and SimThermalCamera
/// so the Simulation page's RawImage displays are kept up to date.
/// Attach this to a GameObject in the scene; reference the cameras via inspector.
/// </summary>
public class SimCameraRenderer : MonoBehaviour
{
    public Camera packCamera;
    public Camera thermalCamera;

    [Header("Refresh")]
    [Tooltip("Seconds between renders. 0 = every frame.")]
    public float refreshInterval = 0.5f;

    float _timer;

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < refreshInterval) return;
        _timer = 0f;

        if (packCamera != null)
        {
            packCamera.Render();
        }
        if (thermalCamera != null)
        {
            thermalCamera.Render();
        }
    }
}