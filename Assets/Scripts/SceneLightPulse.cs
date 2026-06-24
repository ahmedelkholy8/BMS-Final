using UnityEngine;
using BMS;

/// <summary>
/// Makes scene lights breathe with the live BMS data.
/// Cell light: pulses cyan with current magnitude.
/// BMS light:  throbs orange driven by temperature.
/// Load light: flickers purple only when discharging; dims on charge.
/// Attach to any GameObject — references are set programmatically.
/// </summary>
public class SceneLightPulse : MonoBehaviour
{
    [Header("References")]
    public BmsController bmsController;
    public Light         cellLight;
    public Light         bmsLight;
    public Light         loadLight;

    [Header("Cell light settings")]
    public float cellBaseIntensity  = 2.8f;
    public float cellPulseRange     = 1.2f;
    public float cellPulseSpeed     = 1.8f;

    [Header("BMS board light settings")]
    public float bmsBaseIntensity   = 1.8f;
    public float bmsPulseRange      = 0.6f;
    public float bmsPulseSpeed      = 0.7f;

    [Header("Load / charger light")]
    public float loadDischargeIntensity = 1.4f;
    public float loadChargeIntensity    = 0.4f;

    void Update()
    {
        if (bmsController == null) return;
        BmsData d = bmsController.data;

        // ── Cell light: cyan glow scales with absolute current ─────
        if (cellLight != null)
        {
            float currentFactor = Mathf.Clamp01(Mathf.Abs(d.current) / 15f);
            float pulse = Mathf.Sin(Time.time * cellPulseSpeed) * 0.5f + 0.5f;
            cellLight.intensity = cellBaseIntensity
                + pulse * cellPulseRange * (0.3f + currentFactor * 0.7f);

            // Shift towards warmer cyan when discharging
            if (d.IsDischarging)
                cellLight.color = Color.Lerp(
                    new Color(0f, 0.82f, 1f),
                    new Color(0.2f, 1f, 0.7f),
                    pulse * currentFactor);
            else
                cellLight.color = Color.Lerp(
                    new Color(0f, 0.82f, 1f),
                    new Color(0f, 0.55f, 1f),
                    pulse);
        }

        // ── BMS board: orange throb driven by temperature ──────────
        if (bmsLight != null)
        {
            float tempFactor = Mathf.InverseLerp(20f, 60f, d.temperature);
            float pulse      = Mathf.Sin(Time.time * bmsPulseSpeed) * 0.5f + 0.5f;
            bmsLight.intensity = bmsBaseIntensity + pulse * bmsPulseRange;
            bmsLight.color = Color.Lerp(
                new Color(1f, 0.42f, 0f),
                new Color(1f, 0.10f, 0f),
                tempFactor);
        }

        // ── Load/charger: purple when discharging, blue charging ───
        if (loadLight != null)
        {
            float targetIntensity = d.IsDischarging
                ? loadDischargeIntensity
                : loadChargeIntensity;
            loadLight.intensity = Mathf.Lerp(
                loadLight.intensity, targetIntensity, Time.deltaTime * 3f);

            loadLight.color = d.IsDischarging
                ? new Color(0.60f, 0.20f, 1.00f)
                : new Color(0.20f, 0.60f, 1.00f);
        }
    }
}