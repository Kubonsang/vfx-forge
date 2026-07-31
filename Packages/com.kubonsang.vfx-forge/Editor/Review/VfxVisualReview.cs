using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace Kubonsang.VfxForge.Editor
{
    [Serializable]
    public sealed class VfxVisualReviewCriteria
    {
        public bool meaningClear;
        public bool silhouetteClear;
        public bool shaderPatternFinish;
        public bool timingPolish;
        public bool gameplayReadability;

        public bool AllPassed()
        {
            return meaningClear
                && silhouetteClear
                && shaderPatternFinish
                && timingPolish
                && gameplayReadability;
        }
    }

    [Serializable]
    public sealed class VfxVisualReviewRecord
    {
        public string schemaVersion = "visual-review-1.0";
        public string status = VfxVisualReviewStatus.ReviewRequired;
        public string generatedPrefabDependencyHash = string.Empty;
        public string captureManifestSha256 = string.Empty;
        public string contactSheetSha256 = string.Empty;
        public string reviewer = string.Empty;
        public string reviewTimeUtc = string.Empty;
        public VfxVisualReviewCriteria criteria =
            new VfxVisualReviewCriteria();
        public string rejectionReason = string.Empty;
    }

    public static class VfxVisualReviewStatus
    {
        public const string ReviewRequired = "review_required";
        public const string Rejected = "rejected";
        public const string ReviewStale = "review_stale";
        public const string Accepted = "accepted";
    }

    public sealed class VfxVisualReviewEvaluation
    {
        public string Status = VfxVisualReviewStatus.ReviewRequired;
        public string Message = string.Empty;
        public string OutputPath = string.Empty;
        public VfxVisualReviewRecord Record;
    }

    public sealed class VfxVisualReviewWriteResult
    {
        public bool Success;
        public string ErrorCode = string.Empty;
        public string Message = string.Empty;
        public VfxVisualReviewRecord Record;
    }

    public static class VfxVisualReviewStore
    {
        public const string FileName = "visual-review.json";

        public static VfxVisualReviewEvaluation Prepare(
            string artifactDirectory,
            string generatedPrefabPath,
            string captureManifestPath,
            string contactSheetPath,
            string submittedReviewPath)
        {
            string outputPath =
                Path.Combine(artifactDirectory, FileName);
            VfxVisualReviewRecord expected = CreateExpected(
                generatedPrefabPath,
                captureManifestPath,
                contactSheetPath);
            VfxVisualReviewRecord submitted = null;
            if (!string.IsNullOrWhiteSpace(submittedReviewPath)
                && File.Exists(submittedReviewPath))
            {
                try
                {
                    submitted = JsonUtility.FromJson<VfxVisualReviewRecord>(
                        File.ReadAllText(submittedReviewPath));
                }
                catch (Exception)
                {
                    submitted = null;
                }
            }

            VfxVisualReviewEvaluation evaluation =
                Evaluate(expected, submitted);
            evaluation.OutputPath = outputPath;
            Directory.CreateDirectory(artifactDirectory);
            File.WriteAllText(
                outputPath,
                JsonUtility.ToJson(evaluation.Record, true));
            return evaluation;
        }

        public static VfxVisualReviewRecord CreateExpected(
            string generatedPrefabPath,
            string captureManifestPath,
            string contactSheetPath)
        {
            if (string.IsNullOrWhiteSpace(generatedPrefabPath)
                || string.IsNullOrWhiteSpace(captureManifestPath)
                || string.IsNullOrWhiteSpace(contactSheetPath)
                || !File.Exists(captureManifestPath)
                || !File.Exists(contactSheetPath))
            {
                throw new InvalidOperationException(
                    "Generated Prefab, Capture Manifest, and Contact Sheet are required for visual review.");
            }

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    generatedPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Generated Prefab could not be loaded: {generatedPrefabPath}");
            }

            return new VfxVisualReviewRecord
            {
                generatedPrefabDependencyHash =
                    AssetDatabase.GetAssetDependencyHash(
                        generatedPrefabPath).ToString(),
                captureManifestSha256 =
                    ComputeSha256(captureManifestPath),
                contactSheetSha256 =
                    ComputeSha256(contactSheetPath)
            };
        }

        public static VfxVisualReviewEvaluation Evaluate(
            VfxVisualReviewRecord expected,
            VfxVisualReviewRecord submitted)
        {
            if (expected == null)
            {
                throw new ArgumentNullException(nameof(expected));
            }
            if (submitted == null
                || submitted.schemaVersion != "visual-review-1.0")
            {
                expected.status =
                    VfxVisualReviewStatus.ReviewRequired;
                return Result(
                    expected,
                    VfxVisualReviewStatus.ReviewRequired,
                    "A matching human visual review is required.");
            }

            if (!HashesMatch(expected, submitted))
            {
                submitted.status =
                    VfxVisualReviewStatus.ReviewStale;
                return Result(
                    submitted,
                    VfxVisualReviewStatus.ReviewStale,
                    "The submitted visual review hashes are stale.");
            }

            if (submitted.status
                == VfxVisualReviewStatus.Rejected)
            {
                if (string.IsNullOrWhiteSpace(
                    submitted.reviewer)
                    || string.IsNullOrWhiteSpace(
                        submitted.reviewTimeUtc)
                    || string.IsNullOrWhiteSpace(
                        submitted.rejectionReason))
                {
                    expected.status =
                        VfxVisualReviewStatus.ReviewRequired;
                    return Result(
                        expected,
                        VfxVisualReviewStatus.ReviewRequired,
                        "A rejected review requires reviewer, time, and reason.");
                }

                return Result(
                    submitted,
                    VfxVisualReviewStatus.Rejected,
                    submitted.rejectionReason);
            }

            if (submitted.status
                == VfxVisualReviewStatus.Accepted
                && !string.IsNullOrWhiteSpace(
                    submitted.reviewer)
                && !string.IsNullOrWhiteSpace(
                    submitted.reviewTimeUtc)
                && submitted.criteria != null
                && submitted.criteria.AllPassed())
            {
                submitted.rejectionReason = string.Empty;
                return Result(
                    submitted,
                    VfxVisualReviewStatus.Accepted,
                    "Human visual review accepted.");
            }

            expected.status =
                VfxVisualReviewStatus.ReviewRequired;
            return Result(
                expected,
                VfxVisualReviewStatus.ReviewRequired,
                "All five visual criteria must pass before acceptance.");
        }

        public static VfxVisualReviewWriteResult Submit(
            string outputPath,
            VfxVisualReviewRecord expected,
            string reviewer,
            bool accept,
            VfxVisualReviewCriteria criteria,
            string rejectionReason)
        {
            if (expected == null
                || string.IsNullOrWhiteSpace(outputPath)
                || string.IsNullOrWhiteSpace(reviewer))
            {
                return WriteFailure(
                    "VISUAL-REVIEW-INPUT",
                    "Review path, current hashes, and reviewer are required.");
            }
            if (accept
                && (criteria == null || !criteria.AllPassed()))
            {
                return WriteFailure(
                    "VISUAL-REVIEW-CRITERIA",
                    "All five visual criteria must pass before acceptance.");
            }
            if (!accept
                && string.IsNullOrWhiteSpace(rejectionReason))
            {
                return WriteFailure(
                    "VISUAL-REVIEW-REASON",
                    "Rejection reason is required.");
            }

            var record = new VfxVisualReviewRecord
            {
                status = accept
                    ? VfxVisualReviewStatus.Accepted
                    : VfxVisualReviewStatus.Rejected,
                generatedPrefabDependencyHash =
                    expected.generatedPrefabDependencyHash,
                captureManifestSha256 =
                    expected.captureManifestSha256,
                contactSheetSha256 =
                    expected.contactSheetSha256,
                reviewer = reviewer.Trim(),
                reviewTimeUtc =
                    DateTime.UtcNow.ToString(
                        "O",
                        CultureInfo.InvariantCulture),
                criteria =
                    criteria ?? new VfxVisualReviewCriteria(),
                rejectionReason =
                    accept ? string.Empty : rejectionReason.Trim()
            };
            Directory.CreateDirectory(
                Path.GetDirectoryName(
                    Path.GetFullPath(outputPath)));
            File.WriteAllText(
                outputPath,
                JsonUtility.ToJson(record, true));
            return new VfxVisualReviewWriteResult
            {
                Success = true,
                Message = $"Visual review recorded: {record.status}.",
                Record = record
            };
        }

        private static bool HashesMatch(
            VfxVisualReviewRecord expected,
            VfxVisualReviewRecord submitted)
        {
            return expected.generatedPrefabDependencyHash
                    == submitted.generatedPrefabDependencyHash
                && expected.captureManifestSha256
                    == submitted.captureManifestSha256
                && expected.contactSheetSha256
                    == submitted.contactSheetSha256;
        }

        private static string ComputeSha256(string path)
        {
            using (SHA256 algorithm = SHA256.Create())
            {
                byte[] hash =
                    algorithm.ComputeHash(
                        File.ReadAllBytes(path));
                return BitConverter.ToString(hash)
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }

        private static VfxVisualReviewEvaluation Result(
            VfxVisualReviewRecord record,
            string status,
            string message)
        {
            return new VfxVisualReviewEvaluation
            {
                Status = status,
                Message = message,
                Record = record
            };
        }

        private static VfxVisualReviewWriteResult WriteFailure(
            string errorCode,
            string message)
        {
            return new VfxVisualReviewWriteResult
            {
                ErrorCode = errorCode,
                Message = message
            };
        }
    }
}
