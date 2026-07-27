using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kubonsang.VfxForge.Editor
{
    [CreateAssetMenu(menuName = "VFX Forge/Template Catalog", fileName = "VfxTemplateCatalog")]
    public sealed class VfxTemplateCatalog : ScriptableObject
    {
        public List<VfxTemplateEntry> templates = new List<VfxTemplateEntry>();

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
    public sealed class VfxTemplateEntry
    {
        public string id = string.Empty;
        public GameObject prefab;
        public string playEventName = "OnPlay";
        public string[] supportedLayers = Array.Empty<string>();
        public List<VfxPropertyBinding> bindings = new List<VfxPropertyBinding>();
    }
}
