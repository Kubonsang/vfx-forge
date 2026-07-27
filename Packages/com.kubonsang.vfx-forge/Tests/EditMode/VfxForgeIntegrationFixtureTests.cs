using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.VFX;

namespace Kubonsang.VfxForge.Editor.Tests
{
    public sealed class VfxForgeIntegrationFixtureTests
    {
        private const string FixtureRoot =
            "Assets/__VfxForgeIntegrationFixture";
        private const string TemplatePath =
            FixtureRoot + "/Template.prefab";
        private const string CatalogPath =
            FixtureRoot + "/Catalog.asset";
        private const string MaterialPath =
            FixtureRoot + "/FixtureMaterial.mat";
        private const string RecipeRoot =
            "Packages/com.kubonsang.vfx-forge/Tests/Fixtures/Recipes";
        private const string ReadOnlyVfxAssetPath =
            "Packages/com.unity.visualeffectgraph/Editor/Templates/01_Minimal_System.vfx";

        private string artifactRoot;
        private VfxTemplateCatalog catalog;
        private GameObject templatePrefab;
        private TemplateSnapshot originalSnapshot;

        [SetUp]
        public void SetUp()
        {
            DeleteFixtureAssets();
            AssetDatabase.CreateFolder("Assets", "__VfxForgeIntegrationFixture");
            artifactRoot = Path.Combine(
                Path.GetTempPath(),
                $"vfx-forge-integration-{Guid.NewGuid():N}");
            Directory.CreateDirectory(artifactRoot);

            VisualEffectAsset visualEffectAsset =
                AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(
                    ReadOnlyVfxAssetPath);
            Assert.That(
                visualEffectAsset,
                Is.Not.Null,
                $"VFX Graph package fixture is missing: {ReadOnlyVfxAssetPath}");

            Material material = CreateMaterial();
            templatePrefab =
                CreateTemplatePrefab(visualEffectAsset, material);
            catalog = CreateCatalog(templatePrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            originalSnapshot = CaptureTemplateSnapshot();
        }

        [TearDown]
        public void TearDown()
        {
            DeleteFixtureAssets();
            if (!string.IsNullOrWhiteSpace(artifactRoot)
                && Directory.Exists(artifactRoot))
            {
                Directory.Delete(artifactRoot, true);
            }
        }

        [TestCase(
            "valid_front.json",
            0,
            "",
            "fixture_valid_front",
            "Assets/__VfxForgeIntegrationFixture/Generated/ValidFront.prefab",
            true)]
        [TestCase(
            "valid_side.json",
            0,
            "",
            "fixture_valid_side",
            "Assets/__VfxForgeIntegrationFixture/Generated/ValidSide.prefab",
            true)]
        [TestCase(
            "valid_top.json",
            0,
            "",
            "fixture_valid_top",
            "Assets/__VfxForgeIntegrationFixture/Generated/ValidTop.prefab",
            true)]
        [TestCase(
            "invalid_contract.json",
            20,
            "ParseRecipe",
            "",
            "Assets/__VfxForgeIntegrationFixture/Generated/InvalidContract.prefab",
            false)]
        [TestCase(
            "invalid_template.json",
            30,
            "ValidateInputs",
            "fixture_invalid_template",
            "Assets/__VfxForgeIntegrationFixture/Generated/InvalidTemplate.prefab",
            false)]
        public void BatchCommand_FiveRecipeFixtures_ReturnExpectedResultAndPreserveTemplate(
            string fixtureName,
            int expectedExitCode,
            string expectedFailedStage,
            string expectedRecipeId,
            string expectedOutputPath,
            bool expectedSuccess)
        {
            string recipePath = CopyRecipeFixture(fixtureName);
            var command = new VfxForgeBatchCommand();

            VfxForgeBatchResult result = command.Execute(new[]
            {
                "-batchmode",
                "-recipe", recipePath,
                "-templateCatalog", CatalogPath,
                "-artifactPath", artifactRoot
            });

            Assert.That(
                result.exitCode,
                Is.EqualTo(expectedExitCode),
                result.message);
            Assert.That(result.failedStage, Is.EqualTo(expectedFailedStage));
            Assert.That(result.recipeId, Is.EqualTo(expectedRecipeId));
            Assert.That(result.artifactPath, Is.EqualTo(Path.GetFullPath(artifactRoot)));
            Assert.That(result.status, Is.EqualTo(expectedSuccess ? "passed" : "failed"));

            if (expectedSuccess)
            {
                AssertSuccessfulArtifacts(result, expectedOutputPath);
            }
            else
            {
                Assert.That(
                    AssetDatabase.LoadAssetAtPath<GameObject>(expectedOutputPath),
                    Is.Null);
                Assert.That(
                    Directory.Exists(Path.Combine(artifactRoot, "capture")),
                    Is.False);
                if (expectedExitCode == (int)VfxForgeBatchExitCode.ValidateInputs)
                {
                    Assert.That(File.Exists(result.reportPath), Is.True);
                }
                else
                {
                    Assert.That(result.reportPath, Is.Empty);
                }
            }

            Assert.That(HasPreviewRoot(), Is.False);
            AssertTemplateUnchanged(originalSnapshot);
        }

        [Test]
        public void RecipeFixtureSet_ContainsExactlyThreeValidAndTwoInvalidFiles()
        {
            string[] expected =
            {
                "invalid_contract.json",
                "invalid_template.json",
                "valid_front.json",
                "valid_side.json",
                "valid_top.json"
            };
            var actual = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets(
                "t:TextAsset",
                new[] { RecipeRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".json", StringComparison.Ordinal))
                {
                    actual.Add(Path.GetFileName(path));
                }
            }
            actual.Sort(StringComparer.Ordinal);

            Assert.That(actual, Is.EqualTo(expected));
        }

        private static Material CreateMaterial()
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Sprites/Default");
            Assert.That(shader, Is.Not.Null);

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.cyan);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.cyan);
            }
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static GameObject CreateTemplatePrefab(
            VisualEffectAsset visualEffectAsset,
            Material material)
        {
            var source = new GameObject("Integration Template");
            try
            {
                VisualEffect effect = source.AddComponent<VisualEffect>();
                effect.visualEffectAsset = visualEffectAsset;

                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "Capture Reference";
                cube.transform.SetParent(source.transform, false);
                cube.transform.localPosition = Vector3.up;
                cube.GetComponent<Renderer>().sharedMaterial = material;
                UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());

                GameObject prefab =
                    PrefabUtility.SaveAsPrefabAsset(source, TemplatePath);
                Assert.That(prefab, Is.Not.Null);
                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static VfxTemplateCatalog CreateCatalog(GameObject template)
        {
            var created = ScriptableObject.CreateInstance<VfxTemplateCatalog>();
            created.templates.Add(new VfxTemplateEntry
            {
                id = "fixture_impact",
                prefab = template,
                playEventName = "OnPlay",
                supportedLayers = new[] { "core" },
                bindings = new List<VfxPropertyBinding>()
            });
            AssetDatabase.CreateAsset(created, CatalogPath);
            return created;
        }

        private string CopyRecipeFixture(string fixtureName)
        {
            string assetPath = $"{RecipeRoot}/{fixtureName}";
            TextAsset fixture =
                AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            Assert.That(fixture, Is.Not.Null, $"Recipe fixture is missing: {assetPath}");

            string copyPath = Path.Combine(artifactRoot, fixtureName);
            File.WriteAllText(copyPath, fixture.text);
            return copyPath;
        }

        private static void AssertSuccessfulArtifacts(
            VfxForgeBatchResult result,
            string expectedOutputPath)
        {
            Assert.That(result.generatedPrefab, Is.EqualTo(expectedOutputPath));
            GameObject generated =
                AssetDatabase.LoadAssetAtPath<GameObject>(expectedOutputPath);
            Assert.That(generated, Is.Not.Null);
            Assert.That(generated.GetComponent<VfxMetadata>(), Is.Not.Null);
            Assert.That(File.Exists(result.reportPath), Is.True);
            Assert.That(File.Exists(result.captureManifest), Is.True);
            Assert.That(
                Directory.GetFiles(
                    Path.GetDirectoryName(result.captureManifest),
                    "*.png",
                    SearchOption.TopDirectoryOnly),
                Has.Length.EqualTo(1));

            VfxValidationReport report =
                JsonUtility.FromJson<VfxValidationReport>(
                    File.ReadAllText(result.reportPath));
            Assert.That(report.status, Is.EqualTo("passed"));
            Assert.That(report.generatedPrefab, Is.EqualTo(expectedOutputPath));
        }

        private TemplateSnapshot CaptureTemplateSnapshot()
        {
            Assert.That(templatePrefab, Is.Not.Null);
            Assert.That(catalog, Is.Not.Null);
            return new TemplateSnapshot
            {
                TemplateFileHash = HashFile(ToAbsoluteAssetPath(TemplatePath)),
                TemplateDependencyHash =
                    AssetDatabase.GetAssetDependencyHash(TemplatePath).ToString(),
                VfxDependencyHash =
                    AssetDatabase.GetAssetDependencyHash(
                        ReadOnlyVfxAssetPath).ToString(),
                CatalogJson = EditorJsonUtility.ToJson(catalog, true)
            };
        }

        private void AssertTemplateUnchanged(TemplateSnapshot expected)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Assert.That(
                HashFile(ToAbsoluteAssetPath(TemplatePath)),
                Is.EqualTo(expected.TemplateFileHash));
            Assert.That(
                AssetDatabase.GetAssetDependencyHash(TemplatePath).ToString(),
                Is.EqualTo(expected.TemplateDependencyHash));
            Assert.That(
                AssetDatabase.GetAssetDependencyHash(
                    ReadOnlyVfxAssetPath).ToString(),
                Is.EqualTo(expected.VfxDependencyHash));
            Assert.That(
                EditorJsonUtility.ToJson(catalog, true),
                Is.EqualTo(expected.CatalogJson));
            Assert.That(templatePrefab.GetComponent<VfxMetadata>(), Is.Null);
        }

        private static string ToAbsoluteAssetPath(string assetPath)
        {
            string projectRoot =
                Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        private static string HashFile(string path)
        {
            using (SHA256 algorithm = SHA256.Create())
            {
                byte[] hash = algorithm.ComputeHash(File.ReadAllBytes(path));
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        private static bool HasPreviewRoot()
        {
            foreach (GameObject gameObject in
                Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (gameObject != null
                    && gameObject.name == VfxPreviewSession.PreviewRootName
                    && EditorSceneManager.IsPreviewScene(gameObject.scene))
                {
                    return true;
                }
            }
            return false;
        }

        private static void DeleteFixtureAssets()
        {
            if (AssetDatabase.IsValidFolder(FixtureRoot))
            {
                AssetDatabase.DeleteAsset(FixtureRoot);
            }
        }

        private sealed class TemplateSnapshot
        {
            public string TemplateFileHash;
            public string TemplateDependencyHash;
            public string VfxDependencyHash;
            public string CatalogJson;
        }
    }
}
