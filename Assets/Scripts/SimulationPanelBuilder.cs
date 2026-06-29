using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BMS;
using static SimUIHelpers;

/// <summary>
/// Orchestrator that builds Panel_Simulation + Panel_History under
/// DashboardCanvas at edit time. Runs once on Start(), then disables itself.
///
/// Most of the actual UI construction is delegated to:
///   - SimHeaderBuilder       (top header row)
///   - SimColumnBuilders      (left/center/right columns + sub-components)
///   - SimCameraSetup         (SimPackCamera + SimThermalCamera)
///
/// Shared helpers + palette are in SimUIHelpers.
/// </summary>
public class SimulationPanelBuilder : MonoBehaviour
{
    [Header("Scene References")]
    public Transform batteryPackTransform;
    public BmsController bmsController;
    public ViewModeManager viewModeManager;

    [Header("Battery Pack Cell Renderers")]
    public Renderer[] cellRenderers = new Renderer[8];

    [Header("Row Prefab Templates")]
    public GameObject alertRowPrefab;
    public GameObject scenarioRowPrefab;

    [Header("Layout")]
    public float headerHeight = 70f;
    public float footerHeight = 36f;

    // ── Public results ──────────────────────────────────────────────────
    public GameObject PanelSimulation { get; private set; }
    public GameObject PanelHistory    { get; private set; }

    // ── Internal state ──────────────────────────────────────────────────
    Canvas _canvas;
    RectTransform _canvasRect;
    SimulationUI _ui;
    SimulationController _ctrl;
    ThermalThumbnail _thermalThumb;

    void Start()
    {
        // Guard: don't rebuild if the panel already exists
        if (PanelSimulation != null || GameObject.Find("Panel_Simulation") != null)
        {
            enabled = false;
            return;
        }

        // ── Resolve scene references ────────────────────────────────────
        if (batteryPackTransform == null)
        {
            var bp = GameObject.Find("BatteryPack");
            if (bp != null) batteryPackTransform = bp.transform;
        }
        if (bmsController == null && batteryPackTransform != null)
            bmsController = batteryPackTransform.GetComponent<BmsController>();
        if (viewModeManager == null)
        {
            var dc = GameObject.Find("DashboardCanvas");
            if (dc != null) viewModeManager = dc.GetComponent<ViewModeManager>();
        }

        var canvasGO = GameObject.Find("DashboardCanvas");
        if (canvasGO == null) { Debug.LogError("[SimBuilder] DashboardCanvas not found"); return; }
        _canvas = canvasGO.GetComponent<Canvas>();
        _canvasRect = canvasGO.GetComponent<RectTransform>();

        BuildSimulationPanel();
        BuildHistoryPanel();

        Debug.Log("[SimBuilder] Simulation panel built successfully.");
        enabled = false;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Panel_Simulation
    // ═════════════════════════════════════════════════════════════════════
    void BuildSimulationPanel()
    {
        PanelSimulation = NewUI("Panel_Simulation", _canvasRect, PanelBg);

        // Anchor: leave room for the TopBar (60px tall) and LeftNav (180px wide)
        var psRT = PanelSimulation.GetComponent<RectTransform>();
        psRT.anchorMin = new Vector2(0, 0);
        psRT.anchorMax = new Vector2(1, 1);
        psRT.offsetMin = new Vector2(180, 0);
        psRT.offsetMax = new Vector2(0, -60);
        // Render as the FIRST sibling so TopBar/LeftNav stay visible and clickable
        PanelSimulation.transform.SetSiblingIndex(0);
        // NO_CANVAS_ON_PANEL_SIM — Panel_Simulation must NOT have its own Canvas;
        // DashboardCanvas owns all raycasting. Remove any stray Canvas here.
        var _strayCanvas = PanelSimulation.GetComponent<Canvas>();
        if (_strayCanvas != null) DestroyImmediate(_strayCanvas);
        var _strayGR = PanelSimulation.GetComponent<UnityEngine.UI.GraphicRaycaster>();
        if (_strayGR != null) DestroyImmediate(_strayGR);
        // Root image must block raycasts so clicks don't fall to the 3D scene
        var _rootImg = PanelSimulation.GetComponent<UnityEngine.UI.Image>();
        if (_rootImg != null) _rootImg.raycastTarget = true;
        // Start HIDDEN — only becomes visible when SIMULATION nav is clicked
        PanelSimulation.SetActive(false);

        // Footer (disclaimer)
        var footer = NewUI("Footer", PanelSimulation.GetComponent<RectTransform>(), new Color(0, 0, 0, 0));
        Anchor(footer.GetComponent<RectTransform>(), new Vector2(0,0), new Vector2(1,0),
               new Vector2(8,0), new Vector2(-8, footerHeight));
        var fTxt = NewText(footer.transform, "Disclaimer",
            "Simulation results are approximate and based on battery model parameters. Actual performance may vary.",
            12, TextDim, TextAlignmentOptions.MidlineLeft);
        Stretch(fTxt.GetComponent<RectTransform>(), 12, 4, 12, 4);

        // Content area
        var content = NewUI("Content", PanelSimulation.GetComponent<RectTransform>(), new Color(0, 0, 0, 0));
        Anchor(content.GetComponent<RectTransform>(), new Vector2(0,0), new Vector2(1,1),
               new Vector2(0,0), new Vector2(0, -footerHeight));

        // Content row (3 columns)
        var contentRow = NewUI("ContentRow", content.GetComponent<RectTransform>(), new Color(0, 0, 0, 0));
        Anchor(contentRow.GetComponent<RectTransform>(), new Vector2(0,0), new Vector2(1,1),
               new Vector2(8, 8), new Vector2(-8, -(8 + headerHeight + 8)));
        var crH = contentRow.AddComponent<HorizontalLayoutGroup>();
        crH.spacing = 8;
        crH.childAlignment = TextAnchor.UpperLeft;
        crH.childForceExpandWidth = false;
        crH.childForceExpandHeight = true;
        crH.childControlWidth = true;
        crH.childControlHeight = true;

        BuildLeftColumn(contentRow.transform);
        BuildCenterColumn(contentRow.transform);
        BuildRightColumn(contentRow.transform);

        // Header row (built after _ui exists so we can capture the time label)
        var header = NewUI("Header", content.GetComponent<RectTransform>(), CardBgDim);
        Anchor(header.GetComponent<RectTransform>(), new Vector2(0,1), new Vector2(1,1),
               new Vector2(8, -8 - headerHeight), new Vector2(-8, -8));
        AddOutline(header, Border, 1);
        TMP_Text timeLabel;
        SimHeaderBuilder.Build(header.transform, out timeLabel);
        if (_ui != null) _ui.lblElapsed = timeLabel;

        // Cameras are deferred so the RawImages exist before we wire them
        StartCoroutine(DeferredCameraSetup());
    }

    // ── Left column ──────────────────────────────────────────────────────
    void BuildLeftColumn(Transform parent)
    {
        Slider slL, slA, slC, slS, slD;
        TMP_Text lbL, lbA, lbC, lbS, lbD;
        TMP_Dropdown cellDrop, faultDrop;
        Button injectBtn, runBtn, pauseBtn, stopBtn;

        Slider slSev;
        Toggle tgFault;

        SimColumnBuilders.BuildLeft(parent,
            out slL, out slA, out slC, out slS, out slD,
            out lbL, out lbA, out lbC, out lbS, out lbD,
            out cellDrop, out faultDrop,
            out slSev, out tgFault,
            out injectBtn, out runBtn, out pauseBtn, out stopBtn);

        // ── SimulationUI object (host for references) ───────────────────
        var simGO = new GameObject("SimulationUI");
        simGO.transform.SetParent(ManagerRoot(), false);
        _ui = simGO.AddComponent<SimulationUI>();
        _ui.sliderLoad        = slL;
        _ui.sliderAmbient     = slA;
        _ui.sliderCharge      = slC;
        _ui.sliderSoc         = slS;
        _ui.sliderDuration    = slD;
        _ui.lblLoad           = lbL;
        _ui.lblAmbient        = lbA;
        _ui.lblCharge         = lbC;
        _ui.lblSoc            = lbS;
        _ui.lblDuration       = lbD;
        _ui.dropCell          = cellDrop;
        _ui.dropFaultType     = faultDrop;
        _ui.sliderSeverityFallback = slSev;
        _ui.toggleFault       = tgFault;
        _ui.btnInjectFault    = injectBtn;
        _ui.btnRun            = runBtn;
        _ui.btnPause          = pauseBtn;
        _ui.btnStop           = stopBtn;
        _ui.alertRowPrefab    = alertRowPrefab;
        _ui.scenarioRowPrefab = scenarioRowPrefab;

        // SimulationController
        var ctrlGO = new GameObject("SimulationController");
        ctrlGO.transform.SetParent(ManagerRoot(), false);
        _ctrl = ctrlGO.AddComponent<SimulationController>();
        _ctrl.bmsController     = bmsController;
        _ctrl.viewModeManager   = viewModeManager;
        _ctrl.simulationUI      = _ui;
        _ctrl.cellRenderers     = cellRenderers;
        _ctrl.cellBodyMaterialIndex = 1;
        _ui.simController = _ctrl;
    }

    // ── Center column ───────────────────────────────────────────────────
    void BuildCenterColumn(Transform parent)
    {
        SimulationGraph graphSoc, graphTemp, graphVolt, graphCell4;
        Transform kpisPanel;

        SimColumnBuilders.BuildCenter(parent,
            out graphSoc, out graphTemp, out graphVolt, out graphCell4,
            out kpisPanel);

        if (_ui != null)
        {
            _ui.graphSoc       = graphSoc;
            _ui.graphTemp      = graphTemp;
            _ui.graphVolt      = graphVolt;
            _ui.graphCellFault = graphCell4;
            _ui.kpiRuntime        = FindTMP(kpisPanel, "KPI_Runtime/Value");
            _ui.kpiRuntimeSub     = FindTMP(kpisPanel, "KPI_Runtime/Sub");
            _ui.kpiEndSoc         = FindTMP(kpisPanel, "KPI_EndSoc/Value");
            _ui.kpiEndSocSub      = FindTMP(kpisPanel, "KPI_EndSoc/Sub");
            _ui.kpiMaxTemp        = FindTMP(kpisPanel, "KPI_MaxTemp/Value");
            _ui.kpiMaxTempStatus  = FindTMP(kpisPanel, "KPI_MaxTemp/Sub");
            _ui.kpiEndVolt        = FindTMP(kpisPanel, "KPI_EndVolt/Value");
            _ui.kpiEndVoltStatus  = FindTMP(kpisPanel, "KPI_EndVolt/Sub");
            _ui.kpiCellRisk       = FindTMP(kpisPanel, "KPI_CellRisk/Value");
            _ui.kpiCellRiskStatus = FindTMP(kpisPanel, "KPI_CellRisk/Sub");
            _ui.kpiHealth         = FindTMP(kpisPanel, "KPI_Health/Value");
            _ui.kpiHealthSub      = FindTMP(kpisPanel, "KPI_Health/Sub");
        }
    }

    // ── Right column ────────────────────────────────────────────────────
    void BuildRightColumn(Transform parent)
    {
        Transform scenContent, alertContent;
        Button addBtn, exportBtn;
        RawImage thermalRaw;

        SimColumnBuilders.BuildRight(parent,
            out scenContent, out addBtn, out exportBtn,
            out alertContent, out thermalRaw);

        if (_ui != null)
        {
            _ui.scenarioContent = scenContent;
            _ui.btnAddScenario  = addBtn;
            _ui.btnExport       = exportBtn;
            _ui.alertContent    = alertContent;
        }

        // ThermalThumbnail component (host for the camera + RT reference)
        var ttHost = new GameObject("ThermalThumbnail");
        ttHost.transform.SetParent(ManagerRoot(), false);
        _thermalThumb = ttHost.AddComponent<ThermalThumbnail>();
        var cellGroup = batteryPackTransform != null ? batteryPackTransform.Find("CellGroup") : null;
        _thermalThumb.cellGroupTransform = cellGroup != null ? cellGroup : batteryPackTransform;
        _thermalThumb.targetRawImage     = thermalRaw;
        if (_ui != null) _ui.thermalThumbnail = _thermalThumb;
    }

    // ── Persistent manager root for runtime simulation objects ────────
    Transform ManagerRoot()
    {
        var go = GameObject.Find("BatteryPanel");
        if (go == null)
        {
            go = new GameObject("BatteryPanel");
            // Keep managers alive and stable even if scene objects get reparented
            Object.DontDestroyOnLoad(go);
        }
        return go.transform;
    }

    // ── Camera setup (deferred so RawImages exist first) ────────────────
    IEnumerator DeferredCameraSetup()
    {
        yield return null;

        Transform vizRaw  = PanelSimulation != null
            ? PanelSimulation.transform.Find("Content/ContentRow/CenterColumn/VisualizationPanel/RawHolder/RawImage")
            : null;
        Transform thermRaw = PanelSimulation != null
            ? PanelSimulation.transform.Find("Content/ContentRow/RightColumn/ThermalMapPanel/ThermalHolder/RawImage")
            : null;

        SimCameraSetup.Build(batteryPackTransform, vizRaw, thermRaw, _thermalThumb);

        // Wait one more frame to ensure NavController has started, then wire nav.
        yield return null;
        WireNavigation();
    }

    void WireNavigation()
    {
        var nav = Object.FindFirstObjectByType<NavController>();
        if (nav == null)
        {
            Debug.LogWarning("[SimBuilder] NavController not found; simulation nav will not work.");
            return;
        }

        if (PanelSimulation != null)
            nav.RegisterPanel(5, PanelSimulation);

        if (_ctrl != null)
            nav.RegisterSimulationController(_ctrl);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  Panel_History (placeholder)
    // ═════════════════════════════════════════════════════════════════════
    void BuildHistoryPanel()
    {
        PanelHistory = NewUI("Panel_History", _canvasRect, PanelBg);
        Stretch(PanelHistory.GetComponent<RectTransform>(), 0, 0, 0, 0);
        PanelHistory.SetActive(false);

        var title = NewText(PanelHistory.transform, "Title", "HISTORY", 36, Accent,
            TextAlignmentOptions.Center, true);
        Anchor(title.GetComponent<RectTransform>(), new Vector2(0,1), new Vector2(1,1),
               new Vector2(0,-180), new Vector2(0,-100));
        var sub = NewText(PanelHistory.transform, "Sub", "Simulation history view — coming soon.",
            16, TextDim, TextAlignmentOptions.Center);
        Anchor(sub.GetComponent<RectTransform>(), new Vector2(0,1), new Vector2(1,1),
               new Vector2(0,-110), new Vector2(0,-70));
    }
}