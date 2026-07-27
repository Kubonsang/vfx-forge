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
            return new VfxValidationResult { ruleId = id, severity = VfxValidationSeverity.Info, passed = true, message = message };
        }

        public static VfxValidationResult Warning(string id, string message)
        {
            return new VfxValidationResult { ruleId = id, severity = VfxValidationSeverity.Warning, passed = false, message = message };
        }

        public static VfxValidationResult Error(string id, string message)
        {
            return new VfxValidationResult { ruleId = id, severity = VfxValidationSeverity.Error, passed = false, message = message };
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
            foreach (IVfxValidationRule rule in rules)
            {
                if (rule == null)
                {
                    continue;
                }

                try
                {
                    results.Add(rule.Evaluate(context));
                }
                catch (Exception exception)
                {
                    results.Add(VfxValidationResult.Error(rule.RuleId, $"Rule threw an exception: {exception.Message}"));
                }
            }

            return results;
        }
    }
}
