using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Kubonsang.VfxForge.Editor
{
    public static class VfxPropertyApplier
    {
        public static List<VfxValidationResult> Apply(
            GameObject instance,
            VfxRecipe recipe,
            VfxTemplateEntry template)
        {
            var results = new List<VfxValidationResult>();
            foreach (VfxPropertyBinding binding in template.bindings)
            {
                if (!VfxRecipeValueResolver.TryResolve(
                    recipe,
                    binding.recipePath,
                    out object value))
                {
                    results.Add(binding.required
                        ? VfxValidationResult.Error(
                            "BIND-RESOLVE",
                            $"Cannot resolve recipe path: {binding.recipePath}")
                        : VfxValidationResult.Warning(
                            "BIND-RESOLVE",
                            $"Optional recipe path was not resolved: {binding.recipePath}"));
                    continue;
                }

                bool applied;
                try
                {
                    applied = TryApply(instance, template, binding, value);
                }
                catch (Exception)
                {
                    applied = false;
                }

                results.Add(applied
                    ? VfxValidationResult.Pass(
                        "BIND-APPLY",
                        $"Applied {binding.recipePath} to {DescribeTarget(binding)}.")
                    : binding.required
                        ? VfxValidationResult.Error(
                            "BIND-APPLY",
                            $"Required property could not be applied: {DescribeTarget(binding)}")
                        : VfxValidationResult.Warning(
                            "BIND-APPLY",
                            $"Optional property could not be applied: {DescribeTarget(binding)}"));
            }

            return results;
        }

        private static bool TryApply(
            GameObject instance,
            VfxTemplateEntry template,
            VfxPropertyBinding binding,
            object value)
        {
            Transform target = ResolveTarget(instance.transform, binding.targetPath);
            if (target == null)
            {
                return false;
            }

            switch (binding.targetKind)
            {
                case VfxBindingTargetKind.VisualEffectProperty:
                    return ApplyVisualEffect(target, binding, value);
                case VfxBindingTargetKind.TransformProperty:
                    return ApplyTransform(target, binding, value);
                case VfxBindingTargetKind.MaterialProperty:
                    return ApplyMaterial(target, binding, value);
                case VfxBindingTargetKind.MeshVariant:
                    return ApplyMeshVariant(target, template, binding, value);
                case VfxBindingTargetKind.AdapterProperty:
                    return ApplyAdapter(target, binding, value);
                default:
                    return false;
            }
        }

        private static bool ApplyVisualEffect(
            Transform target,
            VfxPropertyBinding binding,
            object value)
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

            bool applied = false;
            for (int index = start; index < end; index++)
            {
                applied |= TryApplyVisualEffect(effects[index], binding, value);
            }

            return applied;
        }

        private static bool ApplyTransform(
            Transform target,
            VfxPropertyBinding binding,
            object value)
        {
            switch (binding.exposedPropertyName)
            {
                case "localPosition" when value is Vector3 position:
                    target.localPosition = position;
                    return true;
                case "localEulerAngles" when value is Vector3 eulerAngles:
                    target.localEulerAngles = eulerAngles;
                    return true;
                case "localScale" when value is Vector3 scale:
                    target.localScale = scale;
                    return true;
                case "uniformScale":
                    target.localScale = Vector3.one * Convert.ToSingle(value);
                    return true;
                default:
                    return false;
            }
        }

        private static bool ApplyMaterial(
            Transform target,
            VfxPropertyBinding binding,
            object value)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null
                || binding.materialIndex < 0
                || binding.materialIndex >= renderer.sharedMaterials.Length)
            {
                return false;
            }

            VfxRecipeBindingValue bindingValue =
                BuildRuntimeValue(binding.propertyType, value);
            if (bindingValue.Value == null)
            {
                return false;
            }

            VfxMaterialPropertyOverrides overrides =
                target.GetComponent<VfxMaterialPropertyOverrides>()
                ?? target.gameObject.AddComponent<VfxMaterialPropertyOverrides>();
            overrides.Set(
                binding.materialIndex,
                binding.exposedPropertyName,
                bindingValue);
            return true;
        }

        private static bool ApplyMeshVariant(
            Transform target,
            VfxTemplateEntry template,
            VfxPropertyBinding binding,
            object value)
        {
            if (binding.exposedPropertyName != "sharedMesh"
                || !(value is string variantKey))
            {
                return false;
            }

            MeshFilter meshFilter = target.GetComponent<MeshFilter>();
            if (meshFilter == null || template.meshVariants == null)
            {
                return false;
            }

            foreach (VfxMeshVariant variant in template.meshVariants)
            {
                if (variant != null
                    && variant.mesh != null
                    && variant.key == variantKey)
                {
                    meshFilter.sharedMesh = variant.mesh;
                    return true;
                }
            }

            return false;
        }

        private static bool ApplyAdapter(
            Transform target,
            VfxPropertyBinding binding,
            object value)
        {
            VfxRecipeBindingValue bindingValue =
                BuildRuntimeValue(binding.propertyType, value);
            if (bindingValue.Value == null)
            {
                return false;
            }

            foreach (MonoBehaviour behaviour in target.GetComponents<MonoBehaviour>())
            {
                if (behaviour is IVfxRecipeBindingAdapter adapter
                    && adapter.BindingAdapterId == binding.adapterId
                    && adapter.SupportsBinding(
                        binding.exposedPropertyName,
                        bindingValue.Type)
                    && adapter.TryApplyBinding(
                        binding.exposedPropertyName,
                        bindingValue))
                {
                    return true;
                }
            }

            return false;
        }

        private static VfxRecipeBindingValue BuildRuntimeValue(
            VfxPropertyType type,
            object value)
        {
            switch (type)
            {
                case VfxPropertyType.Float:
                    return new VfxRecipeBindingValue(
                        VfxRecipeBindingValueType.Float,
                        Convert.ToSingle(value));
                case VfxPropertyType.Int:
                    return new VfxRecipeBindingValue(
                        VfxRecipeBindingValueType.Int,
                        Convert.ToInt32(value));
                case VfxPropertyType.Bool:
                    return new VfxRecipeBindingValue(
                        VfxRecipeBindingValueType.Bool,
                        Convert.ToBoolean(value));
                case VfxPropertyType.Vector2:
                    return new VfxRecipeBindingValue(
                        VfxRecipeBindingValueType.Vector2,
                        value);
                case VfxPropertyType.Vector3:
                    return new VfxRecipeBindingValue(
                        VfxRecipeBindingValueType.Vector3,
                        value);
                case VfxPropertyType.Vector4:
                    return new VfxRecipeBindingValue(
                        VfxRecipeBindingValueType.Vector4,
                        value);
                case VfxPropertyType.Color:
                    return new VfxRecipeBindingValue(
                        VfxRecipeBindingValueType.Color,
                        value);
                case VfxPropertyType.String:
                    return new VfxRecipeBindingValue(
                        VfxRecipeBindingValueType.String,
                        value);
                default:
                    return default;
            }
        }

        private static bool TryApplyVisualEffect(
            VisualEffect effect,
            VfxPropertyBinding binding,
            object value)
        {
            if (effect == null || string.IsNullOrWhiteSpace(binding.exposedPropertyName))
            {
                return false;
            }

            string property = binding.exposedPropertyName;
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

        private static Transform ResolveTarget(Transform root, string path)
        {
            if (root == null)
            {
                return null;
            }

            return string.IsNullOrEmpty(path) ? root : root.Find(path);
        }

        private static string DescribeTarget(VfxPropertyBinding binding)
        {
            string path = string.IsNullOrEmpty(binding.targetPath)
                ? "<root>"
                : binding.targetPath;
            return $"{binding.targetKind}:{path}:{binding.exposedPropertyName}";
        }
    }
}
