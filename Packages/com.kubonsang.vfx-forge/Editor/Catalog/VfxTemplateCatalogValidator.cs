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
            new Regex("^[a-z0-9][a-z0-9_-]{2,63}$", RegexOptions.CultureInvariant);

        public static List<VfxValidationResult> Validate(VfxTemplateCatalog catalog)
        {
            var results = new List<VfxValidationResult>();
            if (catalog == null)
            {
                results.Add(VfxValidationResult.Error("CATALOG-NULL", "Template Catalog is null."));
                return results;
            }

            if (catalog.templates == null)
            {
                results.Add(VfxValidationResult.Error("CATALOG-TEMPLATES", "Template list is null."));
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
                results.Add(VfxValidationResult.Error("CATALOG-NULL", "Template Catalog is null."));
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
                    && string.Equals(existing.id, candidate.id, StringComparison.Ordinal))
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

            if (string.IsNullOrWhiteSpace(entry.id) || !IdPattern.IsMatch(entry.id))
            {
                results.Add(VfxValidationResult.Error(
                    "CATALOG-ID",
                    $"Template id is invalid at {location}: {entry.id ?? "<null>"}."));
            }

            VisualEffect[] effects = Array.Empty<VisualEffect>();
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

                effects = entry.prefab.GetComponentsInChildren<VisualEffect>(true);
                if (effects.Length == 0)
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
            ValidateBindings(entry.bindings, effects, location, results);
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
                if (string.IsNullOrWhiteSpace(layer) || !uniqueLayers.Add(layer))
                {
                    results.Add(VfxValidationResult.Error(
                        "CATALOG-LAYER",
                        $"Supported layer is empty or duplicated at {location}: {layer ?? "<null>"}."));
                }
            }
        }

        private static void ValidateBindings(
            IList<VfxPropertyBinding> bindings,
            VisualEffect[] effects,
            string location,
            List<VfxValidationResult> results)
        {
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

                bool propertyNameValid = !string.IsNullOrWhiteSpace(binding.exposedPropertyName);
                if (!propertyNameValid)
                {
                    results.Add(VfxValidationResult.Error(
                        "CATALOG-BINDING-NAME",
                        $"Exposed property name is empty at {bindingLocation}."));
                }

                bool componentValid = binding.componentIndex >= -1
                    && (binding.componentIndex < 0 || binding.componentIndex < effects.Length);
                if (!componentValid)
                {
                    results.Add(VfxValidationResult.Error(
                        "CATALOG-BINDING-COMPONENT",
                        $"VisualEffect component index is invalid at {bindingLocation}: {binding.componentIndex}."));
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

                if (pathValid
                    && binding.propertyType == expectedType
                    && propertyNameValid
                    && componentValid
                    && effects.Length > 0
                    && !HasExposedProperty(effects, binding))
                {
                    results.Add(binding.required
                        ? VfxValidationResult.Error(
                            "CATALOG-BINDING-PROPERTY",
                            $"Required exposed property was not found at {bindingLocation}: {binding.exposedPropertyName}.")
                        : VfxValidationResult.Warning(
                            "CATALOG-BINDING-PROPERTY",
                            $"Optional exposed property was not found at {bindingLocation}: {binding.exposedPropertyName}."));
                }

                priorBindings.Add(binding);
            }
        }

        private static bool TargetsOverlap(VfxPropertyBinding left, VfxPropertyBinding right)
        {
            return left != null
                && right != null
                && !string.IsNullOrWhiteSpace(left.exposedPropertyName)
                && string.Equals(
                    left.exposedPropertyName,
                    right.exposedPropertyName,
                    StringComparison.Ordinal)
                && (left.componentIndex == right.componentIndex
                    || left.componentIndex < 0
                    || right.componentIndex < 0);
        }

        private static bool HasExposedProperty(
            IReadOnlyList<VisualEffect> effects,
            VfxPropertyBinding binding)
        {
            int start = binding.componentIndex < 0 ? 0 : binding.componentIndex;
            int end = binding.componentIndex < 0 ? effects.Count : binding.componentIndex + 1;
            for (int index = start; index < end; index++)
            {
                VisualEffect effect = effects[index];
                if (effect != null && HasExposedProperty(effect, binding))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasExposedProperty(
            VisualEffect effect,
            VfxPropertyBinding binding)
        {
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
