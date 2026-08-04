using UnityEngine;

namespace VfxForge.Dogfood
{
    [DisallowMultipleComponent]
    public sealed class SymmetricShieldBlockoutMarker : MonoBehaviour
    {
        [SerializeField] private string schemaVersion =
            "symmetric-shield-blockout-1.0";
        [SerializeField] private string modelSheetSha256 = string.Empty;
        [SerializeField] private string meshReferenceSha256 = string.Empty;
        [SerializeField] private string authoringRevision = string.Empty;

        public string SchemaVersion => schemaVersion;
        public string ModelSheetSha256 => modelSheetSha256;
        public string MeshReferenceSha256 => meshReferenceSha256;
        public string AuthoringRevision => authoringRevision;

        public void Configure(
            string sheetSha256,
            string referenceSha256,
            string revision)
        {
            modelSheetSha256 = sheetSha256 ?? string.Empty;
            meshReferenceSha256 = referenceSha256 ?? string.Empty;
            authoringRevision = revision ?? string.Empty;
        }
    }
}
