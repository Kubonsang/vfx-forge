using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Kubonsang.VfxForge.Editor
{
    public enum VfxForgeBatchExitCode
    {
        Success = 0,
        Arguments = 10,
        ParseRecipe = 20,
        ValidateInputs = 30,
        CompilePrefab = 40,
        ValidatePrefab = 50,
        OpenPreview = 60,
        CaptureFrames = 70,
        WriteReport = 80,
        Unexpected = 90
    }

    [Serializable]
    public sealed class VfxForgeBatchResult
    {
        public string schemaVersion = "1.0";
        public string tool = "VFXForge";
        public string status = "failed";
        public int exitCode = (int)VfxForgeBatchExitCode.Unexpected;
        public string failedStage = string.Empty;
        public string recipeId = string.Empty;
        public string artifactPath = string.Empty;
        public string reportPath = string.Empty;
        public string generatedPrefab = string.Empty;
        public string captureManifest = string.Empty;
        public string reviewManifest = string.Empty;
        public string contactSheet = string.Empty;
        public string message = string.Empty;

        public string ToJson()
        {
            return JsonUtility.ToJson(this, false);
        }
    }

    public class VfxForgeBatchCommand
    {
        private const string RecipeArgument = "-recipe";
        private const string CatalogArgument = "-templateCatalog";
        private const string ArtifactArgument = "-artifactPath";

        private static readonly string[] RequiredArguments =
        {
            RecipeArgument,
            CatalogArgument,
            ArtifactArgument
        };

        public VfxForgeBatchResult Execute(string[] rawArguments)
        {
            string artifactPath = string.Empty;
            try
            {
                if (!TryParseArguments(
                    rawArguments,
                    out Dictionary<string, string> arguments,
                    out string argumentError))
                {
                    return Failure(
                        VfxForgeBatchExitCode.Arguments,
                        "Arguments",
                        argumentError);
                }

                string recipePath;
                string catalogPath;
                try
                {
                    recipePath = ToAbsolutePath(arguments[RecipeArgument]);
                    artifactPath = ToAbsolutePath(arguments[ArtifactArgument]);
                    catalogPath = ToProjectAssetPath(arguments[CatalogArgument]);
                }
                catch (Exception exception)
                {
                    return Failure(
                        VfxForgeBatchExitCode.Arguments,
                        "Arguments",
                        $"Argument path could not be resolved: {exception.Message}");
                }

                if (string.IsNullOrWhiteSpace(catalogPath))
                {
                    return Failure(
                        VfxForgeBatchExitCode.Arguments,
                        "Arguments",
                        "Template Catalog must be an asset inside the project Assets directory.",
                        artifactPath);
                }

                string recipeJson;
                try
                {
                    recipeJson = File.ReadAllText(recipePath);
                }
                catch (Exception)
                {
                    return Failure(
                        VfxForgeBatchExitCode.ParseRecipe,
                        VfxForgePipelineStage.ParseRecipe.ToString(),
                        $"Recipe file could not be read: {recipePath}",
                        artifactPath);
                }

                VfxTemplateCatalog catalog = LoadCatalog(catalogPath);
                if (catalog == null)
                {
                    return Failure(
                        VfxForgeBatchExitCode.ValidateInputs,
                        VfxForgePipelineStage.ValidateInputs.ToString(),
                        $"Template Catalog could not be loaded: {catalogPath}",
                        artifactPath);
                }

                VfxForgePipelineRunResult pipelineResult = RunPipeline(
                    new VfxForgePipelineRequest
                    {
                        RecipeJson = recipeJson,
                        RecipeAssetPath = arguments[RecipeArgument],
                        TemplateCatalog = catalog,
                        ArtifactDirectory = artifactPath
                    });
                return FromPipelineResult(pipelineResult, artifactPath);
            }
            catch (Exception exception)
            {
                return Failure(
                    VfxForgeBatchExitCode.Unexpected,
                    "Unexpected",
                    exception.Message,
                    artifactPath);
            }
        }

        public static VfxForgeBatchExitCode MapExitCode(
            VfxForgePipelineRunResult pipelineResult)
        {
            if (pipelineResult == null)
            {
                return VfxForgeBatchExitCode.Unexpected;
            }

            if (pipelineResult.Success)
            {
                return VfxForgeBatchExitCode.Success;
            }

            switch (pipelineResult.FailedStage)
            {
                case VfxForgePipelineStage.ParseRecipe:
                    return VfxForgeBatchExitCode.ParseRecipe;
                case VfxForgePipelineStage.ValidateInputs:
                    return VfxForgeBatchExitCode.ValidateInputs;
                case VfxForgePipelineStage.CompilePrefab:
                    return VfxForgeBatchExitCode.CompilePrefab;
                case VfxForgePipelineStage.ValidatePrefab:
                    return VfxForgeBatchExitCode.ValidatePrefab;
                case VfxForgePipelineStage.OpenPreview:
                    return VfxForgeBatchExitCode.OpenPreview;
                case VfxForgePipelineStage.CaptureFrames:
                    return VfxForgeBatchExitCode.CaptureFrames;
                case VfxForgePipelineStage.WriteReport:
                    return VfxForgeBatchExitCode.WriteReport;
                default:
                    return VfxForgeBatchExitCode.Unexpected;
            }
        }

        protected virtual VfxTemplateCatalog LoadCatalog(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<VfxTemplateCatalog>(assetPath);
        }

        protected virtual VfxForgePipelineRunResult RunPipeline(
            VfxForgePipelineRequest request)
        {
            return new VfxForgePipelineRunner().Run(request);
        }

        private static VfxForgeBatchResult FromPipelineResult(
            VfxForgePipelineRunResult pipelineResult,
            string artifactPath)
        {
            if (pipelineResult == null)
            {
                return Failure(
                    VfxForgeBatchExitCode.Unexpected,
                    "Unexpected",
                    "Pipeline returned no result.",
                    artifactPath);
            }

            VfxForgeBatchExitCode exitCode = MapExitCode(pipelineResult);
            return new VfxForgeBatchResult
            {
                status = pipelineResult.Success
                    ? VfxReportWriter.ResolveStatus(pipelineResult.Results)
                    : "failed",
                exitCode = (int)exitCode,
                failedStage = pipelineResult.Success
                    ? string.Empty
                    : exitCode == VfxForgeBatchExitCode.Unexpected
                        ? "Unexpected"
                        : pipelineResult.FailedStage.ToString(),
                recipeId = pipelineResult.Recipe?.id ?? string.Empty,
                artifactPath = artifactPath,
                reportPath = pipelineResult.ReportPath,
                generatedPrefab = pipelineResult.PrefabPath,
                captureManifest = pipelineResult.CaptureManifestPath,
                reviewManifest = pipelineResult.ReviewManifestPath,
                contactSheet = pipelineResult.ContactSheetPath,
                message = pipelineResult.Message
            };
        }

        private static VfxForgeBatchResult Failure(
            VfxForgeBatchExitCode exitCode,
            string failedStage,
            string message,
            string artifactPath = "")
        {
            return new VfxForgeBatchResult
            {
                exitCode = (int)exitCode,
                failedStage = failedStage ?? string.Empty,
                artifactPath = artifactPath ?? string.Empty,
                message = message ?? string.Empty
            };
        }

        private static bool TryParseArguments(
            string[] rawArguments,
            out Dictionary<string, string> arguments,
            out string error)
        {
            arguments = new Dictionary<string, string>(StringComparer.Ordinal);
            error = string.Empty;
            if (rawArguments == null)
            {
                error = RequiredArgumentMessage();
                return false;
            }

            for (int index = 0; index < rawArguments.Length; index++)
            {
                string token = rawArguments[index];
                if (!IsRequiredArgument(token))
                {
                    continue;
                }

                if (arguments.ContainsKey(token))
                {
                    error = $"Duplicate argument: {token}";
                    return false;
                }

                if (index + 1 >= rawArguments.Length
                    || string.IsNullOrWhiteSpace(rawArguments[index + 1])
                    || rawArguments[index + 1].StartsWith("-", StringComparison.Ordinal))
                {
                    error = $"Argument requires a value: {token}";
                    return false;
                }

                arguments.Add(token, rawArguments[++index]);
            }

            foreach (string required in RequiredArguments)
            {
                if (!arguments.ContainsKey(required))
                {
                    error = RequiredArgumentMessage();
                    return false;
                }
            }

            return true;
        }

        private static bool IsRequiredArgument(string value)
        {
            foreach (string required in RequiredArguments)
            {
                if (string.Equals(value, required, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private static string RequiredArgumentMessage()
        {
            return "Required arguments: -recipe, -templateCatalog, and -artifactPath.";
        }

        private static string ToAbsolutePath(string path)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.GetFullPath(
                Path.IsPathRooted(path)
                    ? path
                    : Path.Combine(projectRoot, path));
        }

        private static string ToProjectAssetPath(string path)
        {
            string assetsRoot = Path.GetFullPath(Application.dataPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string candidate = Path.GetFullPath(
                Path.IsPathRooted(path)
                    ? path
                    : Path.Combine(Path.GetDirectoryName(assetsRoot), path));
            string prefix = assetsRoot + Path.DirectorySeparatorChar;
            if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return $"Assets/{candidate.Substring(prefix.Length).Replace('\\', '/')}";
        }
    }

    public static class VfxForgeBatchEntry
    {
        public static void Run()
        {
            VfxForgeBatchResult result =
                new VfxForgeBatchCommand().Execute(Environment.GetCommandLineArgs());
            Console.Out.WriteLine(result.ToJson());
            Console.Out.Flush();
            EditorApplication.Exit(result.exitCode);
        }
    }
}
