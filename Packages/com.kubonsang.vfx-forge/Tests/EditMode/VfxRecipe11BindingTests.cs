using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

namespace Kubonsang.VfxForge.Editor.Tests
{
    public sealed class VfxRecipe11BindingTests
    {
        private const string Recipe11Json =
            "{\"schemaVersion\":\"1.1\",\"id\":\"shield_primary\","
            + "\"template\":\"typed_binding\",\"outputPath\":\"Assets/VFXForge/Generated/Shield.prefab\","
            + "\"timing\":{\"duration\":1.8},"
            + "\"motion\":{\"speed\":11.0,\"localDirection\":{\"x\":0,\"y\":0,\"z\":1}},"
            + "\"geometry\":{\"variant\":\"round\"},"
            + "\"budget\":{\"maxParticles\":64,\"maxDuration\":2.0},"
            + "\"capture\":{\"contexts\":[\"topdown\"]},"
            + "\"quality\":{\"minimumForegroundRatio\":0.02,"
            + "\"maximumBorderForegroundRatio\":0.004,\"requireHumanReview\":true}}";

        private string testAssetRoot;
        private VfxTemplateCatalog catalog;
        private string templatePath;
        private Mesh roundMesh;
        private Mesh kiteMesh;
        private string materialColorProperty;

        [SetUp]
        public void SetUp()
        {
            string folderName = $"__VfxForgeRecipe11Tests_{Guid.NewGuid():N}";
            AssetDatabase.CreateFolder("Assets", folderName);
            testAssetRoot = $"Assets/{folderName}";
            templatePath = $"{testAssetRoot}/Template.prefab";
            roundMesh = CreateMesh("RoundMesh");
            kiteMesh = CreateMesh("KiteMesh");
            catalog = ScriptableObject.CreateInstance<VfxTemplateCatalog>();

            GameObject prefab = CreateTemplate();
            var entry = new VfxTemplateEntry
            {
                id = "typed_binding",
                prefab = prefab,
                meshVariants = new List<VfxMeshVariant>
                {
                    new VfxMeshVariant { key = "round", mesh = roundMesh },
                    new VfxMeshVariant { key = "kite", mesh = kiteMesh }
                },
                bindings = CreateBindings()
            };

            Assert.That(
                catalog.TryRegister(entry, out List<VfxValidationResult> results),
                Is.True,
                FormatFailures(results));
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
        public void ParseJson_Recipe11_ReadsNewFieldsAndDefaults()
        {
            VfxRecipeParseResult result =
                VfxRecipeParser.ParseJson(Recipe11Json);

            Assert.That(result.Success, Is.True, result.Error);
            Assert.That(result.Recipe.schemaVersion, Is.EqualTo("1.1"));
            Assert.That(result.Recipe.motion.speed, Is.EqualTo(11f));
            Assert.That(
                result.Recipe.motion.localDirection,
                Is.EqualTo(Vector3.forward));
            Assert.That(result.Recipe.geometry.variant, Is.EqualTo("round"));
            Assert.That(
                result.Recipe.capture.contexts,
                Is.EqualTo(new[] { "topdown" }));
            Assert.That(
                result.Recipe.quality.minimumForegroundRatio,
                Is.EqualTo(0.02f));
            Assert.That(result.Recipe.quality.requireHumanReview, Is.True);
        }

        [Test]
        public void Validate_Recipe10WithRecipe11Override_IsRejected()
        {
            string json = Recipe11Json.Replace(
                "\"schemaVersion\":\"1.1\"",
                "\"schemaVersion\":\"1.0\"");

            VfxRecipeParseResult result = VfxRecipeParser.ParseJson(json);

            Assert.That(result.Success, Is.True, result.Error);
            Assert.That(
                VfxRecipeValidator.HasErrors(
                    VfxRecipeValidator.Validate(result.Recipe)),
                Is.True);
        }

        [Test]
        public void Validate_Recipe11InvalidRanges_ReturnStableRuleIds()
        {
            VfxRecipe recipe = CreateRecipe(
                $"{testAssetRoot}/Invalid.prefab",
                "round",
                1f,
                1f);
            recipe.motion.speed = -1f;
            recipe.quality.minimumForegroundRatio = 1.1f;
            recipe.quality.maximumBorderForegroundRatio = -0.1f;

            List<VfxValidationResult> results =
                VfxRecipeValidator.Validate(recipe);

            Assert.That(HasError(results, "RECIPE-MOTION-SPEED"), Is.True);
            Assert.That(
                HasError(results, "RECIPE-QUALITY-FOREGROUND"),
                Is.True);
            Assert.That(HasError(results, "RECIPE-QUALITY-BORDER"), Is.True);
        }

        [Test]
        public void Validate_UnsafeTargetPath_IsRejected()
        {
            VfxTemplateEntry entry = catalog.templates[0];
            entry.bindings[0].targetPath = "../Visual";

            List<VfxValidationResult> results =
                VfxTemplateCatalogValidator.ValidateEntry(entry);

            Assert.That(
                HasError(results, "CATALOG-BINDING-TARGET"),
                Is.True);
        }

        [Test]
        public void Validate_AdapterTypeMismatch_IsRejected()
        {
            VfxTemplateEntry entry = catalog.templates[0];
            VfxPropertyBinding adapterBinding = entry.bindings[3];
            adapterBinding.propertyType = VfxPropertyType.Vector3;

            List<VfxValidationResult> results =
                VfxTemplateCatalogValidator.ValidateEntry(entry);

            Assert.That(
                HasError(results, "CATALOG-BINDING-TYPE"),
                Is.True);
        }

        [Test]
        public void Compile_PrimaryAndVariant_ApplyEveryTypedTargetWithoutSourceMutation()
        {
            Hash128 sourceHashBefore =
                AssetDatabase.GetAssetDependencyHash(templatePath);
            VfxRecipe primary = CreateRecipe(
                $"{testAssetRoot}/Primary.prefab",
                "round",
                2f,
                8f);
            VfxRecipe variant = CreateRecipe(
                $"{testAssetRoot}/Variant.prefab",
                "kite",
                1.25f,
                3f);
            variant.style.primaryColor = "#22AA66FF";
            variant.motion.localDirection = Vector3.right;

            VfxCompileResult primaryResult = VfxRecipeCompiler.Compile(
                primary,
                "Assets/Recipes/primary.json",
                catalog);
            VfxCompileResult variantResult = VfxRecipeCompiler.Compile(
                variant,
                "Assets/Recipes/variant.json",
                catalog);

            Assert.That(
                primaryResult.Success,
                Is.True,
                FormatFailures(primaryResult.Results));
            Assert.That(
                variantResult.Success,
                Is.True,
                FormatFailures(variantResult.Results));

            Transform primaryVisual =
                primaryResult.Prefab.transform.Find("Visual");
            Transform variantVisual =
                variantResult.Prefab.transform.Find("Visual");
            Assert.That(primaryVisual.localScale, Is.EqualTo(Vector3.one * 2f));
            Assert.That(
                variantVisual.localScale,
                Is.EqualTo(Vector3.one * 1.25f));
            Assert.That(
                primaryVisual.GetComponent<MeshFilter>().sharedMesh,
                Is.SameAs(roundMesh));
            Assert.That(
                variantVisual.GetComponent<MeshFilter>().sharedMesh,
                Is.SameAs(kiteMesh));

            VfxMotionBindingAdapter primaryMotion =
                primaryResult.Prefab.transform.Find("Motion")
                    .GetComponent<VfxMotionBindingAdapter>();
            VfxMotionBindingAdapter variantMotion =
                variantResult.Prefab.transform.Find("Motion")
                    .GetComponent<VfxMotionBindingAdapter>();
            Assert.That(primaryMotion.Speed, Is.EqualTo(8f));
            Assert.That(variantMotion.Speed, Is.EqualTo(3f));
            Assert.That(primaryMotion.LocalDirection, Is.EqualTo(Vector3.forward));
            Assert.That(variantMotion.LocalDirection, Is.EqualTo(Vector3.right));

            VfxMaterialPropertyOverrides primaryOverrides =
                primaryVisual.GetComponent<VfxMaterialPropertyOverrides>();
            VfxMaterialPropertyOverrides variantOverrides =
                variantVisual.GetComponent<VfxMaterialPropertyOverrides>();
            Assert.That(primaryOverrides, Is.Not.Null);
            Assert.That(variantOverrides, Is.Not.Null);
            Assert.That(primaryOverrides.Overrides, Has.Count.EqualTo(1));
            Assert.That(variantOverrides.Overrides, Has.Count.EqualTo(1));
            Assert.That(
                primaryOverrides.Overrides[0].colorValue,
                Is.Not.EqualTo(variantOverrides.Overrides[0].colorValue));

            Hash128 sourceHashAfter =
                AssetDatabase.GetAssetDependencyHash(templatePath);
            Assert.That(sourceHashAfter, Is.EqualTo(sourceHashBefore));
            Assert.That(
                catalog.templates[0].prefab.transform.Find("Visual")
                    .GetComponent<MeshFilter>().sharedMesh,
                Is.SameAs(roundMesh));
            Assert.That(
                catalog.templates[0].prefab.transform.Find("Visual")
                    .GetComponent<VfxMaterialPropertyOverrides>(),
                Is.Null);
        }

        private GameObject CreateTemplate()
        {
            var root = new GameObject("Typed Binding Template");
            try
            {
                root.AddComponent<VisualEffect>();
                var visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);
                MeshFilter filter = visual.AddComponent<MeshFilter>();
                filter.sharedMesh = roundMesh;
                MeshRenderer renderer = visual.AddComponent<MeshRenderer>();

                Shader shader =
                    Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Standard");
                Assert.That(shader, Is.Not.Null);
                var material = new Material(shader);
                materialColorProperty = material.HasProperty("_BaseColor")
                    ? "_BaseColor"
                    : "_Color";
                string materialPath = $"{testAssetRoot}/TemplateMaterial.mat";
                AssetDatabase.CreateAsset(material, materialPath);
                renderer.sharedMaterial = material;

                var motion = new GameObject("Motion");
                motion.transform.SetParent(root.transform, false);
                motion.AddComponent<VfxMotionBindingAdapter>();

                GameObject prefab =
                    PrefabUtility.SaveAsPrefabAsset(root, templatePath);
                Assert.That(prefab, Is.Not.Null);
                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private Mesh CreateMesh(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.vertices = new[]
            {
                Vector3.zero,
                Vector3.right,
                Vector3.up
            };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, $"{testAssetRoot}/{name}.asset");
            return mesh;
        }

        private List<VfxPropertyBinding> CreateBindings()
        {
            return new List<VfxPropertyBinding>
            {
                new VfxPropertyBinding
                {
                    recipePath = "shape.radius",
                    exposedPropertyName = "uniformScale",
                    propertyType = VfxPropertyType.Float,
                    targetKind = VfxBindingTargetKind.TransformProperty,
                    targetPath = "Visual"
                },
                new VfxPropertyBinding
                {
                    recipePath = "style.primaryColor",
                    exposedPropertyName = materialColorProperty,
                    propertyType = VfxPropertyType.Color,
                    targetKind = VfxBindingTargetKind.MaterialProperty,
                    targetPath = "Visual"
                },
                new VfxPropertyBinding
                {
                    recipePath = "geometry.variant",
                    exposedPropertyName = "sharedMesh",
                    propertyType = VfxPropertyType.String,
                    targetKind = VfxBindingTargetKind.MeshVariant,
                    targetPath = "Visual"
                },
                new VfxPropertyBinding
                {
                    recipePath = "motion.speed",
                    exposedPropertyName = "speed",
                    propertyType = VfxPropertyType.Float,
                    targetKind = VfxBindingTargetKind.AdapterProperty,
                    targetPath = "Motion",
                    adapterId = VfxMotionBindingAdapter.AdapterId
                },
                new VfxPropertyBinding
                {
                    recipePath = "motion.localDirection",
                    exposedPropertyName = "localDirection",
                    propertyType = VfxPropertyType.Vector3,
                    targetKind = VfxBindingTargetKind.AdapterProperty,
                    targetPath = "Motion",
                    adapterId = VfxMotionBindingAdapter.AdapterId
                }
            };
        }

        private VfxRecipe CreateRecipe(
            string outputPath,
            string geometryVariant,
            float radius,
            float speed)
        {
            return new VfxRecipe
            {
                schemaVersion = "1.1",
                id = $"typed_{Guid.NewGuid():N}".Substring(0, 20),
                template = "typed_binding",
                outputPath = outputPath,
                timing = new VfxTiming { duration = 1f },
                shape = new VfxShape { radius = radius },
                style = new VfxStyle { primaryColor = "#FFFFFFFF" },
                motion = new VfxMotion
                {
                    speed = speed,
                    localDirection = Vector3.forward
                },
                geometry = new VfxGeometry { variant = geometryVariant },
                budget = new VfxBudget
                {
                    maxParticles = 64,
                    maxDuration = 2f,
                    maxBoundsRadius = 5f
                }
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

        private static string FormatFailures(
            IEnumerable<VfxValidationResult> results)
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
