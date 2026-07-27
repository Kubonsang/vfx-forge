namespace Kubonsang.VfxForge.Editor
{
    public sealed class DurationBudgetRule : IVfxValidationRule
    {
        public string RuleId => "VAL-003";

        public VfxValidationResult Evaluate(VfxValidationContext context)
        {
            if (context?.Recipe?.timing == null || context.Recipe.budget == null)
            {
                return VfxValidationResult.Error(RuleId, "Duration data is missing.");
            }

            float maxDuration = context.Recipe.budget.maxDuration;
            if (context.StyleProfile != null)
            {
                maxDuration = System.Math.Min(maxDuration, context.StyleProfile.maxDuration);
            }

            return context.Recipe.timing.duration <= maxDuration
                ? VfxValidationResult.Pass(RuleId, "Duration is within budget.")
                : VfxValidationResult.Error(RuleId, $"Duration {context.Recipe.timing.duration} exceeds {maxDuration}.");
        }
    }
}
