using UnityEngine;

namespace Kubonsang.VfxForge
{
    [DisallowMultipleComponent]
    public sealed class VfxMetadata : MonoBehaviour
    {
        public string recipeId = string.Empty;
        public string schemaVersion = string.Empty;
        public string templateId = string.Empty;
        public string recipeAssetPath = string.Empty;
        public string generatedAtUtc = string.Empty;
    }
}
