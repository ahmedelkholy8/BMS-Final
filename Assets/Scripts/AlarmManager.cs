using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AlarmManager : MonoBehaviour
{
    [Header("BMS Controller")]
    public BmsController bmsController;

    [Header("Alarm Rows (UI) — assign AlarmRow_01, AlarmRow_02")]
    public GameObject[] alarmRows   = new GameObject[2];
    public TMP_Text[]   alarmMsgs   = new TMP_Text[2];
    public TMP_Text[]   alarmTimes  = new TMP_Text[2];
    public Image[]      alarmIcons  = new Image[2];

    [Header("Colors")]
    public Color colorWarning = new Color(1.00f, 0.53f, 0.00f);
    public Color colorFault   = new Color(0.90f, 0.10f, 0.10f);

    // Internal alarm queue
    private Queue<AlarmEntry> _alarmQueue = new Queue<AlarmEntry>();
    private float _checkInterval = 2f;
    private float _checkTimer    = 0f;

    // Track previous states to detect new faults
    private bool _prevOverVoltage;
    private bool _prevUnderVoltage;
    private bool _prevOverCurrent;
    private bool _prevOverTemp;

    struct AlarmEntry
    {
        public string message;
        public string time;
        public Color  color;
    }

    void Start()
    {
        // Seed with the two alarms from the reference image
        PushAlarm("Cell 4 Voltage Low",       colorWarning);
        PushAlarm("High Temperature MOSFET",  colorWarning);
        RefreshUI();
    }

    void Update()
    {
        if (bmsController == null) return;

        _checkTimer += Time.deltaTime;
        if (_checkTimer >= _checkInterval)
        {
            _checkTimer = 0f;
            CheckForNewAlarms();
        }
    }

    void CheckForNewAlarms()
    {
        var d = bmsController.data;

        if (d.overVoltage  && !_prevOverVoltage)
            PushAlarm("Over Voltage Fault",    colorFault);
        if (d.underVoltage && !_prevUnderVoltage)
            PushAlarm("Under Voltage Fault",   colorFault);
        if (d.overCurrent  && !_prevOverCurrent)
            PushAlarm("Over Current Fault",    colorFault);
        if (d.overTemperature && !_prevOverTemp)
            PushAlarm("Over Temperature Fault", colorFault);

        // Check cell 4 voltage specifically
        float cell4 = bmsController.GetCellVoltages()[3];
        if (cell4 < 3.66f)
            PushAlarm("Cell 4 Voltage Low",    colorWarning);

        _prevOverVoltage  = d.overVoltage;
        _prevUnderVoltage = d.underVoltage;
        _prevOverCurrent  = d.overCurrent;
        _prevOverTemp     = d.overTemperature;

        RefreshUI();
    }

    void PushAlarm(string message, Color color)
    {
        // Avoid duplicate consecutive alarms
        if (_alarmQueue.Count > 0)
        {
            var last = ((AlarmEntry[])_alarmQueue.ToArray())[_alarmQueue.Count - 1];
            if (last.message == message) return;
        }

        _alarmQueue.Enqueue(new AlarmEntry
        {
            message = message,
            time    = DateTime.Now.ToString("HH:mm:ss"),
            color   = color
        });

        // Keep only last 2 visible
        while (_alarmQueue.Count > 2)
            _alarmQueue.Dequeue();
    }

    void RefreshUI()
    {
        AlarmEntry[] entries = _alarmQueue.ToArray();

        for (int i = 0; i < alarmRows.Length; i++)
        {
            if (i < entries.Length)
            {
                alarmRows[i].SetActive(true);
                alarmMsgs[i].text   = entries[i].message;
                alarmMsgs[i].color  = entries[i].color;
                alarmTimes[i].text  = entries[i].time;
                alarmIcons[i].color = entries[i].color;
            }
            else
            {
                alarmRows[i].SetActive(false);
            }
        }
    }
}