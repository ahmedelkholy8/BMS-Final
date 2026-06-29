using UnityEngine;
using TMPro;
[RequireComponent(typeof(RectTransform))]
public class SimulationGraph : MonoBehaviour {
    public LineRenderer lineRenderer;
    public Color lineColor=new Color(0f,.85f,1f);
    public float lineWidth=2f;
    public TMP_Text labelTitle,labelYMax,labelYMin,labelXMax;
    public string titleText="GRAPH",yUnit="%",xUnit="h";
    public float yMin=0f,yMax=0f;

    RectTransform _rect;
    float[] _values,_times;
    int _totalSamples,_drawnCount;
    float _durationH;
    bool _layoutReady;

    void Awake(){
        _rect=GetComponent<RectTransform>();
        if(lineRenderer!=null){
            lineRenderer.useWorldSpace=false;
            lineRenderer.positionCount=0;
            lineRenderer.startWidth=lineWidth; lineRenderer.endWidth=lineWidth;
            var mat=new Material(Shader.Find("Sprites/Default")); mat.renderQueue=3000;
            lineRenderer.material=mat;
            lineRenderer.startColor=lineColor; lineRenderer.endColor=lineColor;
            lineRenderer.numCapVertices=4; lineRenderer.numCornerVertices=4;
        }
        if(labelTitle!=null)labelTitle.text=titleText;
    }

    public void Load(float[] values,float[] timeSamplesSeconds,float durationHours){
        _values=values; _times=timeSamplesSeconds;
        _totalSamples=values!=null?values.Length:0;
        _durationH=durationHours; _drawnCount=0;
        _layoutReady=false;  // force layout rebuild on next RevealUpTo
        yMin=0f; yMax=0f;   // reset so auto-scale always runs fresh
        if(lineRenderer!=null)lineRenderer.positionCount=0;
        if(yMax<=yMin&&values!=null&&values.Length>0){
            float lo=float.MaxValue,hi=float.MinValue;
            foreach(float v in values){if(v<lo)lo=v;if(v>hi)hi=v;}
            float pad=Mathf.Max((hi-lo)*.1f,.5f);
            yMin=lo-pad; yMax=hi+pad;
        }
        if(labelYMax!=null)labelYMax.text=$"{yMax:F0}{yUnit}";
        if(labelYMin!=null)labelYMin.text=$"{yMin:F0}{yUnit}";
        if(labelXMax!=null)labelXMax.text=$"{_durationH:F1}{xUnit}";
    }

    public void RevealUpTo(int sampleIndex){
        if(_values==null||lineRenderer==null)return;
        int target=Mathf.Clamp(sampleIndex+1,0,_totalSamples);
        if(target==_drawnCount)return;

        // Make sure the RectTransform has been laid out before reading its rect.
        // Layout is delayed when a panel is first shown, so we force a rebuild once.
        if(!_layoutReady){
            Canvas.ForceUpdateCanvases();
            Rect lr=_rect.rect;
            if(lr.width<=0f||lr.height<=0f)return;
            _layoutReady=true;
        }

        _drawnCount=target; lineRenderer.positionCount=_drawnCount;
        Rect r=_rect.rect;
        float w=r.width-12f,h=r.height-24f,ox=r.x+6f,oy=r.y+12f;
        for(int i=0;i<_drawnCount;i++){
            float tN=_durationH>0?(_times[i]/3600f)/_durationH:(float)i/Mathf.Max(1,_totalSamples-1);
            float vN=yMax>yMin?(_values[i]-yMin)/(yMax-yMin):.5f;
            lineRenderer.SetPosition(i,new Vector3(ox+tN*w,oy+Mathf.Clamp01(vN)*h,0f));
        }
    }
    public void ShowFull(){if(_totalSamples>0)RevealUpTo(_totalSamples-1);}
    public void Clear(){_values=null;_drawnCount=0;if(lineRenderer!=null)lineRenderer.positionCount=0;}
}