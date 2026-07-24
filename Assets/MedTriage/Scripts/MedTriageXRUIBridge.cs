using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class MedTriageXRUIBridge : MonoBehaviour
{
    IEnumerator Start(){for(int i=0;i<45;i++){UpgradeWorldSpaceCanvases();yield return null;}}
    void UpgradeWorldSpaceCanvases(){foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None)){if(canvas.renderMode!=RenderMode.WorldSpace)continue;if(!canvas.GetComponent<TrackedDeviceGraphicRaycaster>())canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();var graphic=canvas.GetComponent<GraphicRaycaster>();if(graphic)graphic.enabled=false;}}
}