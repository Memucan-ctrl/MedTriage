using System.IO;
using UnityEditor;
using UnityEngine;

public class CreateHospitalMaterials
{
    // ==== EDIT THESE TO MATCH YOUR PROJECT ====
    private const string TexturesRoot = "Assets/AssetsHospitalKit/textures";
    private const string MaterialsOut = "Assets/AssetsHospitalKit/Materials";

    [MenuItem("Tools/Hospital/1. Create Materials From Textures")]
    public static void CreateMaterials()
    {
        if (!AssetDatabase.IsValidFolder(TexturesRoot))
        {
            Debug.LogError($"Textures folder not found: {TexturesRoot}. Fix TexturesRoot.");
            return;
        }
        if (!AssetDatabase.IsValidFolder(MaterialsOut))
        {
            Directory.CreateDirectory(MaterialsOut);
            AssetDatabase.Refresh();
        }

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) { Debug.LogError("URP/Lit shader not found — is this a URP project?"); return; }

        string[] subFolders = AssetDatabase.GetSubFolders(TexturesRoot);
        int created = 0;

        foreach (string folder in subFolders)
        {
            string folderName = Path.GetFileName(folder);
            Texture2D baseColor = null, normal = null;

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string lower = path.ToLower();
                if (lower.Contains("basecolor") || lower.Contains("albedo") || lower.Contains("_color"))
                    baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                else if (lower.Contains("normal"))
                {
                    var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (ti != null && ti.textureType != TextureImporterType.NormalMap)
                    { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
                    normal = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }
            }

            if (baseColor == null) { Debug.LogWarning($"No BaseColor in {folder}, skipped."); continue; }

            Material mat = new Material(urpLit);
            mat.SetTexture("_BaseMap", baseColor);
            if (normal != null) { mat.SetTexture("_BumpMap", normal); mat.EnableKeyword("_NORMALMAP"); }

            AssetDatabase.CreateAsset(mat, $"{MaterialsOut}/{folderName}.mat");
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"✅ Created {created} materials in {MaterialsOut}");
    }
}