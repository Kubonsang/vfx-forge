using System;
using UnityEngine;

namespace Kubonsang.VfxForge.Editor
{
    public sealed class VfxReferenceBoardParseResult
    {
        public bool Success;
        public VfxReferenceBoard Board;
        public string ErrorCode = string.Empty;
        public string Error = string.Empty;
    }

    public sealed class VfxArtDirectionBriefParseResult
    {
        public bool Success;
        public VfxArtDirectionBrief Brief;
        public string ErrorCode = string.Empty;
        public string Error = string.Empty;
    }

    public static class VfxReferenceContractParser
    {
        public static VfxReferenceBoardParseResult ParseBoard(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return BoardFailure(
                    "REF-JSON-EMPTY",
                    "Reference Board JSON is empty.");
            }

            try
            {
                VfxReferenceBoard board =
                    JsonUtility.FromJson<VfxReferenceBoard>(json);
                if (board == null)
                {
                    return BoardFailure(
                        "REF-JSON-MALFORMED",
                        "Reference Board JSON is malformed.");
                }

                return new VfxReferenceBoardParseResult
                {
                    Success = true,
                    Board = board
                };
            }
            catch (Exception)
            {
                return BoardFailure(
                    "REF-JSON-MALFORMED",
                    "Reference Board JSON is malformed.");
            }
        }

        public static VfxArtDirectionBriefParseResult ParseBrief(
            string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return BriefFailure(
                    "ART-JSON-EMPTY",
                    "Art Direction Brief JSON is empty.");
            }

            try
            {
                VfxArtDirectionBrief brief =
                    JsonUtility.FromJson<VfxArtDirectionBrief>(json);
                if (brief == null)
                {
                    return BriefFailure(
                        "ART-JSON-MALFORMED",
                        "Art Direction Brief JSON is malformed.");
                }

                return new VfxArtDirectionBriefParseResult
                {
                    Success = true,
                    Brief = brief
                };
            }
            catch (Exception)
            {
                return BriefFailure(
                    "ART-JSON-MALFORMED",
                    "Art Direction Brief JSON is malformed.");
            }
        }

        private static VfxReferenceBoardParseResult BoardFailure(
            string code,
            string message)
        {
            return new VfxReferenceBoardParseResult
            {
                ErrorCode = code,
                Error = message
            };
        }

        private static VfxArtDirectionBriefParseResult BriefFailure(
            string code,
            string message)
        {
            return new VfxArtDirectionBriefParseResult
            {
                ErrorCode = code,
                Error = message
            };
        }
    }
}
