using UnityEngine;

/// <summary>
/// Creates the SimPackCamera + SimThermalCamera that render the battery pack
/// into the RawImages in Panel_Simulation. Camera positions are tuned so all
/// 8 cells fit inside the RawImage view.
/// </summary>
public static class SimCameraSetup
{
    /// <summary>
    /// Create the two cameras, wire their RenderTextures into the
    /// RawImages inside Panel_Simulation, and assign them to the
    /// ThermalThumbnail component. Returns the two RenderTextures.
    /// </summary>
    public static void Build(Transform batteryPack,
        Transform vizRawImage, Transform thermRawImage,
        ThermalThumbnail thermalThumb)
    {
        if (batteryPack == null) return;

        // ── Main 3D pack camera ────────────────────────────────────────
        var packCamGO = new GameObject("SimPackCamera");
        packCamGO.transform.position = batteryPack.position + new Vector3(5.5f, 3.5f, 6.5f);
        packCamGO.transform.LookAt(batteryPack.position);
        var packCam = packCamGO.AddComponent<Camera>();
        packCam.depth = -2;
        packCam.clearFlags = CameraClearFlags.SolidColor;
        packCam.backgroundColor = new Color(0.02f, 0.04f, 0.08f);
        packCam.fieldOfView = 50f;     // wider so all 8 cells fit
        if (Camera.main != null) packCam.cullingMask = Camera.main.cullingMask;
        packCam.enabled = false;
        var packRT = new RenderTexture(1024, 512, 16, RenderTextureFormat.ARGB32);
        packRT.name = "SimPackRT";
        packRT.Create();
        packCam.targetTexture = packRT;

        if (vizRawImage != null)
        {
            var raw = vizRawImage.GetComponent<UnityEngine.UI.RawImage>();
            if (raw != null) raw.texture = packRT;
        }

        // ── Thermal thumbnail camera ───────────────────────────────────
        var thermCamGO = new GameObject("SimThermalCamera");
        thermCamGO.transform.position = batteryPack.position + new Vector3(5.5f, 3.0f, 5.5f);
        thermCamGO.transform.LookAt(batteryPack.position);
        var thermCam = thermCamGO.AddComponent<Camera>();
        thermCam.depth = -3;
        thermCam.clearFlags = CameraClearFlags.SolidColor;
        thermCam.backgroundColor = new Color(0.02f, 0.04f, 0.08f);
        thermCam.fieldOfView = 45f;
        if (Camera.main != null) thermCam.cullingMask = Camera.main.cullingMask;
        thermCam.enabled = false;
        var thermRT = new RenderTexture(512, 256, 16, RenderTextureFormat.ARGB32);
        thermRT.name = "SimThermalRT";
        thermRT.Create();
        thermCam.targetTexture = thermRT;

        if (thermRawImage != null)
        {
            var raw = thermRawImage.GetComponent<UnityEngine.UI.RawImage>();
            if (raw != null) raw.texture = thermRT;
        }

        if (thermalThumb != null)
        {
            thermalThumb.thumbCamera   = thermCam;
            thermalThumb.renderTexture = thermRT;
        }

        WireRenderer(packCam, thermCam);
    }

    static void WireRenderer(Camera packCam, Camera thermCam)
    {
        var renderer = Object.FindFirstObjectByType<SimCameraRenderer>();
        if (renderer == null)
        {
            var go = new GameObject("SimCameraRenderer");
            renderer = go.AddComponent<SimCameraRenderer>();
            renderer.refreshInterval = 0.5f;
        }

        renderer.packCamera    = packCam;
        renderer.thermalCamera = thermCam;
    }
}