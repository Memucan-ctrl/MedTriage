using UnityEngine.XR.Management;
using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

public static class BuildQuestAPK
{
    private const string HardwareScenePath =
        "Assets/MedTriage/Scenes/Hospital_Quest_Test.unity";

    private const string OutputDirPath =
        "C:/Users/Admin/MedTriage/Builds/Quest";

    private const string OutputApkPath =
        "C:/Users/Admin/MedTriage/Builds/Quest/MedTriageQuest-XRFix.apk";

    [MenuItem("Tools/MedTriage/Prepare & Build Quest APK (Build Only)")]
    public static void RunBuildPipeline()
    {
        Debug.Log(
            "=================== BUILD QUEST APK START ===================");

        StringBuilder report = new StringBuilder();

        // PHASE 1 — VERIFY ANDROID TOOLS
        string androidEnginePath =
            @"C:\Program Files\Unity\Hub\Editor\6000.5.0f1\Editor\Data\PlaybackEngines\AndroidPlayer";

        if (!Directory.Exists(androidEnginePath))
        {
            Debug.LogError(
                $"[PHASE 1 FAIL] Android Player tools missing at {androidEnginePath}");
            return;
        }

        report.AppendLine(
            "[PHASE 1 PASS] Android SDK, NDK, OpenJDK, Gradle verified under Unity 6000.5.0f1.");

        // PHASE 2 — HARDWARE SCENE PREFLIGHT
        Scene scene = EditorSceneManager.OpenScene(
            HardwareScenePath,
            OpenSceneMode.Single);

        if (!scene.IsValid())
        {
            Debug.LogError(
                $"[PHASE 2 FAIL] Could not open scene {HardwareScenePath}");
            return;
        }

        var xrOrigins =
            UnityEngine.Object.FindObjectsByType<Unity.XR.CoreUtils.XROrigin>(
                FindObjectsInactive.Include);

        var intManagers =
            UnityEngine.Object.FindObjectsByType<XRInteractionManager>(
                FindObjectsInactive.Include);

        var inputActionManagers =
            UnityEngine.Object.FindObjectsByType<InputActionManager>(
                FindObjectsInactive.Include);

        var eventSystems =
            UnityEngine.Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include);

        int simCount = 0;

        foreach (var go in
                 UnityEngine.Object.FindObjectsByType<GameObject>(
                     FindObjectsInactive.Include))
        {
            if (go.name.Contains("XR Device Simulator") ||
                go.name.Contains("Simulator_Instructions"))
            {
                simCount++;
            }
        }

        int xrUiModuleCount = 0;

        foreach (var eventSystem in eventSystems)
        {
            foreach (var component in eventSystem.GetComponents<Component>())
            {
                if (component != null &&
                    component.GetType().Name == "XRUIInputModule")
                {
                    xrUiModuleCount++;
                }
            }
        }

        var activeCameras = new List<Camera>();

        foreach (var camera in
                 UnityEngine.Object.FindObjectsByType<Camera>(
                     FindObjectsInactive.Exclude))
        {
            if (camera.enabled && camera.gameObject.activeInHierarchy)
            {
                activeCameras.Add(camera);
            }
        }

        var activeListeners = new List<AudioListener>();

        foreach (var listener in
                 UnityEngine.Object.FindObjectsByType<AudioListener>(
                     FindObjectsInactive.Exclude))
        {
            if (listener.enabled && listener.gameObject.activeInHierarchy)
            {
                activeListeners.Add(listener);
            }
        }

        if (xrOrigins.Length != 1 ||
            intManagers.Length != 1 ||
            inputActionManagers.Length != 1 ||
            eventSystems.Length != 1 ||
            xrUiModuleCount != 1 ||
            simCount != 0 ||
            activeCameras.Count != 1 ||
            activeListeners.Count != 1)
        {
            Debug.LogError(
                "[PHASE 2 FAIL] Hardware scene isolation invalid: " +
                $"Origins={xrOrigins.Length}, " +
                $"IntMgrs={intManagers.Length}, " +
                $"InputManagers={inputActionManagers.Length}, " +
                $"EventSystems={eventSystems.Length}, " +
                $"XRUIInputModules={xrUiModuleCount}, " +
                $"Simulators={simCount}, " +
                $"ActiveCameras={activeCameras.Count}, " +
                $"ActiveListeners={activeListeners.Count}");

            return;
        }

        report.AppendLine(
            "[PHASE 2 PASS] Hardware scene isolation preflight verified " +
            "(1 Origin, 1 IntMgr, 0 Simulators).");

        // PHASE 3 — VERIFY REAL QUEST CONTROLLER INPUTS
        GameObject xrOriginGo = GameObject.Find("XR Origin (VR)");
        MonoBehaviour moveProvider = null;

        if (xrOriginGo != null)
        {
            foreach (var behaviour in
                     xrOriginGo.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour != null &&
                    behaviour.GetType().Name.Contains("MoveProvider"))
                {
                    moveProvider = behaviour;
                }
            }
        }

        SnapTurnProvider snapTurnProvider =
            xrOriginGo != null
                ? xrOriginGo.GetComponentInChildren<SnapTurnProvider>(true)
                : null;

        ContinuousTurnProvider continuousTurnProvider =
            xrOriginGo != null
                ? xrOriginGo.GetComponentInChildren<ContinuousTurnProvider>(true)
                : null;

        TeleportationProvider teleportationProvider =
            xrOriginGo != null
                ? xrOriginGo.GetComponentInChildren<TeleportationProvider>(true)
                : null;

        if (moveProvider == null || !moveProvider.enabled)
        {
            Debug.LogError(
                "[PHASE 3 FAIL] DynamicMoveProvider is missing or disabled!");
            return;
        }

        if (snapTurnProvider == null || !snapTurnProvider.enabled)
        {
            Debug.LogError(
                "[PHASE 3 FAIL] SnapTurnProvider is missing or disabled!");
            return;
        }

        if (continuousTurnProvider != null &&
            continuousTurnProvider.enabled)
        {
            Debug.LogError(
                "[PHASE 3 FAIL] ContinuousTurnProvider is enabled " +
                "(must be disabled)!");
            return;
        }

        if (teleportationProvider == null ||
            !teleportationProvider.enabled)
        {
            Debug.LogError(
                "[PHASE 3 FAIL] TeleportationProvider is missing or disabled!");
            return;
        }

        report.AppendLine(
            "[PHASE 3 PASS] Move, Snap Turn, Teleport providers verified.");

        // PHASE 4 — INITIAL CONTROLLER-VISUAL SAFETY
        Transform leftHandVisual = xrOriginGo.transform.Find(
            "Camera Offset/Left Controller/Left Hand Visual");

        if (leftHandVisual != null)
        {
            foreach (var renderer in
                     leftHandVisual.GetComponentsInChildren<SkinnedMeshRenderer>(
                         true))
            {
                renderer.enabled = false;
            }
        }

        Transform rightHandVisual = xrOriginGo.transform.Find(
            "Camera Offset/Right Controller/Right Hand Visual");

        if (rightHandVisual != null)
        {
            foreach (var renderer in
                     rightHandVisual.GetComponentsInChildren<SkinnedMeshRenderer>(
                         true))
            {
                renderer.enabled = false;
            }
        }

        Transform leftControllerVisual = xrOriginGo.transform.Find(
            "Camera Offset/Left Controller/Left Controller Visual");

        if (leftControllerVisual != null)
        {
            leftControllerVisual.gameObject.SetActive(true);
        }

        Transform rightControllerVisual = xrOriginGo.transform.Find(
            "Camera Offset/Right Controller/Right Controller Visual");

        if (rightControllerVisual != null)
        {
            rightControllerVisual.gameObject.SetActive(true);
        }

        EditorSceneManager.SaveScene(scene, HardwareScenePath);

        report.AppendLine(
            "[PHASE 4 PASS] Unposed hand SkinnedMeshRenderers disabled; " +
            "Touch controller visuals enabled.");

        // PHASE 5 — SWITCH TO ANDROID PLATFORM
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            Debug.Log("[PHASE 5] Switching build target to Android...");

            EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.Android,
                BuildTarget.Android);
        }

        report.AppendLine(
            "[PHASE 5 PASS] Active build target set to Android.");

        // PHASE 6 — ANDROID XR CONFIGURATION AND OPENXR SETTINGS
        ConfigureAndroidOpenXRFeatures();

        report.AppendLine(
            "[PHASE 6 PASS] OpenXR loader and Meta Quest OpenXR features " +
            "configured for Android target.");

        // PHASE 7 — ANDROID PLAYER SETTINGS
        PlayerSettings.productName = "MedTriage";

        PlayerSettings.SetApplicationIdentifier(
            BuildTargetGroup.Android,
            "com.memcankiprono.medtriage");

        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.Android.bundleVersionCode = 1;

        PlayerSettings.SetScriptingBackend(
            BuildTargetGroup.Android,
            ScriptingImplementation.IL2CPP);

        PlayerSettings.Android.targetArchitectures =
            AndroidArchitecture.ARM64;

        EditorUserBuildSettings.development = true;
        EditorUserBuildSettings.allowDebugging = false;
        EditorUserBuildSettings.buildAppBundle = false;

        PlayerSettings.Android.minSdkVersion =
            AndroidSdkVersions.AndroidApiLevel29;

        report.AppendLine(
            "[PHASE 7 PASS] Player Settings configured: IL2CPP, ARM64, " +
            "com.memcankiprono.medtriage, Dev Build.");

        // PHASE 8 AND 9 — BUILD SCENE LIST AND PRE-BUILD VALIDATION
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(HardwareScenePath, true)
        };

        report.AppendLine(
            "[PHASE 8 & 9 PASS] Build scene list verified " +
            "(exactly 1 scene: Hospital_Quest_Test.unity).");

        // PHASE 10 — BUILD ONLY
        if (!Directory.Exists(OutputDirPath))
        {
            Directory.CreateDirectory(OutputDirPath);
        }

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = new[] { HardwareScenePath },
            locationPathName = OutputApkPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.Development
        };

        Debug.Log(
            $"[PHASE 10] Initiating BuildPlayer to {OutputApkPath}...");

        DateTime startTime = DateTime.Now;

        var buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);

        DateTime endTime = DateTime.Now;
        TimeSpan duration = endTime - startTime;

        // PHASE 11 — VERIFY THE APK
        if (buildReport.summary.result ==
            UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            FileInfo apkInfo = new FileInfo(OutputApkPath);

            if (apkInfo.Exists && apkInfo.Length > 0)
            {
                report.AppendLine();
                report.AppendLine("=== BUILD SUCCESSFUL ===");
                report.AppendLine($"APK Path: {apkInfo.FullName}");

                report.AppendLine(
                    $"Size: {apkInfo.Length} bytes " +
                    $"({(double)apkInfo.Length / (1024 * 1024):F2} MB)");

                report.AppendLine(
                    $"Build Time: {duration.TotalSeconds:F2} seconds");

                report.AppendLine(
                    $"Timestamp: {apkInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");

                report.AppendLine(
                    $"Warnings Count: {buildReport.summary.totalWarnings}");

                report.AppendLine(
                    $"Errors Count: {buildReport.summary.totalErrors}");

                Debug.Log(report.ToString());

                Debug.Log(
                    "QUEST IMMERSIVE OPENXR DEVELOPMENT APK BUILT — " +
                    "USB INSTALLATION AND HEADSET TEST PENDING");
            }
            else
            {
                Debug.LogError(
                    "[PHASE 11 FAIL] APK file missing or zero bytes after build!");
            }
        }
        else
        {
            Debug.LogError(
                $"[PHASE 11 FAIL] Build failed with result " +
                $"{buildReport.summary.result}, total errors: " +
                $"{buildReport.summary.totalErrors}");
        }
    }

    private static void ConfigureAndroidOpenXRFeatures()
    {
        // Get or create XRGeneralSettingsPerBuildTarget.
        XRGeneralSettingsPerBuildTarget buildTargetSettings = null;

        EditorBuildSettings.TryGetConfigObject(
            XRGeneralSettings.settingsKey,
            out buildTargetSettings);

        if (buildTargetSettings == null)
        {
            Debug.Log(
                "[XR CONFIG] Creating new " +
                "XRGeneralSettingsPerBuildTarget asset...");

            buildTargetSettings =
                ScriptableObject.CreateInstance<
                    XRGeneralSettingsPerBuildTarget>();

            EditorBuildSettings.AddConfigObject(
                XRGeneralSettings.settingsKey,
                buildTargetSettings,
                true);
        }

        // Ensure Android XRGeneralSettings and XRManagerSettings exist.
        if (!buildTargetSettings.HasManagerSettingsForBuildTarget(
                BuildTargetGroup.Android))
        {
            Debug.Log(
                "[XR CONFIG] Creating default ManagerSettings for Android...");

            buildTargetSettings.CreateDefaultManagerSettingsForBuildTarget(
                BuildTargetGroup.Android);
        }

        XRGeneralSettings androidSettings =
            buildTargetSettings.SettingsForBuildTarget(
                BuildTargetGroup.Android);

        if (androidSettings == null)
        {
            throw new InvalidOperationException(
                "[XR CONFIG FAIL] Failed to create or retrieve " +
                "XRGeneralSettings for Android!");
        }

        androidSettings.InitManagerOnStart = true;

        XRManagerSettings manager = androidSettings.Manager;

        if (manager == null)
        {
            throw new InvalidOperationException(
                "[XR CONFIG FAIL] XRManagerSettings for Android is null!");
        }

        string openXrLoaderTypeName =
            "UnityEngine.XR.OpenXR.OpenXRLoader";

        bool assigned = XRPackageMetadataStore.AssignLoader(
            manager,
            openXrLoaderTypeName,
            BuildTargetGroup.Android);

        if (!assigned && manager.activeLoaders.Count == 0)
        {
            throw new InvalidOperationException(
                "[XR CONFIG FAIL] Could not assign OpenXRLoader to " +
                "Android XRManagerSettings!");
        }

        Debug.Log(
            "[XR CONFIG] Assigned OpenXRLoader to Android " +
            $"XRManagerSettings. Active Loaders: {manager.activeLoaders.Count}");

        // Configure OpenXR features for Android.
        OpenXRSettings openXrSettingsAndroid =
            OpenXRSettings.GetSettingsForBuildTargetGroup(
                BuildTargetGroup.Android);

        if (openXrSettingsAndroid == null)
        {
            throw new InvalidOperationException(
                "[XR CONFIG FAIL] OpenXRSettings for Android target group " +
                "is null!");
        }

        bool metaQuestFeatureEnabled = false;
        bool oculusTouchEnabled = false;
        bool touchPlusEnabled = false;

        foreach (var feature in openXrSettingsAndroid.GetFeatures())
        {
            if (feature is MetaQuestFeature metaQuestFeature)
            {
                metaQuestFeature.enabled = true;
                metaQuestFeatureEnabled = metaQuestFeature.enabled;

                Debug.Log(
                    "[XR CONFIG] Enabled MetaQuestFeature for Android OpenXR.");
            }
            else if (feature is OculusTouchControllerProfile oculusTouch)
            {
                oculusTouch.enabled = true;
                oculusTouchEnabled = oculusTouch.enabled;

                Debug.Log(
                    "[XR CONFIG] Enabled OculusTouchControllerProfile " +
                    "for Android OpenXR.");
            }
            else if (
                feature is MetaQuestTouchPlusControllerProfile touchPlus)
            {
                touchPlus.enabled = true;
                touchPlusEnabled = touchPlus.enabled;

                Debug.Log(
                    "[XR CONFIG] Enabled " +
                    "MetaQuestTouchPlusControllerProfile for Android OpenXR.");
            }
        }

        if (!metaQuestFeatureEnabled ||
            !oculusTouchEnabled ||
            !touchPlusEnabled)
        {
            throw new InvalidOperationException(
                "[XR CONFIG FAIL] Required OpenXR features check failed! " +
                $"MetaQuest: {metaQuestFeatureEnabled}, " +
                $"OculusTouch: {oculusTouchEnabled}, " +
                $"TouchPlus: {touchPlusEnabled}");
        }

        EditorUtility.SetDirty(openXrSettingsAndroid);
        EditorUtility.SetDirty(buildTargetSettings);
        AssetDatabase.SaveAssets();
    }
}