using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class MedTriageDesktopFlyCamera : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float sprintSpeed = 6f;
    public float verticalSpeed = 2.5f;
    public float lookSensitivity = 0.12f;
    public bool requireRightMouseToLook = true;
    float yaw;
    float pitch;
    void Awake(){var e=transform.eulerAngles;yaw=e.y;pitch=e.x>180f?e.x-360f:e.x;}
    void Update(){
        if(XRSettings.isDeviceActive) return;
        var keyboard=Keyboard.current;if(keyboard==null)return;
        var mouse=Mouse.current;bool looking=!requireRightMouseToLook||(mouse!=null&&mouse.rightButton.isPressed);
        if(looking&&mouse!=null){Vector2 d=mouse.delta.ReadValue();yaw+=d.x*lookSensitivity;pitch=Mathf.Clamp(pitch-d.y*lookSensitivity,-85f,85f);transform.rotation=Quaternion.Euler(pitch,yaw,0);}
        Vector3 input=Vector3.zero;if(keyboard.wKey.isPressed)input+=Vector3.forward;if(keyboard.sKey.isPressed)input+=Vector3.back;if(keyboard.aKey.isPressed)input+=Vector3.left;if(keyboard.dKey.isPressed)input+=Vector3.right;if(input.sqrMagnitude>1)input.Normalize();
        float speed=keyboard.leftShiftKey.isPressed||keyboard.rightShiftKey.isPressed?sprintSpeed:moveSpeed;Vector3 f=Vector3.ProjectOnPlane(transform.forward,Vector3.up).normalized;Vector3 r=Vector3.ProjectOnPlane(transform.right,Vector3.up).normalized;Vector3 motion=f*input.z+r*input.x;if(keyboard.eKey.isPressed||keyboard.spaceKey.isPressed)motion+=Vector3.up*(verticalSpeed/Mathf.Max(speed,.01f));if(keyboard.qKey.isPressed||keyboard.leftCtrlKey.isPressed)motion+=Vector3.down*(verticalSpeed/Mathf.Max(speed,.01f));transform.position+=motion*speed*Time.unscaledDeltaTime;
    }
}