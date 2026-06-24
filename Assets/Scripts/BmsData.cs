using System;

namespace BMS
{
    [Serializable]
    public class BmsData
    {
        // Pack-level
        public float packVoltage    = 29.64f;   // V
        public float current        = -3.21f;   // A  (negative = discharging)
        public float temperature    = 31.4f;    // °C
        public float soc            = 0.78f;    // 0.0 – 1.0
        public float soh            = 0.92f;    // 0.0 – 1.0
        public int cycleCount = 0;
        public float[] cellTemperatures = new float[8]
        {
            31.4f, 31.4f, 31.4f, 31.4f,
            31.4f, 31.4f, 31.4f, 31.4f
        };
        
        // Per-cell voltages (8 cells)
        public float[] cellVoltages = new float[8]
        {
            3.71f, 3.70f, 3.72f, 3.69f,
            3.71f, 3.70f, 3.73f, 3.71f
        };
        
        public float[] cellSoc = new float[8]
        {
            0.78f, 0.78f, 0.78f, 0.78f,
            0.78f, 0.78f, 0.78f, 0.78f
        };

        // Per-cell State of Health — not part of live telemetry; derived from pack SOH
        public float[] cellSoh = new float[8]
        {
            0.92f, 0.92f, 0.92f, 0.92f,
            0.92f, 0.92f, 0.92f, 0.92f
        };

        // Protection flags
        public bool overVoltage     = false;
        public bool underVoltage    = false;
        public bool overCurrent     = false;
        public bool shortCircuit    = false;
        public bool overTemperature = false;
        public bool cellBalancing   = true;

        // Derived helpers
        public bool IsDischarging => current < 0f;
        public float MinCellVoltage => Array.IndexOf(cellVoltages,
            System.Linq.Enumerable.Min(cellVoltages)) >= 0
            ? System.Linq.Enumerable.Min(cellVoltages) : 0f;
    }
}