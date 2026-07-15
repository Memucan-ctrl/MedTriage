using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Hands;

public class ControllerHandAnimator : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionProperty gripAction;
    public InputActionProperty triggerAction;

    [Header("Hand Tracking")]
    [SerializeField]
    private XRHandTrackingEvents m_HandTrackingEvents;

    [Header("Finger Joint Chains")]
    // Index
    public Transform[] indexJoints; // Metacarpal, Proximal, Intermediate, Distal
    // Middle
    public Transform[] middleJoints;
    // Ring
    public Transform[] ringJoints;
    // Little
    public Transform[] littleJoints;
    // Thumb
    public Transform[] thumbJoints;

    [Header("Flexion Settings")]
    public Vector3 indexFlexAxis = new Vector3(1, 0, 0);
    public float indexMaxFlexAngle = 80f;

    public Vector3 middleFlexAxis = new Vector3(1, 0, 0);
    public float middleMaxFlexAngle = 80f;

    public Vector3 ringFlexAxis = new Vector3(1, 0, 0);
    public float ringMaxFlexAngle = 80f;

    public Vector3 littleFlexAxis = new Vector3(1, 0, 0);
    public float littleMaxFlexAngle = 80f;

    public Vector3 thumbFlexAxis = new Vector3(1, 0, 0);
    public float thumbMaxFlexAngle = 45f;

    private Quaternion[] m_IndexInitialRotations;
    private Quaternion[] m_MiddleInitialRotations;
    private Quaternion[] m_RingInitialRotations;
    private Quaternion[] m_LittleInitialRotations;
    private Quaternion[] m_ThumbInitialRotations;

    void Awake()
    {
        if (m_HandTrackingEvents == null)
            m_HandTrackingEvents = GetComponent<XRHandTrackingEvents>();

        // Cache initial local rotations
        m_IndexInitialRotations = CacheInitialRotations(indexJoints);
        m_MiddleInitialRotations = CacheInitialRotations(middleJoints);
        m_RingInitialRotations = CacheInitialRotations(ringJoints);
        m_LittleInitialRotations = CacheInitialRotations(littleJoints);
        m_ThumbInitialRotations = CacheInitialRotations(thumbJoints);
    }

    Quaternion[] CacheInitialRotations(Transform[] joints)
    {
        if (joints == null) return null;
        var rots = new Quaternion[joints.Length];
        for (int i = 0; i < joints.Length; i++)
        {
            if (joints[i] != null)
                rots[i] = joints[i].localRotation;
        }
        return rots;
    }

    void LateUpdate()
    {
        // If true hand tracking is active, let XRHandSkeletonDriver drive the hand.
        if (m_HandTrackingEvents != null && m_HandTrackingEvents.handIsTracked)
        {
            return;
        }

        // Otherwise, drive joints with controller inputs
        float gripValue = gripAction.action != null ? gripAction.action.ReadValue<float>() : 0f;
        float triggerValue = triggerAction.action != null ? triggerAction.action.ReadValue<float>() : 0f;

        // Apply flexion to fingers
        ApplyFlexion(indexJoints, m_IndexInitialRotations, indexFlexAxis, triggerValue * indexMaxFlexAngle);
        ApplyFlexion(middleJoints, m_MiddleInitialRotations, middleFlexAxis, gripValue * middleMaxFlexAngle);
        ApplyFlexion(ringJoints, m_RingInitialRotations, ringFlexAxis, gripValue * ringMaxFlexAngle);
        ApplyFlexion(littleJoints, m_LittleInitialRotations, littleFlexAxis, gripValue * littleMaxFlexAngle);
        
        // Thumb responds to both grip and trigger slightly
        float thumbValue = Mathf.Max(gripValue, triggerValue);
        ApplyFlexion(thumbJoints, m_ThumbInitialRotations, thumbFlexAxis, thumbValue * thumbMaxFlexAngle);
    }

    void ApplyFlexion(Transform[] joints, Quaternion[] initialRots, Vector3 axis, float angle)
    {
        if (joints == null || initialRots == null) return;
        // Skip Metacarpal (index 0) and rotate Proximal, Intermediate, and Distal joints (indices 1, 2, 3)
        for (int i = 1; i < joints.Length; i++)
        {
            if (joints[i] != null && i < initialRots.Length)
            {
                joints[i].localRotation = initialRots[i] * Quaternion.AngleAxis(angle, axis);
            }
        }
    }
}
