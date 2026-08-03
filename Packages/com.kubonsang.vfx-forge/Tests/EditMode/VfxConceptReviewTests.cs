using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Kubonsang.VfxForge.Editor.Tests
{
    public sealed class VfxConceptReviewTests
    {
        private string fileRoot;

        [SetUp]
        public void SetUp()
        {
            fileRoot = Path.Combine(
                Path.GetTempPath(),
                $"vfx-concepts-{System.Guid.NewGuid():N}");
            Directory.CreateDirectory(fileRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(fileRoot))
            {
                Directory.Delete(fileRoot, true);
            }
        }

        [Test]
        public void Manifest_ValidFourCandidates_Passes()
        {
            VfxConceptManifestValidation result =
                VfxConceptManifestValidator.ParseAndValidate(
                    UnityEngine.JsonUtility.ToJson(
                        CreateManifest(),
                        true));

            Assert.That(result.Valid, Is.True);
        }

        [Test]
        public void Manifest_DifferentFraming_FailsCameraLock()
        {
            VfxConceptCandidateManifest manifest = CreateManifest();
            manifest.candidates[3].width = 1400;

            var results = VfxConceptManifestValidator.Validate(manifest);

            Assert.That(
                results.Exists(
                    item => item.ruleId == "CONCEPT-CAMERA-LOCK"
                        && !item.passed),
                Is.True);
        }

        [Test]
        public void Manifest_MissingEvidence_Fails()
        {
            VfxConceptCandidateManifest manifest = CreateManifest();
            manifest.candidates[1].evidence = new[]
            {
                "grayscale_silhouette"
            };

            var results = VfxConceptManifestValidator.Validate(manifest);

            Assert.That(
                results.Exists(
                    item => item.ruleId == "CONCEPT-EVIDENCE"
                        && !item.passed),
                Is.True);
        }

        [Test]
        public void ManifestFiles_TamperedBoardHash_Fails()
        {
            VfxConceptCandidateManifest manifest = CreateManifest();
            string promptPath = Path.Combine(fileRoot, "prompt.md");
            File.WriteAllText(promptPath, "prompt");
            manifest.promptSetPath = "prompt.md";
            manifest.promptSetSha256 =
                VfxConceptReviewStore.ComputeFileSha256(promptPath);
            manifest.candidates = new[]
            {
                manifest.candidates[0]
            };
            string boardPath = Path.Combine(fileRoot, "board.png");
            WritePng(boardPath, 16, 16);
            manifest.candidates[0].boardPath = "board.png";
            manifest.candidates[0].boardSha256 = new string('f', 64);
            manifest.candidates[0].width = 16;
            manifest.candidates[0].height = 16;

            var results = VfxConceptManifestValidator.ValidateFiles(
                manifest,
                fileRoot);

            Assert.That(
                results.Exists(
                    item => item.ruleId == "CONCEPT-BOARD-FILE"
                        && !item.passed),
                Is.True);
        }

        [Test]
        public void Review_MissingSelection_RemainsRequired()
        {
            VfxConceptReviewEvaluation result =
                VfxConceptReviewStore.Evaluate(
                    CreateExpected(),
                    null,
                    CreateManifest());

            Assert.That(
                result.Status,
                Is.EqualTo(
                    VfxConceptReviewStatus.SelectionRequired));
        }

        [Test]
        public void Review_StaleManifest_IsRejectedAsStale()
        {
            VfxConceptReviewRecord submitted = CreateSelection();
            submitted.candidateManifestSha256 = "stale";

            VfxConceptReviewEvaluation result =
                VfxConceptReviewStore.Evaluate(
                    CreateExpected(),
                    submitted,
                    CreateManifest());

            Assert.That(
                result.Status,
                Is.EqualTo(VfxConceptReviewStatus.ReviewStale));
        }

        [Test]
        public void Review_ValidHumanSelection_IsSelected()
        {
            VfxConceptReviewEvaluation result =
                VfxConceptReviewStore.Evaluate(
                    CreateExpected(),
                    CreateSelection(),
                    CreateManifest());

            Assert.That(
                result.Status,
                Is.EqualTo(VfxConceptReviewStatus.Selected));
        }

        [Test]
        public void Review_FailedCriterion_RemainsRequired()
        {
            VfxConceptReviewRecord submitted = CreateSelection();
            submitted.criteria.silhouetteQuality = false;

            VfxConceptReviewEvaluation result =
                VfxConceptReviewStore.Evaluate(
                    CreateExpected(),
                    submitted,
                    CreateManifest());

            Assert.That(
                result.Status,
                Is.EqualTo(
                    VfxConceptReviewStatus.SelectionRequired));
        }

        [Test]
        public void Review_UnknownCandidate_RemainsRequired()
        {
            VfxConceptReviewRecord submitted = CreateSelection();
            submitted.selectedCandidateId = "candidate_z";

            VfxConceptReviewEvaluation result =
                VfxConceptReviewStore.Evaluate(
                    CreateExpected(),
                    submitted,
                    CreateManifest());

            Assert.That(
                result.Status,
                Is.EqualTo(
                    VfxConceptReviewStatus.SelectionRequired));
        }

        private static VfxConceptCandidateManifest CreateManifest()
        {
            var candidates = new VfxConceptCandidate[4];
            for (int index = 0; index < candidates.Length; index++)
            {
                candidates[index] = new VfxConceptCandidate
                {
                    id = $"candidate_{(char)('a' + index)}",
                    title = $"Candidate {index}",
                    boardPath = $"Dogfooding/candidate-{index}.png",
                    boardSha256 = new string('a', 64),
                    cameraView = "strict_top_down",
                    width = 1536,
                    height = 1024,
                    effectFootprintPixels = 360,
                    evidence = new[]
                    {
                        "grayscale_silhouette",
                        "full_color_concept",
                        "three_ground_composite",
                        "labeled_breakdown"
                    },
                    shapeSummary = "Dominant connected shield plate.",
                    knownRisks = new[]
                    {
                        "May read too symmetrically."
                    }
                };
            }

            return new VfxConceptCandidateManifest
            {
                taskId = "VF-021",
                referenceBoardSha256 = new string('b', 64),
                artDirectionBriefSha256 = new string('c', 64),
                promptSetPath = "Dogfooding/concept-prompts.md",
                promptSetSha256 = new string('d', 64),
                generator = "OpenAI built-in image generation",
                candidates = candidates
            };
        }

        private static VfxConceptReviewRecord CreateExpected()
        {
            return new VfxConceptReviewRecord
            {
                candidateManifestSha256 = new string('e', 64)
            };
        }

        private static VfxConceptReviewRecord CreateSelection()
        {
            return new VfxConceptReviewRecord
            {
                status = VfxConceptReviewStatus.Selected,
                candidateManifestSha256 = new string('e', 64),
                selectedCandidateId = "candidate_a",
                reviewer = "Fixture Reviewer",
                reviewTimeUtc =
                    "2026-08-03T00:00:00.0000000Z",
                decisionReason = "Best connected silhouette.",
                criteria = new VfxConceptReviewCriteria
                {
                    shieldMeaning = true,
                    silhouetteQuality = true,
                    connectedStructure = true,
                    contemporaryFinishPotential = true,
                    gameplayReadability = true
                }
            };
        }

        private static void WritePng(
            string path,
            int width,
            int height)
        {
            var texture = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
