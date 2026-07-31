using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Kubonsang.VfxForge.Editor.Tests
{
    public sealed class VfxForgeBatchEntryTests
    {
        private string recipePath;
        private string artifactPath;
        private VfxTemplateCatalog catalog;

        [SetUp]
        public void SetUp()
        {
            string suffix = Guid.NewGuid().ToString("N");
            recipePath = Path.Combine(
                Path.GetTempPath(),
                $"vfx-forge-batch-recipe-{suffix}.json");
            artifactPath = Path.Combine(
                Path.GetTempPath(),
                $"vfx-forge-batch-artifacts-{suffix}");
            File.WriteAllText(recipePath, "{\"schemaVersion\":\"1.0\"}");
            catalog = ScriptableObject.CreateInstance<VfxTemplateCatalog>();
        }

        [TearDown]
        public void TearDown()
        {
            if (catalog != null)
            {
                UnityEngine.Object.DestroyImmediate(catalog);
            }

            if (File.Exists(recipePath))
            {
                File.Delete(recipePath);
            }

            if (Directory.Exists(artifactPath))
            {
                Directory.Delete(artifactPath, true);
            }
        }

        [Test]
        public void Execute_MissingArguments_ReturnsArgumentsExitCode()
        {
            var command = new TestBatchCommand { Catalog = catalog };

            VfxForgeBatchResult result = command.Execute(new[] { "-batchmode" });

            Assert.That(result.exitCode, Is.EqualTo(10));
            Assert.That(result.failedStage, Is.EqualTo("Arguments"));
            Assert.That(command.PipelineCalls, Is.Zero);
        }

        [Test]
        public void Execute_DuplicateArgument_ReturnsArgumentsExitCode()
        {
            var command = new TestBatchCommand { Catalog = catalog };

            VfxForgeBatchResult result = command.Execute(new[]
            {
                "-recipe", recipePath,
                "-recipe", recipePath,
                "-templateCatalog", "Assets/Catalog.asset",
                "-artifactPath", artifactPath
            });

            Assert.That(result.exitCode, Is.EqualTo(10));
            StringAssert.Contains("Duplicate argument", result.message);
            Assert.That(command.PipelineCalls, Is.Zero);
        }

        [Test]
        public void Execute_MissingRecipeFile_ReturnsParseExitCode()
        {
            File.Delete(recipePath);
            var command = new TestBatchCommand { Catalog = catalog };

            VfxForgeBatchResult result = command.Execute(CreateArguments());

            Assert.That(result.exitCode, Is.EqualTo(20));
            Assert.That(result.failedStage, Is.EqualTo("ParseRecipe"));
            Assert.That(result.artifactPath, Is.EqualTo(Path.GetFullPath(artifactPath)));
            Assert.That(command.PipelineCalls, Is.Zero);
        }

        [Test]
        public void Execute_MissingCatalog_ReturnsInputValidationExitCode()
        {
            var command = new TestBatchCommand();

            VfxForgeBatchResult result = command.Execute(CreateArguments());

            Assert.That(result.exitCode, Is.EqualTo(30));
            Assert.That(result.failedStage, Is.EqualTo("ValidateInputs"));
            Assert.That(command.PipelineCalls, Is.Zero);
        }

        [Test]
        public void Execute_ValidArguments_ForwardsNormalizedRequestAndResultPaths()
        {
            string reportPath = Path.Combine(artifactPath, "validation.json");
            string manifestPath = Path.Combine(
                artifactPath,
                "capture",
                "capture-manifest.json");
            var command = new TestBatchCommand
            {
                Catalog = catalog,
                PipelineResult = new VfxForgePipelineRunResult
                {
                    Success = true,
                    Stage = VfxForgePipelineStage.Completed,
                    Recipe = new VfxRecipe { id = "batch_recipe" },
                    PrefabPath = "Assets/Generated/Batch.prefab",
                    ReportPath = reportPath,
                    CaptureManifestPath = manifestPath,
                    Message = "Run All completed.",
                    Results = new List<VfxValidationResult>
                    {
                        VfxValidationResult.Pass("TEST", "Passed.")
                    }
                }
            };

            VfxForgeBatchResult result = command.Execute(CreateArguments());

            Assert.That(result.exitCode, Is.Zero);
            Assert.That(result.status, Is.EqualTo("passed"));
            Assert.That(result.recipeId, Is.EqualTo("batch_recipe"));
            Assert.That(result.artifactPath, Is.EqualTo(Path.GetFullPath(artifactPath)));
            Assert.That(result.reportPath, Is.EqualTo(reportPath));
            Assert.That(result.generatedPrefab, Is.EqualTo("Assets/Generated/Batch.prefab"));
            Assert.That(result.captureManifest, Is.EqualTo(manifestPath));
            Assert.That(command.PipelineCalls, Is.EqualTo(1));
            Assert.That(command.Request.TemplateCatalog, Is.SameAs(catalog));
            Assert.That(
                command.Request.ArtifactDirectory,
                Is.EqualTo(Path.GetFullPath(artifactPath)));
            Assert.That(command.Request.RecipeJson, Does.Contain("schemaVersion"));
        }

        [Test]
        public void Execute_VisualReviewArgument_ForwardsNormalizedPath()
        {
            var command = new TestBatchCommand
            {
                Catalog = catalog,
                PipelineResult =
                    new VfxForgePipelineRunResult
                    {
                        Success = true,
                        ProductStatus = "passed"
                    }
            };
            string[] arguments = CreateArguments();
            var withReview =
                new List<string>(arguments)
                {
                    "-visualReview",
                    recipePath
                };

            VfxForgeBatchResult result =
                command.Execute(withReview.ToArray());

            Assert.That(result.exitCode, Is.Zero);
            Assert.That(
                command.Request.VisualReviewPath,
                Is.EqualTo(Path.GetFullPath(recipePath)));
        }

        [TestCase(
            VfxVisualReviewStatus.ReviewRequired,
            80)]
        [TestCase(
            VfxVisualReviewStatus.Rejected,
            81)]
        [TestCase(
            VfxVisualReviewStatus.ReviewStale,
            82)]
        [TestCase(
            VfxVisualReviewStatus.Accepted,
            0)]
        public void Execute_VisualReviewStatus_ReturnsStableExitCode(
            string productStatus,
            int expectedExitCode)
        {
            var command = new TestBatchCommand
            {
                Catalog = catalog,
                PipelineResult =
                    new VfxForgePipelineRunResult
                    {
                        Success = true,
                        ProductStatus = productStatus,
                        VisualReviewPath =
                            Path.Combine(
                                artifactPath,
                                "visual-review.json")
                    }
            };

            VfxForgeBatchResult result =
                command.Execute(CreateArguments());

            Assert.That(
                result.exitCode,
                Is.EqualTo(expectedExitCode));
            Assert.That(
                result.status,
                Is.EqualTo(productStatus));
            Assert.That(
                result.failedStage,
                expectedExitCode == 0
                    ? Is.Empty
                    : Is.EqualTo("VisualReview"));
        }

        [TestCase(VfxForgePipelineStage.ParseRecipe, 20)]
        [TestCase(VfxForgePipelineStage.ValidateInputs, 30)]
        [TestCase(VfxForgePipelineStage.CompilePrefab, 40)]
        [TestCase(VfxForgePipelineStage.ValidatePrefab, 50)]
        [TestCase(VfxForgePipelineStage.OpenPreview, 60)]
        [TestCase(VfxForgePipelineStage.CaptureFrames, 70)]
        [TestCase(VfxForgePipelineStage.WriteReport, 80)]
        public void MapExitCode_FailureStage_ReturnsStableCode(
            VfxForgePipelineStage failedStage,
            int expectedCode)
        {
            var pipelineResult = new VfxForgePipelineRunResult
            {
                FailedStage = failedStage
            };

            VfxForgeBatchExitCode result =
                VfxForgeBatchCommand.MapExitCode(pipelineResult);

            Assert.That((int)result, Is.EqualTo(expectedCode));
        }

        [Test]
        public void Execute_FailedPipeline_UsesFailedStageAndOneLineJson()
        {
            var command = new TestBatchCommand
            {
                Catalog = catalog,
                PipelineResult = new VfxForgePipelineRunResult
                {
                    FailedStage = VfxForgePipelineStage.CaptureFrames,
                    Recipe = new VfxRecipe { id = "failed_recipe" },
                    ReportPath = Path.Combine(artifactPath, "validation.json"),
                    Message = "Capture failed.\nSee validation report."
                }
            };

            VfxForgeBatchResult result = command.Execute(CreateArguments());
            string json = result.ToJson();

            Assert.That(result.exitCode, Is.EqualTo(70));
            Assert.That(result.failedStage, Is.EqualTo("CaptureFrames"));
            Assert.That(result.status, Is.EqualTo("failed"));
            Assert.That(json, Does.Not.Contain("\n"));
            Assert.That(json, Does.Not.Contain("\r"));
            VfxForgeBatchResult roundTrip =
                JsonUtility.FromJson<VfxForgeBatchResult>(json);
            Assert.That(roundTrip.exitCode, Is.EqualTo(70));
            Assert.That(roundTrip.recipeId, Is.EqualTo("failed_recipe"));
        }

        [Test]
        public void Execute_UnexpectedPipelineException_ReturnsUnexpectedExitCode()
        {
            var command = new TestBatchCommand
            {
                Catalog = catalog,
                ThrowFromPipeline = true
            };

            VfxForgeBatchResult result = command.Execute(CreateArguments());

            Assert.That(result.exitCode, Is.EqualTo(90));
            Assert.That(result.failedStage, Is.EqualTo("Unexpected"));
        }

        private string[] CreateArguments()
        {
            return new[]
            {
                "-batchmode",
                "-recipe", recipePath,
                "-templateCatalog", "Assets/Catalog.asset",
                "-artifactPath", artifactPath,
                "-logFile", "-"
            };
        }

        private sealed class TestBatchCommand : VfxForgeBatchCommand
        {
            public VfxTemplateCatalog Catalog;
            public VfxForgePipelineRunResult PipelineResult;
            public VfxForgePipelineRequest Request;
            public bool ThrowFromPipeline;
            public int PipelineCalls;

            protected override VfxTemplateCatalog LoadCatalog(string assetPath)
            {
                return Catalog;
            }

            protected override VfxForgePipelineRunResult RunPipeline(
                VfxForgePipelineRequest request)
            {
                PipelineCalls++;
                Request = request;
                if (ThrowFromPipeline)
                {
                    throw new InvalidOperationException("Test pipeline failure.");
                }
                return PipelineResult;
            }
        }
    }
}
