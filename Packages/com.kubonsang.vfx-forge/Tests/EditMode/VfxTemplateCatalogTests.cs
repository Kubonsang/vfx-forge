using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

namespace Kubonsang.VfxForge.Editor.Tests
{
    public sealed class VfxTemplateCatalogTests
    {
        private string testAssetRoot;
        private VfxTemplateCatalog catalog;

        [SetUp]
        public void SetUp()
        {
            string folderName = $"__VfxForgeCatalogTests_{Guid.NewGuid():N}";
            AssetDatabase.CreateFolder("Assets", folderName);
            testAssetRoot = $"Assets/{folderName}";
            catalog = ScriptableObject.CreateInstance<VfxTemplateCatalog>();
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
        public void TryRegister_FiveUniqueTemplateIds_RegistersAll()
        {
            for (int index = 0; index < 5; index++)
            {
                VfxTemplateEntry entry = CreateValidEntry($"template_{index}", $"Template{index}");

                bool registered = catalog.TryRegister(entry, out List<VfxValidationResult> results);

                Assert.That(registered, Is.True, FormatFailures(results));
            }

            Assert.That(catalog.templates, Has.Count.EqualTo(5));
            Assert.That(
                VfxRecipeValidator.HasErrors(VfxTemplateCatalogValidator.Validate(catalog)),
                Is.False);
        }

        [Test]
        public void TryRegister_InvalidEntry_DoesNotMutateCatalog()
        {
            var entry = new VfxTemplateEntry { id = "invalid_template" };

            bool registered = catalog.TryRegister(entry, out List<VfxValidationResult> results);

            Assert.That(registered, Is.False);
            Assert.That(catalog.templates, Is.Empty);
            Assert.That(HasError(results, "CATALOG-PREFAB"), Is.True);
        }

        [Test]
        public void TryRegister_NullBackingList_InitializesAndRegisters()
        {
            catalog.templates = null;
            VfxTemplateEntry entry = CreateValidEntry("impact_core", "RecoveredList");

            bool registered = catalog.TryRegister(entry, out List<VfxValidationResult> results);

            Assert.That(registered, Is.True, FormatFailures(results));
            Assert.That(catalog.templates, Has.Count.EqualTo(1));
        }

        [Test]
        public void TryRegister_DuplicateId_DoesNotAddSecondEntry()
        {
            VfxTemplateEntry first = CreateValidEntry("impact_core", "First");
            VfxTemplateEntry duplicate = CreateValidEntry("impact_core", "Second");
            Assert.That(catalog.TryRegister(first, out _), Is.True);

            bool registered = catalog.TryRegister(
                duplicate,
                out List<VfxValidationResult> results);

            Assert.That(registered, Is.False);
            Assert.That(catalog.templates, Has.Count.EqualTo(1));
            Assert.That(HasError(results, "CATALOG-ID-DUPLICATE"), Is.True);
        }

        [Test]
        public void Validate_ManuallyDuplicatedIds_ReturnsStableRuleId()
        {
            catalog.templates.Add(CreateValidEntry("impact_core", "First"));
            catalog.templates.Add(CreateValidEntry("impact_core", "Second"));

            List<VfxValidationResult> results = VfxTemplateCatalogValidator.Validate(catalog);

            Assert.That(HasError(results, "CATALOG-ID-DUPLICATE"), Is.True);
        }

        [Test]
        public void Validate_SceneObjectInsteadOfPrefab_ReturnsStableRuleId()
        {
            var sceneObject = new GameObject("SceneTemplate");
            sceneObject.AddComponent<VisualEffect>();
            try
            {
                var entry = new VfxTemplateEntry
                {
                    id = "scene_template",
                    prefab = sceneObject
                };

                List<VfxValidationResult> results =
                    VfxTemplateCatalogValidator.ValidateEntry(entry);

                Assert.That(HasError(results, "CATALOG-PREFAB-ASSET"), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sceneObject);
            }
        }

        [Test]
        public void Validate_UnsupportedRecipePath_ReturnsStableRuleId()
        {
            VfxTemplateEntry entry = CreateValidEntry("impact_core", "UnsupportedPath");
            entry.bindings.Add(new VfxPropertyBinding
            {
                recipePath = "unknown.value",
                exposedPropertyName = "Duration",
                propertyType = VfxPropertyType.Float
            });

            List<VfxValidationResult> results =
                VfxTemplateCatalogValidator.ValidateEntry(entry);

            Assert.That(HasError(results, "CATALOG-BINDING-PATH"), Is.True);
        }

        [Test]
        public void Validate_BindingTypeMismatch_ReturnsStableRuleId()
        {
            VfxTemplateEntry entry = CreateValidEntry("impact_core", "WrongType");
            entry.bindings.Add(new VfxPropertyBinding
            {
                recipePath = "timing.duration",
                exposedPropertyName = "Duration",
                propertyType = VfxPropertyType.Int
            });

            List<VfxValidationResult> results =
                VfxTemplateCatalogValidator.ValidateEntry(entry);

            Assert.That(HasError(results, "CATALOG-BINDING-TYPE"), Is.True);
        }

        [Test]
        public void Validate_OverlappingBindingTargets_ReturnsStableRuleId()
        {
            VfxTemplateEntry entry = CreateValidEntry("impact_core", "DuplicateBinding");
            entry.bindings.Add(CreateFloatBinding("timing.duration", "Duration", -1));
            entry.bindings.Add(CreateFloatBinding("timing.impact", "Duration", 0));

            List<VfxValidationResult> results =
                VfxTemplateCatalogValidator.ValidateEntry(entry);

            Assert.That(HasError(results, "CATALOG-BINDING-DUPLICATE"), Is.True);
        }

        [Test]
        public void Validate_InvalidComponentIndex_ReturnsStableRuleId()
        {
            VfxTemplateEntry entry = CreateValidEntry("impact_core", "ComponentIndex");
            entry.bindings.Add(CreateFloatBinding("timing.duration", "Duration", 1));

            List<VfxValidationResult> results =
                VfxTemplateCatalogValidator.ValidateEntry(entry);

            Assert.That(HasError(results, "CATALOG-BINDING-COMPONENT"), Is.True);
        }

        [Test]
        public void TryRegister_MissingRequiredExposedProperty_IsRejected()
        {
            VfxTemplateEntry entry = CreateValidEntry("impact_core", "MissingProperty");
            entry.bindings.Add(CreateFloatBinding("timing.duration", "MissingDuration", 0));

            bool registered = catalog.TryRegister(
                entry,
                out List<VfxValidationResult> results);

            Assert.That(registered, Is.False);
            Assert.That(catalog.templates, Is.Empty);
            Assert.That(HasError(results, "CATALOG-BINDING-PROPERTY"), Is.True);
        }

        [Test]
        public void TryRegister_MissingOptionalExposedProperty_IsAcceptedWithWarning()
        {
            VfxTemplateEntry entry = CreateValidEntry("impact_core", "OptionalProperty");
            VfxPropertyBinding binding =
                CreateFloatBinding("timing.duration", "MissingDuration", 0);
            binding.required = false;
            entry.bindings.Add(binding);

            bool registered = catalog.TryRegister(
                entry,
                out List<VfxValidationResult> results);

            Assert.That(registered, Is.True, FormatFailures(results));
            Assert.That(HasWarning(results, "CATALOG-BINDING-PROPERTY"), Is.True);
            Assert.That(catalog.templates, Has.Count.EqualTo(1));
        }

        [Test]
        public void Compile_InvalidCatalog_IsRejectedBeforeAssetWrite()
        {
            catalog.templates.Add(new VfxTemplateEntry { id = "impact_core" });
            var recipe = new VfxRecipe
            {
                id = "impact_recipe",
                template = "impact_core",
                outputPath = $"{testAssetRoot}/Generated.prefab"
            };

            VfxCompileResult result = VfxRecipeCompiler.Compile(
                recipe,
                "Assets/Recipes/impact_recipe.json",
                catalog);

            Assert.That(result.Success, Is.False);
            Assert.That(HasError(result.Results, "CATALOG-PREFAB"), Is.True);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<GameObject>(recipe.outputPath),
                Is.Null);
        }

        [Test]
        public void Validate_ReviewContextWithMissingReferences_ReturnsStableRuleId()
        {
            var source = new GameObject("Invalid Review Context");
            try
            {
                source.AddComponent<VfxReviewContext>();
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
                    source,
                    $"{testAssetRoot}/InvalidContext.prefab");
                catalog.reviewContexts.Add(
                    new VfxReviewContextEntry
                    {
                        id = "invalid_context",
                        prefab = prefab
                    });

                List<VfxValidationResult> results =
                    VfxTemplateCatalogValidator.Validate(catalog);

                Assert.That(
                    HasError(
                        results,
                        "CATALOG-CONTEXT-REFERENCES"),
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private VfxTemplateEntry CreateValidEntry(string id, string prefabName)
        {
            var source = new GameObject(prefabName);
            source.AddComponent<VisualEffect>();
            try
            {
                string path = $"{testAssetRoot}/{prefabName}.prefab";
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
                Assert.That(prefab, Is.Not.Null);
                return new VfxTemplateEntry
                {
                    id = id,
                    prefab = prefab
                };
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static VfxPropertyBinding CreateFloatBinding(
            string recipePath,
            string propertyName,
            int componentIndex)
        {
            return new VfxPropertyBinding
            {
                recipePath = recipePath,
                exposedPropertyName = propertyName,
                propertyType = VfxPropertyType.Float,
                componentIndex = componentIndex
            };
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
