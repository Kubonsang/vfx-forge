using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Kubonsang.VfxForge.Editor
{
    [Serializable]
    public sealed class VfxReportItem
    {
        public string ruleId = string.Empty;
        public string severity = "Info";
        public bool passed;
        public string message = string.Empty;
        public string assetPath = string.Empty;
        public string propertyName = string.Empty;
    }

    [Serializable]
    public sealed class VfxValidationReport
    {
        public string schemaVersion = "1.0";
        public string recipeId = string.Empty;
        public string status = "failed";
        public string generatedPrefab = string.Empty;
        public string templateId = string.Empty;
        public List<VfxReportItem> results = new List<VfxReportItem>();
    }

    public static class VfxReportWriter
    {
        public static string Write(string artifactDirectory, VfxRecipe recipe, string prefabPath, List<VfxValidationResult> results)
        {
            Directory.CreateDirectory(artifactDirectory);
            var report = new VfxValidationReport
            {
                recipeId = recipe != null ? recipe.id : string.Empty,
                templateId = recipe != null ? recipe.template : string.Empty,
                generatedPrefab = prefabPath ?? string.Empty,
                results = ConvertResults(results)
            };
            report.status = ResolveStatus(results);
            string path = Path.Combine(artifactDirectory, "validation.json");
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
            return path;
        }

        public static string ResolveStatus(IEnumerable<VfxValidationResult> results)
        {
            bool warning = false;
            if (results == null)
            {
                return "passed";
            }

            foreach (VfxValidationResult result in results)
            {
                if (result == null || result.passed)
                {
                    continue;
                }

                if (result.severity == VfxValidationSeverity.Error)
                {
                    return "failed";
                }

                if (result.severity == VfxValidationSeverity.Warning)
                {
                    warning = true;
                }
            }

            return warning ? "warning" : "passed";
        }

        private static List<VfxReportItem> ConvertResults(IEnumerable<VfxValidationResult> results)
        {
            var converted = new List<VfxReportItem>();
            if (results == null)
            {
                return converted;
            }

            foreach (VfxValidationResult result in results)
            {
                if (result == null)
                {
                    continue;
                }

                converted.Add(new VfxReportItem
                {
                    ruleId = result.ruleId,
                    severity = result.severity.ToString(),
                    passed = result.passed,
                    message = result.message,
                    assetPath = result.assetPath,
                    propertyName = result.propertyName
                });
            }

            return converted;
        }
    }
}
