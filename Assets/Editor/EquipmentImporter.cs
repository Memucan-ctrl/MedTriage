using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class EquipmentImporter
{
    private const string BaseDir = "Assets/MedTriage/Art/Equipment";

    [MenuItem("Tools/MedTriage/Build Materials And Prefabs")]
    public static void BuildAll()
    {
        Debug.Log("=== Build Materials & Prefabs START ===");

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("URP Lit shader not found!");
            return;
        }

        BuildPatientMonitor(urpLit);
        BuildUltrasoundMachine(urpLit);
        BuildMechanicalVentilator(urpLit);
        BuildImagingScanner(urpLit);
        BuildBedsideMonitor(urpLit);
        BuildMedicalConsole(urpLit);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("=== Build Materials & Prefabs COMPLETE ===");
    }

    private static Material GetOrCreateMaterial(string matPath, Shader shader)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        return mat;
    }

    private static Texture2D LoadTex(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Color ParseHex(string hex)
    {
        Color c;
        if (ColorUtility.TryParseHtmlString(hex, out c)) return c;
        return Color.white;
    }

    // -------------------------------------------------------------
    // 1. PatientMonitor
    // -------------------------------------------------------------
    private static void BuildPatientMonitor(Shader shader)
    {
        string dir = $"{BaseDir}/PatientMonitor";
        string matPath = $"{dir}/Materials/monitor.mat";
        Material mat = GetOrCreateMaterial(matPath, shader);

        Texture2D baseMap = LoadTex($"{dir}/Textures/monitor_baseColor.png");
        Texture2D normalMap = LoadTex($"{dir}/Textures/monitor_normal.png");
        Texture2D msMap = LoadTex($"{dir}/Textures/monitor_MetallicSmoothness.png");
        Texture2D emissiveMap = LoadTex($"{dir}/Textures/monitor_emissive.png");

        if (baseMap) mat.SetTexture("_BaseMap", baseMap);
        if (normalMap)
        {
            mat.SetTexture("_BumpMap", normalMap);
            mat.EnableKeyword("_NORMALMAP");
        }
        if (msMap)
        {
            mat.SetTexture("_MetallicGlossMap", msMap);
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.SetFloat("_Smoothness", 1.0f);
        }
        if (emissiveMap)
        {
            mat.SetTexture("_EmissionMap", emissiveMap);
            mat.SetColor("_EmissionColor", Color.white);
            mat.EnableKeyword("_EMISSION");
        }

        string fbxPath = $"{dir}/Models/PatientMonitor.fbx";
        string prefabPath = $"{dir}/Prefabs/PatientMonitor.prefab";
        CreatePrefabWithMaterials(fbxPath, prefabPath, new Dictionary<string, Material>
        {
            { "monitor", mat }
        }, addBoxCollider: true);
    }

    // -------------------------------------------------------------
    // 2. UltrasoundMachine
    // -------------------------------------------------------------
    private static void BuildUltrasoundMachine(Shader shader)
    {
        string dir = $"{BaseDir}/UltrasoundMachine";
        string[] parts = { "Lower_Case", "Operator_Panel", "Screen_1", "Screen_2", "Upper_Case", "Wheels" };
        var matMap = new Dictionary<string, Material>();

        foreach (var p in parts)
        {
            string matPath = $"{dir}/Materials/{p}.mat";
            Material mat = GetOrCreateMaterial(matPath, shader);

            Texture2D baseMap = LoadTex($"{dir}/Textures/{p}_baseColor.png");
            Texture2D normalMap = LoadTex($"{dir}/Textures/{p}_normal.png");
            Texture2D msMap = LoadTex($"{dir}/Textures/{p}_MetallicSmoothness.png");

            if (baseMap) mat.SetTexture("_BaseMap", baseMap);
            if (normalMap)
            {
                mat.SetTexture("_BumpMap", normalMap);
                mat.EnableKeyword("_NORMALMAP");
            }
            if (msMap)
            {
                mat.SetTexture("_MetallicGlossMap", msMap);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                mat.SetFloat("_Smoothness", 1.0f);
            }
            matMap[p] = mat;
        }

        string fbxPath = $"{dir}/Models/UltrasoundMachine.fbx";
        string prefabPath = $"{dir}/Prefabs/UltrasoundMachine.prefab";
        CreatePrefabWithMaterials(fbxPath, prefabPath, matMap, addBoxCollider: true);
    }

    // -------------------------------------------------------------
    // 3. MechanicalVentilator
    // -------------------------------------------------------------
    private static void BuildMechanicalVentilator(Shader shader)
    {
        string dir = $"{BaseDir}/MechanicalVentilator";
        string[] parts = { "Base", "Other", "Tubes" };
        var matMap = new Dictionary<string, Material>();

        foreach (var p in parts)
        {
            string matPath = $"{dir}/Materials/{p}.mat";
            Material mat = GetOrCreateMaterial(matPath, shader);

            string ext = p == "Tubes" ? "jpeg" : "png";
            Texture2D baseMap = LoadTex($"{dir}/Textures/{p}_baseColor.{ext}");
            Texture2D normalMap = LoadTex($"{dir}/Textures/{p}_normal.png");
            Texture2D msMap = LoadTex($"{dir}/Textures/{p}_MetallicSmoothness.png");

            if (baseMap) mat.SetTexture("_BaseMap", baseMap);
            if (normalMap)
            {
                mat.SetTexture("_BumpMap", normalMap);
                mat.EnableKeyword("_NORMALMAP");
            }
            if (msMap)
            {
                mat.SetTexture("_MetallicGlossMap", msMap);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                mat.SetFloat("_Smoothness", 1.0f);
            }
            matMap[p] = mat;
        }

        string fbxPath = $"{dir}/Models/MechanicalVentilator.fbx";
        string prefabPath = $"{dir}/Prefabs/MechanicalVentilator.prefab";
        CreatePrefabWithMaterials(fbxPath, prefabPath, matMap, addBoxCollider: true);
    }

    // -------------------------------------------------------------
    // 4. ImagingScanner (CArmScanner)
    // -------------------------------------------------------------
    private static void BuildImagingScanner(Shader shader)
    {
        string dir = $"{BaseDir}/ImagingScanner";
        var matMap = new Dictionary<string, Material>();

        Texture2D ecg1 = LoadTex($"{dir}/Textures/ECG_1.png");
        Texture2D ecg2 = LoadTex($"{dir}/Textures/ECG_2.png");

        // Color / Procedural definitions
        matMap["Matte dark blue plastic"] = CreateColorMat(shader, $"{dir}/Materials/MatteDarkBluePlastic.mat", ParseHex("#1b2a47"), 0.3f);
        matMap["Beige plastic"]           = CreateColorMat(shader, $"{dir}/Materials/BeigePlastic.mat", ParseHex("#d4cbb8"), 0.5f);
        matMap["Grey plastic"]            = CreateColorMat(shader, $"{dir}/Materials/GreyPlastic.mat", ParseHex("#70757d"), 0.5f);
        matMap["Light blue plastic"]      = CreateColorMat(shader, $"{dir}/Materials/LightBluePlastic.mat", ParseHex("#5b82a6"), 0.5f);
        matMap["Dark blue plastic"]       = CreateColorMat(shader, $"{dir}/Materials/DarkBluePlastic.mat", ParseHex("#132238"), 0.5f);
        matMap["Black rubber"]            = CreateColorMat(shader, $"{dir}/Materials/BlackRubber.mat", ParseHex("#1a1a1a"), 0.2f);
        matMap["LED lit"]                 = CreateEmissiveMat(shader, $"{dir}/Materials/LEDLit.mat", ParseHex("#00ffcc"), null, Color.cyan * 2f);

        // Screens with textures
        matMap["Screen"]   = CreateEmissiveMat(shader, $"{dir}/Materials/Screen.mat", Color.white, ecg1, Color.white);
        matMap["Screen 2"] = CreateEmissiveMat(shader, $"{dir}/Materials/Screen2.mat", Color.white, ecg2, Color.white);

        string fbxPath = $"{dir}/Models/ImagingScanner.fbx";
        string prefabPath = $"{dir}/Prefabs/ImagingScanner.prefab";
        // Scanner bore must stay open: addBoxCollider = false or targeted
        CreatePrefabWithMaterials(fbxPath, prefabPath, matMap, addBoxCollider: false);
    }

    // -------------------------------------------------------------
    // 5. BedsideMonitor
    // -------------------------------------------------------------
    private static void BuildBedsideMonitor(Shader shader)
    {
        string dir = $"{BaseDir}/BedsideMonitor";
        var matMap = new Dictionary<string, Material>();

        Texture2D screen5 = LoadTex($"{dir}/Textures/Screen_5.png");
        Texture2D screen6 = LoadTex($"{dir}/Textures/Screen_6.png");

        matMap["Dark blue plastic"]  = CreateColorMat(shader, $"{dir}/Materials/DarkBluePlastic.mat", ParseHex("#132238"), 0.5f);
        matMap["Beige plastic"]      = CreateColorMat(shader, $"{dir}/Materials/BeigePlastic.mat", ParseHex("#d4cbb8"), 0.5f);
        matMap["Light blue plastic"] = CreateColorMat(shader, $"{dir}/Materials/LightBluePlastic.mat", ParseHex("#5b82a6"), 0.5f);
        matMap["Empty screen"]       = CreateColorMat(shader, $"{dir}/Materials/EmptyScreen.mat", ParseHex("#050a10"), 0.8f);
        matMap["Blue clear plastic"] = CreateColorMat(shader, $"{dir}/Materials/BlueClearPlastic.mat", ParseHex("#2a5078"), 0.85f);
        matMap["Clear plastic"]      = CreateColorMat(shader, $"{dir}/Materials/ClearPlastic.mat", ParseHex("#e0e0e0"), 0.9f);
        matMap["LED unlit"]          = CreateColorMat(shader, $"{dir}/Materials/LEDUnlit.mat", ParseHex("#203020"), 0.3f);
        matMap["LED lit"]            = CreateEmissiveMat(shader, $"{dir}/Materials/LEDLit.mat", ParseHex("#00ff44"), null, Color.green * 2f);

        matMap["Upper mini screen 2"] = CreateEmissiveMat(shader, $"{dir}/Materials/UpperMiniScreen2.mat", Color.white, screen5, Color.white);
        matMap["Monitor screen.001"]  = CreateEmissiveMat(shader, $"{dir}/Materials/MonitorScreen001.mat", Color.white, screen6, Color.white);

        string fbxPath = $"{dir}/Models/BedsideMonitor.fbx";
        string prefabPath = $"{dir}/Prefabs/BedsideMonitor.prefab";
        CreatePrefabWithMaterials(fbxPath, prefabPath, matMap, addBoxCollider: true);
    }

    // -------------------------------------------------------------
    // 6. MedicalConsole
    // -------------------------------------------------------------
    private static void BuildMedicalConsole(Shader shader)
    {
        string dir = $"{BaseDir}/MedicalConsole";
        var matMap = new Dictionary<string, Material>();

        Texture2D ecg2 = LoadTex($"{dir}/Textures/ECG_2.png");
        Texture2D screen5 = LoadTex($"{dir}/Textures/Screen_5.png");

        matMap["Light blue plastic"]  = CreateColorMat(shader, $"{dir}/Materials/LightBluePlastic.mat", ParseHex("#5b82a6"), 0.5f);
        matMap["Beige plastic"]       = CreateColorMat(shader, $"{dir}/Materials/BeigePlastic.mat", ParseHex("#d4cbb8"), 0.5f);
        matMap["Dark blue plastic"]    = CreateColorMat(shader, $"{dir}/Materials/DarkBluePlastic.mat", ParseHex("#132238"), 0.5f);
        matMap["Black rubber"]         = CreateColorMat(shader, $"{dir}/Materials/BlackRubber.mat", ParseHex("#1a1a1a"), 0.2f);
        matMap["Blue clear plastic"]  = CreateColorMat(shader, $"{dir}/Materials/BlueClearPlastic.mat", ParseHex("#2a5078"), 0.85f);
        matMap["Shiny black plastic"] = CreateColorMat(shader, $"{dir}/Materials/ShinyBlackPlastic.mat", ParseHex("#0a0a0a"), 0.9f);
        matMap["Matte blue plastic"]  = CreateColorMat(shader, $"{dir}/Materials/MatteBluePlastic.mat", ParseHex("#203a5e"), 0.2f);
        matMap["LED lit"]             = CreateEmissiveMat(shader, $"{dir}/Materials/LEDLit.mat", ParseHex("#00e5ff"), null, Color.cyan * 2f);

        matMap["Scanner screen 1"]    = CreateEmissiveMat(shader, $"{dir}/Materials/ScannerScreen1.mat", Color.white, ecg2, Color.white);
        matMap["Scanner screen 2"]    = CreateEmissiveMat(shader, $"{dir}/Materials/ScannerScreen2.mat", Color.white, screen5, Color.white);

        string fbxPath = $"{dir}/Models/MedicalConsole.fbx";
        string prefabPath = $"{dir}/Prefabs/MedicalConsole.prefab";
        CreatePrefabWithMaterials(fbxPath, prefabPath, matMap, addBoxCollider: true);
    }

    // -------------------------------------------------------------
    // Helper Material Constructors
    // -------------------------------------------------------------
    private static Material CreateColorMat(Shader shader, string path, Color baseColor, float smoothness)
    {
        Material mat = GetOrCreateMaterial(path, shader);
        mat.SetColor("_BaseColor", baseColor);
        mat.SetFloat("_Smoothness", smoothness);
        return mat;
    }

    private static Material CreateEmissiveMat(Shader shader, string path, Color baseColor, Texture2D tex, Color emissiveColor)
    {
        Material mat = GetOrCreateMaterial(path, shader);
        mat.SetColor("_BaseColor", baseColor);
        if (tex != null)
        {
            mat.SetTexture("_BaseMap", tex);
            mat.SetTexture("_EmissionMap", tex);
        }
        mat.SetColor("_EmissionColor", emissiveColor);
        mat.EnableKeyword("_EMISSION");
        return mat;
    }

    // -------------------------------------------------------------
    // Prefab Generator & Material Assigner
    // -------------------------------------------------------------
    private static void CreatePrefabWithMaterials(string fbxPath, string prefabPath, Dictionary<string, Material> matMap, bool addBoxCollider)
    {
        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxAsset == null)
        {
            Debug.LogError($"[PREFAB] FBX not found at {fbxPath}");
            return;
        }

        GameObject instance = Object.Instantiate(fbxAsset);
        instance.name = Path.GetFileNameWithoutExtension(prefabPath);

        // Assign materials matching material slot names
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            Material[] origMats = r.sharedMaterials;
            Material[] newMats = new Material[origMats.Length];

            for (int i = 0; i < origMats.Length; i++)
            {
                string origName = origMats[i] != null ? origMats[i].name : "";
                if (matMap.ContainsKey(origName))
                {
                    newMats[i] = matMap[origName];
                }
                else
                {
                    // Fallback search by key prefix
                    Material found = null;
                    foreach (var kvp in matMap)
                    {
                        if (origName.StartsWith(kvp.Key, System.StringComparison.OrdinalIgnoreCase))
                        {
                            found = kvp.Value;
                            break;
                        }
                    }
                    newMats[i] = found != null ? found : origMats[i];
                }
            }
            r.sharedMaterials = newMats;
        }

        // Add approximate main body Box Collider if requested
        if (addBoxCollider)
        {
            var bounds = GetCompositeBounds(instance);
            BoxCollider col = instance.AddComponent<BoxCollider>();
            col.center = instance.transform.InverseTransformPoint(bounds.center);
            col.size = bounds.size;
        }

        // Save Prefab
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
        Debug.Log($"[PREFAB CREATED] {prefabPath}");
    }

    private static Bounds GetCompositeBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one);
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b;
    }
}
