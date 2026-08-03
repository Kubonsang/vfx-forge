using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Kubonsang.VfxForge.Editor
{
    [Serializable]
    public sealed class VfxConceptCandidateManifest
    {
        public string schemaVersion = "concept-candidates-1.0";
        public string taskId = string.Empty;
        public string referenceBoardSha256 = string.Empty;
        public string artDirectionBriefSha256 = string.Empty;
        public string promptSetPath = string.Empty;
        public string promptSetSha256 = string.Empty;
        public string generator = string.Empty;
        public VfxConceptCandidate[] candidates =
            Array.Empty<VfxConceptCandidate>();
    }

    [Serializable]
    public sealed class VfxConceptCandidate
    {
        public string id = string.Empty;
        public string title = string.Empty;
        public string boardPath = string.Empty;
        public string boardSha256 = string.Empty;
        public string cameraView = "strict_top_down";
        public int width;
        public int height;
        public int effectFootprintPixels;
        public string[] evidence = Array.Empty<string>();
        public string shapeSummary = string.Empty;
        public string[] knownRisks = Array.Empty<string>();
    }

    [Serializable]
    public sealed class VfxConceptReviewCriteria
    {
        public bool shieldMeaning;
        public bool silhouetteQuality;
        public bool connectedStructure;
        public bool contemporaryFinishPotential;
        public bool gameplayReadability;

        public bool AllPassed()
        {
            return shieldMeaning
                && silhouetteQuality
                && connectedStructure
                && contemporaryFinishPotential
                && gameplayReadability;
        }
    }

    [Serializable]
    public sealed class VfxConceptReviewRecord
    {
        public string schemaVersion = "concept-review-1.0";
        public string status = VfxConceptReviewStatus.SelectionRequired;
        public string candidateManifestSha256 = string.Empty;
        public string selectedCandidateId = string.Empty;
        public string reviewer = string.Empty;
        public string reviewTimeUtc = string.Empty;
        public VfxConceptReviewCriteria criteria =
            new VfxConceptReviewCriteria();
        public string decisionReason = string.Empty;
    }

    public static class VfxConceptReviewStatus
    {
        public const string SelectionRequired = "selection_required";
        public const string Selected = "selected";
        public const string Rejected = "rejected";
        public const string ReviewStale = "review_stale";
    }

    public sealed class VfxConceptManifestValidation
    {
        public VfxConceptCandidateManifest Manifest;
        public List<VfxValidationResult> Results =
            new List<VfxValidationResult>();
        public bool Valid => !VfxRecipeValidator.HasErrors(Results);
    }

    public sealed class VfxConceptReviewEvaluation
    {
        public string Status = VfxConceptReviewStatus.SelectionRequired;
        public string Message = string.Empty;
        public VfxConceptReviewRecord Record;
    }

    public static class VfxConceptManifestValidator
    {
        private static readonly Regex TaskPattern =
            new Regex("^VF-[0-9]{3}$");
        private static readonly Regex IdPattern =
            new Regex("^[a-z0-9][a-z0-9_-]{2,63}$");
        private static readonly Regex Sha256Pattern =
            new Regex("^[a-f0-9]{64}$");
        private static readonly HashSet<string> RequiredEvidence =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "grayscale_silhouette",
                "full_color_concept",
                "three_ground_composite",
                "labeled_breakdown"
            };

        public static VfxConceptManifestValidation ParseAndValidate(
            string json)
        {
            var result = new VfxConceptManifestValidation();
            if (string.IsNullOrWhiteSpace(json))
            {
                result.Results.Add(VfxValidationResult.Error(
                    "CONCEPT-JSON-EMPTY",
                    "Concept Candidate Manifest JSON is empty."));
                return result;
            }

            try
            {
                result.Manifest =
                    JsonUtility.FromJson<VfxConceptCandidateManifest>(json);
            }
            catch (Exception)
            {
                result.Results.Add(VfxValidationResult.Error(
                    "CONCEPT-JSON-MALFORMED",
                    "Concept Candidate Manifest JSON is malformed."));
                return result;
            }

            result.Results.AddRange(Validate(result.Manifest));
            return result;
        }

        public static List<VfxValidationResult> Validate(
            VfxConceptCandidateManifest manifest)
        {
            var results = new List<VfxValidationResult>();
            if (manifest == null)
            {
                results.Add(VfxValidationResult.Error(
                    "CONCEPT-NULL",
                    "Concept Candidate Manifest is null."));
                return results;
            }

            Require(
                results,
                "CONCEPT-SCHEMA",
                manifest.schemaVersion == "concept-candidates-1.0",
                "Only concept-candidates-1.0 is supported.");
            Require(
                results,
                "CONCEPT-TASK",
                !string.IsNullOrWhiteSpace(manifest.taskId)
                    && TaskPattern.IsMatch(manifest.taskId),
                "Concept taskId must use VF-000 form.");
            RequireHash(results, "CONCEPT-BOARD-HASH", manifest.referenceBoardSha256);
            RequireHash(results, "CONCEPT-BRIEF-HASH", manifest.artDirectionBriefSha256);
            RequireHash(results, "CONCEPT-PROMPT-HASH", manifest.promptSetSha256);
            Require(
                results,
                "CONCEPT-PROMPT-PATH",
                IsSafeRelativePath(manifest.promptSetPath),
                "Prompt Set path must be relative and traversal-free.");
            Require(
                results,
                "CONCEPT-GENERATOR",
                !string.IsNullOrWhiteSpace(manifest.generator),
                "Concept generator metadata is required.");
            Require(
                results,
                "CONCEPT-COUNT",
                manifest.candidates != null
                    && manifest.candidates.Length >= 3
                    && manifest.candidates.Length <= 6,
                "Concept Manifest must contain three to six candidates.");

            if (manifest.candidates == null
                || manifest.candidates.Length == 0)
            {
                return results;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            int width = manifest.candidates[0]?.width ?? 0;
            int height = manifest.candidates[0]?.height ?? 0;
            int footprint =
                manifest.candidates[0]?.effectFootprintPixels ?? 0;
            foreach (VfxConceptCandidate candidate in manifest.candidates)
            {
                bool identity = candidate != null
                    && !string.IsNullOrWhiteSpace(candidate.id)
                    && IdPattern.IsMatch(candidate.id)
                    && ids.Add(candidate.id)
                    && !string.IsNullOrWhiteSpace(candidate.title);
                Require(
                    results,
                    "CONCEPT-CANDIDATE-ID",
                    identity,
                    "Candidate id must be valid and unique, with a title.");
                if (candidate == null)
                {
                    continue;
                }

                Require(
                    results,
                    "CONCEPT-CAMERA-LOCK",
                    candidate.cameraView == "strict_top_down"
                        && candidate.width == width
                        && candidate.height == height
                        && candidate.effectFootprintPixels == footprint
                        && width > 0
                        && height > 0
                        && footprint > 0,
                    "Every candidate must use identical strict-top-down framing and gameplay footprint.");
                Require(
                    results,
                    "CONCEPT-BOARD-PATH",
                    IsSafeRelativePath(candidate.boardPath),
                    $"Candidate {candidate.id} board path is unsafe.");
                RequireHash(
                    results,
                    "CONCEPT-BOARD-SHA",
                    candidate.boardSha256);
                Require(
                    results,
                    "CONCEPT-EVIDENCE",
                    HasExactEvidence(candidate.evidence),
                    $"Candidate {candidate.id} must provide all four evidence roles exactly once.");
                Require(
                    results,
                    "CONCEPT-SHAPE-SUMMARY",
                    !string.IsNullOrWhiteSpace(candidate.shapeSummary),
                    $"Candidate {candidate.id} shape summary is required.");
                Require(
                    results,
                    "CONCEPT-KNOWN-RISKS",
                    candidate.knownRisks != null
                        && candidate.knownRisks.Length > 0,
                    $"Candidate {candidate.id} must state at least one known risk.");
            }

            return results;
        }

        public static List<VfxValidationResult> ValidateFiles(
            VfxConceptCandidateManifest manifest,
            string rootDirectory)
        {
            var results = new List<VfxValidationResult>();
            if (manifest == null
                || string.IsNullOrWhiteSpace(rootDirectory))
            {
                results.Add(VfxValidationResult.Error(
                    "CONCEPT-FILE-INPUT",
                    "Manifest and root directory are required."));
                return results;
            }

            ValidateHashedFile(
                results,
                rootDirectory,
                manifest.promptSetPath,
                manifest.promptSetSha256,
                0,
                0,
                "CONCEPT-PROMPT-FILE");
            if (manifest.candidates == null)
            {
                return results;
            }

            foreach (VfxConceptCandidate candidate in manifest.candidates)
            {
                if (candidate == null)
                {
                    continue;
                }

                ValidateHashedFile(
                    results,
                    rootDirectory,
                    candidate.boardPath,
                    candidate.boardSha256,
                    candidate.width,
                    candidate.height,
                    "CONCEPT-BOARD-FILE");
            }

            return results;
        }

        private static bool HasExactEvidence(string[] evidence)
        {
            if (evidence == null
                || evidence.Length != RequiredEvidence.Count)
            {
                return false;
            }

            return new HashSet<string>(
                evidence,
                StringComparer.Ordinal).SetEquals(RequiredEvidence);
        }

        private static void ValidateHashedFile(
            List<VfxValidationResult> results,
            string rootDirectory,
            string relativePath,
            string expectedHash,
            int expectedWidth,
            int expectedHeight,
            string ruleId)
        {
            if (!TryResolvePath(
                rootDirectory,
                relativePath,
                out string path)
                || !File.Exists(path))
            {
                results.Add(VfxValidationResult.Error(
                    ruleId,
                    $"Required concept file is missing: {relativePath}"));
                return;
            }

            bool hashMatches = string.Equals(
                VfxConceptReviewStore.ComputeFileSha256(path),
                expectedHash,
                StringComparison.OrdinalIgnoreCase);
            Require(
                results,
                ruleId,
                hashMatches,
                $"Concept file hash changed: {relativePath}");
            if (expectedWidth <= 0 || expectedHeight <= 0)
            {
                return;
            }

            byte[] bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            bool loaded = ImageConversion.LoadImage(texture, bytes, false);
            bool dimensionsMatch = loaded
                && texture.width == expectedWidth
                && texture.height == expectedHeight;
            UnityEngine.Object.DestroyImmediate(texture);
            Require(
                results,
                "CONCEPT-BOARD-DIMENSIONS",
                dimensionsMatch,
                $"Concept board dimensions changed: {relativePath}");
        }

        private static bool TryResolvePath(
            string rootDirectory,
            string relativePath,
            out string fullPath)
        {
            fullPath = string.Empty;
            if (!IsSafeRelativePath(relativePath))
            {
                return false;
            }

            string root = Path.GetFullPath(rootDirectory)
                .TrimEnd(Path.DirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string candidate = Path.GetFullPath(
                Path.Combine(root, relativePath));
            if (!candidate.StartsWith(root, StringComparison.Ordinal))
            {
                return false;
            }

            fullPath = candidate;
            return true;
        }

        private static void RequireHash(
            List<VfxValidationResult> results,
            string id,
            string hash)
        {
            Require(
                results,
                id,
                !string.IsNullOrWhiteSpace(hash)
                    && Sha256Pattern.IsMatch(hash.ToLowerInvariant()),
                "SHA-256 must contain 64 hexadecimal characters.");
        }

        private static bool IsSafeRelativePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)
                || Path.IsPathRooted(path))
            {
                return false;
            }

            string normalized = path.Replace('\\', '/');
            return !normalized.StartsWith("../", StringComparison.Ordinal)
                && !normalized.Contains("/../")
                && !normalized.EndsWith("/", StringComparison.Ordinal);
        }

        private static void Require(
            List<VfxValidationResult> results,
            string id,
            bool condition,
            string message)
        {
            results.Add(condition
                ? VfxValidationResult.Pass(id, message)
                : VfxValidationResult.Error(id, message));
        }
    }

    public static class VfxConceptReviewStore
    {
        public static VfxConceptReviewRecord CreateExpected(
            string candidateManifestPath)
        {
            if (string.IsNullOrWhiteSpace(candidateManifestPath)
                || !File.Exists(candidateManifestPath))
            {
                throw new InvalidOperationException(
                    "A Concept Candidate Manifest file is required.");
            }

            return new VfxConceptReviewRecord
            {
                candidateManifestSha256 =
                    ComputeFileSha256(candidateManifestPath)
            };
        }

        public static VfxConceptReviewEvaluation Evaluate(
            VfxConceptReviewRecord expected,
            VfxConceptReviewRecord submitted,
            VfxConceptCandidateManifest manifest)
        {
            if (expected == null)
            {
                throw new ArgumentNullException(nameof(expected));
            }

            if (submitted == null
                || submitted.schemaVersion != "concept-review-1.0")
            {
                return Result(
                    expected,
                    VfxConceptReviewStatus.SelectionRequired,
                    "A human concept selection is required.");
            }

            if (!string.Equals(
                expected.candidateManifestSha256,
                submitted.candidateManifestSha256,
                StringComparison.OrdinalIgnoreCase))
            {
                submitted.status = VfxConceptReviewStatus.ReviewStale;
                return Result(
                    submitted,
                    VfxConceptReviewStatus.ReviewStale,
                    "The submitted concept selection is stale.");
            }

            if (submitted.status == VfxConceptReviewStatus.Rejected)
            {
                if (HasDecisionIdentity(submitted)
                    && !string.IsNullOrWhiteSpace(submitted.decisionReason))
                {
                    return Result(
                        submitted,
                        VfxConceptReviewStatus.Rejected,
                        submitted.decisionReason);
                }

                return Result(
                    expected,
                    VfxConceptReviewStatus.SelectionRequired,
                    "A rejected concept set requires reviewer, time, and reason.");
            }

            bool selected = submitted.status
                    == VfxConceptReviewStatus.Selected
                && HasDecisionIdentity(submitted)
                && submitted.criteria != null
                && submitted.criteria.AllPassed()
                && !string.IsNullOrWhiteSpace(submitted.decisionReason)
                && CandidateExists(
                    manifest,
                    submitted.selectedCandidateId);
            return selected
                ? Result(
                    submitted,
                    VfxConceptReviewStatus.Selected,
                    "Human concept selection accepted.")
                : Result(
                    expected,
                    VfxConceptReviewStatus.SelectionRequired,
                    "Selection requires a valid candidate, five passing criteria, reviewer, time, and reason.");
        }

        public static VfxConceptReviewRecord CreateSelection(
            VfxConceptReviewRecord expected,
            string candidateId,
            string reviewer,
            VfxConceptReviewCriteria criteria,
            string reason)
        {
            if (expected == null)
            {
                throw new ArgumentNullException(nameof(expected));
            }

            return new VfxConceptReviewRecord
            {
                status = VfxConceptReviewStatus.Selected,
                candidateManifestSha256 =
                    expected.candidateManifestSha256,
                selectedCandidateId = candidateId?.Trim() ?? string.Empty,
                reviewer = reviewer?.Trim() ?? string.Empty,
                reviewTimeUtc = DateTime.UtcNow.ToString(
                    "O",
                    CultureInfo.InvariantCulture),
                criteria = criteria ?? new VfxConceptReviewCriteria(),
                decisionReason = reason?.Trim() ?? string.Empty
            };
        }

        public static string ComputeFileSha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                byte[] hash = sha.ComputeHash(stream);
                var builder = new StringBuilder(hash.Length * 2);
                foreach (byte value in hash)
                {
                    builder.Append(value.ToString("x2"));
                }

                return builder.ToString();
            }
        }

        private static bool HasDecisionIdentity(
            VfxConceptReviewRecord record)
        {
            return !string.IsNullOrWhiteSpace(record.reviewer)
                && !string.IsNullOrWhiteSpace(record.reviewTimeUtc);
        }

        private static bool CandidateExists(
            VfxConceptCandidateManifest manifest,
            string candidateId)
        {
            if (manifest?.candidates == null
                || string.IsNullOrWhiteSpace(candidateId))
            {
                return false;
            }

            foreach (VfxConceptCandidate candidate in manifest.candidates)
            {
                if (candidate != null && candidate.id == candidateId)
                {
                    return true;
                }
            }

            return false;
        }

        private static VfxConceptReviewEvaluation Result(
            VfxConceptReviewRecord record,
            string status,
            string message)
        {
            record.status = status;
            return new VfxConceptReviewEvaluation
            {
                Status = status,
                Message = message,
                Record = record
            };
        }
    }
}
