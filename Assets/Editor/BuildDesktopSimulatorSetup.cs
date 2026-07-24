using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

public static class BuildDesktopSimulatorSetup
{
    private const string TargetScenePath = "Assets/MedTriage/Scenes/Hospital_DesktopSimulator_Test.unity";
    private const string SimPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/XR Device Simulator/XR Device Simulator.prefab";

    [MenuItem("Tools/MedTriage/Repair & Setup Desktop XR Simulator")]
    public static void SetupDesktopSimulator()
    {
        Debug.Log("=== SetupDesktopSimulator START ===");

        Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);

        // 1. Create root container XR_DESKTOP_TEST_ONLY
        GameObject desktopRoot = GameObject.Find("XR_DESKTOP_TEST_ONLY");
        if (desktopRoot != null)
        {
            Object.DestroyImmediate(desktopRoot);
        }
        desktopRoot = new GameObject("XR_DESKTOP_TEST_ONLY");

        // 2. Instantiate Official Full XR Device Simulator (Phase 2)
        GameObject simPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SimPrefabPath);
        if (simPrefab == null)
        {
            Debug.LogError($"XR Device Simulator prefab not found at {SimPrefabPath}");
            return;
        }

        GameObject simInstance = (GameObject)PrefabUtility.InstantiatePrefab(simPrefab);
        simInstance.name = "XR Device Simulator";
        simInstance.transform.SetParent(desktopRoot.transform, false);

        XRDeviceSimulator simComponent = simInstance.GetComponentInChildren<XRDeviceSimulator>(true);
        if (simComponent == null)
        {
            Debug.LogError("XRDeviceSimulator component missing from instantiated prefab!");
            return;
        }
        Debug.Log("[SIMULATOR] Official XRDeviceSimulator instantiated successfully.");

        // 3. Configure CharacterController on XR Origin for Body Collision (Phase 4)
        GameObject xrOriginGo = GameObject.Find("XR Origin (VR)");
        if (xrOriginGo != null)
        {
            CharacterController cc = xrOriginGo.GetComponent<CharacterController>();
            if (cc == null) cc = xrOriginGo.AddComponent<CharacterController>();

            cc.enabled = true;
            cc.radius = 0.30f;       // Human body radius
            cc.height = 1.80f;       // Standing height
            cc.center = new Vector3(0f, 0.90f, 0f);
            cc.stepOffset = 0.30f;
            cc.slopeLimit = 45f;
            Debug.Log("[PHYSICS] Configured CharacterController radius=0.30m, height=1.80m on XR Origin.");
        }

        // 4. Clean Up Colored Box Fallbacks & Configure Controller Visuals (Phase 2 & 4)
        CleanUpControllerVisuals();

        // 5. Create Compact Screen Space - Overlay Control Panel UI (Phase 3 & 6)
        CreateSimulatorControlPanel(desktopRoot, simComponent);

        // Save scene
        EditorSceneManager.SaveScene(scene, TargetScenePath);
        Debug.Log("=== SetupDesktopSimulator COMPLETE ===");
    }

    private static void CleanUpControllerVisuals()
    {
        GameObject xrOrigin = GameObject.Find("XR Origin (VR)");
        if (xrOrigin == null) return;

        // Remove any colored fallback boxes (Phase 2)
        Transform leftFb = xrOrigin.transform.Find("Camera Offset/Left Controller/Desktop_LeftController_Fallback");
        if (leftFb != null) Object.DestroyImmediate(leftFb.gameObject);

        Transform rightFb = xrOrigin.transform.Find("Camera Offset/Right Controller/Desktop_RightController_Fallback");
        if (rightFb != null) Object.DestroyImmediate(rightFb.gameObject);

        // Ensure normal white controller model renderers remain ENABLED
        Transform leftCtrlVis = xrOrigin.transform.Find("Camera Offset/Left Controller/Left Controller Visual");
        if (leftCtrlVis != null)
        {
            leftCtrlVis.gameObject.SetActive(true);
            foreach (var r in leftCtrlVis.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
        }

        Transform rightCtrlVis = xrOrigin.transform.Find("Camera Offset/Right Controller/Right Controller Visual");
        if (rightCtrlVis != null)
        {
            rightCtrlVis.gameObject.SetActive(true);
            foreach (var r in rightCtrlVis.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
        }

        // Keep unposed hand SkinnedMeshRenderers DISABLED (they cause white/jagged shapes near camera clip plane)
        Transform leftHandVis = xrOrigin.transform.Find("Camera Offset/Left Controller/Left Hand Visual");
        if (leftHandVis != null)
        {
            foreach (var smr in leftHandVis.GetComponentsInChildren<SkinnedMeshRenderer>(true)) smr.enabled = false;
        }

        Transform rightHandVis = xrOrigin.transform.Find("Camera Offset/Right Controller/Right Hand Visual");
        if (rightHandVis != null)
        {
            foreach (var smr in rightHandVis.GetComponentsInChildren<SkinnedMeshRenderer>(true)) smr.enabled = false;
        }

        Debug.Log("[VISUALS] Cleaned up fallback boxes. White controller models enabled. Unposed hand mesh renderers disabled.");
    }

    private static void CreateSimulatorControlPanel(GameObject desktopRoot, XRDeviceSimulator sim)
    {
        GameObject panelRoot = new GameObject("Simulator_Instructions");
        panelRoot.transform.SetParent(desktopRoot.transform, false);

        // Canvas Setup (Screen Space - Overlay)
        Canvas canvas = panelRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = panelRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GraphicRaycaster raycaster = panelRoot.AddComponent<GraphicRaycaster>();

        // Background Panel (Top-Right Anchored, Compact)
        GameObject bgGo = new GameObject("BackgroundPanel");
        bgGo.transform.SetParent(panelRoot.transform, false);
        Image bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.10f, 0.16f, 0.88f); // Dark semi-transparent
        bgImg.raycastTarget = false;                          // Non-blocking for raycasts

        RectTransform bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(1f, 1f);
        bgRect.anchorMax = new Vector2(1f, 1f);
        bgRect.pivot = new Vector2(1f, 1f);
        bgRect.anchoredPosition = new Vector2(-15f, -15f);
        bgRect.sizeDelta = new Vector2(460f, 620f);

        // Title Text
        GameObject titleGo = new GameObject("TitleText");
        titleGo.transform.SetParent(bgGo.transform, false);
        Text titleText = titleGo.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.text = "HOSPITAL DESKTOP XR SIMULATOR CONTROLS";
        titleText.fontSize = 15;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0.25f, 0.85f, 1.0f);
        titleText.raycastTarget = false;

        RectTransform titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -10f);
        titleRect.sizeDelta = new Vector2(440f, 28f);

        // Bindings Body Text
        GameObject bodyGo = new GameObject("BindingsText");
        bodyGo.transform.SetParent(bgGo.transform, false);
        Text bodyText = bodyGo.AddComponent<Text>();
        bodyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bodyText.fontSize = 12;
        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.color = Color.white;
        bodyText.raycastTarget = false;

        RectTransform bodyRect = bodyGo.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyRect.offsetMin = new Vector2(12f, 12f);
        bodyRect.offsetMax = new Vector2(-12f, -42f);

        // Dynamically read effective bindings from active XRDeviceSimulator
        string hmdManipKey = GetActionBinding(sim, "m_ManipulateHeadAction", "RMB");
        string leftManipKey = GetActionBinding(sim, "m_ManipulateLeftAction", "Left Shift");
        string leftToggleKey = GetActionBinding(sim, "m_ToggleManipulateLeftAction", "T");
        string rightManipKey = GetActionBinding(sim, "m_ManipulateRightAction", "Space");
        string rightToggleKey = GetActionBinding(sim, "m_ToggleManipulateRightAction", "Y");
        string axis2DKey = GetActionBinding(sim, "m_Axis2DAction", "W/A/S/D");
        string axisTargetKey = GetActionBinding(sim, "m_TogglePrimary2DAxisTargetAction", "1");
        string triggerKey = GetActionBinding(sim, "m_TriggerAction", "LMB");
        string gripKey = GetActionBinding(sim, "m_GripAction", "G");
        string primaryKey = GetActionBinding(sim, "m_PrimaryButtonAction", "B");
        string secondaryKey = GetActionBinding(sim, "m_SecondaryButtonAction", "N");
        string cycleKey = GetActionBinding(sim, "m_CycleDevicesAction", "Tab");
        string lockKey = GetActionBinding(sim, "m_ToggleCursorLockAction", "\\");
        string stopKey = GetActionBinding(sim, "m_StopManipulationAction", "Esc");

        StringBuilder sb = new StringBuilder();

        sb.AppendLine("=== A. HMD POSE MANIPULATION ===");
        sb.AppendLine($"• Select/Manipulate HMD: Hold [{hmdManipKey}] + Move Mouse");
        sb.AppendLine($"• Translate HMD: Hold [{hmdManipKey}] + [W/A/S/D] (XZ), [Q/E] (Y)");
        sb.AppendLine("⚠️ WARNING: HMD pose movement bypasses wall collision!");

        sb.AppendLine("\n=== B. COLLISION-AWARE PLAYER LOCOMOTION ===");
        sb.AppendLine($"• Step 1: Select Left Controller: Hold [{leftManipKey}] or Press [{leftToggleKey}]");
        sb.AppendLine($"• Step 2: Target 2D Axis: Press [{axisTargetKey}] (or hold [{leftManipKey}])");
        sb.AppendLine($"• Step 3: Walk: Press [{axis2DKey}] (Forward/Left/Back/Right)");
        sb.AppendLine("  -> Drives CharacterController (Stops at walls & stays on floor)");
        sb.AppendLine($"• Snap Turn: Select Right Controller ([{rightManipKey}]/[{rightToggleKey}]), Press [{axis2DKey}] (A/D)");

        sb.AppendLine("\n=== C. STEP-BY-STEP TELEPORTATION ===");
        sb.AppendLine($"1. Select Left/Right Controller: Press [{leftToggleKey}] or [{rightToggleKey}]");
        sb.AppendLine($"2. Aim Teleport Ray: Hold [{leftManipKey}] / [{rightManipKey}] & Move Mouse");
        sb.AppendLine($"3. Activate Teleport Target: Press [{axisTargetKey}] then Press/Hold [W]");
        sb.AppendLine("4. Confirm Indicator: Green ring appears on Teleportation Area");
        sb.AppendLine("5. Execute Teleport: Release [W] to teleport to target floor");

        sb.AppendLine("\n=== D. UTILITY & CONTROLS ===");
        sb.AppendLine($"• Trigger/Select: [{triggerKey}] | Grip: [{gripKey}] | Buttons: [{primaryKey}]/[{secondaryKey}]");
        sb.AppendLine($"• Cycle Devices: [{cycleKey}] | Cursor Lock: [{lockKey}] | Deselect: [{stopKey}]");
        sb.AppendLine("• Toggle Panel: Press [F1]");

        bodyText.text = sb.ToString();

        // Add Input System compatible Instruction Toggle Component (Phase 1 fix)
        var toggleComp = panelRoot.AddComponent<SimulatorInstructionToggle>();
        toggleComp.targetPanel = bgGo;
    }

    private static string GetActionBinding(XRDeviceSimulator sim, string fieldName, string defaultFallback)
    {
        if (sim == null) return defaultFallback;
        FieldInfo f = sim.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null)
        {
            InputActionReference iar = f.GetValue(sim) as InputActionReference;
            if (iar != null && iar.action != null)
            {
                string display = iar.action.GetBindingDisplayString();
                if (!string.IsNullOrEmpty(display)) return display;
            }
        }
        return defaultFallback;
    }
}

// Runtime Helper script to toggle instruction overlay panel using New Input System API (Phase 1 Fix)
public class SimulatorInstructionToggle : MonoBehaviour
{
    public GameObject targetPanel;

    private void Update()
    {
        // Safe check using New Input System API (prevents InvalidOperationException)
        if (targetPanel != null && Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
        {
            targetPanel.SetActive(!targetPanel.activeSelf);
        }
    }
}
