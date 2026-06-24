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
    public string brokerHost    = "mqtt-dashboard.com";
    public int    brokerPort    = 1883;          // WebSocket SSL port
    public string topic         = "Pack";
    public float  reconnectDelay = 5f;

    [Header("BMS Controller")]
    public BmsController bmsController;

    [Header("Debug")]
    public bool logRawJson = false;

    // Internal
    private MqttClient  _client;
    private bool        _running = true;
    private string      _lastJson = "";
    private bool        _hasNewData = false;

    void Start()
    {
        StartCoroutine(ConnectLoop());
    }

    void OnDestroy()
    {
        _running = false;
        Disconnect();
    }

    // ── Connection loop with auto-reconnect ───────────────────────
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
            // WebSocket connection
            _client = new MqttClient(
                brokerHost,
                1883,
                false,
                MqttSslProtocols.None,
                null, null);

            _client.MqttMsgPublishReceived += OnMessageReceived;

            string clientId = "UnityBMSTwin_" + 
                              UnityEngine.Random.Range(1000, 9999);

            _client.Connect(clientId, "", "", true, 60);

            if (_client.IsConnected)
            {
                Debug.Log("[MQTT] Connected. Subscribing to: " + topic);
                _client.Subscribe(
                    new string[] { topic },
                    new byte[]   { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
            }
            else
            {
                Debug.LogWarning("[MQTT] Connection failed.");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[MQTT] Exception: " + e.Message);
        }
    }

    // ── Runs on MQTT thread — just store the JSON ─────────────────
    void OnMessageReceived(object sender, MqttMsgPublishEventArgs e)
    {
        _lastJson   = Encoding.UTF8.GetString(e.Message);
        _hasNewData = true;

        if (logRawJson)
            Debug.Log("[MQTT] Raw: " + _lastJson);
    }

    // ── Parse on main thread to avoid Unity API threading issues ──
    void Update()
    {
        if (!_hasNewData) return;
        _hasNewData = false;
        ParseAndApply(_lastJson);
    }

    void ParseAndApply(string json)
    {
         // Strip all whitespace so parser works on both compact and formatted JSON
        json = System.Text.RegularExpressions.Regex.Replace(json, @"\s+", "");
        try
        {
            if (bmsController == null) return;
            BmsData d = bmsController.data;

            // ── Cell voltages ────────────────────────────────────────
            float[] voltages = SimpleJsonParser.ExtractFloatArray(json, "voltage");
            Debug.Log($"[PARSE] voltages null? {voltages == null}, count: {voltages?.Length}");

            if (voltages != null)
            {
                float sum = 0;
                for (int i = 0; i < Mathf.Min(8, voltages.Length); i++)
                {
                    d.cellVoltages[i] = voltages[i];
                    sum += voltages[i];
                }
                d.packVoltage = sum;
            }

            // ── SOC ──────────────────────────────────────────────────
            float[] socArr = SimpleJsonParser.ExtractFloatArray(json, "soc");
            if (socArr != null)
            {
                float sum = 0;
                for (int i = 0; i < Mathf.Min(8, socArr.Length); i++)
                {
                    d.cellSoc[i] = Mathf.Clamp01(socArr[i]);
                    sum += socArr[i];
                }
                d.soc = sum / socArr.Length;
            }
            float[] totalSocArr = SimpleJsonParser.ExtractFloatArray(json, "total_soc");
            if (totalSocArr != null && totalSocArr.Length > 0)
                d.soc = Mathf.Clamp01(totalSocArr[0]);

            // ── Current ──────────────────────────────────────────────
            float[] currArr = SimpleJsonParser.ExtractFloatArray(json, "current");
            if (currArr != null)
            {
                float sum = 0;
                foreach (float v in currArr) sum += v;
                d.current = sum / currArr.Length;
            }

            // ── Temperature — Kelvin to Celsius ──────────────────────
            float[] tempArr = SimpleJsonParser.ExtractFloatArray(json, "temp");
            if (tempArr != null)
            {
                float sum = 0;
                for (int i = 0; i < Mathf.Min(8, tempArr.Length); i++)
                {
                    float celsius = tempArr[i] - 273.15f;
                    d.cellTemperatures[i] = celsius;
                    sum += celsius;
                }
                d.temperature = sum / tempArr.Length;
            }

            // ── Cycles ───────────────────────────────────────────────
            float[] cyclesFloat = SimpleJsonParser.ExtractFloatArray(json, "cycles");
            if (cyclesFloat != null && cyclesFloat.Length > 0)
                d.cycleCount = (int)Math.Round(cyclesFloat[0]);

            // ── Lock out simulation permanently once live data arrives ──
            bmsController.simulateData  = false;
            bmsController.liveDataActive = true;
            // Force DashboardUI to refresh immediately
            DashboardUI ui = FindObjectOfType<DashboardUI>();
            if (ui != null) ui.ForceRefresh();
            Debug.Log($"[MQTT] Live data — " +
                    $"SOC:{d.soc:P0} " +
                    $"V:{d.packVoltage:F2}V " +
                    $"I:{d.current:F2}A " +
                    $"T:{d.temperature:F1}°C");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[MQTT] Parse error: " + e.Message);
        }
    }

    bool IsConnected() => _client != null && _client.IsConnected;

    void Disconnect()
    {
        try { if (IsConnected()) _client.Disconnect(); }
        catch { }
    }
    
    public static class SimpleJsonParser
    {
        // Extracts all float values from a named field like "voltage":[[3.89,3.89,...]]
    public static float[] ExtractFloatArray(string json, string fieldName)
    {
        try
        {
            string search = $"\"{fieldName}\":[";
            int start = json.IndexOf(search);
            if (start < 0) return null;

            start += search.Length; // position after the '['
            int bracketCount = 1;
            int end = start;
            while (end < json.Length && bracketCount > 0)
            {
                char c = json[end];
                if (c == '[') bracketCount++;
                else if (c == ']') bracketCount--;
                end++;
            }
            if (bracketCount != 0) return null; // unbalanced

            // Extract substring between the brackets (excluding the outer brackets)
            string inner = json.Substring(start, end - start - 1);
            string[] parts = inner.Split(',');
            float[] result = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                result[i] = float.Parse(parts[i].Trim(), System.Globalization.CultureInfo.InvariantCulture);
            return result;
        }
        catch { return null; }
    }

        public static int[] ExtractIntArray(string json, string fieldName)
        {
            try
            {
                string search = $"\"{fieldName}\":[";
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