using UnityEngine;
using UnityEngine.UI;
public class ThermalThumbnail : MonoBehaviour {
    public Camera        thumbCamera;
    public RenderTexture renderTexture;
    public RawImage      targetRawImage;
    public Transform     cellGroupTransform;
    public Vector3       cameraOffset=new Vector3(3.5f,2.5f,4.0f);
    public float         fieldOfView=35f;

    void Start(){
        if(renderTexture==null){
            renderTexture=new RenderTexture(512,512,16,RenderTextureFormat.ARGB32);
            renderTexture.name="ThermalThumbRT"; renderTexture.Create();
        }
        if(thumbCamera!=null){
            thumbCamera.targetTexture=renderTexture; thumbCamera.fieldOfView=fieldOfView;
            thumbCamera.clearFlags=CameraClearFlags.SolidColor;
            thumbCamera.backgroundColor=new Color(.02f,.04f,.08f);
            thumbCamera.cullingMask=Camera.main.cullingMask;
            thumbCamera.enabled=false;
        }
        if(targetRawImage!=null)targetRawImage.texture=renderTexture;
    }
    public void CaptureSnapshot(){
        if(thumbCamera==null||cellGroupTransform==null)return;
        Vector3 target=cellGroupTransform.position;
        thumbCamera.transform.position=target+cameraOffset;
        thumbCamera.transform.LookAt(target);
        thumbCamera.enabled=true; thumbCamera.Render(); thumbCamera.enabled=false;
    }
    public void RenderLive(){
        if(thumbCamera==null||cellGroupTransform==null)return;
        Vector3 target=cellGroupTransform.position;
        thumbCamera.transform.position=target+cameraOffset;
        thumbCamera.transform.LookAt(target);
        thumbCamera.enabled=true; thumbCamera.Render(); thumbCamera.enabled=false;
    }
}