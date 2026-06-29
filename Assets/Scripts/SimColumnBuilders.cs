using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BMS;
using System.Collections.Generic;
using static SimUIHelpers;

/// <summary>
/// Builds the three columns of Panel_Simulation (Left, Center, Right)
/// plus the inner sub-components (sliders, dropdowns, KPI cards, graphs, alerts).
/// </summary>
public static class SimColumnBuilders
{
    // ─────────────────────────────────────────────────────────────────
    //  Left column: parameters / fault injection / run controls
    // ─────────────────────────────────────────────────────────────────
    public static void BuildLeft(Transform parent,
        out Slider slL, out Slider slA, out Slider slC, out Slider slS, out Slider slD,
        out TMP_Text lbL, out TMP_Text lbA, out TMP_Text lbC, out TMP_Text lbS, out TMP_Text lbD,
        out TMP_Dropdown cellDrop, out TMP_Dropdown faultDrop,
        out Slider severitySlider, out Toggle faultToggle,
        out Button injectBtn, out Button runBtn, out Button pauseBtn, out Button stopBtn)
    {
        slL = slA = slC = slS = slD = null;
        lbL = lbA = lbC = lbS = lbD = null;
        cellDrop = faultDrop = null;
        severitySlider = null;
        faultToggle = null;
        injectBtn = runBtn = pauseBtn = stopBtn = null;

        var col = NewUI("LeftColumn", parent, new Color(0, 0, 0, 0));
        var vl = col.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 8;
        vl.childForceExpandHeight = false;
        vl.childForceExpandWidth = true;
        vl.childControlHeight = true;
        vl.childControlWidth = true;
        var colLE = col.AddComponent<LayoutElement>();
        colLE.preferredWidth = 280;
        colLE.minWidth = 260;

        // ── Parameters panel ──────────────────────────────────────────
        var paramsPanel = NewUI("ParametersPanel", col.transform, CardBg);
        AddOutline(paramsPanel, Border, 1);
        var ppl = paramsPanel.AddComponent<VerticalLayoutGroup>();
        ppl.padding = new RectOffset(14, 14, 12, 14);
        ppl.spacing = 10;
        ppl.childForceExpandHeight = false;
        ppl.childControlHeight = true;
        ppl.childControlWidth = true;
        var paramsLE = paramsPanel.AddComponent<LayoutElement>();
        paramsLE.preferredHeight = 280;
        paramsLE.minHeight = 260;

        NewText(paramsPanel.transform, "Title", "SIMULATION PARAMETERS", 14, Accent,
            TextAlignmentOptions.MidlineLeft, true);

        AddSliderRow(paramsPanel.transform, "Load Current",        "40", "A",  5, 80, 40, out slL, out lbL);
        AddSliderRow(paramsPanel.transform, "Ambient Temperature", "35", "°C", 5, 50, 35, out slA, out lbA);
        AddSliderRow(paramsPanel.transform, "Charge Current",      "20", "A",  0, 40, 20, out slC, out lbC);
        AddSliderRow(paramsPanel.transform, "Initial SOC",         "78", "%", 10,100, 78, out slS, out lbS);
        AddSliderRow(paramsPanel.transform, "Simulation Duration", "2.0","h",0.5f,8f,2f, out slD, out lbD);

        // ── Fault injection panel ──────────────────────────────────────
        var faultPanel = NewUI("FaultInjectionPanel", col.transform, CardBg);
        AddOutline(faultPanel, Border, 1);
        var fpl = faultPanel.AddComponent<VerticalLayoutGroup>();
        fpl.padding = new RectOffset(14, 14, 12, 14);
        fpl.spacing = 8;
        fpl.childForceExpandHeight = false;
        fpl.childControlHeight = true;
        fpl.childControlWidth = true;
        var faultLE = faultPanel.AddComponent<LayoutElement>();
        faultLE.preferredHeight = 210;
        faultLE.minHeight = 190;

        NewText(faultPanel.transform, "Title", "FAULT INJECTION (OPTIONAL)", 14, Orange,
            TextAlignmentOptions.MidlineLeft, true);

        cellDrop = AddDropdownRow(faultPanel.transform, "Select Cell",
            new[]{"Cell 1","Cell 2","Cell 3","Cell 4","Cell 5","Cell 6","Cell 7","Cell 8"}, 3);
        faultDrop = AddDropdownRow(faultPanel.transform, "Fault Type",
            new[]{"Increased Internal Resistance","Capacity Fade","Thermal Runaway"}, 0);

        TMP_Text lbSev;
        AddSliderRow(faultPanel.transform, "Severity", "High", "", 0, 2, 2, out severitySlider, out lbSev, true);

        // Fault enable toggle
        faultToggle = AddToggleRow(faultPanel.transform, "Enable Fault Injection");

        injectBtn = NewButton(faultPanel.transform, "Btn_InjectFault", "INJECT FAULT",
            13, new Color(1f, 0.85f, 0.4f), new Color(0.20f, 0.13f, 0.02f, 1f));
        var injLE = injectBtn.gameObject.AddComponent<LayoutElement>();
        injLE.preferredHeight = 36;
        injLE.minHeight = 36;

        // ── Run / Pause / Stop controls (horizontal row) ──────────────
        var ctrlPanel = NewUI("ControlsPanel", col.transform, CardBg);
        AddOutline(ctrlPanel, Border, 1);
        var cpl = ctrlPanel.AddComponent<HorizontalLayoutGroup>();
        cpl.padding = new RectOffset(14, 14, 12, 14);
        cpl.spacing = 8;
        cpl.childForceExpandWidth = true;
        cpl.childForceExpandHeight = true;
        cpl.childControlWidth = true;
        cpl.childControlHeight = true;
        var ctrlLE = ctrlPanel.AddComponent<LayoutElement>();
        ctrlLE.preferredHeight = 60;
        ctrlLE.minHeight = 56;

        runBtn   = NewButton(ctrlPanel.transform, "Btn_Run",   "RUN",       13, Color.white, GreenBtn);
        pauseBtn = NewButton(ctrlPanel.transform, "Btn_Pause", "‖ PAUSE",     13, Color.white, BlueBtn);
        stopBtn  = NewButton(ctrlPanel.transform, "Btn_Stop",  "■ STOP",      13, Color.white, RedBtn);

        foreach (var b in new[]{runBtn, pauseBtn, stopBtn})
        {
            var le = b.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = b.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 36;
            le.minHeight = 36;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    //  Center column: 3D viz + KPI cards + forecast graphs
    // ─────────────────────────────────────────────────────────────────
    public static void BuildCenter(Transform parent,
        out SimulationGraph graphSoc, out SimulationGraph graphTemp,
        out SimulationGraph graphVolt, out SimulationGraph graphCell4,
        out Transform kpisPanel)
    {
        graphSoc = graphTemp = graphVolt = graphCell4 = null;
        kpisPanel = null;

        var col = NewUI("CenterColumn", parent, new Color(0, 0, 0, 0));
        var vl = col.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 8;
        vl.childForceExpandHeight = false;
        vl.childForceExpandWidth = true;
        vl.childControlHeight = true;
        vl.childControlWidth = true;
        var colLE = col.AddComponent<LayoutElement>();
        colLE.flexibleWidth = 1;
        colLE.minWidth = 400;

        // ── 3D viz panel ──────────────────────────────────────────────
        var vizPanel = NewUI("VisualizationPanel", col.transform, CardBg);
        AddOutline(vizPanel, Border, 1);
        var vizLE = vizPanel.AddComponent<LayoutElement>();
        vizLE.preferredHeight = 320;
        vizLE.minHeight = 280;

        var vizTitle = NewText(vizPanel.transform, "Title",
            "BATTERY PACK VISUALIZATION (SIMULATION)", 13, Accent,
            TextAlignmentOptions.MidlineLeft, true);
        Anchor(vizTitle.GetComponent<RectTransform>(), new Vector2(0,1), new Vector2(1,1),
               new Vector2(14,-30), new Vector2(-14,-6));

        var vizTime = NewText(vizPanel.transform, "VizTime",
            "00:00:00 / 02:00:00", 12, TextDim, TextAlignmentOptions.MidlineRight);
        Anchor(vizTime.GetComponent<RectTransform>(), new Vector2(0,1), new Vector2(1,1),
               new Vector2(14,-30), new Vector2(-14,-6));

        var rawHolder = NewUI("RawHolder", vizPanel.transform, new Color(0, 0, 0, 0));
        Anchor(rawHolder.GetComponent<RectTransform>(), new Vector2(0,0), new Vector2(1,1),
               new Vector2(10,10), new Vector2(-10, 36));
        // Bg child for the dark fill
        var rawBg = NewUI("Bg", rawHolder.transform, new Color(0.02f,0.04f,0.08f,1f));
        Stretch(rawBg.GetComponent<RectTransform>(), 0, 0, 0, 0);
        // RawImage child on its own GameObject (Image + RawImage conflict via MaskableGraphic)
        var rawImgGO = NewUI("RawImage", rawHolder.transform, new Color(0, 0, 0, 0));
        Stretch(rawImgGO.GetComponent<RectTransform>(), 0, 0, 0, 0);
        var rawImg = rawImgGO.AddComponent<RawImage>();
        rawImg.color = Color.white;

        var legend = NewUI("Legend", vizPanel.transform, new Color(0, 0, 0, 0));
        Anchor(legend.GetComponent<RectTransform>(), new Vector2(0,0), new Vector2(1,0),
               new Vector2(14,6), new Vector2(-14, 32));
        var legH = legend.AddComponent<HorizontalLayoutGroup>();
        legH.spacing = 16;
        legH.childAlignment = TextAnchor.MiddleLeft;
        legH.childForceExpandWidth = false;
        legH.childControlWidth = true;
        AddLegendDot(legend.transform, "Normal",  Green,  "(3.30V - 4.20V)");
        AddLegendDot(legend.transform, "Warning", Orange, "(3.00V - 3.30V or 4.20V - 4.25V)");
        AddLegendDot(legend.transform, "Critical",Red,    "(< 3.00V or > 4.25V)");

        // ── KPI panel (6 horizontal cards) ─────────────────────────────
        kpisPanel = NewUI("KPIsPanel", col.transform, CardBg).transform;
        AddOutline(kpisPanel.gameObject, Border, 1);
        var kpH = kpisPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
        kpH.padding = new RectOffset(10,10,10,10);
        kpH.spacing = 6;
        kpH.childForceExpandWidth = true;
        kpH.childForceExpandHeight = true;
        kpH.childControlWidth = true;
        kpH.childControlHeight = true;
        var kpLE = kpisPanel.gameObject.AddComponent<LayoutElement>();
        kpLE.preferredHeight = 110;
        kpLE.minHeight = 100;

        NewKPICard(kpisPanel, "KPI_Runtime",  "PREDICTED\nRUNTIME",       "1.85 h",  "± 5 min",                TextPri, Accent);
        NewKPICard(kpisPanel, "KPI_EndSoc",   "PREDICTED\nSOC (END)",     "23%",     "± 2%",                   TextPri, Accent);
        NewKPICard(kpisPanel, "KPI_MaxTemp",  "PREDICTED\nMAX TEMP",      "48.7°C",  "High",                   Red,    Accent);
        NewKPICard(kpisPanel, "KPI_EndVolt",  "PREDICTED\nPACK VOLTAGE (END)", "24.12 V", "Low",              TextPri, Accent);
        NewKPICard(kpisPanel, "KPI_CellRisk", "CELL\nAT RISK",            "Cell 4",  "High Risk",              Red,    Accent);
        NewKPICard(kpisPanel, "KPI_Health",   "HEALTH\nIMPACT",           "-3.2%",   "Est. SOH after cycle",   Orange, Accent);

        // ── Forecast graphs panel (4 graphs in a row) ──────────────────
        var graphsPanel = NewUI("GraphsPanel", col.transform, CardBg);
        AddOutline(graphsPanel, Border, 1);
        var gH = graphsPanel.AddComponent<HorizontalLayoutGroup>();
        gH.padding = new RectOffset(10,10,10,10);
        gH.spacing = 8;
        gH.childForceExpandWidth = true;
        gH.childForceExpandHeight = true;
        gH.childControlWidth = true;
        gH.childControlHeight = true;
        var gLE = graphsPanel.AddComponent<LayoutElement>();
        gLE.preferredHeight = 220;
        gLE.minHeight = 200;
        gLE.flexibleHeight = 1;

        graphSoc   = NewGraphCard(graphsPanel.transform, "Graph_SOC",       "SOC OVER TIME",            "%",  new Color(0.0f,0.85f,0.4f));
        graphTemp  = NewGraphCard(graphsPanel.transform, "Graph_Temp",      "TEMPERATURE OVER TIME",   "°C", new Color(1.0f,0.55f,0.0f));
        graphVolt  = NewGraphCard(graphsPanel.transform, "Graph_Volt",      "PACK VOLTAGE OVER TIME",  "V",  new Color(0.0f,0.65f,1.0f));
        graphCell4 = NewGraphCard(graphsPanel.transform, "Graph_Cell4Volt", "CELL 4 VOLTAGE OVER TIME","V",  new Color(1.0f,0.25f,0.25f));
    }

    // ─────────────────────────────────────────────────────────────────
    //  Right column: scenarios / alerts / thermal map
    // ─────────────────────────────────────────────────────────────────
    public static void BuildRight(Transform parent,
        out Transform scenContent, out Button addBtn, out Button exportBtn,
        out Transform alertContent, out RawImage thermalRaw)
    {
        scenContent = alertContent = null;
        addBtn = exportBtn = null;
        thermalRaw = null;

        var col = NewUI("RightColumn", parent, new Color(0, 0, 0, 0));
        var vl = col.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 8;
        vl.childForceExpandHeight = false;
        vl.childForceExpandWidth = true;
        vl.childControlHeight = true;
        vl.childControlWidth = true;
        var colLE = col.AddComponent<LayoutElement>();
        colLE.preferredWidth = 320;
        colLE.minWidth = 300;

        // ── Scenarios panel ───────────────────────────────────────────
        var scenPanel = NewUI("ScenariosPanel", col.transform, CardBg);
        AddOutline(scenPanel, Border, 1);
        var sH = scenPanel.AddComponent<VerticalLayoutGroup>();
        sH.padding = new RectOffset(12,12,10,12);
        sH.spacing = 6;
        sH.childForceExpandHeight = false;
        sH.childForceExpandWidth = true;
        sH.childControlHeight = true;
        sH.childControlWidth = true;
        var sLE = scenPanel.AddComponent<LayoutElement>();
        sLE.preferredHeight = 220;
        sLE.minHeight = 200;

        var titleRow = NewUI("TitleRow", scenPanel.transform, new Color(0, 0, 0, 0));
        var trH = titleRow.AddComponent<HorizontalLayoutGroup>();
        trH.childAlignment = TextAnchor.MiddleLeft;
        trH.childForceExpandWidth = false;
        trH.childControlWidth = true;
        var trLE = titleRow.AddComponent<LayoutElement>();
        trLE.preferredHeight = 24;
        var titleTxt = NewText(titleRow.transform, "Title", "WHAT-IF SCENARIO COMPARISON",
            13, Accent, TextAlignmentOptions.MidlineLeft, true);
        var titleTxtLE = titleTxt.gameObject.AddComponent<LayoutElement>();
        titleTxtLE.flexibleWidth = 1;
        addBtn = NewButton(titleRow.transform, "Btn_AddScenario", "+ ADD SCENARIO",
            11, Accent, new Color(0, 0, 0, 0));
        var addLE = addBtn.gameObject.AddComponent<LayoutElement>();
        addLE.preferredWidth = 130;

        var tableHeader = NewUI("TableHeader", scenPanel.transform, new Color(0, 0, 0, 0));
        var thH = tableHeader.AddComponent<HorizontalLayoutGroup>();
        thH.padding = new RectOffset(8,8,4,4);
        thH.spacing = 4;
        thH.childAlignment = TextAnchor.MiddleLeft;
        thH.childForceExpandWidth = true;
        thH.childControlWidth = true;
        var thLE = tableHeader.AddComponent<LayoutElement>();
        thLE.preferredHeight = 24;
        thLE.minHeight = 24;
        string[] hdrs = {"Scenario", "Load (A)", "Amb. (°C)", "Runtime (h)", "Max T (°C)", "End SOC (%)"};
        foreach (var hh in hdrs)
            NewText(tableHeader.transform, "H_"+hh, hh, 10, TextDim, TextAlignmentOptions.Center, true);

        var scenContentGO = NewUI("ScenarioContent", scenPanel.transform, new Color(0, 0, 0, 0));
        var scLE = scenContentGO.AddComponent<LayoutElement>();
        scLE.flexibleHeight = 1;
        scLE.minHeight = 60;
        var scV = scenContentGO.AddComponent<VerticalLayoutGroup>();
        scV.spacing = 2;
        scV.childForceExpandHeight = false;
        scV.childForceExpandWidth = true;
        scV.childControlHeight = true;
        scV.childControlWidth = true;
        scenContent = scenContentGO.transform;

        exportBtn = NewButton(scenPanel.transform, "Btn_Export", "EXPORT RESULTS",
            12, Accent, new Color(0.04f, 0.10f, 0.18f, 1f));
        var exLE = exportBtn.gameObject.AddComponent<LayoutElement>();
        exLE.preferredHeight = 30;

        // ── Alerts panel ───────────────────────────────────────────────
        var alertsPanel = NewUI("AlertsPanel", col.transform, CardBg);
        AddOutline(alertsPanel, Border, 1);
        var aH = alertsPanel.AddComponent<VerticalLayoutGroup>();
        aH.padding = new RectOffset(12,12,10,12);
        aH.spacing = 6;
        aH.childForceExpandHeight = false;
        aH.childForceExpandWidth = true;
        aH.childControlHeight = true;
        aH.childControlWidth = true;
        var alLE = alertsPanel.AddComponent<LayoutElement>();
        alLE.preferredHeight = 220;
        alLE.minHeight = 200;

        NewText(alertsPanel.transform, "Title", "SIMULATION ALERTS & PREDICTIONS",
            13, Orange, TextAlignmentOptions.MidlineLeft, true);

        var alertContentGO = NewUI("AlertContent", alertsPanel.transform, new Color(0, 0, 0, 0));
        var acLE = alertContentGO.AddComponent<LayoutElement>();
        acLE.flexibleHeight = 1;
        acLE.minHeight = 60;
        var acV = alertContentGO.AddComponent<VerticalLayoutGroup>();
        acV.spacing = 6;
        acV.childForceExpandHeight = false;
        acV.childForceExpandWidth = true;
        acV.childControlHeight = true;
        acV.childControlWidth = true;
        alertContent = alertContentGO.transform;

        // ── Thermal map panel ──────────────────────────────────────────
        var thermalPanel = NewUI("ThermalMapPanel", col.transform, CardBg);
        AddOutline(thermalPanel, Border, 1);
        var tH = thermalPanel.AddComponent<VerticalLayoutGroup>();
        tH.padding = new RectOffset(12,12,10,12);
        tH.spacing = 6;
        tH.childForceExpandHeight = false;
        tH.childForceExpandWidth = true;
        tH.childControlHeight = true;
        tH.childControlWidth = true;
        var tLE = thermalPanel.AddComponent<LayoutElement>();
        tLE.preferredHeight = 220;
        tLE.minHeight = 200;
        tLE.flexibleHeight = 1;

        NewText(thermalPanel.transform, "Title", "3D THERMAL MAP (PEAK PREDICTION)",
            13, Accent, TextAlignmentOptions.MidlineLeft, true);

        var thermalHolder = NewUI("ThermalHolder", thermalPanel.transform, new Color(0, 0, 0, 0));
        var th2LE = thermalHolder.AddComponent<LayoutElement>();
        th2LE.flexibleHeight = 1;
        th2LE.minHeight = 100;
        var thBg = NewUI("Bg", thermalHolder.transform, new Color(0.02f,0.04f,0.08f,1f));
        Stretch(thBg.GetComponent<RectTransform>(), 0, 0, 0, 0);
        var thRawImgGO = NewUI("RawImage", thermalHolder.transform, new Color(0, 0, 0, 0));
        Stretch(thRawImgGO.GetComponent<RectTransform>(), 0, 0, 0, 0);
        thermalRaw = thRawImgGO.AddComponent<RawImage>();
        thermalRaw.color = Color.white;

        var scale = NewUI("Scale", thermalPanel.transform, new Color(0, 0, 0, 0));
        var scLE2 = scale.AddComponent<LayoutElement>();
        scLE2.preferredHeight = 14;
        scLE2.minHeight = 14;
        var scH = scale.AddComponent<HorizontalLayoutGroup>();
        scH.spacing = 4;
        scH.childAlignment = TextAnchor.MiddleLeft;
        scH.childForceExpandWidth = false;
        scH.childControlWidth = true;
        var sLeft = NewText(scale.transform, "ScaleMin", "0", 10,
            new Color(0, 0.4f, 1), TextAlignmentOptions.Center);
        var sLeftLE = sLeft.gameObject.AddComponent<LayoutElement>();
        sLeftLE.preferredWidth = 30;
        var sMid = NewUI("Spacer", scale.transform, new Color(0, 0, 0, 0));
        var sMidLE = sMid.AddComponent<LayoutElement>();
        sMidLE.flexibleWidth = 1;
        var sRight = NewText(scale.transform, "ScaleMax", "60 °C", 10,
            new Color(1, 0.3f, 0), TextAlignmentOptions.MidlineRight);
        var sRightLE = sRight.gameObject.AddComponent<LayoutElement>();
        sRightLE.preferredWidth = 60;
    }

    // ─────────────────────────────────────────────────────────────────
    //  Inner component helpers
    // ─────────────────────────────────────────────────────────────────

    /// <summary>Add a labeled slider row to the given parent.</summary>
    public static void AddSliderRow(Transform parent, string label, string initial, string unit,
        float min, float max, float val,
        out Slider slider, out TMP_Text valueLabel, bool integerSteps = false)
    {
        var row = NewUI("Slider_" + label, parent, new Color(0, 0, 0, 0));
        var vl = row.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 2;
        vl.childForceExpandHeight = false;
        vl.childForceExpandWidth = true;
        vl.childControlHeight = true;
        vl.childControlWidth = true;
        var rLE = row.AddComponent<LayoutElement>();
        rLE.preferredHeight = 36;
        rLE.minHeight = 34;

        var headerRow = NewUI("Header", row.transform, new Color(0, 0, 0, 0));
        var hh = headerRow.AddComponent<HorizontalLayoutGroup>();
        hh.childForceExpandWidth = false;
        hh.childControlWidth = true;
        hh.childAlignment = TextAnchor.MiddleLeft;
        var hLE = headerRow.AddComponent<LayoutElement>();
        hLE.preferredHeight = 18;
        hLE.minHeight = 18;

        var nameTxt = NewText(headerRow.transform, "Name", label, 12, TextLabel,
            TextAlignmentOptions.MidlineLeft);
        var nameLE = nameTxt.gameObject.AddComponent<LayoutElement>();
        nameLE.flexibleWidth = 1;

        valueLabel = NewText(headerRow.transform, "Value",
            initial + (string.IsNullOrEmpty(unit) ? "" : " " + unit),
            12, Accent, TextAlignmentOptions.MidlineRight, true);

        var sliderGO = NewUI("Slider", row.transform, new Color(0, 0, 0, 0));
        var sLE = sliderGO.AddComponent<LayoutElement>();
        sLE.preferredHeight = 14;
        sLE.minHeight = 14;
        slider = sliderGO.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = val;
        slider.wholeNumbers = integerSteps;

        var bg = NewUI("Background", sliderGO.transform, new Color(0.04f, 0.10f, 0.18f, 1f));
        Stretch(bg.GetComponent<RectTransform>(), 0, 5, 0, 5);
        bg.GetComponent<Image>().raycastTarget = false;

        var fillArea = NewUI("Fill Area", sliderGO.transform, new Color(0, 0, 0, 0));
        Stretch(fillArea.GetComponent<RectTransform>(), 5, 5, 5, 5);
        var fill = NewUI("Fill", fillArea.transform, Accent);
        Stretch(fill.GetComponent<RectTransform>(), 0, 0, 0, 0);
        fill.GetComponent<Image>().raycastTarget = false;

        var handleArea = NewUI("Handle Slide Area", sliderGO.transform, new Color(0, 0, 0, 0));
        Stretch(handleArea.GetComponent<RectTransform>(), 5, 5, 5, 5);
        var handle = NewUI("Handle", handleArea.transform, Accent);
        var handleRT = handle.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(8, 0);
        Anchor(handleRT, new Vector2(0,0), new Vector2(0,1), new Vector2(-4,0), new Vector2(4,0));
        var handleImg = handle.GetComponent<Image>();
        handleImg.raycastTarget = true;

        slider.targetGraphic = handleImg;
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handleRT;
    }

    /// <summary>Add a labeled TMP_Dropdown row.</summary>
    public static TMP_Dropdown AddDropdownRow(Transform parent, string label, string[] options, int defaultIdx)
    {
        var row = NewUI("Drop_" + label, parent, new Color(0, 0, 0, 0));
        var vl = row.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 2;
        vl.childForceExpandHeight = false;
        vl.childControlHeight = true;
        vl.childForceExpandWidth = true;
        vl.childControlWidth = true;
        var rLE = row.AddComponent<LayoutElement>();
        rLE.preferredHeight = 44;
        rLE.minHeight = 42;

        NewText(row.transform, "Name", label, 12, TextLabel, TextAlignmentOptions.MidlineLeft);

        var dropGO = NewUI("Dropdown", row.transform, new Color(0.04f, 0.10f, 0.18f, 1f));
        var dLE = dropGO.AddComponent<LayoutElement>();
        dLE.preferredHeight = 28;
        dLE.minHeight = 26;
        var drop = dropGO.AddComponent<TMP_Dropdown>();
        drop.captionText = NewText(dropGO.transform, "Label", options[defaultIdx], 13, TextPri,
            TextAlignmentOptions.MidlineLeft, false);
        Stretch(drop.captionText.GetComponent<RectTransform>(), 8, 4, 24, 4);

        var arrow = NewUI("Arrow", dropGO.transform, new Color(0, 0, 0, 0));
        Anchor(arrow.GetComponent<RectTransform>(), new Vector2(1,0), new Vector2(1,1),
               new Vector2(-20,2), new Vector2(-4,-2));
        var aTxt = NewText(arrow.transform, "A", "▼", 12, TextDim, TextAlignmentOptions.Center);
        Stretch(aTxt.GetComponent<RectTransform>(), 0, 0, 0, 0);

        var tmpl = NewUI("Template", dropGO.transform, new Color(0.06f, 0.13f, 0.20f, 1f));
        Anchor(tmpl.GetComponent<RectTransform>(), new Vector2(0,0), new Vector2(1,0),
               new Vector2(0,-120), new Vector2(0,0));
        var tv = tmpl.AddComponent<VerticalLayoutGroup>();
        tv.spacing = 0;
        tv.childForceExpandHeight = false;
        tv.childControlHeight = true;
        var item = NewUI("Item", tmpl.transform, new Color(0, 0, 0, 0));
        var itLE = item.AddComponent<LayoutElement>();
        itLE.preferredHeight = 24;
        var itemImg = item.AddComponent<Image>();
        itemImg.color = new Color(0.08f, 0.16f, 0.24f, 1f);
        var itTxt = NewText(item.transform, "Item Label", "Option", 13, TextPri, TextAlignmentOptions.MidlineLeft);
        Stretch(itTxt.GetComponent<RectTransform>(), 8, 2, 8, 2);
        var tg = item.AddComponent<Toggle>();
        tg.targetGraphic = itemImg;
        drop.template = tmpl.GetComponent<RectTransform>();
        drop.itemText = itTxt;
        tmpl.gameObject.SetActive(false);

        drop.ClearOptions();
        drop.AddOptions(new List<string>(options));
        drop.value = defaultIdx;
        drop.RefreshShownValue();

        return drop;
    }

    /// <summary>Add a single KPI card with title, value, and sub-label.</summary>
    public static void NewKPICard(Transform parent, string name, string title, string value, string sub,
        Color valueColor, Color titleColor)
    {
        var card = NewUI(name, parent, CardBgDim);
        AddOutline(card, Border, 1);
        var vl = card.AddComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset(8,8,6,8);
        vl.spacing = 2;
        vl.childForceExpandHeight = true;
        vl.childForceExpandWidth = true;
        vl.childControlHeight = true;
        vl.childControlWidth = true;
        NewText(card.transform, "Title", title, 10, titleColor, TextAlignmentOptions.TopLeft, true);
        NewText(card.transform, "Value", value, 22, valueColor, TextAlignmentOptions.BottomLeft, true);
        NewText(card.transform, "Sub",   sub,   10, TextDim, TextAlignmentOptions.BottomLeft);
    }

    /// <summary>Add a small labeled toggle row.</summary>
    public static Toggle AddToggleRow(Transform parent, string label)
    {
        var row = NewUI("Toggle_" + label, parent, new Color(0, 0, 0, 0));
        var hl = row.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 8;
        hl.childAlignment = TextAnchor.MiddleLeft;
        hl.childForceExpandWidth = false;
        hl.childControlWidth = true;
        var rLE = row.AddComponent<LayoutElement>();
        rLE.preferredHeight = 24;
        rLE.minHeight = 22;

        var toggleGO = NewUI("Toggle", row.transform, new Color(0, 0, 0, 0));
        var tLE = toggleGO.AddComponent<LayoutElement>();
        tLE.preferredWidth = 24;
        tLE.preferredHeight = 24;
        var toggle = toggleGO.AddComponent<Toggle>();

        var bg = NewUI("Background", toggleGO.transform, new Color(0.04f, 0.10f, 0.18f, 1f));
        Stretch(bg.GetComponent<RectTransform>(), 1, 1, 1, 1);
        bg.GetComponent<Image>().raycastTarget = false;

        var check = NewUI("Checkmark", toggleGO.transform, Accent);
        Stretch(check.GetComponent<RectTransform>(), 4, 4, 4, 4);
        check.SetActive(false);
        toggle.targetGraphic = bg.GetComponent<Image>();
        toggle.graphic = check.GetComponent<Image>();
        toggle.onValueChanged.AddListener(on => check.SetActive(on));

        NewText(row.transform, "Label", label, 12, TextLabel, TextAlignmentOptions.MidlineLeft);

        return toggle;
    }

    /// <summary>Add a single graph card with a LineRenderer and axis labels.</summary>
    public static SimulationGraph NewGraphCard(Transform parent, string name, string title, string yUnit, Color lineColor)
    {
        var card = NewUI(name, parent, CardBgDim);
        AddOutline(card, Border, 1);
        var vl = card.AddComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset(8,8,6,8);
        vl.spacing = 2;
        vl.childForceExpandHeight = true;
        vl.childForceExpandWidth = true;
        vl.childControlHeight = true;
        vl.childControlWidth = true;

        NewText(card.transform, "Title", title, 10, Accent, TextAlignmentOptions.TopLeft, true);

        var graphGO = NewUI("Graph", card.transform, new Color(0.02f,0.04f,0.08f,1f));
        var gLE = graphGO.AddComponent<LayoutElement>();
        gLE.flexibleHeight = 1;
        gLE.minHeight = 80;

        var lineGO = new GameObject("Line", typeof(RectTransform), typeof(CanvasRenderer));
        lineGO.transform.SetParent(graphGO.transform, false);
        Stretch(lineGO.GetComponent<RectTransform>(), 4, 12, 4, 14);
        var lr = lineGO.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.startWidth = 2f; lr.endWidth = 2f;
        var mat = new Material(Shader.Find("Sprites/Default"));
        lr.material = mat;
        lr.startColor = lineColor; lr.endColor = lineColor;
        lr.numCapVertices = 4; lr.numCornerVertices = 4;

        var yMax = NewText(graphGO.transform, "YMax", $"0{yUnit}", 9, TextDim, TextAlignmentOptions.TopRight);
        Anchor(yMax.GetComponent<RectTransform>(), new Vector2(1,1), new Vector2(1,1), new Vector2(-30,-2), new Vector2(-2,-2));
        var yMin = NewText(graphGO.transform, "YMin", $"0{yUnit}", 9, TextDim, TextAlignmentOptions.BottomRight);
        Anchor(yMin.GetComponent<RectTransform>(), new Vector2(1,0), new Vector2(1,0), new Vector2(-30,2), new Vector2(-2,14));
        var xMax = NewText(graphGO.transform, "XMax", "0h", 9, TextDim, TextAlignmentOptions.BottomRight);
        Anchor(xMax.GetComponent<RectTransform>(), new Vector2(1,0), new Vector2(1,0), new Vector2(-30,2), new Vector2(-2,2));

        var simGraph = graphGO.AddComponent<SimulationGraph>();
        simGraph.lineRenderer = lr;
        simGraph.labelTitle   = FindTMP(card.transform, "Title");
        simGraph.labelYMax    = yMax;
        simGraph.labelYMin    = yMin;
        simGraph.labelXMax    = xMax;
        simGraph.lineColor    = lineColor;
        simGraph.yUnit        = yUnit;
        simGraph.titleText    = title;
        return simGraph;
    }

    /// <summary>Instantiate an alert prefab with the given title + recommendation text.</summary>
    public static void AddPlaceholderAlert(Transform parent, GameObject prefab, string title, string rec, Color bg)
    {
        var row = Object.Instantiate(prefab, parent);
        row.name = "AlertRow";
        var img = row.GetComponent<Image>();
        if (img != null) img.color = bg;
        var titleTxt = row.transform.Find("TextContainer/Title")?.GetComponent<TMP_Text>();
        var recTxt   = row.transform.Find("TextContainer/Recommendation")?.GetComponent<TMP_Text>();
        if (titleTxt != null) titleTxt.text = title;
        if (recTxt   != null) recTxt.text   = rec;
    }
}