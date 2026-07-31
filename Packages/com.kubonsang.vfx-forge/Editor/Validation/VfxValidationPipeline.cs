using System.Collections.Generic;

namespace Kubonsang.VfxForge.Editor
{
    public static class VfxValidationPipeline
    {
        public static List<VfxValidationResult> Run(VfxValidationContext context)
        {
            return VfxValidationRunner.Run(context, CreateDefaultRules());
        }

        public static IReadOnlyList<IVfxValidationRule> CreateDefaultRules()
        {
            return new IVfxValidationRule[]
            {
                new MissingAssetRule(),
                new PropertyBindingRule(),
                new DurationBudgetRule(),
                new FiniteBoundsRule(),
                new ParticleBudgetRule(),
                new LayerSupportRule(),
                new LightPolicyRule()
            };
        }
    }
}
