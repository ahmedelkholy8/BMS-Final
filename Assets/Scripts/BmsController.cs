using UnityEngine;
using BMS;

public class BmsController : MonoBehaviour
{

    [Header("Data")]
    public BmsData data = new BmsData();

    [Header("Cell Renderers (assign Cell_01 … Cell_08)")]
    public Renderer[] cellRenderers = new Renderer[8];

    [Header("View Mode Override")]
    public bool overrideVisuals = false;

    [Header("Cell Voltage Thresholds")]
    public float voltageMin  = 3.60f;   // red below this
    public float voltageWarn = 3.68f;   // yellow below this
    public float voltageMax  = 3.75f;   // green at/above this

    [Header("Simulate Live Data")]
    public bool simulateData = true;
    public float simulationSpeed = 0.5f;

    // Internal
    private float _simTime = 0f;
    
    [Header("Material Slot")]
    public int cellBodyMaterialIndex = 1;   // ← matches your Blender export (Element 1)

    [Header("Live Data Lock")]
    public bool liveDataActive = false;
/// <summary>
/// Update is called once per frame
/// </summary>
    void Update()
    {
        if (simulateData && !liveDataActive) SimulateData();
        UpdateCellColors();
        UpdatePackValues();
        UpdateCellSoh();
    }

    // Per-cell SOH isn't part of live MQTT telemetry, so it's always derived
    // here from pack SOH plus a small, stable per-cell offset — works the
    // same whether data is simulated or live.
    void UpdateCellSoh()
    {
        for (int i = 0; i < data.cellSoh.Length; i++)
        {
            float cellOffset = Mathf.Sin(i * 1.7f) * 0.02f;
            float drift = Mathf.Sin(Time.time * 0.05f + i) * 0.005f;
            data.cellSoh[i] = Mathf.Clamp01(data.soh + cellOffset + drift);
        }
    }
    void UpdatePackValues()
    {
        // Ensure derived pack values stay in sync with cell data
        if (!simulateData)
        {
            // Pack voltage already set by MQTT parser
            // Just clamp SOC to valid range
            data.soc = Mathf.Clamp01(data.soc);
            data.temperature = Mathf.Clamp(data.temperature, -40f, 100f);
        }
    }
    // ─── Simulation ──────────────────────────────────────────────
    void SimulateData()
    {
        _simTime += Time.deltaTime * simulationSpeed;

        // Slowly oscillate SOC
        data.soc = Mathf.Clamp01(0.78f + Mathf.Sin(_simTime * 0.3f) * 0.08f);

        // Oscillate pack voltage
        data.packVoltage = 28.8f + data.soc * 1.2f;

        // Simulate cell voltages — cell 4 dips lower (like the alarm in the image)
        for (int i = 0; i < 8; i++)
        {
            float noise = Mathf.Sin(_simTime * (0.4f + i * 0.15f)) * 0.02f;
            data.cellVoltages[i] = 3.70f + noise;
        }
        // Cell 4 (index 3) stays low to trigger the warning color
        data.cellVoltages[3] = 3.65f + Mathf.Sin(_simTime * 0.5f) * 0.01f;

        // Temperature drift
        data.temperature = 31.4f + Mathf.Sin(_simTime * 0.2f) * 1.5f;
    }

    // ─── Visual Updates ──────────────────────────────────────────
    void UpdateCellColors()
    {
        if (overrideVisuals) return;

        for (int i = 0; i < cellRenderers.Length; i++)
        {
            if (cellRenderers[i] == null) continue;

            float v    = data.cellVoltages[i];

            // 3D cell fill uses SOC, color still uses voltage
            float fill = (!simulateData && data.cellSoc != null && i < data.cellSoc.Length)
                ? Mathf.Clamp01(data.cellSoc[i])
                : Mathf.Clamp01(data.soc);  // fallback to pack SOC when simulating

            // Use per-cell SOC if available (live data), otherwise derive from voltage
            if (!simulateData && data.cellSoc != null && i < data.cellSoc.Length)
                fill = Mathf.Clamp01(data.cellSoc[i]);
            else
                fill = Mathf.InverseLerp(voltageMin, voltageMax, v);

            Material[] mats = cellRenderers[i].materials;
            if (cellBodyMaterialIndex >= mats.Length) continue;

            Material mat = mats[cellBodyMaterialIndex];
            mat.SetFloat("_FillLevel", fill);
            mat.SetFloat("_ColorOverride", 0f); // normal SOC/voltage liquid color, not a thermal/balancing override

            // IMPORTANT: reassign the array back to the renderer
            cellRenderers[i].materials = mats;
        }
    }
    void Start()
    {
        AutoConfigureMeshBounds();
        if (cellRenderers.Length > 0 && cellRenderers[0] != null)
        {
            Bounds b = cellRenderers[0].GetComponent<MeshFilter>().sharedMesh.bounds;
            Debug.Log($"Cell top local Y: {b.max.y}");
        }
    }

    void AutoConfigureMeshBounds()
    {
        foreach (var r in cellRenderers)
        {
            if (r == null) continue;
            MeshFilter mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            Bounds b = mf.sharedMesh.bounds;
            Material[] mats = r.materials;
            if (cellBodyMaterialIndex >= mats.Length) continue;

            mats[cellBodyMaterialIndex].SetFloat("_MeshMinY", b.min.y);
            mats[cellBodyMaterialIndex].SetFloat("_MeshMaxY", b.max.y);
            r.materials = mats;
        }
    }

    // ─── Public API (for UI scripts later) ───────────────────────
    public float GetSOC()         => data.soc;
    public float GetSOH()         => data.soh;
    public float GetPackVoltage() => data.packVoltage;
    public float GetCurrent()     => data.current;
    public float GetTemperature() => data.temperature;
    public float[] GetCellVoltages() => data.cellVoltages;
}

