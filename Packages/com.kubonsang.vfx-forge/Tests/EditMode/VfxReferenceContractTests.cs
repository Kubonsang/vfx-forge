using NUnit.Framework;
using UnityEngine;

namespace Kubonsang.VfxForge.Editor.Tests
{
    public sealed class VfxReferenceContractTests
    {
        [Test]
        public void Gate_ValidBoardAndBrief_IsReadyForConcepts()
        {
            string boardJson = CreateBoardJson();
            string briefJson = CreateBriefJson(boardJson);

            VfxPreproductionGateResult result =
                VfxPreproductionGate.EvaluateJson(
                    boardJson,
                    briefJson);

            Assert.That(result.Ready, Is.True);
            Assert.That(
                result.Status,
                Is.EqualTo(
                    VfxPreproductionStatus.ReadyForConcepts));
            Assert.That(
                VfxRecipeValidator.HasErrors(result.Results),
                Is.False);
        }

        [Test]
        public void Gate_ChangedBoard_IsBlockedAsStale()
        {
            string boardJson = CreateBoardJson();
            string briefJson = CreateBriefJson(boardJson);
            boardJson = boardJson.Replace(
                "Modern layered energy shield",
                "Changed layered energy shield");

            VfxPreproductionGateResult result =
                VfxPreproductionGate.EvaluateJson(
                    boardJson,
                    briefJson);

            Assert.That(result.Ready, Is.False);
            Assert.That(
                result.Results.Exists(
                    item => item.ruleId == "PREPROD-BOARD-HASH"
                        && !item.passed),
                Is.True);
        }

        [Test]
        public void ValidateBoard_MissingRightsStatus_IsRejected()
        {
            VfxReferenceBoard board = CreateBoard();
            board.references[0].license = string.Empty;

            var results =
                VfxReferenceContractValidator.ValidateBoard(board);

            Assert.That(
                results.Exists(
                    item => item.ruleId == "REF-LICENSE"
                        && !item.passed),
                Is.True);
        }

        [Test]
        public void ValidateBoard_UnsafeProjectPath_IsRejected()
        {
            VfxReferenceBoard board = CreateBoard();
            board.references[0].sourceType = "project_asset";
            board.references[0].source =
                "Assets/References/../../secret.png";

            var results =
                VfxReferenceContractValidator.ValidateBoard(board);

            Assert.That(
                results.Exists(
                    item => item.ruleId == "REF-SOURCE"
                        && !item.passed),
                Is.True);
        }

        [Test]
        public void ValidateBrief_TooFewCandidates_IsRejected()
        {
            VfxArtDirectionBrief brief = CreateBrief("a".PadLeft(64, 'a'));
            brief.candidateCount = 2;

            var results =
                VfxReferenceContractValidator.ValidateBrief(brief);

            Assert.That(
                results.Exists(
                    item => item.ruleId == "ART-CANDIDATES"
                        && !item.passed),
                Is.True);
        }

        [Test]
        public void ValidateBrief_UnbalancedMassRatios_IsRejected()
        {
            VfxArtDirectionBrief brief = CreateBrief("b".PadLeft(64, 'b'));
            brief.silhouette.negativeSpaceRatio = 0.4f;

            var results =
                VfxReferenceContractValidator.ValidateBrief(brief);

            Assert.That(
                results.Exists(
                    item => item.ruleId == "ART-SILHOUETTE"
                        && !item.passed),
                Is.True);
        }

        [Test]
        public void ContentHash_NormalizesBomAndLineEndings()
        {
            string unix = "{\n  \"id\": \"sample\"\n}";
            string windows = "\uFEFF{\r\n  \"id\": \"sample\"\r\n}";

            Assert.That(
                VfxPreproductionGate.ComputeContentSha256(windows),
                Is.EqualTo(
                    VfxPreproductionGate.ComputeContentSha256(unix)));
        }

        private static string CreateBoardJson()
        {
            return JsonUtility.ToJson(CreateBoard(), true);
        }

        private static string CreateBriefJson(string boardJson)
        {
            VfxArtDirectionBrief brief = CreateBrief(
                VfxPreproductionGate.ComputeContentSha256(boardJson));
            return JsonUtility.ToJson(brief, true);
        }

        private static VfxReferenceBoard CreateBoard()
        {
            return new VfxReferenceBoard
            {
                id = "holy_aegis_modern",
                taskId = "VF-021",
                title = "Modern Holy Aegis reference board",
                styleGoals = new[]
                {
                    "Modern layered energy shield"
                },
                globalAvoids = new[]
                {
                    "flat mobile-era icon silhouette"
                },
                references = new[]
                {
                    new VfxReferenceItem
                    {
                        id = "ref_aegis_01",
                        title = "Layered shield reference",
                        sourceType = "url",
                        source = "https://example.com/reference",
                        creator = "Example creator",
                        license = "link-only; rights not granted",
                        usage = "inspiration_only",
                        desiredElements = new[]
                        {
                            "large readable primary mass"
                        },
                        avoidElements = new[]
                        {
                            "literal motif copying"
                        },
                        frame = new VfxReferenceFrame
                        {
                            cameraAngle = "strict top-down",
                            cropFocus = "shield silhouette"
                        }
                    }
                }
            };
        }

        private static VfxArtDirectionBrief CreateBrief(string boardHash)
        {
            return new VfxArtDirectionBrief
            {
                id = "holy_aegis_modern_brief",
                taskId = "VF-021",
                referenceBoardId = "holy_aegis_modern",
                referenceBoardSha256 = boardHash,
                effectIntent =
                    "A contemporary top-down fantasy knight deploys a massive holy barrier.",
                candidateCount = 4,
                camera = new VfxConceptCamera
                {
                    view = "strict_top_down",
                    resolutionWidth = 1920,
                    resolutionHeight = 1080,
                    effectFootprintPixels = 360
                },
                silhouette = new VfxSilhouetteDirection
                {
                    primaryMass = "faceted circular shield plate",
                    primaryMassRatio = 0.65f,
                    secondaryMassRatio = 0.25f,
                    negativeSpaceRatio = 0.1f,
                    asymmetry = "slight forward-weighted crown split",
                    connections = new[]
                    {
                        "all ornaments grow from the primary rim"
                    },
                    motifs = new[]
                    {
                        "single knight crest"
                    }
                },
                depthLayers = new[]
                {
                    new VfxDepthLayer
                    {
                        id = "energy_plate",
                        role = "primary translucent volume",
                        order = 0
                    },
                    new VfxDepthLayer
                    {
                        id = "metal_rim",
                        role = "foreground structural rim",
                        order = 1
                    }
                },
                materialZones = new[]
                {
                    new VfxMaterialZone
                    {
                        id = "emerald_energy",
                        role = "shield plane",
                        finish = "layered translucent energy"
                    },
                    new VfxMaterialZone
                    {
                        id = "aged_gold",
                        role = "rim and crest",
                        finish = "metallic gold with controlled highlights"
                    }
                },
                forbiddenTraits = new[]
                {
                    "detached UI-like glyphs",
                    "flat additive yellow shapes"
                },
                acceptanceQuestions = new[]
                {
                    "Does the shield read before its decoration?",
                    "Does it retain depth in strict top view?",
                    "Does it look contemporary at gameplay scale?"
                },
                outputs = new VfxConceptOutputRequirements()
            };
        }
    }
}
