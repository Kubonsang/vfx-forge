using System;
using System.Collections.Generic;

namespace Kubonsang.VfxForge.Editor
{
    public sealed class LayerSupportRule : IVfxValidationRule
    {
        public string RuleId => "VAL-006";

        public VfxValidationResult Evaluate(VfxValidationContext context)
        {
            if (context?.Recipe == null || context.Template == null)
            {
                return VfxValidationResult.Error(RuleId, "Recipe or template is missing.");
            }

            var supported = new HashSet<string>(context.Template.supportedLayers ?? Array.Empty<string>(), StringComparer.Ordinal);
            foreach (string layer in context.Recipe.layers ?? Array.Empty<string>())
            {
                if (!supported.Contains(layer))
                {
                    return VfxValidationResult.Error(RuleId, $"Template does not support layer: {layer}");
                }
            }

            return VfxValidationResult.Pass(RuleId, "All requested layers are supported.");
        }
    }
}
