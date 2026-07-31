using System;
using System.IO;
using NUnit.Framework;

namespace Kubonsang.VfxForge.Editor.Tests
{
    public sealed class VfxVisualReviewTests
    {
        private string outputPath;

        [SetUp]
        public void SetUp()
        {
            outputPath = Path.Combine(
                Path.GetTempPath(),
                $"visual-review-{Guid.NewGuid():N}.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }

        [Test]
        public void Evaluate_MissingReview_ReturnsReviewRequired()
        {
            VfxVisualReviewEvaluation result =
                VfxVisualReviewStore.Evaluate(
                    CreateExpected(),
                    null);

            Assert.That(
                result.Status,
                Is.EqualTo(
                    VfxVisualReviewStatus.ReviewRequired));
        }

        [Test]
        public void Evaluate_RejectedWithReason_ReturnsRejected()
        {
            VfxVisualReviewRecord submitted =
                CreateDecision(
                    VfxVisualReviewStatus.Rejected);
            submitted.rejectionReason =
                "Silhouette is unclear.";

            VfxVisualReviewEvaluation result =
                VfxVisualReviewStore.Evaluate(
                    CreateExpected(),
                    submitted);

            Assert.That(
                result.Status,
                Is.EqualTo(VfxVisualReviewStatus.Rejected));
        }

        [Test]
        public void Evaluate_StaleHash_ReturnsReviewStale()
        {
            VfxVisualReviewRecord submitted =
                CreateDecision(
                    VfxVisualReviewStatus.Accepted);
            submitted.contactSheetSha256 = "stale";

            VfxVisualReviewEvaluation result =
                VfxVisualReviewStore.Evaluate(
                    CreateExpected(),
                    submitted);

            Assert.That(
                result.Status,
                Is.EqualTo(
                    VfxVisualReviewStatus.ReviewStale));
        }

        [Test]
        public void Evaluate_MatchingFiveCriteria_ReturnsAccepted()
        {
            VfxVisualReviewEvaluation result =
                VfxVisualReviewStore.Evaluate(
                    CreateExpected(),
                    CreateDecision(
                        VfxVisualReviewStatus.Accepted));

            Assert.That(
                result.Status,
                Is.EqualTo(VfxVisualReviewStatus.Accepted));
        }

        [Test]
        public void Evaluate_AcceptedWithFailedCriterion_ReturnsReviewRequired()
        {
            VfxVisualReviewRecord submitted =
                CreateDecision(
                    VfxVisualReviewStatus.Accepted);
            submitted.criteria.timingPolish = false;

            VfxVisualReviewEvaluation result =
                VfxVisualReviewStore.Evaluate(
                    CreateExpected(),
                    submitted);

            Assert.That(
                result.Status,
                Is.EqualTo(
                    VfxVisualReviewStatus.ReviewRequired));
        }

        [Test]
        public void Submit_RejectWithoutReason_ReturnsStableError()
        {
            VfxVisualReviewWriteResult result =
                VfxVisualReviewStore.Submit(
                    outputPath,
                    CreateExpected(),
                    "Fixture Reviewer",
                    false,
                    CreatePassingCriteria(),
                    string.Empty);

            Assert.That(result.Success, Is.False);
            Assert.That(
                result.ErrorCode,
                Is.EqualTo("VISUAL-REVIEW-REASON"));
            Assert.That(File.Exists(outputPath), Is.False);
        }

        private static VfxVisualReviewRecord CreateExpected()
        {
            return new VfxVisualReviewRecord
            {
                generatedPrefabDependencyHash =
                    "prefab-hash",
                captureManifestSha256 =
                    "capture-hash",
                contactSheetSha256 =
                    "contact-hash"
            };
        }

        private static VfxVisualReviewRecord CreateDecision(
            string status)
        {
            VfxVisualReviewRecord record =
                CreateExpected();
            record.status = status;
            record.reviewer = "Fixture Reviewer";
            record.reviewTimeUtc =
                "2026-07-31T00:00:00.0000000Z";
            record.criteria = CreatePassingCriteria();
            return record;
        }

        private static VfxVisualReviewCriteria CreatePassingCriteria()
        {
            return new VfxVisualReviewCriteria
            {
                meaningClear = true,
                silhouetteClear = true,
                shaderPatternFinish = true,
                timingPolish = true,
                gameplayReadability = true
            };
        }
    }
}
