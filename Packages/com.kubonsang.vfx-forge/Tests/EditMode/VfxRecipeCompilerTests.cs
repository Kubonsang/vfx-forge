using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

namespace Kubonsang.VfxForge.Editor.Tests
{
    public sealed class VfxRecipeCompilerTests
    {
        private string testAssetRoot;
        private string templatePath;
        private VfxTemplateCatalog catalog;

        [SetUp]
        public void SetUp()
        {
            string folderName = $"__VfxForgeCompilerTests_{Guid.NewGuid():N}";
            AssetDatabase.CreateFolder("Assets", folderName);
            testAssetRoot = $"Assets/{folderName}";
            templatePath = $"{testAssetRoot}/Template.prefab";

            GameObject templatePrefab = CreatePrefab(templatePath, "Template", true);
            catalog = ScriptableObject.CreateInstance<VfxTemplateCatalog>();
            bool registered = catalog.TryRegister(
                new VfxTemplateEntry
                {
                    id = "impact_core",
                    prefab = templatePrefab
                },
                out List<VfxValidationResult> results);
            Assert.That(registered, Is.True, FormatFailures(results));
        }

        [TearDown]
        public void TearDown()
        {
            if (catalog != null)
            {
                UnityEngine.Object.DestroyImmediate(catalog);
            }

            if (!string.IsNullOrWhiteSpace(testAssetRoot)
                && AssetDatabase.IsValidFolder(testAssetRoot))
            {
                AssetDatabase.DeleteAsset(testAssetRoot);
            }
        }

        [Test]
        public void Compile_ValidRecipe_CreatesIndependentPrefabWithMetadata()
        {
            VfxRecipe recipe = CreateValidRecipe($"{testAssetRoot}/Generated/Impact.prefab");
            Hash128 templateHashBefore = AssetDatabase.GetAssetDependencyHash(templatePath);

            VfxCompileResult result = VfxRecipeCompiler.Compile(
                recipe,
                "Assets/VFXForge/Recipes/impact_recipe.json",
                catalog);

            Assert.That(result.Success, Is.True, FormatFailures(result.Results));
            Assert.That(result.PrefabPath, Is.EqualTo(recipe.outputPath));
            Assert.That(result.Prefab, Is.Not.Null);
            Assert.That(result.Prefab, Is.Not.SameAs(catalog.templates[0].prefab));
            Assert.That(
                PrefabUtility.GetPrefabAssetType(result.Prefab),
                Is.EqualTo(PrefabAssetType.Regular));
            Assert.That(result.Prefab.transform.Find("TemplateChild"), Is.Not.Null);

            VfxMetadata metadata = result.Prefab.GetComponent<VfxMetadata>();
            Assert.That(metadata, Is.Not.Null);
            Assert.That(metadata.recipeId, Is.EqualTo(recipe.id));
            Assert.That(metadata.schemaVersion, Is.EqualTo(recipe.schemaVersion));
            Assert.That(metadata.templateId, Is.EqualTo(recipe.template));
            Assert.That(
                metadata.recipeAssetPath,
                Is.EqualTo("Assets/VFXForge/Recipes/impact_recipe.json"));
            Assert.That(
                DateTime.TryParse(
                    metadata.generatedAtUtc,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out _),
                Is.True);

            Hash128 templateHashAfter = AssetDatabase.GetAssetDependencyHash(templatePath);
            Assert.That(templateHashAfter, Is.EqualTo(templateHashBefore));
        }

        [Test]
        public void Compile_NonGeneratedPrefabAtOutput_IsNeverOverwritten()
        {
            string outputPath = $"{testAssetRoot}/UserOwned.prefab";
            CreatePrefab(outputPath, "UserOwned", false);
            Hash128 existingHashBefore = AssetDatabase.GetAssetDependencyHash(outputPath);
            VfxRecipe recipe = CreateValidRecipe(outputPath);

            VfxCompileResult result = VfxRecipeCompiler.Compile(
                recipe,
                "Assets/VFXForge/Recipes/impact_recipe.json",
                catalog,
                VfxOverwritePolicy.OverwriteGeneratedOnly);

            Assert.That(result.Success, Is.False);
            Assert.That(HasError(result.Results, "COMPILE-OUTPUT"), Is.True);
            Assert.That(
                AssetDatabase.GetAssetDependencyHash(outputPath),
                Is.EqualTo(existingHashBefore));
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
            Assert.That(existing.name, Is.EqualTo("UserOwned"));
            Assert.That(existing.GetComponent<VfxMetadata>(), Is.Null);
        }

        [Test]
        public void Compile_CreateVariantPolicy_PreservesExistingAndUsesUniquePath()
        {
            string requestedPath = $"{testAssetRoot}/UserOwned.prefab";
            CreatePrefab(requestedPath, "UserOwned", false);
            Hash128 existingHashBefore = AssetDatabase.GetAssetDependencyHash(requestedPath);
            VfxRecipe recipe = CreateValidRecipe(requestedPath);

            VfxCompileResult result = VfxRecipeCompiler.Compile(
                recipe,
                "Assets/VFXForge/Recipes/impact_recipe.json",
                catalog,
                VfxOverwritePolicy.CreateVariant);

            Assert.That(result.Success, Is.True, FormatFailures(result.Results));
            Assert.That(result.PrefabPath, Is.Not.EqualTo(requestedPath));
            Assert.That(
                AssetDatabase.GetAssetDependencyHash(requestedPath),
                Is.EqualTo(existingHashBefore));
            Assert.That(result.Prefab.GetComponent<VfxMetadata>(), Is.Not.Null);
        }

        [Test]
        public void Compile_OverwriteGeneratedOnly_ReplacesGeneratedPrefab()
        {
            string outputPath = $"{testAssetRoot}/Generated.prefab";
            VfxRecipe firstRecipe = CreateValidRecipe(outputPath);
            Assert.That(
                VfxRecipeCompiler.Compile(firstRecipe, "Assets/Recipes/first.json", catalog).Success,
                Is.True);

            VfxRecipe secondRecipe = CreateValidRecipe(outputPath);
            secondRecipe.id = "impact_second";
            secondRecipe.displayName = "Second Impact";

            VfxCompileResult result = VfxRecipeCompiler.Compile(
                secondRecipe,
                "Assets/Recipes/second.json",
                catalog,
                VfxOverwritePolicy.OverwriteGeneratedOnly);

            Assert.That(result.Success, Is.True, FormatFailures(result.Results));
            VfxMetadata metadata = result.Prefab.GetComponent<VfxMetadata>();
            Assert.That(metadata.recipeId, Is.EqualTo("impact_second"));
            Assert.That(metadata.recipeAssetPath, Is.EqualTo("Assets/Recipes/second.json"));
        }

        [Test]
        public void Compile_FailPolicy_DoesNotReplaceGeneratedPrefab()
        {
            string outputPath = $"{testAssetRoot}/Generated.prefab";
            VfxRecipe firstRecipe = CreateValidRecipe(outputPath);
            Assert.That(
                VfxRecipeCompiler.Compile(firstRecipe, "Assets/Recipes/first.json", catalog).Success,
                Is.True);
            Hash128 existingHashBefore = AssetDatabase.GetAssetDependencyHash(outputPath);

            VfxRecipe secondRecipe = CreateValidRecipe(outputPath);
            secondRecipe.id = "impact_second";
            VfxCompileResult result = VfxRecipeCompiler.Compile(
                secondRecipe,
                "Assets/Recipes/second.json",
                catalog,
                VfxOverwritePolicy.Fail);

            Assert.That(result.Success, Is.False);
            Assert.That(HasError(result.Results, "COMPILE-OUTPUT"), Is.True);
            Assert.That(
                AssetDatabase.GetAssetDependencyHash(outputPath),
                Is.EqualTo(existingHashBefore));
        }

        [Test]
        public void Compile_InvalidRecipe_DoesNotCreateOutputFolder()
        {
            string outputFolder = $"{testAssetRoot}/InvalidOutput";
            VfxRecipe recipe = CreateValidRecipe($"{outputFolder}/Impact.prefab");
            recipe.id = "INVALID ID";

            VfxCompileResult result = VfxRecipeCompiler.Compile(
                recipe,
                "Assets/Recipes/invalid.json",
                catalog);

            Assert.That(result.Success, Is.False);
            Assert.That(HasError(result.Results, "RECIPE-ID"), Is.True);
            Assert.That(AssetDatabase.IsValidFolder(outputFolder), Is.False);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<GameObject>(recipe.outputPath),
                Is.Null);
        }

        [Test]
        public void Compile_MissingOptionalBinding_SavesWithWarning()
        {
            VfxTemplateEntry entry = catalog.templates[0];
            entry.bindings.Add(new VfxPropertyBinding
            {
                recipePath = "timing.duration",
                exposedPropertyName = "OptionalDuration",
                propertyType = VfxPropertyType.Float,
                required = false,
                componentIndex = 0
            });
            VfxRecipe recipe = CreateValidRecipe($"{testAssetRoot}/OptionalBinding.prefab");

            VfxCompileResult result = VfxRecipeCompiler.Compile(
                recipe,
                "Assets/Recipes/optional.json",
                catalog);

            Assert.That(result.Success, Is.True, FormatFailures(result.Results));
            Assert.That(HasWarning(result.Results, "CATALOG-BINDING-PROPERTY"), Is.True);
            Assert.That(HasWarning(result.Results, "BIND-APPLY"), Is.True);
            Assert.That(result.Prefab, Is.Not.Null);
        }

        private VfxRecipe CreateValidRecipe(string outputPath)
        {
            return new VfxRecipe
            {
                id = "impact_recipe",
                displayName = "Impact Recipe",
                template = "impact_core",
                outputPath = outputPath,
                timing = new VfxTiming { duration = 0.5f },
                budget = new VfxBudget
                {
                    maxParticles = 100,
                    maxDuration = 1f,
                    maxBoundsRadius = 5f
                }
            };
        }

        private static GameObject CreatePrefab(
            string path,
            string name,
            bool includeVisualEffect)
        {
            var source = new GameObject(name);
            try
            {
                if (includeVisualEffect)
                {
                    source.AddComponent<VisualEffect>();
                    var child = new GameObject("TemplateChild");
                    child.transform.SetParent(source.transform);
                }

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
                Assert.That(prefab, Is.Not.Null);
                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static bool HasError(
            IEnumerable<VfxValidationResult> results,
            string ruleId)
        {
            foreach (VfxValidationResult result in results)
            {
                if (result != null
                    && result.ruleId == ruleId
                    && result.severity == VfxValidationSeverity.Error
                    && !result.passed)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasWarning(
            IEnumerable<VfxValidationResult> results,
            string ruleId)
        {
            foreach (VfxValidationResult result in results)
            {
                if (result != null
                    && result.ruleId == ruleId
                    && result.severity == VfxValidationSeverity.Warning
                    && !result.passed)
                {
                    return true;
                }
            }

            return false;
        }

        private static string FormatFailures(IEnumerable<VfxValidationResult> results)
        {
            var messages = new List<string>();
            foreach (VfxValidationResult result in results)
            {
                if (result != null && !result.passed)
                {
                    messages.Add($"{result.ruleId}: {result.message}");
                }
            }

            return string.Join(Environment.NewLine, messages);
        }
    }
}
