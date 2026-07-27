using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kubonsang.VfxForge.Editor
{
    public enum VfxValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    [Serializable]
    public sealed class VfxValidationResult
    {
        public string ruleId = string.Empty;
        public VfxValidationSeverity severity;
        public bool passed;
        public string message = string.Empty;
        public string assetPath = string.Empty;
        public string propertyName = string.Empty;

        public static VfxValidationResult Pass(string id, string message)
        {
            return new VfxValidationResult
            {
                ruleId = NormalizeRuleId(id),
                severity = VfxValidationSeverity.Info,
                passed = true,
                message = message
            };
        }

        public static VfxValidationResult Warning(string id, string message)
        {
            return new VfxValidationResult
            {
                ruleId = NormalizeRuleId(id),
                severity = VfxValidationSeverity.Warning,
                passed = false,
                message = message
            };
        }

        public static VfxValidationResult Error(string id, string message)
        {
            return new VfxValidationResult
            {
                ruleId = NormalizeRuleId(id),
                severity = VfxValidationSeverity.Error,
                passed = false,
                message = message
            };
        }

        private static string NormalizeRuleId(string id)
        {
            return string.IsNullOrWhiteSpace(id)
                ? "VALIDATION-UNSPECIFIED"
                : id;
        }
    }

    public sealed class VfxValidationContext
    {
        public VfxRecipe Recipe;
        public GameObject Prefab;
        public VfxTemplateEntry Template;
        public VfxStyleProfile StyleProfile;
        public string AssetPath = string.Empty;
    }

    public interface IVfxValidationRule
    {
        string RuleId { get; }
        VfxValidationResult Evaluate(VfxValidationContext context);
    }

    public static class VfxValidationRunner
    {
        public static List<VfxValidationResult> Run(VfxValidationContext context, IEnumerable<IVfxValidationRule> rules)
        {
            var results = new List<VfxValidationResult>();
            if (rules == null)
            {
                results.Add(VfxValidationResult.Error(
                    "PIPELINE-RULES",
                    "Validation rule collection is null."));
                return results;
            }

            var seenRuleIds = new HashSet<string>(StringComparer.Ordinal);
            int index = 0;
            foreach (IVfxValidationRule rule in rules)
            {
                if (rule == null)
                {
                    index++;
                    continue;
                }

                string ruleId = ResolveRuleId(rule, index);
                if (!seenRuleIds.Add(ruleId))
                {
                    results.Add(VfxValidationResult.Error(
                        "PIPELINE-RULE-ID-DUPLICATE",
                        $"Validation rule id is duplicated: {ruleId}."));
                }

                try
                {
                    VfxValidationResult result = rule.Evaluate(context);
                    if (result == null)
                    {
                        results.Add(VfxValidationResult.Error(
                            ruleId,
                            "Validation rule returned no result."));
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(result.ruleId))
                        {
                            result.ruleId = ruleId;
                        }

                        results.Add(result);
                    }
                }
                catch (Exception exception)
                {
                    results.Add(VfxValidationResult.Error(
                        ruleId,
                        $"Rule threw an exception: {exception.Message}"));
                }

                index++;
            }

            return results;
        }

        private static string ResolveRuleId(IVfxValidationRule rule, int index)
        {
            try
            {
                return string.IsNullOrWhiteSpace(rule.RuleId)
                    ? $"PIPELINE-RULE-{index}"
                    : rule.RuleId;
            }
            catch (Exception)
            {
                return $"PIPELINE-RULE-{index}";
            }
        }
    }
}
