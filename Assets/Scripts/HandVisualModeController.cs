using UnityEngine;
using UnityEngine.XR.Hands;

public class HandVisualModeController : MonoBehaviour
{
    [Header("Left Side")]
    public GameObject leftControllerHandVisual;
    public GameObject leftTrackedHandRig;
    public XRHandTrackingEvents leftTrackingEvents;

    [Header("Right Side")]
    public GameObject rightControllerHandVisual;
    public GameObject rightTrackedHandRig;
    public XRHandTrackingEvents rightTrackingEvents;

    private enum HandMode
    {
        None,
        ControllerDriven,
        TrackedHands
    }

    private HandMode m_CurrentMode = HandMode.None;

    void Update()
    {
        // Determine mode based on whether either hand is tracked.
        bool leftTracked = leftTrackingEvents != null && leftTrackingEvents.handIsTracked;
        bool rightTracked = rightTrackingEvents != null && rightTrackingEvents.handIsTracked;
        
        HandMode targetMode = (leftTracked || rightTracked) ? HandMode.TrackedHands : HandMode.ControllerDriven;

        if (targetMode != m_CurrentMode)
        {
            m_CurrentMode = targetMode;
            ApplyMode(m_CurrentMode);
        }
    }

    void ApplyMode(HandMode mode)
    {
        Debug.Log($"[HandVisualModeController] Switching to {mode} mode.");

        if (mode == HandMode.TrackedHands)
        {
            // Enable tracked rigs, disable controller visuals
            if (leftTrackedHandRig != null) leftTrackedHandRig.SetActive(true);
            if (rightTrackedHandRig != null) rightTrackedHandRig.SetActive(true);
            if (leftControllerHandVisual != null) leftControllerHandVisual.SetActive(false);
            if (rightControllerHandVisual != null) rightControllerHandVisual.SetActive(false);
        }
        else
        {
            // Enable controller visuals, disable tracked rigs
            if (leftTrackedHandRig != null) leftTrackedHandRig.SetActive(false);
            if (rightTrackedHandRig != null) rightTrackedHandRig.SetActive(false);
            if (leftControllerHandVisual != null) leftControllerHandVisual.SetActive(true);
            if (rightControllerHandVisual != null) rightControllerHandVisual.SetActive(true);
            
            // Force SMR to be enabled on controller visuals
            EnableSkinnedMeshRenderers(leftControllerHandVisual);
            EnableSkinnedMeshRenderers(rightControllerHandVisual);
        }
    }

    void EnableSkinnedMeshRenderers(GameObject root)
    {
        if (root == null) return;
        var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var smr in renderers)
        {
            smr.enabled = true;
            smr.gameObject.SetActive(true);
        }
    }
}
