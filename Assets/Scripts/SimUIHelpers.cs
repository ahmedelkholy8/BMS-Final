using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shared UI helpers + color palette for the Simulation page.
/// All members are static — no instance state.
/// </summary>
public static class SimUIHelpers
{
    // ── Palette ─────────────────────────────────────────────────────────
    public static readonly Color PanelBg     = new Color(0.051f, 0.086f, 0.122f, 0.95f);
    public static readonly Color CardBg      = new Color(0.086f, 0.129f, 0.192f, 0.92f);
    public static readonly Color CardBgDim   = new Color(0.065f, 0.095f, 0.140f, 0.92f);
    public static readonly Color Border      = new Color(0.149f, 0.220f, 0.314f, 1.0f);
    public static readonly Color Accent      = new Color(0.000f, 0.667f, 1.000f, 1.0f);
    public static readonly Color Green       = new Color(0.000f, 0.800f, 0.400f, 1.0f);
    public static readonly Color GreenBtn    = new Color(0.000f, 0.502f, 0.165f, 1.0f);
    public static readonly Color Orange      = new Color(1.000f, 0.600f, 0.000f, 1.0f);
    public static readonly Color Red         = new Color(0.900f, 0.118f, 0.118f, 1.0f);
    public static readonly Color RedBtn      = new Color(0.580f, 0.060f, 0.060f, 1.0f);
    public static readonly Color BlueBtn     = new Color(0.000f, 0.300f, 0.520f, 1.0f);
    public static readonly Color TextPri     = new Color(0.86f, 0.91f, 0.96f, 1.0f);
    public static readonly Color TextDim     = new Color(0.55f, 0.62f, 0.70f, 1.0f);
    public static readonly Color TextLabel   = new Color(0.65f, 0.72f, 0.80f, 1.0f);

    // ── Element factories ───────────────────────────────────────────────

    /// <summary>Create a UI GameObject with RectTransform + CanvasRenderer.
    /// If bg has visible alpha, an Image is added as background.</summary>
    public static GameObject NewUI(string name, Transform parent, Color bg)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        if (parent != null) go.transform.SetParent(parent, false);
        if (bg.a > 0.001f)
        {
            var img = go.AddComponent<Image>();
            img.color = bg;
            img.raycastTarget = true;
        }
        return go;
    }

    /// <summary>Create a TMP_Text on a new GameObject.</summary>
    public static TMP_Text NewText(Transform parent, string name, string text, int size, Color color,
        TextAlignmentOptions align, bool bold = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        if (parent != null) go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        if (bold) t.fontStyle = FontStyles.Bold;
        t.textWrappingMode = TMPro.TextWrappingModes.Normal;
        t.raycastTarget = false;
        return t;
    }

    /// <summary>Create a Button with an Image background and a centered TMP_Text child.</summary>
    public static Button NewButton(Transform parent, string name, string label, int size, Color textColor, Color bg)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        if (parent != null) go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = bg;
        img.raycastTarget = true;
        var txt = NewText(go.transform, "Text", label, size, textColor, TextAlignmentOptions.Center, true);
        Stretch(txt.GetComponent<RectTransform>(), 4, 4, 4, 4);
        return go.GetComponent<Button>();
    }

    /// <summary>Add a thin Outline effect to a UI element.</summary>
    public static void AddOutline(GameObject go, Color color, float thickness)
    {
        var o = go.AddComponent<Outline>();
        o.effectColor = color;
        o.effectDistance = new Vector2(thickness, -thickness);
    }

    /// <summary>Style a button as part of an active/inactive toggle pair.</summary>
    public static void StyleToggleButton(GameObject btn, bool active)
    {
        var img = btn.GetComponent<Image>();
        var txt = btn.GetComponentInChildren<TMP_Text>();
        if (active)
        {
            img.color = Accent;
            if (txt != null) txt.color = new Color(0.02f, 0.04f, 0.08f, 1f);
        }
        else
        {
            img.color = new Color(0.04f, 0.10f, 0.18f, 1f);
            if (txt != null) txt.color = TextDim;
        }
    }

    /// <summary>Add a color-dot legend row (dot + label + sub-label).</summary>
    public static void AddLegendDot(Transform parent, string label, Color color, string sub)
    {
        var row = NewUI("Dot_" + label, parent, new Color(0, 0, 0, 0));
        var h = row.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 6;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childForceExpandWidth = false;
        h.childControlWidth = true;
        var dot = NewUI("Dot", row.transform, color);
        var dotLE = dot.AddComponent<LayoutElement>();
        dotLE.preferredWidth = 10;
        dotLE.minWidth = 10;
        dotLE.preferredHeight = 10;
        dotLE.minHeight = 10;
        dot.GetComponent<Image>().raycastTarget = false;
        NewText(row.transform, "Lbl", label, 11, color, TextAlignmentOptions.MidlineLeft, true);
        NewText(row.transform, "Sub", sub, 11, TextDim, TextAlignmentOptions.MidlineLeft);
    }

    // ── Layout helpers ──────────────────────────────────────────────────

    /// <summary>Stretch a RectTransform to fill its parent with margins.</summary>
    public static void Stretch(RectTransform rt, float l, float b, float r, float t)
    {
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
    }

    /// <summary>Anchor a RectTransform with explicit min/max anchors and offsets.</summary>
    public static void Anchor(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = oMin;
        rt.offsetMax = oMax;
    }

    /// <summary>Find a child TMP_Text by hierarchical path (e.g. "KPI_Runtime/Value").</summary>
    public static TMP_Text FindTMP(Transform parent, string path)
    {
        var t = parent.Find(path);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }
}