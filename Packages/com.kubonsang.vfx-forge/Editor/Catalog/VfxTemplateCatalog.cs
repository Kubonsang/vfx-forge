using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kubonsang.VfxForge.Editor
{
    [CreateAssetMenu(menuName = "VFX Forge/Template Catalog", fileName = "VfxTemplateCatalog")]
    public sealed class VfxTemplateCatalog : ScriptableObject
    {
        public List<VfxTemplateEntry> templates = new List<VfxTemplateEntry>();

        public bool TryRegister(
            VfxTemplateEntry candidate,
            out List<VfxValidationResult> validationResults)
        {
            validationResults = VfxTemplateCatalogValidator.ValidateRegistration(this, candidate);
            if (VfxRecipeValidator.HasErrors(validationResults))
            {
                return false;
            }

            if (templates == null)
            {
                templates = new List<VfxTemplateEntry>();
            }

            templates.Add(candidate);
            return true;
        }

        public bool TryGet(string id, out VfxTemplateEntry entry)
        {
            entry = null;
            if (string.IsNullOrWhiteSpace(id) || templates == null)
            {
                return false;
            }

            foreach (VfxTemplateEntry candidate in templates)
            {
                if (candidate != null && string.Equals(candidate.id, id, StringComparison.Ordinal))
                {
                    entry = candidate;
                    return true;
                }
            }

            return false;
        }
    }

    [Serializable]
    public sealed class VfxMeshVariant
    {
        public string key = string.Empty;
        public Mesh mesh;
    }

    [Serializable]
    public sealed class VfxTemplateEntry
    {
        public string id = string.Empty;
        public GameObject prefab;
        public string playEventName = "OnPlay";
        public string[] supportedLayers = Array.Empty<string>();
        public List<VfxPropertyBinding> bindings = new List<VfxPropertyBinding>();
        public List<VfxMeshVariant> meshVariants = new List<VfxMeshVariant>();
    }
}
