using UnityEngine;
using BMS;
public class SimulationController : MonoBehaviour {
    [Header("References")]
    public BmsController   bmsController;
    public ViewModeManager viewModeManager;
    public SimulationUI    simulationUI;
    public Renderer[]      cellRenderers=new Renderer[8];
    public int             cellBodyMaterialIndex=1;
    public float           playbackSpeed=60f;

    public SimulationData Data  {get;private set;}=new SimulationData();
    public SimState       State =>Data.state;
    private float _playheadSec=0f;

    static readonly int PropBase=Shader.PropertyToID("_BaseColor"),
                        PropOvr =Shader.PropertyToID("_ColorOverride"),
                        PropFill=Shader.PropertyToID("_FillLevel"),
                        PropRim =Shader.PropertyToID("_RimIntensity");

    public void RunSimulation(SimulationData inputs){
        Data=inputs; Data.state=SimState.Idle;
        SimulationEngine.Run(Data);
        if(!Data.isValid){Debug.LogError("[SimCtrl] Engine failed");return;}
        _playheadSec=0f; Data.playheadIndex=0; Data.state=SimState.Running;
        if(viewModeManager!=null)viewModeManager.enabled=false;
        simulationUI?.OnSimulationStarted(Data);
    }
    public void Pause(){
        if(Data.state==SimState.Running)Data.state=SimState.Paused;
        else if(Data.state==SimState.Paused)Data.state=SimState.Running;
    }
    public void Stop(){Data.state=SimState.Idle;RestoreVisuals();simulationUI?.OnSimulationStopped();}
    public void SetSimPageOpen(bool open){if(!open&&Data.state==SimState.Running)Data.state=SimState.Paused;}

    void Update(){
        if(Data.state!=SimState.Running)return;
        if(Data.timeSamples==null||Data.timeSamples.Length==0)return;
        _playheadSec+=Time.deltaTime*playbackSpeed;
        float total=Data.durationHours*3600f;
        if(_playheadSec>=total){_playheadSec=total;Data.state=SimState.Finished;}
        Data.playheadTime=_playheadSec;
        int idx=Mathf.Min(Mathf.FloorToInt(_playheadSec/SimulationEngine.SAMPLE_INTERVAL_S),Data.timeSamples.Length-1);
        Data.playheadIndex=idx;
        UpdateCellVisuals(idx);
        simulationUI?.OnPlayheadUpdated(Data,idx);
        if(Data.state==SimState.Finished)simulationUI?.OnSimulationFinished(Data);
    }

    void UpdateCellVisuals(int idx){
        for(int c=0;c<cellRenderers.Length;c++){
            if(cellRenderers[c]==null)continue;
            Material[] mats=cellRenderers[c].materials;
            if(cellBodyMaterialIndex>=mats.Length)continue;
            Material mat=mats[cellBodyMaterialIndex];
            float ct=Data.cellTempCurves!=null&&c<Data.cellTempCurves.Length?Data.cellTempCurves[c][idx]:Data.ambientTemp;
            float cv=Data.cellVoltCurves!=null&&c<Data.cellVoltCurves.Length?Data.cellVoltCurves[c][idx]:3.7f;
            mat.SetColor(PropBase,GetSimColor(c,ct,cv));
            mat.SetFloat(PropOvr,1f); mat.SetFloat(PropFill,1f); mat.SetFloat(PropRim,1.3f);
        }
    }
    Color GetSimColor(int c,float temp,float volt){
        if(Data.faultEnabled&&c==Data.faultCellIndex){
            float p=.5f+.5f*Mathf.Sin(Time.time*4f);
            if(temp>50f||volt<3f)return Color.Lerp(new Color(.9f,.1f,0f),new Color(1f,.4f,0f),p);
            return Color.Lerp(new Color(1f,.5f,0f),new Color(1f,.8f,0f),p);
        }
        if(volt<3.0f)return new Color(.9f,.1f,.1f);
        if(volt<3.3f)return new Color(1f,.6f,0f);
        if(temp>50f) return new Color(1f,.4f,0f);
        if(temp>40f) return new Color(1f,.75f,0f);
        return new Color(.05f,.78f,.25f);
    }
    void RestoreVisuals(){
        for(int c=0;c<cellRenderers.Length;c++){
            if(cellRenderers[c]==null)continue;
            Material[] mats=cellRenderers[c].materials;
            if(cellBodyMaterialIndex>=mats.Length)continue;
            mats[cellBodyMaterialIndex].SetFloat(PropOvr,0f);
            mats[cellBodyMaterialIndex].SetFloat(PropRim,0.6f);
        }
        if(viewModeManager!=null)viewModeManager.enabled=true;
    }
}