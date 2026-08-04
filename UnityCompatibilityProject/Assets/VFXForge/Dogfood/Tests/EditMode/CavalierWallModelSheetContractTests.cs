using System.IO;
using Kubonsang.VfxForge.Editor;
using NUnit.Framework;
using UnityEngine;

namespace VfxForge.Dogfood.Tests
{
    public sealed class CavalierWallModelSheetContractTests
    {
        private const string EvidenceDirectory =
            "Dogfooding/Evidence/VF-022R-model-sheet";

        [Test]
        public void CandidateEModelSheet_MatchesHashesAndReferenceContract()
        {
            string repository = RepositoryRoot();
            VfxMeshReferenceManifest manifest = ReadJson<VfxMeshReferenceManifest>(
                Path.Combine(repository, EvidenceDirectory, "mesh-reference.json"));

            VfxMeshContractValidation validation =
                VfxMeshContractValidator.Validate(manifest);
            Assert.That(validation.Valid, Is.True, FormatErrors(validation));
            Assert.That(
                VfxMeshReviewStore.ComputeFileSha256(
                    Path.Combine(repository, manifest.candidateBoardPath)),
                Is.EqualTo(manifest.candidateBoardSha256));
            Assert.That(
                VfxMeshReviewStore.ComputeFileSha256(
                    Path.Combine(repository, manifest.modelSheetPath)),
                Is.EqualTo(manifest.modelSheetSha256));
            Assert.That(manifest.views, Has.Length.EqualTo(4));
            Assert.That(manifest.landmarks, Has.Length.EqualTo(25));
        }

        [Test]
        public void CandidateEModelSheet_CamerasAimAtLockedTargets()
        {
            VfxMeshReferenceManifest manifest = ReadReferenceManifest();
            foreach (VfxMeshReferenceView view in manifest.views)
            {
                Vector3 expected = (view.target - view.position).normalized;
                Vector3 actual = Quaternion.Euler(view.rotationEuler)
                    * Vector3.forward;
                Assert.That(
                    Vector3.Dot(expected, actual),
                    Is.GreaterThan(0.999f),
                    $"Locked camera {view.id} does not aim at its target.");
            }
        }

        [Test]
        public void CandidateEModelSheet_RecordsRejectionAndStalesOnInputChange()
        {
            VfxMeshReferenceManifest manifest = ReadReferenceManifest();
            string reviewPath = Path.Combine(
                RepositoryRoot(),
                EvidenceDirectory,
                "model-sheet-review.json");
            VfxMeshReviewRecord submitted =
                ReadJson<VfxMeshReviewRecord>(reviewPath);
            string inputHash = VfxMeshReviewStore.ComputeCombinedSha256(
                manifest.candidateBoardSha256,
                manifest.modelSheetSha256);
            VfxMeshReviewRecord expected = VfxMeshReviewStore.CreateExpected(
                "VF-022",
                VfxMeshReviewStage.ModelSheet,
                inputHash);

            Assert.That(submitted.inputSha256, Is.EqualTo(inputHash));
            Assert.That(
                VfxMeshReviewStore.Evaluate(expected, submitted),
                Is.EqualTo(VfxMeshReviewStatus.Rejected));

            expected.inputSha256 = new string('f', 64);
            Assert.That(
                VfxMeshReviewStore.Evaluate(expected, submitted),
                Is.EqualTo(VfxMeshReviewStatus.ReviewStale));
        }

        private static VfxMeshReferenceManifest ReadReferenceManifest()
        {
            return ReadJson<VfxMeshReferenceManifest>(Path.Combine(
                RepositoryRoot(),
                EvidenceDirectory,
                "mesh-reference.json"));
        }

        private static T ReadJson<T>(string path)
        {
            Assert.That(File.Exists(path), Is.True, path);
            return JsonUtility.FromJson<T>(File.ReadAllText(path));
        }

        private static string RepositoryRoot()
        {
            return Directory.GetParent(Application.dataPath).Parent.FullName;
        }

        private static string FormatErrors(VfxMeshContractValidation validation)
        {
            return string.Join("\n", validation.Results.FindAll(
                result => !result.passed).ConvertAll(
                result => result.ruleId + ": " + result.message));
        }
    }
}
