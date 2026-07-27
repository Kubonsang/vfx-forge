using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Kubonsang.VfxForge.Editor
{
    public static class VfxPropertyApplier
    {
        public static List<VfxValidationResult> Apply(GameObject instance, VfxRecipe recipe, VfxTemplateEntry template)
        {
            var results = new List<VfxValidationResult>();
            VisualEffect[] effects = instance.GetComponentsInChildren<VisualEffect>(true);

            foreach (VfxPropertyBinding binding in template.bindings)
            {
                if (!VfxRecipeValueResolver.TryResolve(recipe, binding.recipePath, out object value))
                {
                    results.Add(binding.required
                        ? VfxValidationResult.Error("BIND-RESOLVE", $"Cannot resolve recipe path: {binding.recipePath}")
                        : VfxValidationResult.Warning("BIND-RESOLVE", $"Optional recipe path was not resolved: {binding.recipePath}"));
                    continue;
                }

                int start = binding.componentIndex < 0 ? 0 : binding.componentIndex;
                int end = binding.componentIndex < 0 ? effects.Length : binding.componentIndex + 1;
                if (start < 0 || start >= effects.Length || end > effects.Length)
                {
                    results.Add(VfxValidationResult.Error("BIND-COMPONENT", $"Invalid VisualEffect component index: {binding.componentIndex}"));
                    continue;
                }

                bool applied = false;
                for (int index = start; index < end; index++)
                {
                    applied |= TryApply(effects[index], binding, value);
                }

                results.Add(applied
                    ? VfxValidationResult.Pass("BIND-APPLY", $"Applied {binding.recipePath} to {binding.exposedPropertyName}.")
                    : binding.required
                        ? VfxValidationResult.Error("BIND-APPLY", $"Required property could not be applied: {binding.exposedPropertyName}")
                        : VfxValidationResult.Warning("BIND-APPLY", $"Optional property could not be applied: {binding.exposedPropertyName}"));
            }

            return results;
        }

        private static bool TryApply(VisualEffect effect, VfxPropertyBinding binding, object value)
        {
            if (effect == null || string.IsNullOrWhiteSpace(binding.exposedPropertyName))
            {
                return false;
            }

            string property = binding.exposedPropertyName;
            try
            {
                switch (binding.propertyType)
                {
                    case VfxPropertyType.Float:
                        if (!effect.HasFloat(property)) return false;
                        effect.SetFloat(property, Convert.ToSingle(value));
                        return true;
                    case VfxPropertyType.Int:
                        if (!effect.HasInt(property)) return false;
                        effect.SetInt(property, Convert.ToInt32(value));
                        return true;
                    case VfxPropertyType.Bool:
                        if (!effect.HasBool(property)) return false;
                        effect.SetBool(property, Convert.ToBoolean(value));
                        return true;
                    case VfxPropertyType.Vector2:
                        if (!effect.HasVector2(property) || !(value is Vector2)) return false;
                        effect.SetVector2(property, (Vector2)value);
                        return true;
                    case VfxPropertyType.Vector3:
                        if (!effect.HasVector3(property) || !(value is Vector3)) return false;
                        effect.SetVector3(property, (Vector3)value);
                        return true;
                    case VfxPropertyType.Vector4:
                        if (!effect.HasVector4(property) || !(value is Vector4)) return false;
                        effect.SetVector4(property, (Vector4)value);
                        return true;
                    case VfxPropertyType.Color:
                        if (!effect.HasVector4(property) || !(value is Color)) return false;
                        effect.SetVector4(property, (Color)value);
                        return true;
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
