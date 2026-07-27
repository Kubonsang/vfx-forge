using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Kubonsang.VfxForge.Editor
{
    public static class VfxForgeBatchEntry
    {
        public static void Run()
        {
            int exitCode = 9;
            string recipeId = string.Empty;
            string reportPath = string.Empty;
            string status = "failed";

            try
            {
                Dictionary<string, string> args = ParseArgs(Environment.GetCommandLineArgs());
                if (!args.TryGetValue("-recipe", out string recipePath) || !args.TryGetValue("-artifactPath", out string artifactPath))
                {
                    Exit(1, status, recipeId, reportPath, "Required arguments: -recipe and -artifactPath");
                    return;
                }

                VfxRecipeParseResult parsed = VfxRecipeParser.ParseFile(ToAbsoluteProjectPath(recipePath));
                if (!parsed.Success)
                {
                    Exit(1, status, recipeId, reportPath, parsed.Error);
                    return;
                }

                recipeId = parsed.Recipe.id;
                List<VfxValidationResult> results = VfxRecipeValidator.Validate(parsed.Recipe);
                if (VfxRecipeValidator.HasErrors(results))
                {
                    reportPath = VfxReportWriter.Write(ToAbsoluteProjectPath(artifactPath), parsed.Recipe, string.Empty, results);
                    Exit(2, "failed", recipeId, reportPath, "Recipe validation failed.");
                    return;
                }

                VfxTemplateCatalog catalog = ResolveCatalog(args);
                if (catalog == null)
                {
                    Exit(3, status, recipeId, reportPath, "Template Catalog not found.");
                    return;
                }

                results.Clear();
                VfxCompileResult compile = VfxRecipeCompiler.Compile(parsed.Recipe, recipePath, catalog);
                results.AddRange(compile.Results);
                if (compile.Success
                    && catalog.TryGet(parsed.Recipe.template, out VfxTemplateEntry template))
                {
                    results.AddRange(VfxValidationPipeline.Run(new VfxValidationContext
                    {
                        Recipe = parsed.Recipe,
                        Prefab = compile.Prefab,
                        Template = template,
                        AssetPath = compile.PrefabPath
                    }));
                }

                reportPath = VfxReportWriter.Write(ToAbsoluteProjectPath(artifactPath), parsed.Recipe, compile.PrefabPath, results);
                status = VfxReportWriter.ResolveStatus(results);
                exitCode = compile.Success && status != "failed" ? 0 : 4;
                Exit(exitCode, status, recipeId, reportPath, compile.Success ? "Compile finished." : "Compile failed.");
            }
            catch (Exception exception)
            {
                Exit(exitCode, status, recipeId, reportPath, exception.ToString());
            }
        }

        private static VfxTemplateCatalog ResolveCatalog(IReadOnlyDictionary<string, string> args)
        {
            if (args.TryGetValue("-templateCatalog", out string explicitPath))
            {
                return AssetDatabase.LoadAssetAtPath<VfxTemplateCatalog>(explicitPath);
            }

            string[] guids = AssetDatabase.FindAssets("t:VfxTemplateCatalog");
            return guids.Length == 1
                ? AssetDatabase.LoadAssetAtPath<VfxTemplateCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }

        private static Dictionary<string, string> ParseArgs(string[] raw)
        {
            var parsed = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int index = 0; index < raw.Length - 1; index++)
            {
                if (raw[index].StartsWith("-", StringComparison.Ordinal))
                {
                    parsed[raw[index]] = raw[index + 1];
                }
            }
            return parsed;
        }

        private static string ToAbsoluteProjectPath(string path)
        {
            if (Path.IsPathRooted(path))
            {
                return path;
            }
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
        }

        private static void Exit(int code, string status, string recipeId, string reportPath, string message)
        {
            var line = new BatchResult
            {
                status = status,
                exitCode = code,
                recipeId = recipeId,
                reportPath = reportPath,
                message = message
            };
            Debug.Log($"[VFXForge] {JsonUtility.ToJson(line)}");
            EditorApplication.Exit(code);
        }

        [Serializable]
        private sealed class BatchResult
        {
            public string status = "failed";
            public int exitCode;
            public string recipeId = string.Empty;
            public string reportPath = string.Empty;
            public string message = string.Empty;
        }
    }
}
