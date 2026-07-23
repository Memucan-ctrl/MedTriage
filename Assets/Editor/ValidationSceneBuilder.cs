using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ValidationSceneBuilder
{
    private const string ScenePath = "Assets/MedTriage/Scenes/Equipment_Validation.unity";
    private const string EquipmentRoot = "Assets/MedTriage/Art/Equipment";

    [MenuItem("Tools/MedTriage/Build Validation Scene")]
    public static void BuildValidationScene()
    {
        Debug.Log("=== Build Validation Scene START ===");

        // Create new empty scene
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // 1. Create a temporary floor plane
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "TemporaryFloor";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(3f, 1f, 3f); // 30m x 30m

        // Dark grey floor material for contrast
        Material floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        floorMat.SetColor("_BaseColor", new Color(0.2f, 0.22f, 0.25f));
        floorMat.SetFloat("_Smoothness", 0.3f);
        floor.GetComponent<Renderer>().sharedMaterial = floorMat;

        // 2. Prefab list and spacing along X axis
        string[] prefabNames = {
            "PatientMonitor",
            "UltrasoundMachine",
            "MechanicalVentilator",
            "ImagingScanner",
            "BedsideMonitor",
            "MedicalConsole"
        };

        float spacing = 3.5f;
        float startX = -((prefabNames.Length - 1) * spacing) / 2f; // center the row

        for (int i = 0; i < prefabNames.Length; i++)
        {
            string pName = prefabNames[i];
            string pPath = $"{EquipmentRoot}/{pName}/Prefabs/{pName}.prefab";

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(pPath);
            if (prefabAsset == null)
            {
                Debug.LogError($"[VALIDATION] Prefab not found at {pPath}");
                continue;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
            instance.name = pName;

            // Place on floor at Z=0, Y=0 (or ground aligned)
            float xPos = startX + (i * spacing);

            // Compute bottom alignment so base sits on floor Y=0
            Bounds b = GetBounds(instance);
            float yOffset = -b.min.y;

            instance.transform.position = new Vector3(xPos, yOffset, 0f);
            instance.transform.rotation = Quaternion.identity;

            Debug.Log($"Placed {pName} at Position=({xPos:F2}, {yOffset:F2}, 0.00)");
        }

        // 3. Position Camera & Directional Light for optimal viewing of all 6 assets
        GameObject camGo = GameObject.Find("Main Camera");
        if (camGo != null)
        {
            camGo.transform.position = new Vector3(0f, 3.5f, -10f);
            camGo.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
        }

        GameObject lightGo = GameObject.Find("Directional Light");
        if (lightGo != null)
        {
            lightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            Light l = lightGo.GetComponent<Light>();
            if (l != null) l.intensity = 1.2f;
        }

        // Save scene
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"[VALIDATION SCENE SAVED] {ScenePath}");
        Debug.Log("=== Build Validation Scene COMPLETE ===");
    }

    private static Bounds GetBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b;
    }
}
