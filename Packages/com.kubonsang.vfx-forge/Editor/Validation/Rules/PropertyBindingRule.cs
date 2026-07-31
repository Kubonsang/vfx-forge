using System;
using UnityEngine;
using UnityEngine.VFX;

namespace Kubonsang.VfxForge.Editor
{
    public sealed class PropertyBindingRule : IVfxValidationRule
    {
        public string RuleId => "VAL-002";

        public VfxValidationResult Evaluate(VfxValidationContext context)
        {
            if (context?.Recipe == null
                || context.Prefab == null
                || context.Template == null)
            {
                return VfxValidationResult.Error(
                    RuleId,
                    "Recipe, Prefab, or Template is missing.");
            }

            if (context.Template.bindings == null)
            {
                return VfxValidationResult.Error(
                    RuleId,
                    "Template Binding list is null.");
            }

            VfxValidationResult optionalWarning = null;
            foreach (VfxPropertyBinding binding in context.Template.bindings)
            {
                if (binding == null)
                {
                    return VfxValidationResult.Error(
                        RuleId,
                        "Template contains a null Binding.");
                }

                if (!VfxRecipeValueResolver.TryResolve(
                    context.Recipe,
                    binding.recipePath,
                    out object value))
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

                Transform target = string.IsNullOrEmpty(binding.targetPath)
                    ? context.Prefab.transform
                    : context.Prefab.transform.Find(binding.targetPath);
                bool found = target != null
                    && HasAppliedTarget(
                        target,
                        context.Template,
                        binding,
                        value);
                if (!found)
                {
                    VfxValidationResult missing = CreateFailure(
                        binding,
                        $"Bound target is missing or has the wrong type: "
                        + $"{binding.exposedPropertyName}.");
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

        private static bool HasAppliedTarget(
            Transform target,
            VfxTemplateEntry template,
            VfxPropertyBinding binding,
            object value)
        {
            switch (binding.targetKind)
            {
                case VfxBindingTargetKind.VisualEffectProperty:
                    return HasVisualEffectProperty(target, binding);
                case VfxBindingTargetKind.TransformProperty:
                    return binding.exposedPropertyName == "uniformScale"
                        || binding.exposedPropertyName == "localPosition"
                        || binding.exposedPropertyName == "localEulerAngles"
                        || binding.exposedPropertyName == "localScale";
                case VfxBindingTargetKind.MaterialProperty:
                    VfxMaterialPropertyOverrides overrides =
                        target.GetComponent<VfxMaterialPropertyOverrides>();
                    if (overrides == null)
                    {
                        return false;
                    }

                    foreach (VfxMaterialPropertyOverride entry in overrides.Overrides)
                    {
                        if (entry != null
                            && entry.materialIndex == binding.materialIndex
                            && entry.propertyName == binding.exposedPropertyName)
                        {
                            return true;
                        }
                    }

                    return false;
                case VfxBindingTargetKind.MeshVariant:
                    if (!(value is string variantKey))
                    {
                        return false;
                    }

                    MeshFilter filter = target.GetComponent<MeshFilter>();
                    if (filter == null || template.meshVariants == null)
                    {
                        return false;
                    }

                    foreach (VfxMeshVariant variant in template.meshVariants)
                    {
                        if (variant != null
                            && variant.key == variantKey
                            && variant.mesh == filter.sharedMesh)
                        {
                            return true;
                        }
                    }

                    return false;
                case VfxBindingTargetKind.AdapterProperty:
                    VfxRecipeBindingValueType valueType =
                        ToRuntimeType(binding.propertyType);
                    foreach (MonoBehaviour behaviour in
                        target.GetComponents<MonoBehaviour>())
                    {
                        if (behaviour is IVfxRecipeBindingAdapter adapter
                            && adapter.BindingAdapterId == binding.adapterId
                            && adapter.SupportsBinding(
                                binding.exposedPropertyName,
                                valueType))
                        {
                            return true;
                        }
                    }

                    return false;
                default:
                    return false;
            }
        }

        private static bool HasVisualEffectProperty(
            Transform target,
            VfxPropertyBinding binding)
        {
            VisualEffect[] effects =
                target.GetComponentsInChildren<VisualEffect>(true);
            int start = binding.componentIndex < 0 ? 0 : binding.componentIndex;
            int end = binding.componentIndex < 0
                ? effects.Length
                : binding.componentIndex + 1;
            if (start < 0 || start >= effects.Length || end > effects.Length)
            {
                return false;
            }

            for (int index = start; index < end; index++)
            {
                if (HasProperty(effects[index], binding))
                {
                    return true;
                }
            }

            return false;
        }

        private VfxValidationResult CreateFailure(
            VfxPropertyBinding binding,
            string message)
        {
            VfxValidationResult result = binding.required
                ? VfxValidationResult.Error(RuleId, message)
                : VfxValidationResult.Warning(RuleId, message);
            result.propertyName = binding.exposedPropertyName ?? string.Empty;
            return result;
        }

        private static bool HasProperty(
            VisualEffect effect,
            VfxPropertyBinding binding)
        {
            if (effect == null
                || string.IsNullOrWhiteSpace(binding.exposedPropertyName))
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

        private static VfxRecipeBindingValueType ToRuntimeType(
            VfxPropertyType type)
        {
            switch (type)
            {
                case VfxPropertyType.Float:
                    return VfxRecipeBindingValueType.Float;
                case VfxPropertyType.Int:
                    return VfxRecipeBindingValueType.Int;
                case VfxPropertyType.Bool:
                    return VfxRecipeBindingValueType.Bool;
                case VfxPropertyType.Vector2:
                    return VfxRecipeBindingValueType.Vector2;
                case VfxPropertyType.Vector3:
                    return VfxRecipeBindingValueType.Vector3;
                case VfxPropertyType.Vector4:
                    return VfxRecipeBindingValueType.Vector4;
                case VfxPropertyType.Color:
                    return VfxRecipeBindingValueType.Color;
                case VfxPropertyType.String:
                    return VfxRecipeBindingValueType.String;
                default:
                    return default;
            }
        }
    }
}
