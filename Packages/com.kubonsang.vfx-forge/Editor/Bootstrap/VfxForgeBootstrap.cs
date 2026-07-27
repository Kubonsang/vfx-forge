using UnityEditor;
using UnityEngine;

namespace Kubonsang.VfxForge.Editor
{
    public static class VfxForgeBootstrap
    {
        private const string Root = "Assets/VFXForge";

        [MenuItem("Tools/VFX Forge/Bootstrap Project Assets")]
        public static void Bootstrap()
        {
            EnsureFolder("Assets", "VFXForge");
            EnsureFolder(Root, "Config");
            EnsureFolder(Root, "Recipes");
            EnsureFolder(Root, "Templates");
            EnsureFolder(Root, "Generated");
            EnsureFolder(Root, "Artifacts");

            string catalogPath = $"{Root}/Config/VfxTemplateCatalog.asset";
            if (AssetDatabase.LoadAssetAtPath<VfxTemplateCatalog>(catalogPath) == null)
            {
                var catalog = ScriptableObject.CreateInstance<VfxTemplateCatalog>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }

            string profilePath = $"{Root}/Config/DefaultVfxStyleProfile.asset";
            if (AssetDatabase.LoadAssetAtPath<VfxStyleProfile>(profilePath) == null)
            {
                var profile = ScriptableObject.CreateInstance<VfxStyleProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<VfxTemplateCatalog>(catalogPath);
            Debug.Log("[VFXForge] Project assets bootstrapped.");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
