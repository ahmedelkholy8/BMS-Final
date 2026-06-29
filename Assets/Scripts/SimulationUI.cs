using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BMS;

public class SimulationUI : MonoBehaviour {
    [Header("Controller")]
    public SimulationController simController;
    public ThermalThumbnail     thermalThumbnail;

    [Header("Sliders")]
    public Slider sliderLoad, sliderAmbient, sliderCharge, sliderSoc, sliderDuration;
    [Header("Slider Labels")]
    public TMP_Text lblLoad, lblAmbient, lblCharge, lblSoc, lblDuration;

    [Header("Fault")]
    public TMP_Dropdown dropCell, dropFaultType;
    public TMP_Dropdown dropSeverity;   // kept for serialization; if null, uses Slider_Severity
    public Toggle       toggleFault;
    public Button       btnInjectFault;
    [Tooltip("Fallback: Slider mapped 0=Low 1=Med 2=High")]
    public Slider       sliderSeverityFallback;

    [Header("Buttons")]
    public Button btnRun, btnPause, btnStop;

    [Header("Status")]
    public TMP_Text lblElapsed;   // "Time Elapsed: HH:MM:SS"
    public TMP_Text lblTotal;     // "/ HH:MM:SS"

    [Header("KPIs")]
    public TMP_Text kpiRuntime, kpiRuntimeSub, kpiEndSoc, kpiEndSocSub;
    public TMP_Text kpiMaxTemp, kpiMaxTempStatus, kpiEndVolt, kpiEndVoltStatus;
    public TMP_Text kpiCellRisk, kpiCellRiskStatus, kpiHealth, kpiHealthSub;

    [Header("Graphs")]
    public SimulationGraph graphSoc, graphTemp, graphVolt, graphCellFault;

    [Header("What-if")]
    public Transform  scenarioContent;
    public GameObject scenarioRowPrefab;
    public Button     btnAddScenario, btnExport;

    [Header("Alerts")]
    public Transform  alertContent;
    public GameObject alertRowPrefab;

    List<SimScenario> _scenarios = new List<SimScenario>();
    SimulationData    _lastData;

    void Start() {
        if (sliderLoad)    sliderLoad.onValueChanged.AddListener(v  => { if (lblLoad)    lblLoad.text    = $"{v:F0} A"; });
        if (sliderAmbient) sliderAmbient.onValueChanged.AddListener(v => { if (lblAmbient) lblAmbient.text = $"{v:F0} °C"; });
        if (sliderCharge)  sliderCharge.onValueChanged.AddListener(v  => { if (lblCharge)  lblCharge.text  = $"{v:F0} A"; });
        if (sliderSoc)     sliderSoc.onValueChanged.AddListener(v     => { if (lblSoc)     lblSoc.text     = $"{v:F0} %"; });
        if (sliderDuration) sliderDuration.onValueChanged.AddListener(v => {
            if (lblDuration) lblDuration.text = $"{v:F1} h";
            UpdateTotalLabel(v * 3600f);
        });

        if (btnRun)         btnRun.onClick.AddListener(OnRun);
        if (btnPause)       btnPause.onClick.AddListener(OnPause);
        if (btnStop)        btnStop.onClick.AddListener(OnStop);
        if (btnAddScenario) btnAddScenario.onClick.AddListener(OnAddScenario);
        if (btnExport)      btnExport.onClick.AddListener(OnExport);
        if (btnInjectFault) btnInjectFault.onClick.AddListener(() => {
            var l = btnInjectFault.GetComponentInChildren<TMP_Text>();
            if (l) l.text = "FAULT INJECTED";
        });

        SetDefaults();
        SetButtonsIdle();
    }

    void OnRun()  { var d = BuildData(); simController.RunSimulation(d); LoadGraphs(d); SetButtonsRunning(); }
    void OnPause() {
        simController.Pause();
        var l = btnPause.GetComponentInChildren<TMP_Text>();
        if (l) l.text = simController.State == SimState.Paused ? "RESUME" : "PAUSE";
    }
    void OnStop() { simController.Stop(); SetButtonsIdle(); }

    void OnAddScenario() {
        if (_lastData == null || !_lastData.isValid) return;
        int n = _scenarios.Count + 1;
        _scenarios.Add(new SimScenario {
            label        = n == 1 ? "Current (Base)" : "Scenario " + (n - 1),
            loadCurrent  = _lastData.loadCurrent,
            ambientTemp  = _lastData.ambientTemp,
            runtimeH     = _lastData.predictedRuntimeH,
            maxTemp      = _lastData.predictedMaxTemp,
            endSoc       = _lastData.predictedEndSoc
        });
        RebuildTable();
    }

    void OnExport() {
        if (_lastData == null) return;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Time(h),SOC(%),Voltage(V),Temp(C)");
        for (int i = 0; i < _lastData.timeSamples.Length; i++)
            sb.AppendLine($"{_lastData.timeSamples[i] / 3600f:F3},{_lastData.socCurve[i] * 100f:F1},{_lastData.voltageCurve[i]:F3},{_lastData.tempCurve[i]:F1}");
        string path = System.IO.Path.Combine(Application.persistentDataPath, "sim_export.csv");
        System.IO.File.WriteAllText(path, sb.ToString());
        string msg = "Exported → " + path;
        Debug.Log("[SimUI] " + msg);
        // Show brief confirmation on lblTotal if available
        if (lblTotal) { lblTotal.text = msg; }
    }

    public void OnSimulationStarted(SimulationData d)  { _lastData = d; PopulateAlerts(d); }
    public void OnPlayheadUpdated(SimulationData d, int idx) {
        if (lblElapsed != null) lblElapsed.text = "Time Elapsed: " + TimeStr(d.playheadTime);
        UpdateTotalLabel(d.durationHours * 3600f);
        graphSoc?.RevealUpTo(idx);
        graphTemp?.RevealUpTo(idx);
        graphVolt?.RevealUpTo(idx);
        graphCellFault?.RevealUpTo(idx);
        thermalThumbnail?.RenderLive();
    }
    public void OnSimulationFinished(SimulationData d) {
        _lastData = d; PopulateKPIs(d); SetButtonsIdle();
        thermalThumbnail?.CaptureSnapshot();
    }
    public void OnSimulationStopped() { SetButtonsIdle(); }

    SimulationData BuildData() {
        var d = new SimulationData();
        if (sliderLoad)    d.loadCurrent   = sliderLoad.value;
        if (sliderAmbient) d.ambientTemp   = sliderAmbient.value;
        if (sliderCharge)  d.chargeCurrent = sliderCharge.value;
        if (sliderSoc)     d.initialSoc    = sliderSoc.value / 100f;
        if (sliderDuration) d.durationHours = sliderDuration.value;
        d.faultEnabled   = toggleFault != null && toggleFault.isOn;
        d.faultCellIndex = dropCell     != null ? dropCell.value     : 3;
        d.faultType      = dropFaultType != null ? (FaultType)(dropFaultType.value + 1) : FaultType.IncreasedInternalResistance;
        // Severity: prefer dropSeverity; fall back to slider
        if (dropSeverity != null)
            d.faultSeverity = (SimSeverity)dropSeverity.value;
        else if (sliderSeverityFallback != null)
            d.faultSeverity = (SimSeverity)Mathf.RoundToInt(sliderSeverityFallback.value);
        else
            d.faultSeverity = SimSeverity.High;
        return d;
    }

    void LoadGraphs(SimulationData d) {
        var soc100 = System.Array.ConvertAll(d.socCurve, v => v * 100f);
        graphSoc?.Load(soc100,      d.timeSamples, d.durationHours); graphSoc?.RevealUpTo(0);
        graphTemp?.Load(d.tempCurve,   d.timeSamples, d.durationHours); graphTemp?.RevealUpTo(0);
        graphVolt?.Load(d.voltageCurve,d.timeSamples, d.durationHours); graphVolt?.RevealUpTo(0);
        int fc = Mathf.Clamp(d.faultCellIndex, 0, SimulationEngine.CELLS - 1);
        if (d.cellVoltCurves != null && fc < d.cellVoltCurves.Length) {
            graphCellFault?.Load(d.cellVoltCurves[fc], d.timeSamples, d.durationHours);
            graphCellFault?.RevealUpTo(0);
        }
        if (graphCellFault?.labelTitle != null)
            graphCellFault.labelTitle.text = $"CELL {d.faultCellIndex + 1} VOLTAGE";
    }

    void PopulateKPIs(SimulationData d) {
        Set(kpiRuntime, $"{d.predictedRuntimeH:F2} h"); Set(kpiRuntimeSub, "± 5 min");
        Set(kpiEndSoc, $"{d.predictedEndSoc:F0}%");     Set(kpiEndSocSub, "± 2%");
        Color cT = d.predictedMaxTemp > 50f ? new Color(1f,.3f,.1f) : d.predictedMaxTemp > 40f ? new Color(1f,.75f,0f) : new Color(0f,.8f,.4f);
        Set(kpiMaxTemp, $"{d.predictedMaxTemp:F1}°C", cT);
        Set(kpiMaxTempStatus, d.predictedMaxTemp > 50f ? "High" : d.predictedMaxTemp > 40f ? "Warm" : "Normal");
        Set(kpiEndVolt, $"{d.predictedEndVoltage:F2} V");
        Set(kpiEndVoltStatus, d.predictedEndVoltage < 24f ? "Low" : "Normal");
        Set(kpiCellRisk, d.cellAtRisk >= 0 ? $"Cell {d.cellAtRisk + 1}" : "None");
        Set(kpiCellRiskStatus, d.cellAtRisk >= 0 ? "High Risk" : "Normal");
        Color cH = d.healthImpact < -3f ? new Color(1f,.3f,.1f) : new Color(1f,.75f,0f);
        Set(kpiHealth, $"{d.healthImpact:F1}%", cH); Set(kpiHealthSub, "Est. SOH after cycle");
    }

    void PopulateAlerts(SimulationData d) {
        if (alertContent == null || alertRowPrefab == null) return;
        foreach (Transform c in alertContent) Destroy(c.gameObject);
        foreach (var a in d.alerts) {
            var row = Instantiate(alertRowPrefab, alertContent);
            var txts = row.GetComponentsInChildren<TMP_Text>();
            if (txts.Length >= 1) txts[0].text = a.message;
            if (txts.Length >= 2) txts[1].text = a.recommendation;
            var img = row.GetComponent<Image>();
            if (img) img.color = a.isCritical ? new Color(1f,.6f,0f,.12f) : new Color(0f,.7f,1f,.08f);
        }
    }

    void RebuildTable() {
        if (scenarioContent == null || scenarioRowPrefab == null) return;
        foreach (Transform c in scenarioContent) Destroy(c.gameObject);
        foreach (var s in _scenarios) {
            var row = Instantiate(scenarioRowPrefab, scenarioContent);
            var txts = row.GetComponentsInChildren<TMP_Text>();
            if (txts.Length > 0) txts[0].text = s.label;
            if (txts.Length > 1) txts[1].text = $"{s.loadCurrent:F0}";
            if (txts.Length > 2) txts[2].text = $"{s.ambientTemp:F0}";
            if (txts.Length > 3) txts[3].text = $"{s.runtimeH:F2}";
            if (txts.Length > 4) txts[4].text = $"{s.maxTemp:F1}";
            if (txts.Length > 5) txts[5].text = $"{s.endSoc:F0}%";
        }
    }

    void SetDefaults() {
        if (sliderLoad)     { sliderLoad.minValue     = 5;   sliderLoad.maxValue     = 80;  sliderLoad.value     = 20;  if (lblLoad)     lblLoad.text     = "20 A"; }
        if (sliderAmbient)  { sliderAmbient.minValue  = 5;   sliderAmbient.maxValue  = 50;  sliderAmbient.value  = 25;  if (lblAmbient)  lblAmbient.text  = "25 °C"; }
        if (sliderCharge)   { sliderCharge.minValue   = 0;   sliderCharge.maxValue   = 40;  sliderCharge.value   = 0;   if (lblCharge)   lblCharge.text   = "0 A"; }
        if (sliderSoc)      { sliderSoc.minValue      = 10;  sliderSoc.maxValue      = 100; sliderSoc.value      = 78;  if (lblSoc)      lblSoc.text      = "78 %"; }
        if (sliderDuration) { sliderDuration.minValue = .5f; sliderDuration.maxValue = 8f;  sliderDuration.value = 2f;  if (lblDuration) lblDuration.text = "2.0 h"; }
        if (sliderSeverityFallback) { sliderSeverityFallback.minValue = 0; sliderSeverityFallback.maxValue = 2; sliderSeverityFallback.value = 2; }
        if (lblElapsed) lblElapsed.text = "Time Elapsed: 00:00:00";
        UpdateTotalLabel(2f * 3600f);
    }
    void UpdateTotalLabel(float totalSec) { if (lblTotal) lblTotal.text = "/ " + TimeStr(totalSec); }
    void SetButtonsIdle()    { if (btnRun) btnRun.interactable = true;  if (btnPause) btnPause.interactable = false; if (btnStop) btnStop.interactable = false; }
    void SetButtonsRunning() { if (btnRun) btnRun.interactable = false; if (btnPause) btnPause.interactable = true;  if (btnStop) btnStop.interactable = true; }
    void Set(TMP_Text t, string s)          { if (t) t.text = s; }
    void Set(TMP_Text t, string s, Color c) { if (t) { t.text = s; t.color = c; } }
    static string TimeStr(float sec) { int h = (int)(sec/3600), m = (int)(sec%3600/60), s = (int)(sec%60); return $"{h:D2}:{m:D2}:{s:D2}"; }
}
