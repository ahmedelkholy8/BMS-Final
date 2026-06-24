using BMS;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to DashboardCanvas.
/// Raycasts each frame from the mouse against the 8 cell colliders and
/// shows/hides the CellTooltip panel with live per-cell data.
/// Works in every view mode - no dependency on ViewModeManager.
/// </summary>
public class CellTooltip : MonoBehaviour
{
    [Header("References")]
    public BmsController  bmsController;
    public Camera         sceneCamera;

    [Header("Tooltip Panel")]
    public RectTransform  tooltipRoot;
    public TextMeshProUGUI lblTitle;
    public TextMeshProUGUI lblTempValue;
    public TextMeshProUGUI lblSocValue;
    public TextMeshProUGUI lblSohValue;
    public Image           dotTemp;
    public Image           dotSoc;
    public Image           dotSoh;

    [Header("Thresholds")]
    public float tempWarn = 40f;
    public float tempCrit = 50f;
    public float socWarn  = 0.30f;
    public float socCrit  = 0.20f;
    public float sohWarn  = 0.80f;
    public float sohCrit  = 0.70f;

    [Header("Offset (screen pixels)")]
    public Vector2 cursorOffset = new Vector2(18f, -18f);

    // ─── internals ────────────────────────────────────────────────
    private Canvas         _canvas;
    private CanvasScaler   _scaler;
    private RectTransform  _canvasRect;

    // cached colliders; filled in Start
    private Collider[]     _cellColliders = new Collider[8];
    private int            _hoveredCell   = -1;   // 0-based

    // Status colours matching the rest of the dashboard
    private static readonly Color ColOk   = new Color(0f,    0.80f, 0.40f);
    private static readonly Color ColWarn = new Color(1f,    0.75f, 0f   );
    private static readonly Color ColCrit = new Color(0.95f, 0.15f, 0.10f);

    void Start()
    {
        _canvas     = GetComponent<Canvas>();
        _scaler     = GetComponent<CanvasScaler>();
        _canvasRect = GetComponent<RectTransform>();

        for (int i = 0; i < 8; i++)
        {
            string name = "Cell_0" + (i + 1);
            GameObject go = GameObject.Find(name);
            if (go != null)
                _cellColliders[i] = go.GetComponent<Collider>();
        }

        if (tooltipRoot != null) tooltipRoot.gameObject.SetActive(false);
    }

    void Update()
    {
        if (sceneCamera == null || bmsController == null || tooltipRoot == null) return;

        int hit = GetHoveredCell();

        if (hit != _hoveredCell)
        {
            _hoveredCell = hit;
            tooltipRoot.gameObject.SetActive(hit >= 0);
        }

        if (_hoveredCell >= 0)
        {
            RefreshData(_hoveredCell);
            PositionTooltip();
        }
    }

    // ─── Raycast against cell colliders ───────────────────────────
    int GetHoveredCell()
    {
        Ray ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
        float bestDist = float.MaxValue;
        int best = -1;

        for (int i = 0; i < _cellColliders.Length; i++)
        {
            if (_cellColliders[i] == null) continue;
            RaycastHit rh;
            if (_cellColliders[i].Raycast(ray, out rh, 200f))
            {
                if (rh.distance < bestDist)
                {
                    bestDist = rh.distance;
                    best     = i;
                }
            }
        }
        return best;
    }

    // ─── Fill the tooltip with live data ──────────────────────────
    void RefreshData(int cellIndex)
    {
        BmsData d = bmsController.data;

        lblTitle.text = "CELL " + (cellIndex + 1).ToString("D2");

        float temp = d.cellTemperatures[cellIndex];
        float soc  = d.cellSoc[cellIndex];
        float soh  = d.cellSoh[cellIndex];

        lblTempValue.text = temp.ToString("F1") + " \u00B0C";
        lblSocValue .text = (soc * 100f).ToString("F1") + " %";
        lblSohValue .text = (soh * 100f).ToString("F1") + " %";

        // Temperature dot colour
        Color cTemp = temp >= tempCrit ? ColCrit : temp >= tempWarn ? ColWarn : ColOk;
        dotTemp.color           = cTemp;
        lblTempValue.color      = cTemp;

        // SOC dot colour
        Color cSoc = soc <= socCrit ? ColCrit : soc <= socWarn ? ColWarn : ColOk;
        dotSoc.color           = cSoc;
        lblSocValue.color      = cSoc;

        // SOH dot colour
        Color cSoh = soh <= sohCrit ? ColCrit : soh <= sohWarn ? ColWarn : ColOk;
        dotSoh.color           = cSoh;
        lblSohValue.color      = cSoh;
    }

    // ─── Follow the cursor, clamped inside screen bounds ──────────
    void PositionTooltip()
    {
        Vector2 mousePos = Input.mousePosition;

        // Convert screen pos → canvas local pos
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            mousePos,
            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : sceneCamera,
            out Vector2 local);

        // Desired pivot: tooltip appears to the right/below cursor
        Vector2 desired = local + cursorOffset;

        // Canvas half-extents (accounting for scaler)
        float halfW = _canvasRect.rect.width  * 0.5f;
        float halfH = _canvasRect.rect.height * 0.5f;
        float tw    = tooltipRoot.rect.width;
        float th    = tooltipRoot.rect.height;

        // Clamp so it never bleeds off screen
        desired.x = Mathf.Clamp(desired.x, -halfW + 4,           halfW - tw - 4);
        desired.y = Mathf.Clamp(desired.y, -halfH + th + 4,      halfH - 4);

        tooltipRoot.anchoredPosition = desired;
    }
}