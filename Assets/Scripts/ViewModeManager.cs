using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BMS;

public enum ViewMode
{
    Overview,
    Thermal,
    Balancing
}

public class ViewModeManager : MonoBehaviour
{
    [Header("References")]
    public BmsController bmsController;
    public OrbitCamera   orbitCamera;

    [Header("Buttons")]
    public Button btnOverview;
    public Button btnThermal;
    public Button btnBalancing;

    [Header("Button Colors")]
    public Color colorActive   = new Color(0.05f, 0.23f, 0.42f);
    public Color colorInactive = new Color(0.10f, 0.16f, 0.23f);
    public Color textActive    = new Color(0.00f, 0.76f, 1.00f);
    public Color textInactive  = new Color(1.00f, 1.00f, 1.00f);

    [Header("Cell Renderers")]
    public Renderer[] cellRenderers = new Renderer[8];

    [Header("Material Slot")]
    public int cellBodyMaterialIndex = 1;

    [Header("Thermal Settings")]
    [Tooltip("Minimum temperature in Celsius — maps to blue")]
    public float thermalTempMin = 20f;
    [Tooltip("Maximum temperature in Celsius — maps to red")]
    public float thermalTempMax = 35f;

    [Header("Thermal Legend UI")]
    public GameObject thermalLegend;
    public TMP_Text   legendMinLabel;
    public TMP_Text   legendMaxLabel;
    public TMP_Text   legendAvgLabel;

    // Current mode
    private ViewMode _currentMode = ViewMode.Overview;

    // Shader property IDs
    private static readonly int PropFillLevel     = Shader.PropertyToID("_FillLevel");
    private static readonly int PropBaseColor     = Shader.PropertyToID("_BaseColor");
    private static readonly int PropRimIntensity  = Shader.PropertyToID("_RimIntensity");
    private static readonly int PropColorOverride = Shader.PropertyToID("_ColorOverride");

    void Start()
    {
        btnOverview.onClick.AddListener(()  => SetMode(ViewMode.Overview));
        btnThermal.onClick.AddListener(()   => SetMode(ViewMode.Thermal));
        btnBalancing.onClick.AddListener(() => SetMode(ViewMode.Balancing));

        if (thermalLegend != null) thermalLegend.SetActive(false);
        SetMode(ViewMode.Overview);
    }

    void Update()
    {
        if (_currentMode == ViewMode.Thermal)   UpdateThermalColors();
        if (_currentMode == ViewMode.Balancing) UpdateBalancingColors();
    }

    // ── Mode switching ────────────────────────────────────────────
    public void SetMode(ViewMode mode)
    {
        _currentMode = mode;
        UpdateButtonVisuals();

        switch (mode)
        {
            case ViewMode.Overview:  ApplyOverviewMode();  break;
            case ViewMode.Thermal:   ApplyThermalMode();   break;
            case ViewMode.Balancing: ApplyBalancingMode(); break;
        }
    }

    // ── Overview ──────────────────────────────────────────────────
    void ApplyOverviewMode()
    {
        SetOverride(false);
        ShowThermalLegend(false);

        foreach (var r in cellRenderers)
        {
            Material mat = GetBodyMaterial(r);
            if (mat == null) continue;
            mat.SetFloat(PropRimIntensity, 0.6f);
            mat.SetFloat(PropColorOverride, 0f);
        }

        if (orbitCamera != null) orbitCamera.SnapTo(20f, 28f, 6f);
    }

    // ── Thermal ───────────────────────────────────────────────────
    void ApplyThermalMode()
    {
        SetOverride(true);
        ShowThermalLegend(true);
        if (orbitCamera != null) orbitCamera.SnapTo(20f, 55f, 7f);
    }

    void UpdateThermalColors()
    {
        if (bmsController == null) return;

        float avgTemp = 0f;

        for (int i = 0; i < cellRenderers.Length; i++)
        {
            Material mat = GetBodyMaterial(cellRenderers[i]);
            if (mat == null) continue;

            float cellTemp;
            if (!bmsController.simulateData &&
                bmsController.data.cellTemperatures != null &&
                i < bmsController.data.cellTemperatures.Length)
            {
                cellTemp = bmsController.data.cellTemperatures[i];
            }
            else
            {
                cellTemp = bmsController.GetTemperature()
                         + Mathf.Sin(Time.time * 0.5f + i * 0.8f) * 2f;
            }

            avgTemp += cellTemp;

            float t = Mathf.InverseLerp(thermalTempMin, thermalTempMax, cellTemp);
            Color heatColor = GetHeatmapColor(t);

            mat.SetColor(PropBaseColor, heatColor);
            mat.SetFloat(PropFillLevel, 1.0f);
            mat.SetFloat(PropRimIntensity, 1.4f);
            mat.SetFloat(PropColorOverride, 1f);
        }

        if (legendAvgLabel != null)
            legendAvgLabel.text = $"{(avgTemp / cellRenderers.Length):F1}°C avg";
    }

    Color GetHeatmapColor(float t)
    {
        if (t < 0.25f)
            return Color.Lerp(new Color(0.0f, 0.2f, 1.0f),
                              new Color(0.0f, 0.9f, 0.9f), t * 4f);
        if (t < 0.5f)
            return Color.Lerp(new Color(0.0f, 0.9f, 0.9f),
                              new Color(0.2f, 0.9f, 0.1f), (t - 0.25f) * 4f);
        if (t < 0.75f)
            return Color.Lerp(new Color(0.2f, 0.9f, 0.1f),
                              new Color(1.0f, 0.8f, 0.0f), (t - 0.5f) * 4f);
        return Color.Lerp(new Color(1.0f, 0.8f, 0.0f),
                          new Color(1.0f, 0.1f, 0.0f), (t - 0.75f) * 4f);
    }

    // ── Balancing ─────────────────────────────────────────────────
    void ApplyBalancingMode()
    {
        SetOverride(true);
        ShowThermalLegend(false);
        if (orbitCamera != null) orbitCamera.SnapTo(20f, 28f, 5f);
    }

    void UpdateBalancingColors()
    {
        if (bmsController == null) return;

        float[] voltages = bmsController.GetCellVoltages();
        float   minV = float.MaxValue, maxV = float.MinValue;

        foreach (float v in voltages)
        {
            if (v < minV) minV = v;
            if (v > maxV) maxV = v;
        }

        float range = Mathf.Max(maxV - minV, 0.001f);

        for (int i = 0; i < cellRenderers.Length; i++)
        {
            Material mat = GetBodyMaterial(cellRenderers[i]);
            if (mat == null) continue;

            float balance = (voltages[i] - minV) / range;
            float pulse   = 0.5f + 0.5f * Mathf.Sin(Time.time * 3f + i);

            Color balanceColor;
            if (balance > 0.6f)
                balanceColor = Color.Lerp(
                    new Color(0.8f, 0.4f, 0.0f),
                    new Color(1.0f, 0.7f, 0.0f), pulse);
            else if (balance < 0.3f)
                balanceColor = Color.Lerp(
                    new Color(0.0f, 0.4f, 0.9f),
                    new Color(0.0f, 0.7f, 1.0f), pulse);
            else
                balanceColor = new Color(0.1f, 0.75f, 0.25f);

            mat.SetColor(PropBaseColor, balanceColor);
            mat.SetFloat(PropFillLevel, 1.0f);
            mat.SetFloat(PropRimIntensity, 1.0f);
            mat.SetFloat(PropColorOverride, 1f);
        }
    }

    // ── Thermal legend ────────────────────────────────────────────
    void ShowThermalLegend(bool show)
    {
        if (thermalLegend != null) thermalLegend.SetActive(show);

        if (show)
        {
            if (legendMinLabel != null)
                legendMinLabel.text = $"{thermalTempMin:F0}°C";
            if (legendMaxLabel != null)
                legendMaxLabel.text = $"{thermalTempMax:F0}°C";
        }
    }

    // ── Helpers ───────────────────────────────────────────────────
    void SetOverride(bool active)
    {
        if (bmsController != null)
            bmsController.overrideVisuals = active;
    }

    Material GetBodyMaterial(Renderer r)
    {
        if (r == null) return null;
        Material[] mats = r.materials;
        if (cellBodyMaterialIndex >= mats.Length) return null;
        return mats[cellBodyMaterialIndex];
    }

    void UpdateButtonVisuals()
    {
        SetButtonState(btnOverview,  _currentMode == ViewMode.Overview);
        SetButtonState(btnThermal,   _currentMode == ViewMode.Thermal);
        SetButtonState(btnBalancing, _currentMode == ViewMode.Balancing);
    }

    void SetButtonState(Button btn, bool active)
    {
        if (btn == null) return;
        ColorBlock cb  = btn.colors;
        cb.normalColor = active ? colorActive : colorInactive;
        btn.colors     = cb;

        TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.color = active ? textActive : textInactive;
    }
}