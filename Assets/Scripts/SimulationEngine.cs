using UnityEngine;
namespace BMS {
    public static class SimulationEngine {
        public const float SAMPLE_INTERVAL_S=30f;
        public const int   CELLS=8;
        public const float PACK_CAPACITY_AH=8.2f,R_INTERNAL_BASE=0.025f;
        public const float THERMAL_MASS=420f,COOLING_COEFF=4.2f,MIN_CELL_VOLTAGE=2.8f;
        static readonly float[] OCV_SOC={0f,.05f,.10f,.20f,.30f,.40f,.50f,.60f,.70f,.80f,.90f,1f};
        static readonly float[] OCV_V  ={2.80f,2.95f,3.10f,3.30f,3.45f,3.58f,3.68f,3.75f,3.83f,3.90f,4.00f,4.20f};
        static float SevMult(SimSeverity s){return s==SimSeverity.Low?1.5f:s==SimSeverity.Medium?2.5f:4f;}
        public static void Run(SimulationData d){
            d.alerts.Clear(); d.isValid=false;
            float totalSec=d.durationHours*3600f;
            int samples=Mathf.CeilToInt(totalSec/SAMPLE_INTERVAL_S)+1;
            d.timeSamples=new float[samples]; d.socCurve=new float[samples];
            d.tempCurve=new float[samples];   d.voltageCurve=new float[samples];
            d.cellVoltCurves=new float[CELLS][]; d.cellTempCurves=new float[CELLS][];
            for(int c=0;c<CELLS;c++){d.cellVoltCurves[c]=new float[samples];d.cellTempCurves[c]=new float[samples];}
            float[] rCell=new float[CELLS]; for(int c=0;c<CELLS;c++)rCell[c]=R_INTERNAL_BASE;
            if(d.faultEnabled&&d.faultType==FaultType.IncreasedInternalResistance)rCell[d.faultCellIndex]*=SevMult(d.faultSeverity);
            float[] capFactor=new float[CELLS]; for(int c=0;c<CELLS;c++)capFactor[c]=1f;
            if(d.faultEnabled&&d.faultType==FaultType.CapacityFade)capFactor[d.faultCellIndex]=1f/SevMult(d.faultSeverity);
            float soc=d.initialSoc,packTemp=d.ambientTemp;
            float[] cellSoc=new float[CELLS],cellTemp=new float[CELLS];
            for(int c=0;c<CELLS;c++){cellSoc[c]=soc;cellTemp[c]=d.ambientTemp;}
            float endTime=totalSec; int stopIdx=samples-1;
            bool aTmp=false,aVlt=false;
            for(int i=0;i<samples;i++){
                float t=i*SAMPLE_INTERVAL_S,dt=SAMPLE_INTERVAL_S;
                d.timeSamples[i]=t; d.socCurve[i]=soc; d.tempCurve[i]=packTemp;
                float packVolt=0f;
                for(int c=0;c<CELLS;c++){
                    float ocv=SampleOCV(cellSoc[c]),v=Mathf.Max(MIN_CELL_VOLTAGE,ocv-d.loadCurrent*rCell[c]);
                    d.cellVoltCurves[c][i]=v; d.cellTempCurves[c][i]=cellTemp[c]; packVolt+=v;
                    if(!aTmp&&cellTemp[c]>45f){d.alerts.Add(new SimAlert($"Cell {c+1} temperature will reach {cellTemp[c]:F1}°C at {t/3600f:F1}h","Reduce load or improve cooling",t/3600f));aTmp=true;}
                    if(!aVlt&&v<3.0f){d.alerts.Add(new SimAlert($"Cell {c+1} voltage will drop below 3.0V at {t/3600f:F1}h","Check cell condition",t/3600f));aVlt=true;}
                }
                d.voltageCurve[i]=packVolt;
                if(soc<=0.05f||packVolt<MIN_CELL_VOLTAGE*CELLS){
                    endTime=t; stopIdx=i;
                    for(int j=i+1;j<samples;j++){d.timeSamples[j]=j*SAMPLE_INTERVAL_S;d.socCurve[j]=d.socCurve[i];d.tempCurve[j]=d.tempCurve[i];d.voltageCurve[j]=d.voltageCurve[i];for(int c=0;c<CELLS;c++){d.cellVoltCurves[c][j]=d.cellVoltCurves[c][i];d.cellTempCurves[c][j]=d.cellTempCurves[c][i];}}
                    break;
                }
                if(i==samples-1)break;
                float ahPs=d.loadCurrent/3600f; soc=Mathf.Clamp01(soc-(ahPs/PACK_CAPACITY_AH)*dt);
                float pLoss=d.loadCurrent*d.loadCurrent*R_INTERNAL_BASE*CELLS;
                packTemp=Mathf.Min(packTemp+(pLoss-COOLING_COEFF*(packTemp-d.ambientTemp))/THERMAL_MASS*dt,85f);
                for(int c=0;c<CELLS;c++){
                    float cP=d.loadCurrent*d.loadCurrent*rCell[c];
                    cellTemp[c]=Mathf.Min(cellTemp[c]+(cP-COOLING_COEFF*0.125f*(cellTemp[c]-d.ambientTemp))/(THERMAL_MASS/CELLS)*dt,85f);
                    cellSoc[c]=Mathf.Clamp01(cellSoc[c]-(ahPs/(PACK_CAPACITY_AH*capFactor[c]))*dt);
                }
            }
            d.predictedRuntimeH=endTime/3600f; d.predictedEndSoc=d.socCurve[stopIdx]*100f;
            d.predictedMaxTemp=0f; for(int i=0;i<=stopIdx;i++)if(d.tempCurve[i]>d.predictedMaxTemp)d.predictedMaxTemp=d.tempCurve[i];
            d.predictedEndVoltage=d.voltageCurve[stopIdx];
            float dod=d.initialSoc-d.socCurve[stopIdx],tStress=Mathf.Max(0,d.predictedMaxTemp-35f)/25f;
            d.healthImpact=-(dod*.8f+tStress*2.4f);
            d.cellAtRisk=-1; float worst=0f;
            for(int c=0;c<CELLS;c++){float cm=0f;for(int i=0;i<=stopIdx;i++)if(d.cellTempCurves[c][i]>cm)cm=d.cellTempCurves[c][i];if(cm>worst){worst=cm;d.cellAtRisk=c;}}
            if(d.healthImpact<-2f)d.alerts.Add(new SimAlert($"High degradation expected for Cell {d.cellAtRisk+1}",$"Estimated additional SOH loss: {Mathf.Abs(d.healthImpact):F1}%",d.durationHours*.5f,false));
            d.isValid=true;
        }
        static float SampleOCV(float soc){
            soc=Mathf.Clamp01(soc);
            for(int i=1;i<OCV_SOC.Length;i++)if(soc<=OCV_SOC[i]){float t=(soc-OCV_SOC[i-1])/(OCV_SOC[i]-OCV_SOC[i-1]);return Mathf.Lerp(OCV_V[i-1],OCV_V[i],t);}
            return OCV_V[OCV_V.Length-1];
        }
    }
}