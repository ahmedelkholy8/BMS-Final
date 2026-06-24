using UnityEngine;
using TMPro;
using BMS;

public class WorldSpaceLabels : MonoBehaviour
{
    public BmsController bmsController;
    public Camera        sceneCamera;

    // Fixed world-space positions confirmed visible in the default camera view
    // Adjust these in the Inspector if you change the scene layout
    public Vector3 cellLabelWorld  = new Vector3(-3.5f, 4.2f, 1.5f);
    public Vector3 bmsLabelWorld   = new Vector3( 3.8f, 3.2f, 1.5f);

    struct LabelEntry
    {
        public Vector3      worldPos;
        public Vector3      valueOffset;   // offset below title
        public TextMeshPro  titleTMP;
        public TextMeshPro  valueTMP;
        public int          dataType;   // 0=cells 1=bms 2=esp
    }

    LabelEntry[] _labels = new LabelEntry[2];

    void Start()
    {
        if (sceneCamera == null) sceneCamera = Camera.main;
        _labels[0] = Build("CELL PACK", cellLabelWorld, 0);
        _labels[1] = Build("BMS BOARD", bmsLabelWorld,  1);
    }

    void Update()
    {
        if (sceneCamera == null) return;
        Quaternion rot = Quaternion.LookRotation(sceneCamera.transform.forward, Vector3.up);
        for (int i = 0; i < _labels.Length; i++)
        {
            Vector3 titlePos = _labels[i].worldPos;
            Vector3 valuePos = titlePos + _labels[i].valueOffset;

            if (_labels[i].titleTMP != null)
            {
                _labels[i].titleTMP.transform.position = titlePos;
                _labels[i].titleTMP.transform.rotation = rot;
            }
            if (_labels[i].valueTMP != null)
            {
                _labels[i].valueTMP.transform.position = valuePos;
                _labels[i].valueTMP.transform.rotation = rot;
                _labels[i].valueTMP.text = GetValue(_labels[i].dataType);
            }
        }
    }

    string GetValue(int type)
    {
        if (bmsController == null) return "";
        BmsData d = bmsController.data;
        if (type == 0) return (d.soc*100f).ToString("F0") + "% SOC   " + d.packVoltage.ToString("F2") + " V";
        if (type == 1) return d.temperature.ToString("F1") + " C   " + Mathf.Abs(d.current).ToString("F1") + " A";
        return bmsController.liveDataActive ? "MQTT  LIVE" : "SIMULATED";
    }

    LabelEntry Build(string title, Vector3 worldPos, int type)
    {
        var e = new LabelEntry();
        e.worldPos    = worldPos;
        e.valueOffset = Vector3.down * 0.6f;
        e.dataType    = type;

        // Title
        var tgo = new GameObject("WLabel_" + title + "_T");
        var t = tgo.AddComponent<TextMeshPro>();
        t.text  = title;
        t.fontSize = 9f;
        t.fontStyle = FontStyles.Bold;
        t.color = new Color(0f, 0.88f, 1f, 1f);
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = false;
        t.GetComponent<MeshRenderer>().sortingOrder = 10;
        e.titleTMP = t;

        // Value
        var vgo = new GameObject("WLabel_" + title + "_V");
        var v = vgo.AddComponent<TextMeshPro>();
        v.text = "";
        v.fontSize = 7f;
        v.color = new Color(0.75f, 0.95f, 1f, 1f);
        v.alignment = TextAlignmentOptions.Center;
        v.enableWordWrapping = false;
        v.GetComponent<MeshRenderer>().sortingOrder = 10;
        e.valueTMP = v;

        return e;
    }
}
