using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Kubonsang.VfxForge.Editor
{
    public enum VfxOverwritePolicy
    {
        Fail,
        OverwriteGeneratedOnly,
        CreateVariant
    }

    public sealed class VfxCompileResult
    {
        public bool Success;
        public string PrefabPath = string.Empty;
        public GameObject Prefab;
        public List<VfxValidationResult> Results = new List<VfxValidationResult>();
    }

    public static class VfxRecipeCompiler
    {
        public static VfxCompileResult Compile(
            VfxRecipe recipe,
            string recipeAssetPath,
            VfxTemplateCatalog catalog,
            VfxOverwritePolicy overwritePolicy = VfxOverwritePolicy.OverwriteGeneratedOnly)
        {
            var result = new VfxCompileResult();
            if (recipe == null || catalog == null)
            {
                result.Results.Add(VfxValidationResult.Error("COMPILE-INPUT", "Recipe or catalog is null."));
                return result;
            }

            result.Results.AddRange(VfxTemplateCatalogValidator.Validate(catalog));
            if (VfxRecipeValidator.HasErrors(result.Results))
            {
                return result;
            }

            if (!catalog.TryGet(recipe.template, out VfxTemplateEntry template) || template.prefab == null)
            {
                result.Results.Add(VfxValidationResult.Error("COMPILE-TEMPLATE", $"Template not found: {recipe.template}"));
                return result;
            }

            string outputPath = ResolveOutputPath(recipe.outputPath, overwritePolicy);
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                result.Results.Add(VfxValidationResult.Error("COMPILE-OUTPUT", "Output path cannot be used safely."));
                return result;
            }

            EnsureAssetFolder(Path.GetDirectoryName(outputPath)?.Replace('\\', '/'));
            GameObject instance = null;
            try
            {
                instance = PrefabUtility.InstantiatePrefab(template.prefab) as GameObject;
                if (instance == null)
                {
                    result.Results.Add(VfxValidationResult.Error("COMPILE-INSTANTIATE", "Template prefab could not be instantiated."));
                    return result;
                }

                instance.name = string.IsNullOrWhiteSpace(recipe.displayName) ? recipe.id : recipe.displayName;
                VfxMetadata metadata = instance.GetComponent<VfxMetadata>() ?? instance.AddComponent<VfxMetadata>();
                metadata.recipeId = recipe.id;
                metadata.schemaVersion = recipe.schemaVersion;
                metadata.templateId = recipe.template;
                metadata.recipeAssetPath = recipeAssetPath ?? string.Empty;
                metadata.generatedAtUtc = DateTime.UtcNow.ToString("O");

                result.Results.AddRange(VfxPropertyApplier.Apply(instance, recipe, template));
                if (VfxRecipeValidator.HasErrors(result.Results))
                {
                    return result;
                }

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, outputPath);
                if (saved == null)
                {
                    result.Results.Add(VfxValidationResult.Error("COMPILE-SAVE", "Prefab save failed."));
                    return result;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(outputPath);
                result.Success = true;
                result.PrefabPath = outputPath;
                result.Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
                result.Results.Add(VfxValidationResult.Pass("COMPILE-SAVE", $"Prefab saved: {outputPath}"));
                return result;
            }
            finally
            {
                if (instance != null)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }
        }

        private static string ResolveOutputPath(string requestedPath, VfxOverwritePolicy policy)
        {
            if (!VfxRecipePath.TryNormalizePrefabAssetPath(requestedPath, out requestedPath))
            {
                return string.Empty;
            }

            UnityEngine.Object existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(requestedPath);
            if (existing == null)
            {
                return requestedPath;
            }

            if (policy == VfxOverwritePolicy.Fail)
            {
                return string.Empty;
            }

            if (policy == VfxOverwritePolicy.CreateVariant)
            {
                return AssetDatabase.GenerateUniqueAssetPath(requestedPath);
            }

            GameObject existingPrefab = existing as GameObject;
            return existingPrefab != null && existingPrefab.GetComponent<VfxMetadata>() != null
                ? requestedPath
                : string.Empty;
        }

        private static void EnsureAssetFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = $"{current}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }
                current = next;
            }
        }
    }
}
