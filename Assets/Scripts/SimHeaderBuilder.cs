using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static SimUIHelpers;

/// <summary>
/// Builds the top header bar of Panel_Simulation:
/// Title block (left) | 3D/SCHEMATIC toggle | Time container (right)
/// </summary>
public static class SimHeaderBuilder
{
    public static void Build(Transform parent, out TMP_Text timeLabel)
    {
        timeLabel = null;
        var h = parent.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(16, 16, 8, 8);
        h.spacing = 12;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;
        h.childControlWidth = true;
        h.childControlHeight = true;

        // ── Title block (left) ─────────────────────────────────────────
        var titleBlock = NewUI("TitleBlock", parent, new Color(0, 0, 0, 0));
        var tbl = titleBlock.AddComponent<VerticalLayoutGroup>();
        tbl.spacing = 2;
        tbl.childForceExpandHeight = false;
        tbl.childControlHeight = true;
        var tblLE = titleBlock.AddComponent<LayoutElement>();
        tblLE.flexibleWidth = 1;
        tblLE.minWidth = 200;
        NewText(titleBlock.transform, "Title", "SIMULATION ENGINE", 22, Accent,
            TextAlignmentOptions.MidlineLeft, true);
        NewText(titleBlock.transform, "Subtitle",
            "Configure parameters and run simulation to predict battery behavior.",
            13, TextDim, TextAlignmentOptions.MidlineLeft);

        // ── Spacer (pushes toggle + time to the right) ─────────────────
        var spacer = NewUI("Spacer", parent, new Color(0, 0, 0, 0));
        var spacerLE = spacer.AddComponent<LayoutElement>();
        spacerLE.flexibleWidth = 1;

        // ── 3D / Schematic toggle ──────────────────────────────────────
        var toggle = NewUI("ViewToggle", parent, new Color(0, 0, 0, 0));
        var tg = toggle.AddComponent<HorizontalLayoutGroup>();
        tg.spacing = 0;
        tg.childForceExpandWidth = true;
        tg.childForceExpandHeight = true;
        tg.childControlWidth = true;
        tg.childControlHeight = true;
        var tgLE = toggle.AddComponent<LayoutElement>();
        tgLE.preferredWidth = 240;
        tgLE.minWidth = 200;
        tgLE.preferredHeight = 36;

        var btn3D = NewButton(toggle.transform, "Btn_3DView", "3D VIEW", 13,
            new Color(0.02f, 0.04f, 0.08f, 1f), Accent);
        var btnSch = NewButton(toggle.transform, "Btn_SchematicView", "SCHEMATIC VIEW", 13,
            TextDim, new Color(0.04f, 0.10f, 0.18f, 1f));
        StyleToggleButton(btn3D.gameObject, true);
        StyleToggleButton(btnSch.gameObject, false);

        // ── Time container (far right) ─────────────────────────────────
        var time = NewUI("TimeContainer", parent, new Color(0, 0, 0, 0));
        var timeLE = time.AddComponent<LayoutElement>();
        timeLE.preferredWidth = 320;
        timeLE.minWidth = 260;
        timeLE.preferredHeight = 36;
        timeLabel = NewText(time.transform, "TimeLabel",
            "Time Elapsed: 00:00:00 / 02:00:00", 13, TextPri,
            TextAlignmentOptions.MidlineRight);
        Stretch(timeLabel.GetComponent<RectTransform>(), 8, 4, 8, 4);
    }
}