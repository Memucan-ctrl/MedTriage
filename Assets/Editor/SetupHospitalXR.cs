using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class SetupHospitalXR
{
    private const string TargetScenePath = "Assets/MedTriage/Scenes/Hospital_Quest_Test.unity";
    private const string RefScenePath = "Assets/Scenes/XR_Setup_Test.unity";

    [MenuItem("Tools/MedTriage/Setup Hospital XR")]
    public static void SetupXR()
    {
        Debug.Log("=== SetupHospitalXR START ===");

        // 1. Open target scene
        Scene targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);

        // 2. Open reference scene additively to copy validated XR Origin & Interaction Manager
        Scene refScene = EditorSceneManager.OpenScene(RefScenePath, OpenSceneMode.Additive);

        GameObject refXROrigin = null;
        GameObject refIntManager = null;

        foreach (var go in refScene.GetRootGameObjects())
        {
            if (go.name == "XR Origin (VR)") refXROrigin = go;
            if (go.name == "XR Interaction Manager") refIntManager = go;
        }

        if (refXROrigin == null || refIntManager == null)
        {
            Debug.LogError("Failed to find XR Origin (VR) or XR Interaction Manager in reference scene!");
            EditorSceneManager.CloseScene(refScene, true);
            return;
        }

        // Remove any old XR objects in target scene before copying
        foreach (var go in targetScene.GetRootGameObjects())
        {
            if (go.name == "XR Origin (VR)" || go.name == "XR Interaction Manager" || go.name.Contains("XR_Rig_Placeholder"))
            {
                Object.DestroyImmediate(go);
            }
        }

        // Instantiate XR Origin and XR Interaction Manager into target scene
        GameObject newXROrigin = Object.Instantiate(refXROrigin);
        newXROrigin.name = "XR Origin (VR)";
        SceneManager.MoveGameObjectToScene(newXROrigin, targetScene);

        GameObject newIntManager = Object.Instantiate(refIntManager);
        newIntManager.name = "XR Interaction Manager";
        SceneManager.MoveGameObjectToScene(newIntManager, targetScene);

        // Close reference scene without saving
        EditorSceneManager.CloseScene(refScene, true);

        // 3. Configure XR Origin Position & Orientation
        // Entrance position: (-15.0, 0.0, 17.0), Facing South (180 deg) towards sliding glass doors at Z=14.14
        newXROrigin.transform.position = new Vector3(-15.00f, 0.00f, 17.00f);
        newXROrigin.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        // 4. Configure Cameras & Audio Listeners
        ConfigureCameras(targetScene, newXROrigin);

        // 5. Configure Locomotion (Snap Turn ON, Continuous Turn OFF, Move ON)
        ConfigureLocomotion(newXROrigin);

        // 6. Setup EventSystem with XR UI Input Module
        ConfigureEventSystem(targetScene);

        // 7. Setup Structural Colliders & Teleportation Areas
        ConfigureStructuralColliders(targetScene);

        // Save scene
        EditorSceneManager.SaveScene(targetScene, TargetScenePath);
        Debug.Log("=== SetupHospitalXR COMPLETE ===");
    }

    private static void ConfigureCameras(Scene scene, GameObject xrOrigin)
    {
        // 1. Find XR Camera under XR Origin
        Camera xrCam = xrOrigin.GetComponentInChildren<Camera>(true);
        if (xrCam != null)
        {
            xrCam.gameObject.tag = "MainCamera";
            xrCam.enabled = true;

            AudioListener al = xrCam.GetComponent<AudioListener>();
            if (al == null) al = xrCam.gameObject.AddComponent<AudioListener>();
            al.enabled = true;
        }

        // 2. Disable existing non-XR cameras or standalone main camera in target scene
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root == xrOrigin) continue;

            Camera[] cams = root.GetComponentsInChildren<Camera>(true);
            foreach (var c in cams)
            {
                // Disable non-XR camera
                c.enabled = false;
                if (c.gameObject.tag == "MainCamera" && c != xrCam)
                {
                    c.gameObject.tag = "Untagged";
                }

                // Disable any audio listeners on non-XR cameras
                AudioListener al = c.GetComponent<AudioListener>();
                if (al != null) al.enabled = false;
            }
        }
    }

    private static void ConfigureLocomotion(GameObject xrOrigin)
    {
        Transform locomotion = xrOrigin.transform.Find("Locomotion");
        if (locomotion == null) return;

        Transform turn = locomotion.Find("Turn");
        if (turn != null)
        {
            var snapTurn = turn.GetComponent<SnapTurnProvider>();
            if (snapTurn != null) snapTurn.enabled = true;

            var contTurn = turn.GetComponent<ContinuousTurnProvider>();
            if (contTurn != null) contTurn.enabled = false; // Phase 3: Do not enable Snap & Continuous turn simultaneously!
        }

        Transform move = locomotion.Find("Move");
        if (move != null)
        {
            var dynMove = move.GetComponent<DynamicMoveProvider>();
            if (dynMove != null) dynMove.enabled = true;
        }
    }

    private static void ConfigureEventSystem(Scene scene)
    {
        EventSystem existingEs = Object.FindAnyObjectByType<EventSystem>();
        if (existingEs == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            SceneManager.MoveGameObjectToScene(esGo, scene);
            existingEs = esGo.AddComponent<EventSystem>();
        }

        // Replace StandaloneInputModule with XRUIInputModule
        var standaloneInput = existingEs.GetComponent<StandaloneInputModule>();
        if (standaloneInput != null)
        {
            Object.DestroyImmediate(standaloneInput);
        }

        var xrUiInput = existingEs.GetComponent<XRUIInputModule>();
        if (xrUiInput == null)
        {
            existingEs.gameObject.AddComponent<XRUIInputModule>();
        }
    }

    private static void ConfigureStructuralColliders(Scene scene)
    {
        // 1. Walkable Floor Surfaces
        AddFloorCollider("Ground_Plane", new Vector3(0f, -0.08f, 0f), new Vector3(120f, 0.06f, 120f), addTeleport: false);
        AddFloorCollider("Entrance_Plaza", new Vector3(-12f, -0.01f, 24f), new Vector3(36f, 0.04f, 20f), addTeleport: true);
        AddFloorCollider("Waiting_Floor", new Vector3(-15f, -0.05f, 8f), new Vector3(10f, 0.10f, 12f), addTeleport: true);
        AddFloorCollider("Office_Floor", new Vector3(15f, -0.05f, 8f), new Vector3(10f, 0.10f, 12f), addTeleport: true);
        AddFloorCollider("Corridor_Floor", new Vector3(0f, -0.05f, 0f), new Vector3(40f, 0.10f, 4f), addTeleport: true);
        AddFloorCollider("Floor_Cardiac_Arrest", new Vector3(15f, -0.05f, -8f), new Vector3(10f, 0.10f, 12f), addTeleport: true);
        AddFloorCollider("Floor_Isolation", new Vector3(-15f, -0.05f, -8f), new Vector3(10f, 0.10f, 12f), addTeleport: true);
        AddFloorCollider("Floor_Observation", new Vector3(-5f, -0.05f, 8f), new Vector3(10f, 0.10f, 12f), addTeleport: true);
        AddFloorCollider("Floor_Surgical", new Vector3(5f, -0.05f, -8f), new Vector3(10f, 0.10f, 12f), addTeleport: true);
        AddFloorCollider("Floor_Trauma", new Vector3(-5f, -0.05f, -8f), new Vector3(10f, 0.10f, 12f), addTeleport: true);
        AddFloorCollider("Floor_Triage", new Vector3(5f, -0.05f, 8f), new Vector3(10f, 0.10f, 12f), addTeleport: true);

        // 2. Solid Perimeter & Division Walls (BoxColliders)
        AddWallCollider("East_Corridor_Wall", new Vector3(-20.08f, 2.50f, 0f), new Vector3(0.15f, 5.0f, 4.0f));
        AddWallCollider("West_Corridor_Wall", new Vector3(20.08f, 2.50f, 0f), new Vector3(0.15f, 5.0f, 4.0f));
        AddWallCollider("Office_Wall_L", new Vector3(20.08f, 2.50f, 8f), new Vector3(0.15f, 5.0f, 12.0f));
        AddWallCollider("Office_Wall_R", new Vector3(9.93f, 2.50f, 8f), new Vector3(0.15f, 5.0f, 12.0f));
        AddWallCollider("Office_Wall_F_L", new Vector3(19.00f, 2.50f, 14.08f), new Vector3(2.0f, 5.0f, 0.15f));
        AddWallCollider("Office_Wall_F_R", new Vector3(11.00f, 2.50f, 14.08f), new Vector3(2.0f, 5.0f, 0.15f));
        AddWallCollider("Office_Wall_B_L", new Vector3(17.98f, 2.50f, 1.93f), new Vector3(4.04f, 5.0f, 0.15f));
        AddWallCollider("Office_Wall_B_R", new Vector3(12.02f, 2.50f, 1.93f), new Vector3(4.04f, 5.0f, 0.15f));
        AddWallCollider("Waiting_Wall_L", new Vector3(-9.93f, 2.50f, 8f), new Vector3(0.15f, 5.0f, 12.0f));
        AddWallCollider("Waiting_Wall_R", new Vector3(-20.08f, 2.50f, 8f), new Vector3(0.15f, 5.0f, 12.0f));
        AddWallCollider("Waiting_Wall_F_L", new Vector3(-11.42f, 2.50f, 14.08f), new Vector3(2.84f, 5.0f, 0.15f));
        AddWallCollider("Waiting_Wall_F_R", new Vector3(-18.58f, 2.50f, 14.08f), new Vector3(2.84f, 5.0f, 0.15f));
        AddWallCollider("Wall_Back_Solid_Cardiac_Arrest", new Vector3(15.00f, 2.50f, -14.08f), new Vector3(10.0f, 5.0f, 0.15f));
        AddWallCollider("Wall_Left_Solid_Cardiac_Arrest", new Vector3(20.08f, 2.50f, -8.00f), new Vector3(0.15f, 5.0f, 12.0f));
        AddWallCollider("Wall_Back_Solid_Trauma", new Vector3(-5.00f, 2.50f, -14.08f), new Vector3(10.0f, 5.0f, 0.15f));
        AddWallCollider("Wall_Corridor_Trauma", new Vector3(-10.08f, 2.50f, -8.00f), new Vector3(0.15f, 5.0f, 12.0f));
        AddWallCollider("Wall_Back_Solid_Surgical", new Vector3(5.00f, 2.50f, -14.08f), new Vector3(10.0f, 5.0f, 0.15f));
        AddWallCollider("Wall_Left_Solid_Surgical", new Vector3(10.08f, 2.50f, -8.00f), new Vector3(0.15f, 5.0f, 12.0f));
        AddWallCollider("Wall_Corridor_Surgical", new Vector3(-0.07f, 2.50f, -8.00f), new Vector3(0.15f, 5.0f, 12.0f));
        AddWallCollider("Wall_Back_Solid_Triage", new Vector3(5.00f, 2.50f, 14.08f), new Vector3(10.0f, 5.0f, 0.15f));
        AddWallCollider("Wall_Left_Solid_Triage", new Vector3(-0.07f, 2.50f, 8.00f), new Vector3(0.15f, 5.0f, 12.0f));
        AddWallCollider("Wall_Back_Solid_Observation", new Vector3(-5.00f, 2.50f, 14.08f), new Vector3(10.0f, 5.0f, 0.15f));
        AddWallCollider("Wall_Corridor_Isolation", new Vector3(-20.08f, 2.50f, -8.00f), new Vector3(0.15f, 5.0f, 12.0f));
    }

    private static void AddFloorCollider(string goName, Vector3 expectedCenter, Vector3 expectedSize, bool addTeleport)
    {
        GameObject go = GameObject.Find(goName);
        if (go == null) return;

        BoxCollider col = go.GetComponent<BoxCollider>();
        if (col == null) col = go.AddComponent<BoxCollider>();

        // Set local bounds matching world dimensions
        col.center = go.transform.InverseTransformPoint(expectedCenter);
        Vector3 lossy = go.transform.lossyScale;
        col.size = new Vector3(
            lossy.x != 0 ? expectedSize.x / lossy.x : expectedSize.x,
            lossy.y != 0 ? expectedSize.y / lossy.y : expectedSize.y,
            lossy.z != 0 ? expectedSize.z / lossy.z : expectedSize.z
        );

        if (addTeleport)
        {
            var tele = go.GetComponent<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea>();
            if (tele == null) go.AddComponent<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea>();
        }
    }

    private static void AddWallCollider(string goName, Vector3 expectedCenter, Vector3 expectedSize)
    {
        GameObject go = GameObject.Find(goName);
        if (go == null) return;

        BoxCollider col = go.GetComponent<BoxCollider>();
        if (col == null) col = go.AddComponent<BoxCollider>();

        col.center = go.transform.InverseTransformPoint(expectedCenter);
        Vector3 lossy = go.transform.lossyScale;
        col.size = new Vector3(
            lossy.x != 0 ? expectedSize.x / lossy.x : expectedSize.x,
            lossy.y != 0 ? expectedSize.y / lossy.y : expectedSize.y,
            lossy.z != 0 ? expectedSize.z / lossy.z : expectedSize.z
        );
    }
}
