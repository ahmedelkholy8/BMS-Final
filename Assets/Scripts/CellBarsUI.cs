using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CellBarsUI : MonoBehaviour
{
    [Header("BMS Controller")]
    public BmsController bmsController;

    [Header("Cell Bars (assign BarFill image of each bar)")]
    public Image[]   barFills      = new Image[8];
    public TMP_Text[] voltageLabels = new TMP_Text[8];

    [Header("Voltage Range")]
    public float voltageMin  = 3.60f;
    public float voltageMax  = 3.75f;
    public float barMaxHeight = 100f;   // pixels — matches BarBG height

    [Header("Colors")]
    public Color colorGood    = new Color(0.13f, 0.80f, 0.27f);
    public Color colorWarn    = new Color(1.00f, 0.75f, 0.00f);
    public Color colorLow     = new Color(0.90f, 0.10f, 0.10f);
    public float warnThreshold = 3.68f;
    public float lowThreshold  = 3.63f;

    void Update()
    {
        if (bmsController == null) return;

        float[] voltages = bmsController.GetCellVoltages();

        for (int i = 0; i < barFills.Length; i++)
        {
            if (i >= voltages.Length) break;

            float v = voltages[i];

            // ── Height ──────────────────────────────────────────
            float t = Mathf.InverseLerp(voltageMin, voltageMax, v);
            float targetH = Mathf.Lerp(10f, barMaxHeight, t);

            RectTransform rt = barFills[i].rectTransform;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, targetH);

            // ── Color ───────────────────────────────────────────
            Color barColor;
            if (v < lowThreshold)
                barColor = colorLow;
            else if (v < warnThreshold)
            {
                float blend = Mathf.InverseLerp(lowThreshold, warnThreshold, v);
                barColor = Color.Lerp(colorLow, colorWarn, blend);
            }
            else
            {
                float blend = Mathf.InverseLerp(warnThreshold, voltageMax, v);
                barColor = Color.Lerp(colorWarn, colorGood, blend);
            }
            barFills[i].color = barColor;

            // ── Voltage label ───────────────────────────────────
            if (voltageLabels[i] != null)
            {
                voltageLabels[i].text  = $"{v:F2}";
                voltageLabels[i].color = barColor;
            }
        }
    }
}