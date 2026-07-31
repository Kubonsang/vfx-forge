using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.VFX;

namespace Kubonsang.VfxForge.Editor.Tests
{
    public sealed class VfxGameplayReviewCaptureTests
    {
        private string assetRoot;
        private string artifactRoot;
        private GameObject generatedPrefab;
        private GameObject contextPrefab;
        private VfxTemplateCatalog catalog;
        private VfxRecipe recipe;

        [SetUp]
        public void SetUp()
        {
            string suffix = Guid.NewGuid().ToString("N");
            string folderName = $"__VfxForgeReviewTests_{suffix}";
            AssetDatabase.CreateFolder("Assets", folderName);
            assetRoot = $"Assets/{folderName}";
            artifactRoot = Path.Combine(
                Path.GetTempPath(),
                $"vfx-forge-review-{suffix}");

            Material material = CreateMaterial();
            generatedPrefab = CreateGeneratedPrefab(material);
            contextPrefab = CreateContextPrefab(material);
            catalog = ScriptableObject.CreateInstance<VfxTemplateCatalog>();
            catalog.reviewContexts.Add(new VfxReviewContextEntry
            {
                id = "topdown_test",
                prefab = contextPrefab
            });
            recipe = CreateRecipe();
        }

        [TearDown]
        public void TearDown()
        {
            if (catalog != null)
            {
                UnityEngine.Object.DestroyImmediate(catalog);
            }
            if (!string.IsNullOrWhiteSpace(assetRoot)
                && AssetDatabase.IsValidFolder(assetRoot))
            {
                AssetDatabase.DeleteAsset(assetRoot);
            }
            if (!string.IsNullOrWhiteSpace(artifactRoot)
                && Directory.Exists(artifactRoot))
            {
                Directory.Delete(artifactRoot, true);
            }
        }

        [Test]
        public void Capture_TwoRunsProduceDeterministicOrderAndHashes()
        {
            Hash128 contextHashBefore =
                AssetDatabase.GetAssetDependencyHash(
                    AssetDatabase.GetAssetPath(contextPrefab));
            bool activeSceneDirtyBefore =
                EditorSceneManager.GetActiveScene().isDirty;

            VfxGameplayReviewResult first =
                RunReview(Path.Combine(artifactRoot, "first"));
            VfxGameplayReviewResult second =
                RunReview(Path.Combine(artifactRoot, "second"));

            Assert.That(first.Success, Is.True, first.Message);
            Assert.That(second.Success, Is.True, second.Message);
            Assert.That(first.ContextFramePaths, Has.Count.EqualTo(2));
            Assert.That(File.Exists(first.ContactSheetPath), Is.True);
            Assert.That(File.Exists(first.ManifestPath), Is.True);

            VfxReviewManifest firstManifest =
                JsonUtility.FromJson<VfxReviewManifest>(
                    File.ReadAllText(first.ManifestPath));
            VfxReviewManifest secondManifest =
                JsonUtility.FromJson<VfxReviewManifest>(
                    File.ReadAllText(second.ManifestPath));
            Assert.That(
                firstManifest.schemaVersion,
                Is.EqualTo("review-manifest-1.0"));
            Assert.That(firstManifest.frames, Has.Count.EqualTo(4));
            Assert.That(
                FrameKey(firstManifest.frames[0]),
                Is.EqualTo("0.05:isolated:top"));
            Assert.That(
                FrameKey(firstManifest.frames[1]),
                Is.EqualTo("0.05:context:topdown_test"));
            Assert.That(
                FrameKey(firstManifest.frames[2]),
                Is.EqualTo("0.1:isolated:top"));
            Assert.That(
                FrameKey(firstManifest.frames[3]),
                Is.EqualTo("0.1:context:topdown_test"));
            Assert.That(
                secondManifest.contactSheetSha256,
                Is.EqualTo(firstManifest.contactSheetSha256));
            Assert.That(
                secondManifest.isolatedCaptureManifestSha256,
                Is.EqualTo(firstManifest.isolatedCaptureManifestSha256));
            Assert.That(
                AssetDatabase.GetAssetDependencyHash(
                    AssetDatabase.GetAssetPath(contextPrefab)),
                Is.EqualTo(contextHashBefore));
            Assert.That(
                EditorSceneManager.GetActiveScene().isDirty,
                Is.EqualTo(activeSceneDirtyBefore));
            Assert.That(HasReviewPreviewScene(), Is.False);
        }

        [Test]
        public void Catalog_MissingContextReferenceReturnsStableRuleId()
        {
            recipe.capture.contexts =
                new[] { "not_registered" };

            var results =
                VfxTemplateCatalogValidator
                    .ValidateRequestedReviewContexts(
                        recipe,
                        catalog);

            Assert.That(
                results.Exists(
                    item => item.ruleId
                        == "CATALOG-CONTEXT-REFERENCE"
                        && !item.passed),
                Is.True);
        }

        private VfxGameplayReviewResult RunReview(string root)
        {
            string captureDirectory =
                Path.Combine(root, "capture");
            VfxPreviewOpenResult open =
                VfxPreviewSession.Open(generatedPrefab);
            Assert.That(open.Success, Is.True, open.Message);
            VfxFrameCaptureResult isolated;
            using (open.Session)
            {
                isolated = VfxFrameCapture.Capture(
                    open.Session,
                    recipe,
                    captureDirectory);
            }
            Assert.That(isolated.Success, Is.True, isolated.Message);

            return VfxGameplayReviewCapture.Capture(
                recipe,
                generatedPrefab,
                catalog,
                "OnPlay",
                isolated.ManifestPath,
                Path.Combine(root, "review"));
        }

        private Material CreateMaterial()
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
            AssetDatabase.CreateAsset(
                material,
                $"{assetRoot}/ReviewMaterial.mat");
            return material;
        }

        private GameObject CreateGeneratedPrefab(Material material)
        {
            var source = new GameObject("Generated Review Effect");
            try
            {
                VfxMetadata metadata =
                    source.AddComponent<VfxMetadata>();
                metadata.schemaVersion = "1.1";
                source.AddComponent<VisualEffect>();
                GameObject cube =
                    GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "Visible Effect";
                cube.transform.SetParent(source.transform, false);
                cube.transform.localPosition = Vector3.up;
                cube.GetComponent<Renderer>().sharedMaterial = material;
                UnityEngine.Object.DestroyImmediate(
                    cube.GetComponent<Collider>());
                return PrefabUtility.SaveAsPrefabAsset(
                    source,
                    $"{assetRoot}/Generated.prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private GameObject CreateContextPrefab(Material material)
        {
            var root = new GameObject("Topdown Review Context");
            try
            {
                var context = root.AddComponent<VfxReviewContext>();
                GameObject cameraObject =
                    new GameObject("Review Camera");
                cameraObject.transform.SetParent(root.transform, false);
                cameraObject.transform.localPosition =
                    new Vector3(0f, 10f, 0f);
                cameraObject.transform.localRotation =
                    Quaternion.LookRotation(
                        Vector3.down,
                        Vector3.forward);
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.orthographic = true;
                camera.orthographicSize = 4f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor =
                    new Color(0.08f, 0.08f, 0.08f, 1f);

                var anchor =
                    new GameObject("Effect Anchor").transform;
                anchor.SetParent(root.transform, false);
                var caster =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Capsule).transform;
                caster.name = "Caster";
                caster.SetParent(root.transform, false);
                caster.localPosition =
                    new Vector3(0f, 0f, -2f);
                caster.GetComponent<Renderer>().sharedMaterial =
                    material;
                var target =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Capsule).transform;
                target.name = "Target";
                target.SetParent(root.transform, false);
                target.localPosition =
                    new Vector3(0f, 0f, 2f);
                target.GetComponent<Renderer>().sharedMaterial =
                    material;

                context.reviewCamera = camera;
                context.effectAnchor = anchor;
                context.caster = caster;
                context.target = target;
                return PrefabUtility.SaveAsPrefabAsset(
                    root,
                    $"{assetRoot}/Context.prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private VfxRecipe CreateRecipe()
        {
            return new VfxRecipe
            {
                schemaVersion = "1.1",
                id = "review_capture",
                template = "impact_core",
                outputPath = $"{assetRoot}/Unused.prefab",
                timing =
                    new VfxTiming { duration = 0.1f },
                budget = new VfxBudget
                {
                    maxParticles = 100,
                    maxDuration = 1f,
                    maxBoundsRadius = 5f
                },
                capture = new VfxCaptureSettings
                {
                    duration = 0.1f,
                    frameTimes = new[] { 0.1f, 0.05f },
                    views = new[] { "top" },
                    contexts = new[] { "topdown_test" },
                    width = 64,
                    height = 64
                }
            };
        }

        private static string FrameKey(VfxReviewFrame frame)
        {
            return $"{frame.timeSeconds:0.##}:"
                + $"{frame.sourceKind}:{frame.sourceId}";
        }

        private static bool HasReviewPreviewScene()
        {
            foreach (VfxReviewContext context in
                Resources.FindObjectsOfTypeAll<VfxReviewContext>())
            {
                if (context != null
                    && EditorSceneManager.IsPreviewScene(
                        context.gameObject.scene))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
