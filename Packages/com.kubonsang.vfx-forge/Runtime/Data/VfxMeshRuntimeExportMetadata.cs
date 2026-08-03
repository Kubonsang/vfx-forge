using UnityEngine;

namespace Kubonsang.VfxForge
{
    [DisallowMultipleComponent]
    public sealed class VfxMeshRuntimeExportMetadata : MonoBehaviour
    {
        public string schemaVersion = "mesh-runtime-export-1.0";
        public string sourcePrefabGuid = string.Empty;
        public string sourceDependencyHash = string.Empty;
    }
}
