using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Kubonsang.VfxForge.Editor
{
    [Serializable]
    public sealed class VfxMeshReferenceManifest
    {
        public string schemaVersion = "mesh-reference-1.0";
        public string taskId = string.Empty;
        public string selectedCandidateId = string.Empty;
        public string candidateBoardPath = string.Empty;
        public string candidateBoardSha256 = string.Empty;
        public string modelSheetPath = string.Empty;
        public string modelSheetSha256 = string.Empty;
        public float unityUnitsPerMeter = 1f;
        public VfxMeshReferenceView[] views =
            Array.Empty<VfxMeshReferenceView>();
        public VfxMeshLandmark[] landmarks =
            Array.Empty<VfxMeshLandmark>();
        public VfxMeshPartContract[] parts =
            Array.Empty<VfxMeshPartContract>();
    }

    [Serializable]
    public sealed class VfxMeshReferenceView
    {
        public string id = string.Empty;
        public string projection = string.Empty;
        public int width;
        public int height;
        public Vector3 position;
        public Vector3 target;
        public Vector3 rotationEuler;
        public float fieldOfView;
        public float orthographicSize;
        public Rect normalizedImageRect;
    }

    [Serializable]
    public sealed class VfxMeshLandmark
    {
        public string id = string.Empty;
        public string viewId = string.Empty;
        public string partId = string.Empty;
        public Vector2 normalizedPosition;
        public float depthMeters;
    }

    [Serializable]
    public sealed class VfxMeshPartContract
    {
        public string id = string.Empty;
        public string role = string.Empty;
        public string materialZone = string.Empty;
        public string[] connectedTo = Array.Empty<string>();
    }

    [Serializable]
    public sealed class VfxMeshAuthoringManifest
    {
        public string schemaVersion = "mesh-authoring-1.0";
        public string taskId = string.Empty;
        public string meshReferenceSha256 = string.Empty;
        public string sourcePrefabPath = string.Empty;
        public string sourceDependencyHash = string.Empty;
        public string runtimePrefabPath = string.Empty;
        public string runtimeDependencyHash = string.Empty;
        public string runtimeMeshFolder = string.Empty;
        public int renderedTriangles;
        public int maximumRenderedTriangles = 12000;
        public string[] materialZones = Array.Empty<string>();
    }

    [Serializable]
    public sealed class VfxMeshReviewRecord
    {
        public string schemaVersion = "mesh-review-1.0";
        public string taskId = string.Empty;
        public string stage = string.Empty;
        public string status = VfxMeshReviewStatus.ReviewRequired;
        public string inputSha256 = string.Empty;
        public string reviewer = string.Empty;
        public string reviewTimeUtc = string.Empty;
        public bool accepted;
        public VfxMeshReviewCriteria criteria =
            new VfxMeshReviewCriteria();
        public string rejectionReason = string.Empty;
    }

    [Serializable]
    public sealed class VfxMeshReviewCriteria
    {
        public bool visibleShapeFidelity;
        public bool structuralFrameReadability;
        public bool connectedAnchors;
        public bool depthConsistency;
        public bool gameplayReadability;

        public bool AllPassed()
        {
            return visibleShapeFidelity
                && structuralFrameReadability
                && connectedAnchors
                && depthConsistency
                && gameplayReadability;
        }
    }

    public static class VfxMeshReviewStage
    {
        public const string ModelSheet = "model_sheet";
        public const string Blockout = "blockout";
    }

    public static class VfxMeshReviewStatus
    {
        public const string ReviewRequired = "review_required";
        public const string Accepted = "accepted";
        public const string Rejected = "rejected";
        public const string ReviewStale = "review_stale";
    }

    public sealed class VfxMeshContractValidation
    {
        public readonly List<VfxValidationResult> Results =
            new List<VfxValidationResult>();

        public bool Valid => Results.All(result =>
            result.passed
            || result.severity != VfxValidationSeverity.Error);
    }

    public static class VfxMeshContractValidator
    {
        private static readonly Regex TaskPattern =
            new Regex("^VF-[0-9]{3}$");
        private static readonly Regex IdPattern =
            new Regex("^[a-z0-9][a-z0-9_-]{1,63}$");
        private static readonly Regex Sha256Pattern =
            new Regex("^[a-f0-9]{64}$");
        private static readonly HashSet<string> RequiredViews =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "front",
                "top",
                "right_side",
                "gameplay"
            };

        public static VfxMeshContractValidation Validate(
            VfxMeshReferenceManifest manifest)
        {
            var validation = new VfxMeshContractValidation();
            if (manifest == null)
            {
                Error(validation, "MESH-REF-NULL", "Manifest is null.");
                return validation;
            }

            Require(
                validation,
                manifest.schemaVersion == "mesh-reference-1.0",
                "MESH-REF-SCHEMA",
                "Only mesh-reference-1.0 is supported.");
            RequireTask(validation, manifest.taskId);
            RequireId(
                validation,
                manifest.selectedCandidateId,
                "MESH-REF-CANDIDATE");
            RequirePath(
                validation,
                manifest.candidateBoardPath,
                "MESH-REF-CANDIDATE-PATH",
                false);
            RequireSha(
                validation,
                manifest.candidateBoardSha256,
                "MESH-REF-CANDIDATE-HASH");
            RequirePath(
                validation,
                manifest.modelSheetPath,
                "MESH-REF-SHEET-PATH",
                false);
            RequireSha(
                validation,
                manifest.modelSheetSha256,
                "MESH-REF-SHEET-HASH");
            Require(
                validation,
                IsFinite(manifest.unityUnitsPerMeter)
                    && manifest.unityUnitsPerMeter > 0f,
                "MESH-REF-SCALE",
                "Unity units per meter must be finite and positive.");
            ValidateViews(validation, manifest.views);
            ValidateParts(validation, manifest.parts);
            ValidateLandmarks(validation, manifest.landmarks, manifest.views);
            return validation;
        }

        public static VfxMeshContractValidation Validate(
            VfxMeshAuthoringManifest manifest)
        {
            var validation = new VfxMeshContractValidation();
            if (manifest == null)
            {
                Error(validation, "MESH-AUTH-NULL", "Manifest is null.");
                return validation;
            }

            Require(
                validation,
                manifest.schemaVersion == "mesh-authoring-1.0",
                "MESH-AUTH-SCHEMA",
                "Only mesh-authoring-1.0 is supported.");
            RequireTask(validation, manifest.taskId);
            RequireSha(
                validation,
                manifest.meshReferenceSha256,
                "MESH-AUTH-REFERENCE-HASH");
            RequirePath(
                validation,
                manifest.sourcePrefabPath,
                "MESH-AUTH-SOURCE-PATH",
                true);
            RequirePath(
                validation,
                manifest.runtimePrefabPath,
                "MESH-AUTH-RUNTIME-PATH",
                true);
            RequirePath(
                validation,
                manifest.runtimeMeshFolder,
                "MESH-AUTH-MESH-PATH",
                true);
            Require(
                validation,
                manifest.maximumRenderedTriangles > 0,
                "MESH-AUTH-BUDGET",
                "Maximum rendered triangles must be positive.");
            Require(
                validation,
                manifest.renderedTriangles >= 0
                    && manifest.renderedTriangles
                        <= manifest.maximumRenderedTriangles,
                "MESH-AUTH-TRIANGLES",
                "Rendered triangles exceed the configured budget.");
            return validation;
        }

        private static void ValidateViews(
            VfxMeshContractValidation validation,
            VfxMeshReferenceView[] views)
        {
            views = views ?? Array.Empty<VfxMeshReferenceView>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (VfxMeshReferenceView view in views)
            {
                if (view == null || !IdPattern.IsMatch(view.id ?? string.Empty))
                {
                    Error(validation, "MESH-REF-VIEW-ID", "View id is invalid.");
                    continue;
                }

                Require(
                    validation,
                    ids.Add(view.id),
                    "MESH-REF-VIEW-DUPLICATE",
                    $"View id is duplicated: {view.id}.");
                bool orthographic = view.projection == "orthographic";
                bool perspective = view.projection == "perspective";
                Require(
                    validation,
                    orthographic || perspective,
                    "MESH-REF-PROJECTION",
                    $"View {view.id} projection is invalid.");
                Require(
                    validation,
                    view.width > 0 && view.height > 0,
                    "MESH-REF-RESOLUTION",
                    $"View {view.id} resolution must be positive.");
                Require(
                    validation,
                    IsFinite(view.position)
                        && IsFinite(view.target)
                        && IsFinite(view.rotationEuler)
                        && (view.target - view.position).sqrMagnitude > 0.000001f
                        && (orthographic
                        ? IsFinite(view.orthographicSize)
                            && view.orthographicSize > 0f
                        : IsFinite(view.fieldOfView)
                            && view.fieldOfView > 0f
                            && view.fieldOfView < 180f),
                    "MESH-REF-CAMERA",
                    $"View {view.id} camera settings are invalid.");
                Rect rect = view.normalizedImageRect;
                Require(
                    validation,
                    rect.width > 0f
                        && rect.height > 0f
                        && rect.xMin >= 0f
                        && rect.yMin >= 0f
                        && rect.xMax <= 1f
                        && rect.yMax <= 1f,
                    "MESH-REF-IMAGE-RECT",
                    $"View {view.id} image rect is invalid.");
            }

            Require(
                validation,
                RequiredViews.SetEquals(ids),
                "MESH-REF-VIEWS",
                "Exactly front, top, right_side, and gameplay views are required.");
        }

        private static void ValidateParts(
            VfxMeshContractValidation validation,
            VfxMeshPartContract[] parts)
        {
            parts = parts ?? Array.Empty<VfxMeshPartContract>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (VfxMeshPartContract part in parts)
            {
                if (part == null || !IdPattern.IsMatch(part.id ?? string.Empty))
                {
                    Error(validation, "MESH-REF-PART-ID", "Part id is invalid.");
                    continue;
                }
                Require(
                    validation,
                    ids.Add(part.id),
                    "MESH-REF-PART-DUPLICATE",
                    $"Part id is duplicated: {part.id}.");
            }
            Require(
                validation,
                ids.Contains("surface")
                    && ids.Contains("frame")
                    && ids.Contains("anchor_front_left")
                    && ids.Contains("anchor_front_right")
                    && ids.Contains("anchor_root_left")
                    && ids.Contains("anchor_root_right"),
                "MESH-REF-PARTS",
                "Surface, frame, and four distinct anchors are required.");
        }

        private static void ValidateLandmarks(
            VfxMeshContractValidation validation,
            VfxMeshLandmark[] landmarks,
            VfxMeshReferenceView[] views)
        {
            landmarks = landmarks ?? Array.Empty<VfxMeshLandmark>();
            var viewIds = new HashSet<string>(
                (views ?? Array.Empty<VfxMeshReferenceView>())
                    .Where(view => view != null)
                    .Select(view => view.id),
                StringComparer.Ordinal);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (VfxMeshLandmark landmark in landmarks)
            {
                if (landmark == null)
                {
                    Error(validation, "MESH-REF-LANDMARK", "Landmark is null.");
                    continue;
                }
                Require(
                    validation,
                    IdPattern.IsMatch(landmark.id ?? string.Empty)
                        && ids.Add(landmark.id),
                    "MESH-REF-LANDMARK-ID",
                    "Landmark id is invalid or duplicated.");
                Require(
                    validation,
                    viewIds.Contains(landmark.viewId),
                    "MESH-REF-LANDMARK-VIEW",
                    $"Landmark {landmark.id} references an unknown view.");
                Vector2 point = landmark.normalizedPosition;
                Require(
                    validation,
                    IsFinite(point.x)
                        && IsFinite(point.y)
                        && point.x >= 0f
                        && point.x <= 1f
                        && point.y >= 0f
                        && point.y <= 1f,
                    "MESH-REF-LANDMARK-POSITION",
                    $"Landmark {landmark.id} is outside normalized image space.");
                Require(
                    validation,
                    IsFinite(landmark.depthMeters),
                    "MESH-REF-LANDMARK-DEPTH",
                    $"Landmark {landmark.id} depth is invalid.");
            }
            Require(
                validation,
                landmarks.Length > 0,
                "MESH-REF-LANDMARKS",
                "At least one landmark is required.");
        }

        private static void RequireTask(
            VfxMeshContractValidation validation,
            string taskId)
        {
            Require(
                validation,
                TaskPattern.IsMatch(taskId ?? string.Empty),
                "MESH-TASK-ID",
                "Task id must use VF-000 format.");
        }

        private static void RequireId(
            VfxMeshContractValidation validation,
            string id,
            string ruleId)
        {
            Require(
                validation,
                IdPattern.IsMatch(id ?? string.Empty),
                ruleId,
                "Identifier is invalid.");
        }

        private static void RequireSha(
            VfxMeshContractValidation validation,
            string hash,
            string ruleId)
        {
            Require(
                validation,
                Sha256Pattern.IsMatch(hash ?? string.Empty),
                ruleId,
                "SHA-256 must contain 64 lowercase hexadecimal characters.");
        }

        private static void RequirePath(
            VfxMeshContractValidation validation,
            string path,
            string ruleId,
            bool requireAssets)
        {
            bool safe = !string.IsNullOrWhiteSpace(path)
                && !Path.IsPathRooted(path)
                && !path.Split('/', '\\').Contains("..")
                && (!requireAssets
                    || path.StartsWith("Assets/", StringComparison.Ordinal));
            Require(validation, safe, ruleId, "Asset path is unsafe.");
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static void Require(
            VfxMeshContractValidation validation,
            bool condition,
            string ruleId,
            string message)
        {
            validation.Results.Add(condition
                ? VfxValidationResult.Pass(ruleId, message)
                : VfxValidationResult.Error(ruleId, message));
        }

        private static void Error(
            VfxMeshContractValidation validation,
            string ruleId,
            string message)
        {
            validation.Results.Add(VfxValidationResult.Error(ruleId, message));
        }
    }

    public static class VfxMeshReviewStore
    {
        public static VfxMeshReviewRecord CreateExpected(
            string taskId,
            string stage,
            string inputSha256)
        {
            return new VfxMeshReviewRecord
            {
                taskId = taskId ?? string.Empty,
                stage = stage ?? string.Empty,
                status = VfxMeshReviewStatus.ReviewRequired,
                inputSha256 = inputSha256 ?? string.Empty
            };
        }

        public static string Evaluate(
            VfxMeshReviewRecord expected,
            VfxMeshReviewRecord submitted)
        {
            if (expected == null || submitted == null)
            {
                return VfxMeshReviewStatus.ReviewRequired;
            }
            if (submitted.schemaVersion != "mesh-review-1.0"
                || submitted.taskId != expected.taskId
                || submitted.stage != expected.stage
                || submitted.inputSha256 != expected.inputSha256)
            {
                return VfxMeshReviewStatus.ReviewStale;
            }
            if (submitted.status == VfxMeshReviewStatus.Rejected)
            {
                return string.IsNullOrWhiteSpace(submitted.rejectionReason)
                    ? VfxMeshReviewStatus.ReviewRequired
                    : VfxMeshReviewStatus.Rejected;
            }
            if (submitted.status == VfxMeshReviewStatus.Accepted
                && submitted.accepted
                && submitted.criteria != null
                && submitted.criteria.AllPassed()
                && !string.IsNullOrWhiteSpace(submitted.reviewer)
                && !string.IsNullOrWhiteSpace(submitted.reviewTimeUtc))
            {
                return VfxMeshReviewStatus.Accepted;
            }
            return VfxMeshReviewStatus.ReviewRequired;
        }

        public static string ComputeFileSha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                return ToHex(sha.ComputeHash(stream));
            }
        }

        public static string ComputeCombinedSha256(params string[] values)
        {
            string text = string.Join("\n", values ?? Array.Empty<string>());
            using (SHA256 sha = SHA256.Create())
            {
                return ToHex(sha.ComputeHash(Encoding.UTF8.GetBytes(text)));
            }
        }

        private static string ToHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (byte value in bytes)
            {
                builder.Append(value.ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
