using System;
using System.Collections;
using System.Text;
using UnityEngine;
using BMS;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

[Serializable]
public class BmsRawData
{
    public float[][] current;
    public float[][] temp;
    public float[][] voltage;
    public float[][] soc;
    public int[][]   cycles;
}

public class BmsMqttClient : MonoBehaviour
{
    [Header("MQTT Settings")]
    public string brokerHost     = "mqtt-dashboard.com";
    public int    brokerPort     = 1883;
    public string topic          = "Pack";
    public float  reconnectDelay = 5f;

    [Header("BMS Controller")]
    public BmsController bmsController;

    [Header("Debug")]
    public bool logRawJson = false;

    private MqttClient _client;
    private bool       _running    = true;
    private string     _lastJson   = "";
    private bool       _hasNewData = false;

    void Start()    { StartCoroutine(ConnectLoop()); }
    void OnDestroy() { _running = false; Disconnect(); }

    IEnumerator ConnectLoop()
    {
        while (_running)
        {
            if (!IsConnected())
            {
                Debug.Log("[MQTT] Connecting to " + brokerHost);
                yield return StartCoroutine(TryConnect());
            }
            yield return new WaitForSeconds(reconnectDelay);
        }
    }

    IEnumerator TryConnect()
    {
        yield return null;
        try
        {
            _client = new MqttClient(brokerHost, 1883, false, MqttSslProtocols.None, null, null);
            _client.MqttMsgPublishReceived += OnMessageReceived;
            string clientId = "UnityBMSTwin_" + UnityEngine.Random.Range(1000, 9999);
            _client.Connect(clientId, "", "", true, 60);
            if (_client.IsConnected)
            {
                Debug.Log("[MQTT] Connected. Subscribing to: " + topic);
                _client.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
            }
            else { Debug.LogWarning("[MQTT] Connection failed."); }
        }
        catch (Exception e) { Debug.LogWarning("[MQTT] Exception: " + e.Message); }
    }

    void OnMessageReceived(object sender, MqttMsgPublishEventArgs e)
    {
        _lastJson   = Encoding.UTF8.GetString(e.Message);
        _hasNewData = true;
        if (logRawJson) Debug.Log("[MQTT] Raw: " + _lastJson);
    }

    void Update()
    {
        if (!_hasNewData) return;
        _hasNewData = false;
        ParseAndApply(_lastJson);
    }

    void ParseAndApply(string json)
    {
        json = System.Text.RegularExpressions.Regex.Replace(json, @"\s+", "");
        try
        {
            if (bmsController == null) return;
            BmsData d = bmsController.data;

            // Cell voltages
            float[] voltages = SimpleJsonParser.ExtractFloatArray(json, "voltage");
            if (voltages != null)
            {
                float sum = 0;
                for (int i = 0; i < Mathf.Min(8, voltages.Length); i++)
                    { d.cellVoltages[i] = voltages[i]; sum += voltages[i]; }
                d.packVoltage = sum;
            }

            // SOC per-cell array
            float[] socArr = SimpleJsonParser.ExtractFloatArray(json, "soc");
            if (socArr != null)
            {
                float sum = 0;
                for (int i = 0; i < Mathf.Min(8, socArr.Length); i++)
                    { d.cellSoc[i] = Mathf.Clamp01(socArr[i]); sum += socArr[i]; }
                d.soc = sum / socArr.Length;
            }
            // total_soc overrides pack SOC if present
            float[] totalSocArr = SimpleJsonParser.ExtractFloatArray(json, "total_soc");
            if (totalSocArr != null && totalSocArr.Length > 0)
                d.soc = Mathf.Clamp01(totalSocArr[0]);

            // Current (average across cells)
            float[] currArr = SimpleJsonParser.ExtractFloatArray(json, "current");
            if (currArr != null)
            {
                float sum = 0;
                foreach (float v in currArr) sum += v;
                d.current = sum / currArr.Length;
            }

            // Temperature — Kelvin to Celsius
            float[] tempArr = SimpleJsonParser.ExtractFloatArray(json, "temp");
            if (tempArr != null)
            {
                float sum = 0;
                for (int i = 0; i < Mathf.Min(8, tempArr.Length); i++)
                {
                    float c = tempArr[i] - 273.15f;
                    d.cellTemperatures[i] = c;
                    sum += c;
                }
                d.temperature = sum / tempArr.Length;
            }

            // Cycles
            float[] cyclesFloat = SimpleJsonParser.ExtractFloatArray(json, "cycles");
            if (cyclesFloat != null && cyclesFloat.Length > 0)
                d.cycleCount = (int)Math.Round(cyclesFloat[0]);

            // SOH — scalar float field: "SOH":1.00
            float sohVal;
            if (SimpleJsonParser.ExtractFloat(json, "SOH", out sohVal))
                d.soh = Mathf.Clamp01(sohVal);

            // Lock simulation, refresh UI
            bmsController.simulateData   = false;
            bmsController.liveDataActive = true;
            DashboardUI ui = FindFirstObjectByType<DashboardUI>();
            if (ui != null) ui.ForceRefresh();

            Debug.Log(string.Format("[MQTT] SOC:{0:P0} SOH:{1:P0} V:{2:F2}V I:{3:F2}A T:{4:F1}C",
                d.soc, d.soh, d.packVoltage, d.current, d.temperature));
        }
        catch (Exception e) { Debug.LogWarning("[MQTT] Parse error: " + e.Message); }
    }

    bool IsConnected() => _client != null && _client.IsConnected;
    void Disconnect() { try { if (IsConnected()) _client.Disconnect(); } catch { } }

    // ── JSON Parser ───────────────────────────────────────────────
    public static class SimpleJsonParser
    {
        // Extracts all floats from: "key":[1.0, 2.0, ...]
        public static float[] ExtractFloatArray(string json, string fieldName)
        {
            try
            {
                string q     = "\"";
                string search = q + fieldName + q + ":[";
                int start = json.IndexOf(search);
                if (start < 0) return null;
                start += search.Length;
                int depth = 1, end = start;
                while (end < json.Length && depth > 0)
                {
                    if (json[end] == '[') depth++;
                    else if (json[end] == ']') depth--;
                    end++;
                }
                if (depth != 0) return null;
                string inner = json.Substring(start, end - start - 1)
                                   .Replace("[", "").Replace("]", "");
                string[] parts = inner.Split(',');
                float[] result = new float[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                    result[i] = float.Parse(parts[i].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                return result;
            }
            catch { return null; }
        }

        // Extracts a single scalar float: "KEY":1.23
        public static bool ExtractFloat(string json, string fieldName, out float value)
        {
            value = 0f;
            try
            {
                string q      = "\"";
                string search = q + fieldName + q + ":";
                int idx = json.IndexOf(search);
                if (idx < 0) return false;
                int vs = idx + search.Length;
                int ve = vs;
                while (ve < json.Length && (char.IsDigit(json[ve]) || json[ve] == '.' || json[ve] == '-'))
                    ve++;
                if (ve == vs) return false;
                value = float.Parse(json.Substring(vs, ve - vs), System.Globalization.CultureInfo.InvariantCulture);
                return true;
            }
            catch { return false; }
        }

        // Extracts an int array: "key":[1,2,3]
        public static int[] ExtractIntArray(string json, string fieldName)
        {
            try
            {
                string q      = "\"";
                string search = q + fieldName + q + ":[";
                int start = json.IndexOf(search);
                if (start < 0) return null;
                start += search.Length;
                int end = json.IndexOf("]", start);
                if (end < 0) return null;
                string inner = json.Substring(start, end - start);
                string[] parts = inner.Split(',');
                int[] result = new int[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                    result[i] = int.Parse(parts[i].Trim());
                return result;
            }
            catch { return null; }
        }
    }
}
