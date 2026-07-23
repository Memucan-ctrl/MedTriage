using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Phase 2: Creates Unity folder structure and copies FBX + textures for 6 medical equipment assets.
/// CArmScanner is renamed to ImagingScanner in Unity.
/// Run once via Tools > MedTriage > Setup Equipment Folders
/// </summary>
public static class EquipmentFolderSetup
{
    private const string EquipmentRoot = "Assets/MedTriage/Art/Equipment";

    private static readonly Dictionary<string, string[]> AssetTextures = new Dictionary<string, string[]>
    {
        ["PatientMonitor"] = new[] {
            "monitor_baseColor.png",
            "monitor_emissive.png",
            "monitor_metallicRoughness.png",
            "monitor_normal.png"
        },
        ["UltrasoundMachine"] = new[] {
            "Lower_Case_baseColor.png",    "Lower_Case_metallicRoughness.png", "Lower_Case_normal.png",
            "Operator_Panel_baseColor.png","Operator_Panel_metallicRoughness.png","Operator_Panel_normal.png",
            "Screen_1_baseColor.png",      "Screen_1_metallicRoughness.png",   "Screen_1_normal.png",
            "Screen_2_baseColor.png",      "Screen_2_metallicRoughness.png",   "Screen_2_normal.png",
            "Upper_Case_baseColor.png",    "Upper_Case_metallicRoughness.png", "Upper_Case_normal.png",
            "Wheels_baseColor.png",        "Wheels_metallicRoughness.png",     "Wheels_normal.png"
        },
        ["MechanicalVentilator"] = new[] {
            "Base_baseColor.png",  "Base_metallicRoughness.png",  "Base_normal.png",
            "Other_baseColor.png", "Other_metallicRoughness.png", "Other_normal.png",
            "Tubes_baseColor.jpeg","Tubes_metallicRoughness.png", "Tubes_normal.png"
        },
        ["ImagingScanner"] = new[] {
            "ECG_1.png",
            "ECG_2.png"
        },
        ["BedsideMonitor"] = new[] {
            "Screen_5.png",
            "Screen_6.png"
        },
        ["MedicalConsole"] = new[] {
            "ECG_2.png",
            "Screen_5.png"
        }
    };

    // Source FBX name -> Unity asset name mapping
    private static readonly Dictionary<string, string> FbxSourceName = new Dictionary<string, string>
    {
        ["PatientMonitor"]    = "PatientMonitor",
        ["UltrasoundMachine"] = "UltrasoundMachine",
        ["MechanicalVentilator"] = "MechanicalVentilator",
        ["ImagingScanner"]    = "CArmScanner",   // Source FBX is named CArmScanner
        ["BedsideMonitor"]    = "BedsideMonitor",
        ["MedicalConsole"]    = "MedicalConsole"
    };

    private const string SourceRoot = @"C:\Users\Admin\Pictures\final assets\final assets\blenderagent\Unity_Exports";

    [MenuItem("Tools/MedTriage/Setup Equipment Folders")]
    public static void SetupFolders()
    {
        Debug.Log("=== EquipmentFolderSetup START ===");

        foreach (var kvp in AssetTextures)
        {
            string assetName = kvp.Key;
            string[] textures = kvp.Value;
            string sourceFbxName = FbxSourceName[assetName];

            // --- Create subfolders ---
            string[] subFolders = { "Models", "Textures", "Materials", "Prefabs" };
            foreach (var sub in subFolders)
            {
                string folderPath = $"{EquipmentRoot}/{assetName}/{sub}";
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    string parent = $"{EquipmentRoot}/{assetName}";
                    if (!AssetDatabase.IsValidFolder(parent))
                    {
                        if (!AssetDatabase.IsValidFolder(EquipmentRoot))
                            AssetDatabase.CreateFolder("Assets/MedTriage/Art", "Equipment");
                        AssetDatabase.CreateFolder(EquipmentRoot, assetName);
                    }
                    AssetDatabase.CreateFolder(parent, sub);
                    Debug.Log($"Created folder: {folderPath}");
                }
                else
                {
                    Debug.Log($"Folder exists: {folderPath}");
                }
            }

            // --- Copy FBX ---
            string srcFbx = Path.Combine(SourceRoot, sourceFbxName, "Models", sourceFbxName + ".fbx");
            string dstFbx = $"{EquipmentRoot}/{assetName}/Models/{assetName}.fbx";
            string dstFbxAbs = Path.Combine(Application.dataPath, "..", dstFbx.Substring("Assets/".Length));

            if (!File.Exists(srcFbx))
            {
                Debug.LogError($"[MISSING FBX] {srcFbx}");
                continue;
            }

            if (!File.Exists(dstFbxAbs))
            {
                File.Copy(srcFbx, dstFbxAbs, false);
                Debug.Log($"Copied FBX: {srcFbx} -> {dstFbx}");
            }
            else
            {
                Debug.Log($"FBX already exists, skipping copy: {dstFbx}");
            }

            // --- Copy Textures ---
            string srcTexDir = Path.Combine(SourceRoot, sourceFbxName, "Textures");
            string dstTexDir = $"{EquipmentRoot}/{assetName}/Textures";
            string dstTexDirAbs = Path.Combine(Application.dataPath, "..", dstTexDir.Substring("Assets/".Length));

            foreach (var tex in textures)
            {
                string srcTex = Path.Combine(srcTexDir, tex);
                string dstTex = Path.Combine(dstTexDirAbs, tex);

                if (!File.Exists(srcTex))
                {
                    Debug.LogWarning($"[MISSING TEX] {srcTex}");
                    continue;
                }

                if (!File.Exists(dstTex))
                {
                    File.Copy(srcTex, dstTex, false);
                    Debug.Log($"Copied texture: {tex} -> {dstTexDir}");
                }
                else
                {
                    Debug.Log($"Texture already exists: {dstTexDir}/{tex}");
                }
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("=== EquipmentFolderSetup COMPLETE — AssetDatabase.Refresh() called ===");
    }
}
