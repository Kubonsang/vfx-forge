using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Kubonsang.VfxForge.Editor
{
    public enum VfxForgePipelineStage
    {
        Idle,
        ParseRecipe,
        ValidateInputs,
        CompilePrefab,
        ValidatePrefab,
        OpenPreview,
        CaptureFrames,
        WriteReport,
        Completed,
        Failed
    }

    public sealed class VfxForgePipelineProgress
    {
        public VfxForgePipelineStage Stage;
        public float NormalizedProgress;
        public string Message = string.Empty;
    }

    public sealed class VfxForgePipelineRequest
    {
        public string RecipeJson = string.Empty;
        public string RecipeAssetPath = string.Empty;
        public VfxTemplateCatalog TemplateCatalog;
        public string ArtifactDirectory = string.Empty;
        public VfxOverwritePolicy OverwritePolicy =
            VfxOverwritePolicy.OverwriteGeneratedOnly;
    }

    public sealed class VfxForgePipelineRunResult
    {
        public bool Success;
        public VfxForgePipelineStage Stage = VfxForgePipelineStage.Idle;
        public VfxForgePipelineStage FailedStage = VfxForgePipelineStage.Idle;
        public string Message = string.Empty;
        public VfxRecipe Recipe;
        public GameObject Prefab;
        public string PrefabPath = string.Empty;
        public string ReportPath = string.Empty;
        public string CaptureManifestPath = string.Empty;
        public string ReviewManifestPath = string.Empty;
        public string ContactSheetPath = string.Empty;
        public List<string> CaptureFramePaths = new List<string>();
        public List<string> ContextFramePaths = new List<string>();
        public List<VfxValidationResult> Results = new List<VfxValidationResult>();
        public List<VfxForgePipelineProgress> Progress =
            new List<VfxForgePipelineProgress>();
    }

    public class VfxForgePipelineRunner
    {
        public VfxForgePipelineRunResult Run(
            VfxForgePipelineRequest request,
            Action<VfxForgePipelineProgress> progressCallback = null)
        {
            var result = new VfxForgePipelineRunResult();
            VfxForgePipelineStage activeStage = VfxForgePipelineStage.Idle;

            try
            {
                if (request == null)
                {
                    result.Results.Add(VfxValidationResult.Error(
                        "PIPELINE-REQUEST",
                        "Pipeline request is null."));
                    return FinishFailure(
                        result,
                        VfxForgePipelineStage.ValidateInputs,
                        "Pipeline request is invalid.",
                        null,
                        progressCallback);
                }

                activeStage = VfxForgePipelineStage.ParseRecipe;
                Publish(
                    result,
                    activeStage,
                    0.05f,
                    "Parsing Recipe.",
                    progressCallback);
                VfxRecipeParseResult parsed = ParseRecipe(request.RecipeJson);
                if (!parsed.Success)
                {
                    result.Results.Add(VfxValidationResult.Error(
                        string.IsNullOrWhiteSpace(parsed.ErrorCode)
                            ? "PIPELINE-PARSE"
                            : parsed.ErrorCode,
                        parsed.Error));
                    return FinishFailure(
                        result,
                        activeStage,
                        "Recipe parsing failed.",
                        request,
                        progressCallback);
                }
                result.Recipe = parsed.Recipe;

                activeStage = VfxForgePipelineStage.ValidateInputs;
                Publish(
                    result,
                    activeStage,
                    0.15f,
                    "Validating Recipe and Template Catalog.",
                    progressCallback);
                result.Results.AddRange(ValidateInputs(
                    result.Recipe,
                    request.TemplateCatalog,
                    request.ArtifactDirectory));
                if (VfxRecipeValidator.HasErrors(result.Results))
                {
                    return FinishFailure(
                        result,
                        activeStage,
                        "Input validation failed.",
                        request,
                        progressCallback);
                }

                if (!request.TemplateCatalog.TryGet(
                    result.Recipe.template,
                    out VfxTemplateEntry template))
                {
                    result.Results.Add(VfxValidationResult.Error(
                        "PIPELINE-TEMPLATE",
                        $"Template not found: {result.Recipe.template}"));
                    return FinishFailure(
                        result,
                        activeStage,
                        "Template resolution failed.",
                        request,
                        progressCallback);
                }

                activeStage = VfxForgePipelineStage.CompilePrefab;
                Publish(
                    result,
                    activeStage,
                    0.35f,
                    "Compiling generated Prefab.",
                    progressCallback);
                VfxCompileResult compile = Compile(
                    result.Recipe,
                    request.RecipeAssetPath,
                    request.TemplateCatalog,
                    request.OverwritePolicy);
                if (compile?.Results != null)
                {
                    result.Results.AddRange(compile.Results);
                }
                if (compile == null || !compile.Success || compile.Prefab == null)
                {
                    if (!VfxRecipeValidator.HasErrors(result.Results))
                    {
                        result.Results.Add(VfxValidationResult.Error(
                            "PIPELINE-COMPILE",
                            "Compiler did not produce a generated Prefab."));
                    }
                    return FinishFailure(
                        result,
                        activeStage,
                        "Prefab compilation failed.",
                        request,
                        progressCallback);
                }
                result.Prefab = compile.Prefab;
                result.PrefabPath = compile.PrefabPath;

                activeStage = VfxForgePipelineStage.ValidatePrefab;
                Publish(
                    result,
                    activeStage,
                    0.55f,
                    "Validating generated Prefab.",
                    progressCallback);
                result.Results.AddRange(ValidateGenerated(
                    result.Recipe,
                    result.Prefab,
                    result.PrefabPath,
                    template));
                if (VfxRecipeValidator.HasErrors(result.Results))
                {
                    return FinishFailure(
                        result,
                        activeStage,
                        "Generated Prefab validation failed.",
                        request,
                        progressCallback);
                }

                activeStage = VfxForgePipelineStage.OpenPreview;
                Publish(
                    result,
                    activeStage,
                    0.7f,
                    "Opening isolated Preview.",
                    progressCallback);
                VfxPreviewOpenResult open =
                    OpenPreview(result.Prefab, template.playEventName);
                if (open == null || !open.Success || open.Session == null)
                {
                    string errorCode = open == null || string.IsNullOrWhiteSpace(open.ErrorCode)
                        ? "PIPELINE-PREVIEW"
                        : open.ErrorCode;
                    string message = open?.Message ?? "Preview returned no result.";
                    result.Results.Add(VfxValidationResult.Error(errorCode, message));
                    return FinishFailure(
                        result,
                        activeStage,
                        "Preview bootstrap failed.",
                        request,
                        progressCallback);
                }

                VfxFrameCaptureResult capture;
                using (open.Session)
                {
                    activeStage = VfxForgePipelineStage.CaptureFrames;
                    Publish(
                        result,
                        activeStage,
                        0.85f,
                        "Capturing requested frames.",
                        progressCallback);
                    capture = Capture(
                        open.Session,
                        result.Recipe,
                        Path.Combine(request.ArtifactDirectory, "capture"));
                }

                if (capture == null || !capture.Success)
                {
                    string errorCode =
                        capture == null || string.IsNullOrWhiteSpace(capture.ErrorCode)
                            ? "PIPELINE-CAPTURE"
                            : capture.ErrorCode;
                    string message = capture?.Message ?? "Capture returned no result.";
                    result.Results.Add(VfxValidationResult.Error(errorCode, message));
                    return FinishFailure(
                        result,
                        activeStage,
                        "Frame capture failed.",
                        request,
                        progressCallback);
                }

                result.CaptureManifestPath = capture.ManifestPath;
                result.CaptureFramePaths.AddRange(capture.FramePaths);
                result.Results.Add(VfxValidationResult.Pass(
                    "CAPTURE-WRITE",
                    $"Captured {capture.FramePaths.Count} frame(s)."));

                if (result.Recipe.capture.contexts != null
                    && result.Recipe.capture.contexts.Length > 0)
                {
                    VfxGameplayReviewResult review = CaptureReview(
                        result.Recipe,
                        result.Prefab,
                        request.TemplateCatalog,
                        template.playEventName,
                        result.CaptureManifestPath,
                        Path.Combine(
                            request.ArtifactDirectory,
                            "review"));
                    if (review == null || !review.Success)
                    {
                        string errorCode =
                            review == null
                                || string.IsNullOrWhiteSpace(review.ErrorCode)
                                ? "PIPELINE-REVIEW"
                                : review.ErrorCode;
                        string message =
                            review?.Message
                            ?? "Gameplay Review capture returned no result.";
                        result.Results.Add(
                            VfxValidationResult.Error(
                                errorCode,
                                message));
                        return FinishFailure(
                            result,
                            activeStage,
                            "Gameplay Review capture failed.",
                            request,
                            progressCallback);
                    }

                    result.ReviewManifestPath = review.ManifestPath;
                    result.ContactSheetPath = review.ContactSheetPath;
                    result.ContextFramePaths.AddRange(
                        review.ContextFramePaths);
                    result.Results.Add(VfxValidationResult.Pass(
                        "REVIEW-WRITE",
                        $"Captured {review.ContextFramePaths.Count} "
                        + "gameplay frame(s) and Contact Sheet."));
                }

                activeStage = VfxForgePipelineStage.WriteReport;
                Publish(
                    result,
                    activeStage,
                    0.95f,
                    "Writing validation report.",
                    progressCallback);
                result.ReportPath = WriteVerifiedReport(
                    request.ArtifactDirectory,
                    result.Recipe,
                    result.PrefabPath,
                    result.Results);

                result.Success = true;
                result.Stage = VfxForgePipelineStage.Completed;
                result.Message = "Run All completed.";
                Publish(
                    result,
                    VfxForgePipelineStage.Completed,
                    1f,
                    result.Message,
                    progressCallback);
                return result;
            }
            catch (Exception exception)
            {
                result.Results.Add(VfxValidationResult.Error(
                    "PIPELINE-UNEXPECTED",
                    exception.Message));
                return FinishFailure(
                    result,
                    activeStage == VfxForgePipelineStage.Idle
                        ? VfxForgePipelineStage.ValidateInputs
                        : activeStage,
                    "Pipeline threw an unexpected exception.",
                    request,
                    progressCallback);
            }
        }

        protected virtual VfxRecipeParseResult ParseRecipe(string json)
        {
            return VfxRecipeParser.ParseJson(json);
        }

        protected virtual List<VfxValidationResult> ValidateInputs(
            VfxRecipe recipe,
            VfxTemplateCatalog catalog,
            string artifactDirectory)
        {
            var results = VfxRecipeValidator.Validate(recipe);
            results.AddRange(VfxTemplateCatalogValidator.Validate(catalog));
            results.AddRange(
                VfxTemplateCatalogValidator.ValidateRequestedReviewContexts(
                    recipe,
                    catalog));

            if (string.IsNullOrWhiteSpace(artifactDirectory))
            {
                results.Add(VfxValidationResult.Error(
                    "PIPELINE-ARTIFACT-PATH",
                    "Artifact directory is required."));
            }
            else
            {
                try
                {
                    Path.GetFullPath(artifactDirectory);
                }
                catch (Exception exception)
                {
                    results.Add(VfxValidationResult.Error(
                        "PIPELINE-ARTIFACT-PATH",
                        exception.Message));
                }
            }

            return results;
        }

        protected virtual VfxCompileResult Compile(
            VfxRecipe recipe,
            string recipeAssetPath,
            VfxTemplateCatalog catalog,
            VfxOverwritePolicy overwritePolicy)
        {
            return VfxRecipeCompiler.Compile(
                recipe,
                recipeAssetPath,
                catalog,
                overwritePolicy);
        }

        protected virtual List<VfxValidationResult> ValidateGenerated(
            VfxRecipe recipe,
            GameObject prefab,
            string prefabPath,
            VfxTemplateEntry template)
        {
            return VfxValidationPipeline.Run(new VfxValidationContext
            {
                Recipe = recipe,
                Prefab = prefab,
                Template = template,
                AssetPath = prefabPath
            });
        }

        protected virtual VfxPreviewOpenResult OpenPreview(
            GameObject prefab,
            string playEventName)
        {
            return VfxPreviewSession.Open(prefab, playEventName);
        }

        protected virtual VfxFrameCaptureResult Capture(
            VfxPreviewSession session,
            VfxRecipe recipe,
            string captureDirectory)
        {
            return VfxFrameCapture.Capture(session, recipe, captureDirectory);
        }

        protected virtual VfxGameplayReviewResult CaptureReview(
            VfxRecipe recipe,
            GameObject prefab,
            VfxTemplateCatalog catalog,
            string playEventName,
            string isolatedManifestPath,
            string reviewDirectory)
        {
            return VfxGameplayReviewCapture.Capture(
                recipe,
                prefab,
                catalog,
                playEventName,
                isolatedManifestPath,
                reviewDirectory);
        }

        protected virtual string WriteReport(
            string artifactDirectory,
            VfxRecipe recipe,
            string prefabPath,
            List<VfxValidationResult> results)
        {
            return VfxReportWriter.Write(
                artifactDirectory,
                recipe,
                prefabPath,
                results);
        }

        private VfxForgePipelineRunResult FinishFailure(
            VfxForgePipelineRunResult result,
            VfxForgePipelineStage failedStage,
            string message,
            VfxForgePipelineRequest request,
            Action<VfxForgePipelineProgress> progressCallback)
        {
            if (result.Recipe != null
                && request != null
                && !string.IsNullOrWhiteSpace(request.ArtifactDirectory))
            {
                try
                {
                    Publish(
                        result,
                        VfxForgePipelineStage.WriteReport,
                        0.95f,
                        "Writing failure report.",
                        progressCallback);
                    result.ReportPath = WriteVerifiedReport(
                        request.ArtifactDirectory,
                        result.Recipe,
                        result.PrefabPath,
                        result.Results);
                }
                catch (Exception exception)
                {
                    result.Results.Add(VfxValidationResult.Error(
                        "PIPELINE-REPORT",
                        exception.Message));
                    message = $"{message} Failure report could not be written.";
                }
            }

            result.Success = false;
            result.FailedStage = failedStage;
            result.Stage = VfxForgePipelineStage.Failed;
            result.Message = message;
            Publish(
                result,
                VfxForgePipelineStage.Failed,
                1f,
                message,
                progressCallback);
            return result;
        }

        private string WriteVerifiedReport(
            string artifactDirectory,
            VfxRecipe recipe,
            string prefabPath,
            List<VfxValidationResult> results)
        {
            string path = WriteReport(
                artifactDirectory,
                recipe,
                prefabPath,
                results);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                throw new IOException("Validation report was not written.");
            }
            return path;
        }

        private static void Publish(
            VfxForgePipelineRunResult result,
            VfxForgePipelineStage stage,
            float normalizedProgress,
            string message,
            Action<VfxForgePipelineProgress> progressCallback)
        {
            var progress = new VfxForgePipelineProgress
            {
                Stage = stage,
                NormalizedProgress = Mathf.Clamp01(normalizedProgress),
                Message = message ?? string.Empty
            };
            result.Stage = stage;
            result.Progress.Add(progress);
            progressCallback?.Invoke(progress);
        }
    }
}
