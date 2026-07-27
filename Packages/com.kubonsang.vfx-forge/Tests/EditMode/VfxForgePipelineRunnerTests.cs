using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.VFX;

namespace Kubonsang.VfxForge.Editor.Tests
{
    public sealed class VfxForgePipelineRunnerTests
    {
        private string testAssetRoot;
        private string artifactRoot;
        private VfxTemplateCatalog catalog;
        private VfxRecipe recipe;

        [SetUp]
        public void SetUp()
        {
            string suffix = Guid.NewGuid().ToString("N");
            string folderName = $"__VfxForgePipelineTests_{suffix}";
            AssetDatabase.CreateFolder("Assets", folderName);
            testAssetRoot = $"Assets/{folderName}";
            artifactRoot = Path.Combine(
                Path.GetTempPath(),
                $"vfx-forge-pipeline-{suffix}");

            GameObject template =
                CreateTemplatePrefab($"{testAssetRoot}/Template.prefab");
            catalog = ScriptableObject.CreateInstance<VfxTemplateCatalog>();
            catalog.templates.Add(new VfxTemplateEntry
            {
                id = "impact_core",
                prefab = template,
                playEventName = "OnPlay"
            });
            recipe = CreateRecipe($"{testAssetRoot}/Generated/Result.prefab");
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

            if (!string.IsNullOrWhiteSpace(artifactRoot)
                && Directory.Exists(artifactRoot))
            {
                Directory.Delete(artifactRoot, true);
            }
        }

        [Test]
        public void Run_ValidRequest_ExecutesAllStagesAndReturnsNavigableResults()
        {
            var runner = new InstrumentedPipelineRunner
            {
                PassGeneratedValidation = true
            };
            var callbacks = new List<VfxForgePipelineProgress>();

            VfxForgePipelineRunResult result =
                runner.Run(CreateRequest(), callbacks.Add);

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(result.Stage, Is.EqualTo(VfxForgePipelineStage.Completed));
            Assert.That(result.FailedStage, Is.EqualTo(VfxForgePipelineStage.Idle));
            Assert.That(runner.CompileCalls, Is.EqualTo(1));
            Assert.That(runner.GeneratedValidationCalls, Is.EqualTo(1));
            Assert.That(runner.PreviewCalls, Is.EqualTo(1));
            Assert.That(runner.CaptureCalls, Is.EqualTo(1));
            Assert.That(runner.ReportCalls, Is.EqualTo(1));
            Assert.That(result.Prefab, Is.Not.Null);
            Assert.That(File.Exists(result.ReportPath), Is.True);
            Assert.That(File.Exists(result.CaptureManifestPath), Is.True);
            Assert.That(result.CaptureFramePaths, Has.Count.EqualTo(1));
            Assert.That(File.Exists(result.CaptureFramePaths[0]), Is.True);
            Assert.That(
                GetStages(result.Progress),
                Is.EqualTo(new[]
                {
                    VfxForgePipelineStage.ParseRecipe,
                    VfxForgePipelineStage.ValidateInputs,
                    VfxForgePipelineStage.CompilePrefab,
                    VfxForgePipelineStage.ValidatePrefab,
                    VfxForgePipelineStage.OpenPreview,
                    VfxForgePipelineStage.CaptureFrames,
                    VfxForgePipelineStage.WriteReport,
                    VfxForgePipelineStage.Completed
                }));
            AssertProgressIsMonotonic(callbacks);
            Assert.That(HasPreviewRoot(), Is.False);
        }

        [Test]
        public void Run_InvalidInputs_StopBeforeCompilePreviewAndCapture()
        {
            recipe.id = "INVALID ID";
            var runner = new InstrumentedPipelineRunner
            {
                PassGeneratedValidation = true
            };

            VfxForgePipelineRunResult result = runner.Run(CreateRequest());

            Assert.That(result.Success, Is.False);
            Assert.That(result.FailedStage, Is.EqualTo(VfxForgePipelineStage.ValidateInputs));
            Assert.That(runner.CompileCalls, Is.Zero);
            Assert.That(runner.GeneratedValidationCalls, Is.Zero);
            Assert.That(runner.PreviewCalls, Is.Zero);
            Assert.That(runner.CaptureCalls, Is.Zero);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<GameObject>(recipe.outputPath),
                Is.Null);
            Assert.That(File.Exists(result.ReportPath), Is.True);
            VfxValidationReport report = JsonUtility.FromJson<VfxValidationReport>(
                File.ReadAllText(result.ReportPath));
            Assert.That(report.status, Is.EqualTo("failed"));
        }

        [Test]
        public void Run_CompileFailureWithoutBackendError_StopsAndReportsFailure()
        {
            var runner = new InstrumentedPipelineRunner
            {
                PassGeneratedValidation = true,
                FailCompileWithoutResult = true
            };

            VfxForgePipelineRunResult result = runner.Run(CreateRequest());

            Assert.That(result.Success, Is.False);
            Assert.That(result.FailedStage, Is.EqualTo(VfxForgePipelineStage.CompilePrefab));
            Assert.That(runner.CompileCalls, Is.EqualTo(1));
            Assert.That(runner.GeneratedValidationCalls, Is.Zero);
            Assert.That(runner.PreviewCalls, Is.Zero);
            Assert.That(runner.CaptureCalls, Is.Zero);
            Assert.That(HasError(result.Results, "PIPELINE-COMPILE"), Is.True);
            VfxValidationReport report = JsonUtility.FromJson<VfxValidationReport>(
                File.ReadAllText(result.ReportPath));
            Assert.That(report.status, Is.EqualTo("failed"));
        }

        [Test]
        public void Run_GeneratedValidationFailure_StopsPreviewAndCapture()
        {
            var runner = new InstrumentedPipelineRunner();

            VfxForgePipelineRunResult result = runner.Run(CreateRequest());

            Assert.That(result.Success, Is.False);
            Assert.That(result.FailedStage, Is.EqualTo(VfxForgePipelineStage.ValidatePrefab));
            Assert.That(runner.CompileCalls, Is.EqualTo(1));
            Assert.That(runner.GeneratedValidationCalls, Is.EqualTo(1));
            Assert.That(runner.PreviewCalls, Is.Zero);
            Assert.That(runner.CaptureCalls, Is.Zero);
            Assert.That(result.Prefab, Is.Not.Null);
            Assert.That(Directory.Exists(Path.Combine(artifactRoot, "capture")), Is.False);
            Assert.That(File.Exists(result.ReportPath), Is.True);
            Assert.That(HasPreviewRoot(), Is.False);
        }

        [Test]
        public void Run_PreviewFailure_StopsCapture()
        {
            var runner = new InstrumentedPipelineRunner
            {
                PassGeneratedValidation = true,
                FailPreview = true
            };

            VfxForgePipelineRunResult result = runner.Run(CreateRequest());

            Assert.That(result.Success, Is.False);
            Assert.That(result.FailedStage, Is.EqualTo(VfxForgePipelineStage.OpenPreview));
            Assert.That(runner.PreviewCalls, Is.EqualTo(1));
            Assert.That(runner.CaptureCalls, Is.Zero);
            Assert.That(Directory.Exists(Path.Combine(artifactRoot, "capture")), Is.False);
            Assert.That(File.Exists(result.ReportPath), Is.True);
        }

        [Test]
        public void Run_NullRequest_ReturnsFailedProgressWithoutThrowing()
        {
            var runner = new InstrumentedPipelineRunner();

            VfxForgePipelineRunResult result = null;
            Assert.DoesNotThrow(() => result = runner.Run(null));

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Stage, Is.EqualTo(VfxForgePipelineStage.Failed));
            Assert.That(result.FailedStage, Is.EqualTo(VfxForgePipelineStage.ValidateInputs));
            Assert.That(runner.CompileCalls, Is.Zero);
            Assert.That(runner.ReportCalls, Is.Zero);
        }

        private VfxForgePipelineRequest CreateRequest()
        {
            return new VfxForgePipelineRequest
            {
                RecipeJson = JsonUtility.ToJson(recipe),
                RecipeAssetPath = $"{testAssetRoot}/recipe.json",
                TemplateCatalog = catalog,
                ArtifactDirectory = artifactRoot,
                OverwritePolicy = VfxOverwritePolicy.Fail
            };
        }

        private VfxRecipe CreateRecipe(string outputPath)
        {
            return new VfxRecipe
            {
                id = "pipeline_recipe",
                displayName = "Pipeline Recipe",
                template = "impact_core",
                outputPath = outputPath,
                timing = new VfxTiming { duration = 0.1f },
                budget = new VfxBudget
                {
                    maxParticles = 100,
                    maxDuration = 1f,
                    maxBoundsRadius = 5f
                },
                capture = new VfxCaptureSettings
                {
                    duration = 0.1f,
                    frameTimes = new[] { 0f },
                    views = new[] { "front" },
                    width = 64,
                    height = 64
                }
            };
        }

        private GameObject CreateTemplatePrefab(string path)
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
            AssetDatabase.CreateAsset(material, $"{testAssetRoot}/PipelineMaterial.mat");

            var source = new GameObject("Pipeline Template");
            try
            {
                source.AddComponent<VisualEffect>();
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(source.transform, false);
                cube.transform.localPosition = Vector3.up;
                cube.GetComponent<Renderer>().sharedMaterial = material;
                UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
                return PrefabUtility.SaveAsPrefabAsset(source, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static VfxForgePipelineStage[] GetStages(
            IEnumerable<VfxForgePipelineProgress> progress)
        {
            var stages = new List<VfxForgePipelineStage>();
            foreach (VfxForgePipelineProgress item in progress)
            {
                stages.Add(item.Stage);
            }
            return stages.ToArray();
        }

        private static void AssertProgressIsMonotonic(
            IEnumerable<VfxForgePipelineProgress> progress)
        {
            float prior = -1f;
            foreach (VfxForgePipelineProgress item in progress)
            {
                Assert.That(item.NormalizedProgress, Is.GreaterThanOrEqualTo(prior));
                prior = item.NormalizedProgress;
            }
            Assert.That(prior, Is.EqualTo(1f));
        }

        private static bool HasPreviewRoot()
        {
            foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
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

        private sealed class InstrumentedPipelineRunner : VfxForgePipelineRunner
        {
            public bool PassGeneratedValidation;
            public bool FailPreview;
            public bool FailCompileWithoutResult;
            public int CompileCalls;
            public int GeneratedValidationCalls;
            public int PreviewCalls;
            public int CaptureCalls;
            public int ReportCalls;

            protected override VfxCompileResult Compile(
                VfxRecipe candidate,
                string recipeAssetPath,
                VfxTemplateCatalog templateCatalog,
                VfxOverwritePolicy overwritePolicy)
            {
                CompileCalls++;
                if (FailCompileWithoutResult)
                {
                    return new VfxCompileResult();
                }
                return base.Compile(
                    candidate,
                    recipeAssetPath,
                    templateCatalog,
                    overwritePolicy);
            }

            protected override List<VfxValidationResult> ValidateGenerated(
                VfxRecipe candidate,
                GameObject prefab,
                string prefabPath,
                VfxTemplateEntry template)
            {
                GeneratedValidationCalls++;
                return PassGeneratedValidation
                    ? new List<VfxValidationResult>
                    {
                        VfxValidationResult.Pass(
                            "TEST-GENERATED",
                            "Generated fixture accepted.")
                    }
                    : base.ValidateGenerated(candidate, prefab, prefabPath, template);
            }

            protected override VfxPreviewOpenResult OpenPreview(
                GameObject prefab,
                string playEventName)
            {
                PreviewCalls++;
                return FailPreview
                    ? new VfxPreviewOpenResult
                    {
                        ErrorCode = "TEST-PREVIEW",
                        Message = "Preview failed by test."
                    }
                    : base.OpenPreview(prefab, playEventName);
            }

            protected override VfxFrameCaptureResult Capture(
                VfxPreviewSession session,
                VfxRecipe candidate,
                string captureDirectory)
            {
                CaptureCalls++;
                return base.Capture(session, candidate, captureDirectory);
            }

            protected override string WriteReport(
                string artifactDirectory,
                VfxRecipe candidate,
                string prefabPath,
                List<VfxValidationResult> results)
            {
                ReportCalls++;
                return base.WriteReport(
                    artifactDirectory,
                    candidate,
                    prefabPath,
                    results);
            }
        }
    }
}
