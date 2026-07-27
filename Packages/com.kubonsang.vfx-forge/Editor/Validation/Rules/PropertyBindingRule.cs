using System;
using UnityEngine.VFX;

namespace Kubonsang.VfxForge.Editor
{
    public sealed class PropertyBindingRule : IVfxValidationRule
    {
        public string RuleId => "VAL-002";

        public VfxValidationResult Evaluate(VfxValidationContext context)
        {
            if (context?.Recipe == null || context.Prefab == null || context.Template == null)
            {
                return VfxValidationResult.Error(
                    RuleId,
                    "Recipe, Prefab, or Template is missing.");
            }

            if (context.Template.bindings == null)
            {
                return VfxValidationResult.Error(RuleId, "Template Binding list is null.");
            }

            VisualEffect[] effects =
                context.Prefab.GetComponentsInChildren<VisualEffect>(true);
            VfxValidationResult optionalWarning = null;
            foreach (VfxPropertyBinding binding in context.Template.bindings)
            {
                if (binding == null)
                {
                    return VfxValidationResult.Error(RuleId, "Template contains a null Binding.");
                }

                if (!VfxRecipeValueResolver.TryResolve(
                        context.Recipe,
                        binding.recipePath,
                        out _))
                {
                    VfxValidationResult unresolved = CreateFailure(
                        binding,
                        $"Recipe path cannot be resolved: {binding.recipePath}.");
                    if (binding.required)
                    {
                        return unresolved;
                    }

                    optionalWarning = unresolved;
                    continue;
                }

                int start = binding.componentIndex < 0 ? 0 : binding.componentIndex;
                int end = binding.componentIndex < 0
                    ? effects.Length
                    : binding.componentIndex + 1;
                if (start < 0 || start >= effects.Length || end > effects.Length)
                {
                    return WithProperty(
                        VfxValidationResult.Error(
                            RuleId,
                            $"VisualEffect component index is invalid: {binding.componentIndex}."),
                        binding.exposedPropertyName);
                }

                bool found = false;
                for (int index = start; index < end; index++)
                {
                    found |= HasProperty(effects[index], binding);
                }

                if (!found)
                {
                    VfxValidationResult missing = CreateFailure(
                        binding,
                        $"Exposed property is missing or has the wrong type: {binding.exposedPropertyName}.");
                    if (binding.required)
                    {
                        return missing;
                    }

                    optionalWarning = missing;
                }
            }

            return optionalWarning
                ?? VfxValidationResult.Pass(
                    RuleId,
                    "All registered Property Bindings are available.");
        }

        private VfxValidationResult CreateFailure(
            VfxPropertyBinding binding,
            string message)
        {
            VfxValidationResult result = binding.required
                ? VfxValidationResult.Error(RuleId, message)
                : VfxValidationResult.Warning(RuleId, message);
            return WithProperty(result, binding.exposedPropertyName);
        }

        private static VfxValidationResult WithProperty(
            VfxValidationResult result,
            string propertyName)
        {
            result.propertyName = propertyName ?? string.Empty;
            return result;
        }

        private static bool HasProperty(
            VisualEffect effect,
            VfxPropertyBinding binding)
        {
            if (effect == null || string.IsNullOrWhiteSpace(binding.exposedPropertyName))
            {
                return false;
            }

            try
            {
                switch (binding.propertyType)
                {
                    case VfxPropertyType.Float:
                        return effect.HasFloat(binding.exposedPropertyName);
                    case VfxPropertyType.Int:
                        return effect.HasInt(binding.exposedPropertyName);
                    case VfxPropertyType.Bool:
                        return effect.HasBool(binding.exposedPropertyName);
                    case VfxPropertyType.Vector2:
                        return effect.HasVector2(binding.exposedPropertyName);
                    case VfxPropertyType.Vector3:
                        return effect.HasVector3(binding.exposedPropertyName);
                    case VfxPropertyType.Vector4:
                    case VfxPropertyType.Color:
                        return effect.HasVector4(binding.exposedPropertyName);
                    default:
                        return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
