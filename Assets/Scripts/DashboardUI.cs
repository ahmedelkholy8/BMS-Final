using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DashboardUI : MonoBehaviour
{
    [Header("BMS Controller")]
    public BmsController bmsController;

    [Header("Top Bar — Value Labels")]
    public TMP_Text valSOC;
    public TMP_Text valSOH;
    public TMP_Text valVoltage;
    public TMP_Text valCurrent;
    public TMP_Text valTemperature;

    [Header("Top Bar — Current Color")]
    public Color colorCharging    = new Color(0.00f, 1.00f, 0.53f);
    public Color colorDischarging = new Color(1.00f, 0.35f, 0.35f);

    [Header("Protection Status Dots")]
    public Image dotOverVoltage;
    public Image dotUnderVoltage;
    public Image dotOverCurrent;
    public Image dotShortCircuit;
    public Image dotOverTemp;
    public Image dotBalancing;

    [Header("Protection Status Values")]
    public TMP_Text valOverVoltage;
    public TMP_Text valUnderVoltage;
    public TMP_Text valOverCurrent;
    public TMP_Text valShortCircuit;
    public TMP_Text valOverTemp;
    public TMP_Text valBalancing;

    [Header("Battery Health")]
    public Image    sohRingFill;
    public TMP_Text labelSOHValue;
    public TMP_Text valCycleCount;
    public TMP_Text valRemainingLife;

    // Colors
    private Color _colorNormal   = new Color(0.00f, 0.80f, 0.40f);
    private Color _colorFault    = new Color(0.90f, 0.10f, 0.10f);
    private Color _colorActive   = new Color(0.00f, 0.67f, 1.00f);
    [Header("Thermal View Legend")]
    public GameObject thermalLegend;
    public TMP_Text   legendMinTemp;
    public TMP_Text   legendMaxTemp;
    public TMP_Text   legendCurTemp;

    public void ShowThermalLegend(bool show)
    {
        if (thermalLegend != null)
            thermalLegend.SetActive(show);

        if (show && bmsController != null && legendCurTemp != null)
            legendCurTemp.text = $"{bmsController.GetTemperature():F1}°C avg";
    }
    void Update()
    {
        if (bmsController == null) return;
        UpdateTopBar();
        UpdateProtectionStatus();
        UpdateBatteryHealth();
    }

    void UpdateTopBar()
    {
        /*
        Debug.Log($"[UI] SOC:{bmsController.GetSOC():F3} " +
            $"V:{bmsController.GetPackVoltage():F2} " +
            $"T:{bmsController.GetTemperature():F1}");
        */

        if (valSOC != null)
            valSOC.text = $"{(bmsController.GetSOC() * 100f):F0}%";

        if (valSOH != null)
            valSOH.text = $"{(bmsController.GetSOH() * 100f):F0}%";

        if (valVoltage != null)
            valVoltage.text = $"{bmsController.GetPackVoltage():F2} V";

        if (valCurrent != null)
        {
            float current = bmsController.GetCurrent();
            valCurrent.text  = $"{current:F2} A";
            valCurrent.color = current < 0f ? colorDischarging : colorCharging;
        }

        if (valTemperature != null)
            valTemperature.text = $"{bmsController.GetTemperature():F1} °C";
    }

    void UpdateProtectionStatus()
    {
        var d = bmsController.data;

        SetStatus(dotOverVoltage,   valOverVoltage,  d.overVoltage,   "Normal", "FAULT",  false);
        SetStatus(dotUnderVoltage,  valUnderVoltage, d.underVoltage,  "Normal", "FAULT",  false);
        SetStatus(dotOverCurrent,   valOverCurrent,  d.overCurrent,   "Normal", "FAULT",  false);
        SetStatus(dotShortCircuit,  valShortCircuit, d.shortCircuit,  "Normal", "FAULT",  false);
        SetStatus(dotOverTemp,      valOverTemp,     d.overTemperature,"Normal","FAULT",  false);

        // Balancing is different — true means Active (good), false means Off
        SetStatus(dotBalancing,     valBalancing,    d.cellBalancing,  "Active", "Off",   true);
    }

    // isPositive: true means the flag being ON is a good thing (e.g. balancing active)
    void SetStatus(Image dot, TMP_Text label, bool flag,
                   string trueText, string falseText, bool isPositive)
    {
        if (dot == null || label == null) return;

        bool isFault = isPositive ? !flag : flag;

        Color c     = isFault ? _colorFault :
                      (isPositive && flag) ? _colorActive : _colorNormal;

        dot.color   = c;
        label.color = c;
        label.text  = flag ? trueText : falseText;
    }
    void UpdateBatteryHealth()
    {
        float soh = bmsController.GetSOH();

        if (sohRingFill != null)
            sohRingFill.fillAmount = soh;

        if (labelSOHValue != null)
            labelSOHValue.text = $"{(soh * 100f):F0}%";

        if (valCycleCount != null)
            valCycleCount.text = bmsController.data.cycleCount.ToString();
    }
    public void ForceRefresh()
    {
        UpdateTopBar();
        UpdateProtectionStatus();
        UpdateBatteryHealth();
    }
}

