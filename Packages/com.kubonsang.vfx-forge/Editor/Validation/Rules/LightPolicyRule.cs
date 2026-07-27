using UnityEngine;

namespace Kubonsang.VfxForge.Editor
{
    public sealed class LightPolicyRule : IVfxValidationRule
    {
        public string RuleId => "VAL-008";

        public VfxValidationResult Evaluate(VfxValidationContext context)
        {
            if (context?.Prefab == null || context.Recipe?.budget == null)
            {
                return VfxValidationResult.Error(RuleId, "Prefab or budget is missing.");
            }

            bool allowLight = context.Recipe.budget.allowLight && (context.StyleProfile == null || context.StyleProfile.allowLight);
            bool hasLight = context.Prefab.GetComponentInChildren<Light>(true) != null;

            return hasLight && !allowLight
                ? VfxValidationResult.Error(RuleId, "Light component is not allowed by the effective budget.")
                : VfxValidationResult.Pass(RuleId, "Light policy passed.");
        }
    }
}
