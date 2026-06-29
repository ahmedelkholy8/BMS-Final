using System.Collections.Generic;
using UnityEngine;
namespace BMS {
    public enum FaultType    { None, IncreasedInternalResistance, CapacityFade, ThermalRunaway }
    public enum SimSeverity  { Low, Medium, High }
    public enum SimState     { Idle, Running, Paused, Finished }

    [System.Serializable]
    public class SimulationData {
        public float loadCurrent=20f, ambientTemp=25f, chargeCurrent=0f, initialSoc=.78f, durationHours=2f;
        public bool faultEnabled=false; public int faultCellIndex=3;
        public FaultType faultType=FaultType.IncreasedInternalResistance;
        public SimSeverity faultSeverity=SimSeverity.High;
        public SimState state=SimState.Idle; public float playheadTime=0f; public int playheadIndex=0;
        public float[] timeSamples,socCurve,tempCurve,voltageCurve;
        public float[][] cellVoltCurves,cellTempCurves;
        public float predictedRuntimeH,predictedEndSoc,predictedMaxTemp,predictedEndVoltage,healthImpact;
        public int cellAtRisk=-1; public bool isValid=false;
        public List<SimAlert> alerts=new List<SimAlert>();
        public string scenarioLabel="Scenario";
    }
    [System.Serializable]
    public class SimAlert {
        public string message,recommendation; public float atSimTimeH; public bool isCritical;
        public SimAlert(string m,string r,float t,bool c=true){message=m;recommendation=r;atSimTimeH=t;isCritical=c;}
    }
    [System.Serializable]
    public class SimScenario {
        public string label; public float loadCurrent,ambientTemp,runtimeH,maxTemp,endSoc;
    }
}