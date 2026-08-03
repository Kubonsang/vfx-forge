using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Kubonsang.VfxForge.Editor
{
    public static class VfxReferenceContractValidator
    {
        private static readonly Regex IdPattern =
            new Regex("^[a-z0-9][a-z0-9_-]{2,63}$");
        private static readonly Regex TaskPattern =
            new Regex("^VF-[0-9]{3}$");
        private static readonly Regex Sha256Pattern =
            new Regex("^[a-f0-9]{64}$");
        private static readonly HashSet<string> SourceTypes =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "url",
                "project_asset",
                "generated"
            };
        private static readonly HashSet<string> Usages =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "inspiration_only",
                "redistributable",
                "generated_owned"
            };
        private static readonly HashSet<string> CameraViews =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "strict_top_down",
                "top_down_oblique"
            };

        public static List<VfxValidationResult> ValidateBoard(
            VfxReferenceBoard board)
        {
            var results = new List<VfxValidationResult>();
            if (board == null)
            {
                results.Add(VfxValidationResult.Error(
                    "REF-NULL",
                    "Reference Board is null."));
                return results;
            }

            Require(
                results,
                "REF-SCHEMA",
                board.schemaVersion == "reference-board-1.0",
                "Only reference-board-1.0 is supported.");
            Require(
                results,
                "REF-ID",
                IsId(board.id),
                "Reference Board id is invalid.");
            Require(
                results,
                "REF-TASK",
                IsTaskId(board.taskId),
                "Reference Board taskId must use VF-000 form.");
            Require(
                results,
                "REF-TITLE",
                !string.IsNullOrWhiteSpace(board.title),
                "Reference Board title is required.");
            Require(
                results,
                "REF-COUNT",
                board.references != null && board.references.Length > 0,
                "At least one reference is required.");
            RequireNonEmptyList(
                results,
                "REF-STYLE-GOALS",
                board.styleGoals,
                "At least one concrete style goal is required.");
            RequireNonEmptyList(
                results,
                "REF-GLOBAL-AVOIDS",
                board.globalAvoids,
                "At least one global avoid is required.");

            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (board.references == null)
            {
                return results;
            }

            for (int index = 0; index < board.references.Length; index++)
            {
                VfxReferenceItem item = board.references[index];
                string prefix = $"Reference {index}";
                if (item == null)
                {
                    results.Add(VfxValidationResult.Error(
                        "REF-ITEM",
                        $"{prefix} is null."));
                    continue;
                }

                Require(
                    results,
                    "REF-ITEM-ID",
                    IsId(item.id) && ids.Add(item.id),
                    $"{prefix} id is invalid or duplicated.");
                Require(
                    results,
                    "REF-ITEM-TITLE",
                    !string.IsNullOrWhiteSpace(item.title),
                    $"{prefix} title is required.");
                Require(
                    results,
                    "REF-SOURCE-TYPE",
                    SourceTypes.Contains(item.sourceType),
                    $"{prefix} sourceType is unsupported.");
                Require(
                    results,
                    "REF-SOURCE",
                    IsSafeSource(item.sourceType, item.source),
                    $"{prefix} source must be an HTTP(S) URL or a safe Assets path.");
                Require(
                    results,
                    "REF-CREATOR",
                    !string.IsNullOrWhiteSpace(item.creator),
                    $"{prefix} creator or origin is required.");
                Require(
                    results,
                    "REF-LICENSE",
                    !string.IsNullOrWhiteSpace(item.license),
                    $"{prefix} license or rights status is required.");
                Require(
                    results,
                    "REF-USAGE",
                    Usages.Contains(item.usage),
                    $"{prefix} usage is unsupported.");
                RequireNonEmptyList(
                    results,
                    "REF-DESIRED",
                    item.desiredElements,
                    $"{prefix} must identify at least one desired element.");
                RequireNonEmptyList(
                    results,
                    "REF-AVOID",
                    item.avoidElements,
                    $"{prefix} must identify at least one element not to copy.");
                Require(
                    results,
                    "REF-FRAME",
                    item.frame != null
                        && !string.IsNullOrWhiteSpace(item.frame.cameraAngle)
                        && !string.IsNullOrWhiteSpace(item.frame.cropFocus),
                    $"{prefix} camera angle and crop focus are required.");
            }

            return results;
        }

        public static List<VfxValidationResult> ValidateBrief(
            VfxArtDirectionBrief brief)
        {
            var results = new List<VfxValidationResult>();
            if (brief == null)
            {
                results.Add(VfxValidationResult.Error(
                    "ART-NULL",
                    "Art Direction Brief is null."));
                return results;
            }

            Require(
                results,
                "ART-SCHEMA",
                brief.schemaVersion == "art-direction-brief-1.0",
                "Only art-direction-brief-1.0 is supported.");
            Require(results, "ART-ID", IsId(brief.id), "Brief id is invalid.");
            Require(
                results,
                "ART-TASK",
                IsTaskId(brief.taskId),
                "Brief taskId must use VF-000 form.");
            Require(
                results,
                "ART-BOARD-ID",
                IsId(brief.referenceBoardId),
                "A valid Reference Board id is required.");
            Require(
                results,
                "ART-BOARD-HASH",
                !string.IsNullOrWhiteSpace(brief.referenceBoardSha256)
                    && Sha256Pattern.IsMatch(
                        brief.referenceBoardSha256.ToLowerInvariant()),
                "Reference Board SHA-256 must contain 64 hexadecimal characters.");
            Require(
                results,
                "ART-INTENT",
                !string.IsNullOrWhiteSpace(brief.effectIntent),
                "Effect intent is required.");
            Require(
                results,
                "ART-CANDIDATES",
                brief.candidateCount >= 3 && brief.candidateCount <= 6,
                "Candidate count must be between 3 and 6.");

            ValidateCamera(results, brief.camera);
            ValidateSilhouette(results, brief.silhouette);
            ValidateDepthLayers(results, brief.depthLayers);
            ValidateMaterialZones(results, brief.materialZones);
            RequireNonEmptyList(
                results,
                "ART-FORBIDDEN",
                brief.forbiddenTraits,
                "At least one forbidden visual trait is required.");
            Require(
                results,
                "ART-QUESTIONS",
                CountNonEmpty(brief.acceptanceQuestions) >= 3,
                "At least three concrete acceptance questions are required.");
            Require(
                results,
                "ART-OUTPUTS",
                brief.outputs != null
                    && brief.outputs.grayscaleSilhouette
                    && brief.outputs.fullColorConcept
                    && brief.outputs.threeGroundComposite
                    && brief.outputs.labeledBreakdown,
                "All four concept evidence outputs are required.");
            return results;
        }

        private static void ValidateCamera(
            List<VfxValidationResult> results,
            VfxConceptCamera camera)
        {
            Require(
                results,
                "ART-CAMERA",
                camera != null
                    && CameraViews.Contains(camera.view)
                    && camera.resolutionWidth > 0
                    && camera.resolutionHeight > 0
                    && camera.effectFootprintPixels > 0
                    && camera.effectFootprintPixels
                        <= Math.Min(camera.resolutionWidth, camera.resolutionHeight),
                "Camera must define a supported top-down view, resolution, and visible gameplay footprint.");
        }

        private static void ValidateSilhouette(
            List<VfxValidationResult> results,
            VfxSilhouetteDirection silhouette)
        {
            bool finiteRatios = silhouette != null
                && IsRatio(silhouette.primaryMassRatio)
                && IsRatio(silhouette.secondaryMassRatio)
                && IsRatio(silhouette.negativeSpaceRatio);
            float ratioSum = finiteRatios
                ? silhouette.primaryMassRatio
                    + silhouette.secondaryMassRatio
                    + silhouette.negativeSpaceRatio
                : 0f;
            Require(
                results,
                "ART-SILHOUETTE",
                silhouette != null
                    && !string.IsNullOrWhiteSpace(silhouette.primaryMass)
                    && silhouette.primaryMassRatio > silhouette.secondaryMassRatio
                    && Math.Abs(ratioSum - 1f) <= 0.001f
                    && !string.IsNullOrWhiteSpace(silhouette.asymmetry),
                "Silhouette must prioritize one named mass and ratios must sum to 1.");
            RequireNonEmptyList(
                results,
                "ART-CONNECTIONS",
                silhouette?.connections,
                "At least one physical shape connection rule is required.");
            RequireNonEmptyList(
                results,
                "ART-MOTIFS",
                silhouette?.motifs,
                "At least one dominant motif is required.");
        }

        private static void ValidateDepthLayers(
            List<VfxValidationResult> results,
            VfxDepthLayer[] layers)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var orders = new HashSet<int>();
            bool valid = layers != null && layers.Length >= 2;
            if (valid)
            {
                foreach (VfxDepthLayer layer in layers)
                {
                    valid &= layer != null
                        && IsId(layer.id)
                        && ids.Add(layer.id)
                        && !string.IsNullOrWhiteSpace(layer.role)
                        && orders.Add(layer.order);
                }
            }

            Require(
                results,
                "ART-DEPTH-LAYERS",
                valid,
                "At least two uniquely ordered depth layers are required.");
        }

        private static void ValidateMaterialZones(
            List<VfxValidationResult> results,
            VfxMaterialZone[] zones)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            bool valid = zones != null && zones.Length >= 2;
            if (valid)
            {
                foreach (VfxMaterialZone zone in zones)
                {
                    valid &= zone != null
                        && IsId(zone.id)
                        && ids.Add(zone.id)
                        && !string.IsNullOrWhiteSpace(zone.role)
                        && !string.IsNullOrWhiteSpace(zone.finish);
                }
            }

            Require(
                results,
                "ART-MATERIAL-ZONES",
                valid,
                "At least two named material zones are required.");
        }

        private static bool IsSafeSource(string sourceType, string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return false;
            }

            if (sourceType == "url")
            {
                return Uri.TryCreate(source, UriKind.Absolute, out Uri uri)
                    && (uri.Scheme == Uri.UriSchemeHttp
                        || uri.Scheme == Uri.UriSchemeHttps);
            }

            string normalized = source.Replace('\\', '/');
            return (sourceType == "project_asset"
                    || sourceType == "generated")
                && normalized.StartsWith("Assets/", StringComparison.Ordinal)
                && !normalized.Contains("../")
                && !normalized.EndsWith("/", StringComparison.Ordinal);
        }

        private static bool IsId(string value)
        {
            return !string.IsNullOrWhiteSpace(value)
                && IdPattern.IsMatch(value);
        }

        private static bool IsTaskId(string value)
        {
            return !string.IsNullOrWhiteSpace(value)
                && TaskPattern.IsMatch(value);
        }

        private static bool IsRatio(float value)
        {
            return !float.IsNaN(value)
                && !float.IsInfinity(value)
                && value >= 0f
                && value <= 1f;
        }

        private static int CountNonEmpty(string[] values)
        {
            int count = 0;
            if (values == null)
            {
                return count;
            }

            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    count++;
                }
            }

            return count;
        }

        private static void RequireNonEmptyList(
            List<VfxValidationResult> results,
            string id,
            string[] values,
            string message)
        {
            Require(
                results,
                id,
                values != null
                    && values.Length > 0
                    && CountNonEmpty(values) == values.Length,
                message);
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
}
