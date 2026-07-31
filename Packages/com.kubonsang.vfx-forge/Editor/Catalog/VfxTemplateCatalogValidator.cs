using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

namespace Kubonsang.VfxForge.Editor
{
    public static class VfxTemplateCatalogValidator
    {
        private static readonly Regex IdPattern =
            new Regex(
                "^[a-z0-9][a-z0-9_-]{2,63}$",
                RegexOptions.CultureInvariant);

        public static List<VfxValidationResult> Validate(VfxTemplateCatalog catalog)
        {
            var results = new List<VfxValidationResult>();
            if (catalog == null)
            {
                results.Add(VfxValidationResult.Error(
                    "CATALOG-NULL",
                    "Template Catalog is null."));
                return results;
            }

            if (catalog.templates == null)
            {
                results.Add(VfxValidationResult.Error(
                    "CATALOG-TEMPLATES",
                    "Template list is null."));
                return results;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < catalog.templates.Count; index++)
            {
                VfxTemplateEntry entry = catalog.templates[index];
                ValidateEntry(entry, $"templates[{index}]", results);
                if (entry != null
                    && !string.IsNullOrWhiteSpace(entry.id)
                    && !ids.Add(entry.id))
                {
                    results.Add(VfxValidationResult.Error(
                        "CATALOG-ID-DUPLICATE",
                        $"Duplicate template id at templates[{index}]: {entry.id}."));
                }
            }

            return results;
        }

        public static List<VfxValidationResult> ValidateEntry(VfxTemplateEntry entry)
        {
            var results = new List<VfxValidationResult>();
            ValidateEntry(entry, "candidate", results);
            return results;
        }

        public static List<VfxValidationResult> ValidateRegistration(
            VfxTemplateCatalog catalog,
            VfxTemplateEntry candidate)
        {
            var results = new List<VfxValidationResult>();
            if (catalog == null)
            {
                results.Add(VfxValidationResult.Error(
                    "CATALOG-NULL",
                    "Template Catalog is null."));
                return results;
            }

            ValidateEntry(candidate, "candidate", results);
            if (candidate == null || catalog.templates == null)
            {
                return results;
            }

            foreach (VfxTemplateEntry existing in catalog.templates)
            {
                if (existing != null
                    && string.Equals(
                        existing.id,
                        candidate.id,
                        StringComparison.Ordinal))
                {
                    results.Add(VfxValidationResult.Error(
                        "CATALOG-ID-DUPLICATE",
                        $"Template id is already registered: {candidate.id}."));
                    break;
                }
            }

            return results;
        }

        private static void ValidateEntry(
            VfxTemplateEntry entry,
            string location,
            List<VfxValidationResult> results)
        {
            if (entry == null)
            {
                results.Add(VfxValidationResult.Error(
                    "CATALOG-ENTRY-NULL",
                    $"Template entry is null: {location}."));
                return;
            }

            if (string.IsNullOrWhiteSpace(entry.id)
                || !IdPattern.IsMatch(entry.id))
            {
                results.Add(VfxValidationResult.Error(
                    "CATALOG-ID",
                    $"Template id is invalid at {location}: {entry.id ?? "<null>"}."));
            }

            if (entry.prefab == null)
            {
                results.Add(VfxValidationResult.Error(
                    "CATALOG-PREFAB",
                    $"Template Prefab is missing at {location}."));
            }
            else
            {
                if (!EditorUtility.IsPersistent(entry.prefab)
                    || !PrefabUtility.IsPartOfPrefabAsset(entry.prefab))
                {
                    results.Add(VfxValidationResult.Error(
                        "CATALOG-PREFAB-ASSET",
                        $"Template must reference a Prefab asset at {location}."));
                }

                if (entry.prefab.GetComponentsInChildren<VisualEffect>(true).Length == 0)
                {
                    results.Add(VfxValidationResult.Error(
                        "CATALOG-VFX-COMPONENT",
                        $"Template Prefab contains no VisualEffect component at {location}."));
                }
            }

            if (string.IsNullOrWhiteSpace(entry.playEventName))
            {
                results.Add(VfxValidationResult.Error(
                    "CATALOG-PLAY-EVENT",
                    $"Play event name is empty at {location}."));
            }

            ValidateLayers(entry.supportedLayers, location, results);
            ValidateMeshVariants(entry.meshVariants, location, results);
            ValidateBindings(entry, location, results);
        }

        private static void ValidateLayers(
            IEnumerable<string> layers,
            string location,
            List<VfxValidationResult> results)
        {
            if (layers == null)
            {
                return;
            }

            var uniqueLayers = new HashSet<string>(StringComparer.Ordinal);
            foreach (string layer in layers)
            {
                if (string.IsNullOrWhiteSpace(layer)
                    || !uniqueLayers.Add(layer))
                {
                    results.Add(VfxValidationResult.Error(
                        "CATALOG-LAYER",
                        $"Supported layer is empty or duplicated at {location}: {layer ?? "<null>"}."));
                }
            }
        }

        private static void ValidateMeshVariants(
            IEnumerable<VfxMeshVariant> variants,
            string location,
            List<VfxValidationResult> results)
        {
            if (variants == null)
            {
                return;
            }

            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (VfxMeshVariant variant in variants)
            {
                if (variant == null
                    || string.IsNullOrWhiteSpace(variant.key)
                    || !keys.Add(variant.key)
                    || variant.mesh == null
                    || !EditorUtility.IsPersistent(variant.mesh))
                {
                    results.Add(VfxValidationResult.Error(
                        "CATALOG-MESH-VARIANT",
                        $"Mesh variant must have a unique key and persistent Mesh at {location}."));
                }
            }
        }

        private static void ValidateBindings(
            VfxTemplateEntry entry,
            string location,
            List<VfxValidationResult> results)
        {
            IList<VfxPropertyBinding> bindings = entry.bindings;
            if (bindings == null)
            {
                results.Add(VfxValidationResult.Error(
                    "CATALOG-BINDINGS",
                    $"Binding list is null at {location}."));
                return;
            }

            var priorBindings = new List<VfxPropertyBinding>();
            for (int index = 0; index < bindings.Count; index++)
            {
                VfxPropertyBinding binding = bindings[index];
                string bindingLocation = $"{location}.bindings[{index}]";
                if (binding == null)
                {
                    results.Add(VfxValidationResult.Error(
                        "CATALOG-BINDING-NULL",
                        $"Property Binding is null at {bindingLocation}."));
                    continue;
                }

                bool pathValid = VfxRecipeValueResolver.TryGetPropertyType(
                    binding.recipePath,
                    out VfxPropertyType expectedType);
                if (!pathValid)
                {
                    results.Add(VfxValidationResult.Error(
                        "CATALOG-BINDING-PATH",
                        $"Recipe path is unsupported at {bindingLocation}: {binding.recipePath}."));
                }
                else if (binding.propertyType != expectedType)
                {
                    results.Add(VfxValidationResult.Error(
                        "CATALOG-BINDING-TYPE",
                        $"Binding type at {bindingLocation} must be {expectedType} for {binding.recipePath}."));
                }

                bool propertyNameValid =
                    !string.IsNullOrWhiteSpace(binding.exposedPropertyName);
                if (!propertyNameValid)
                {
                    results.Add(VfxValidationResult.Error(
                        "CATALOG-BINDING-NAME",
                        $"Target property name is empty at {bindingLocation}."));
                }

                Transform target = ResolveTarget(entry.prefab, binding.targetPath);
                if (!IsSafeTargetPath(binding.targetPath) || target == null)
                {
                    results.Add(VfxValidationResult.Error(
                        "CATALOG-BINDING-TARGET",
                        $"Binding target path is unsafe or missing at {bindingLocation}: {binding.targetPath}."));
                }
                else if (pathValid
                    && binding.propertyType == expectedType
                    && propertyNameValid)
                {
                    ValidateTarget(entry, target, binding, bindingLocation, results);
                }

                foreach (VfxPropertyBinding prior in priorBindings)
                {
                    if (TargetsOverlap(prior, binding))
                    {
                        results.Add(VfxValidationResult.Error(
                            "CATALOG-BINDING-DUPLICATE",
                            $"Binding target overlaps an earlier binding at {bindingLocation}: {binding.exposedPropertyName}."));
                        break;
                    }
                }

                priorBindings.Add(binding);
            }
        }

        private static void ValidateTarget(
            VfxTemplateEntry entry,
            Transform target,
            VfxPropertyBinding binding,
            string location,
            List<VfxValidationResult> results)
        {
            bool valid;
            switch (binding.targetKind)
            {
                case VfxBindingTargetKind.VisualEffectProperty:
                    valid = ValidateVisualEffectTarget(
                        target,
                        binding,
                        location,
                        results);
                    break;
                case VfxBindingTargetKind.TransformProperty:
                    valid = IsTransformPropertySupported(binding);
                    break;
                case VfxBindingTargetKind.MaterialProperty:
                    valid = IsMaterialPropertySupported(target, binding);
                    break;
                case VfxBindingTargetKind.MeshVariant:
                    valid = binding.propertyType == VfxPropertyType.String
                        && binding.exposedPropertyName == "sharedMesh"
                        && target.GetComponent<MeshFilter>() != null
                        && entry.meshVariants != null
                        && entry.meshVariants.Count > 0;
                    break;
                case VfxBindingTargetKind.AdapterProperty:
                    valid = IsAdapterPropertySupported(target, binding);
                    break;
                default:
                    valid = false;
                    break;
            }

            if (!valid)
            {
                results.Add(binding.required
                    ? VfxValidationResult.Error(
                        "CATALOG-BINDING-PROPERTY",
                        $"Required target property was not found or allowed at {location}: {binding.exposedPropertyName}.")
                    : VfxValidationResult.Warning(
                        "CATALOG-BINDING-PROPERTY",
                        $"Optional target property was not found or allowed at {location}: {binding.exposedPropertyName}."));
            }
        }

        private static bool ValidateVisualEffectTarget(
            Transform target,
            VfxPropertyBinding binding,
            string location,
            List<VfxValidationResult> results)
        {
            VisualEffect[] effects =
                target.GetComponentsInChildren<VisualEffect>(true);
            bool componentValid = binding.componentIndex >= -1
                && (binding.componentIndex < 0
                    || binding.componentIndex < effects.Length);
            if (!componentValid)
            {
                results.Add(VfxValidationResult.Error(
                    "CATALOG-BINDING-COMPONENT",
                    $"VisualEffect component index is invalid at {location}: {binding.componentIndex}."));
                return false;
            }

            int start = binding.componentIndex < 0 ? 0 : binding.componentIndex;
            int end = binding.componentIndex < 0
                ? effects.Length
                : binding.componentIndex + 1;
            for (int index = start; index < end; index++)
            {
                if (HasExposedProperty(effects[index], binding))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsTransformPropertySupported(
            VfxPropertyBinding binding)
        {
            if (binding.exposedPropertyName == "uniformScale")
            {
                return binding.propertyType == VfxPropertyType.Float;
            }

            return binding.propertyType == VfxPropertyType.Vector3
                && (binding.exposedPropertyName == "localPosition"
                    || binding.exposedPropertyName == "localEulerAngles"
                    || binding.exposedPropertyName == "localScale");
        }

        private static bool IsMaterialPropertySupported(
            Transform target,
            VfxPropertyBinding binding)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null
                || binding.materialIndex < 0
                || binding.materialIndex >= renderer.sharedMaterials.Length
                || binding.propertyType == VfxPropertyType.Bool
                || binding.propertyType == VfxPropertyType.String)
            {
                return false;
            }

            Material material = renderer.sharedMaterials[binding.materialIndex];
            return material != null
                && material.shader != null
                && material.HasProperty(binding.exposedPropertyName);
        }

        private static bool IsAdapterPropertySupported(
            Transform target,
            VfxPropertyBinding binding)
        {
            if (string.IsNullOrWhiteSpace(binding.adapterId))
            {
                return false;
            }

            VfxRecipeBindingValueType runtimeType =
                ToRuntimeType(binding.propertyType);
            foreach (MonoBehaviour behaviour in target.GetComponents<MonoBehaviour>())
            {
                if (behaviour is IVfxRecipeBindingAdapter adapter
                    && adapter.BindingAdapterId == binding.adapterId
                    && adapter.SupportsBinding(
                        binding.exposedPropertyName,
                        runtimeType))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TargetsOverlap(
            VfxPropertyBinding left,
            VfxPropertyBinding right)
        {
            return left != null
                && right != null
                && left.targetKind == right.targetKind
                && left.targetPath == right.targetPath
                && left.exposedPropertyName == right.exposedPropertyName
                && left.adapterId == right.adapterId
                && left.materialIndex == right.materialIndex
                && (left.componentIndex == right.componentIndex
                    || left.componentIndex < 0
                    || right.componentIndex < 0);
        }

        private static bool HasExposedProperty(
            VisualEffect effect,
            VfxPropertyBinding binding)
        {
            if (effect == null)
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

        private static Transform ResolveTarget(GameObject prefab, string path)
        {
            if (prefab == null)
            {
                return null;
            }

            return string.IsNullOrEmpty(path)
                ? prefab.transform
                : prefab.transform.Find(path);
        }

        private static bool IsSafeTargetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return true;
            }

            if (path.StartsWith("/", StringComparison.Ordinal)
                || path.EndsWith("/", StringComparison.Ordinal)
                || path.Contains("\\"))
            {
                return false;
            }

            foreach (string segment in path.Split('/'))
            {
                if (string.IsNullOrWhiteSpace(segment)
                    || segment == "."
                    || segment == "..")
                {
                    return false;
                }
            }

            return true;
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
