using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Kubonsang.VfxForge.Editor
{
    public static class VfxPreproductionStatus
    {
        public const string Blocked = "blocked";
        public const string ReadyForConcepts = "ready_for_concepts";
    }

    public sealed class VfxPreproductionGateResult
    {
        public string Status = VfxPreproductionStatus.Blocked;
        public string ReferenceBoardSha256 = string.Empty;
        public VfxReferenceBoard Board;
        public VfxArtDirectionBrief Brief;
        public List<VfxValidationResult> Results =
            new List<VfxValidationResult>();

        public bool Ready => Status == VfxPreproductionStatus.ReadyForConcepts;
    }

    public static class VfxPreproductionGate
    {
        public static VfxPreproductionGateResult EvaluateJson(
            string referenceBoardJson,
            string artDirectionBriefJson)
        {
            var result = new VfxPreproductionGateResult
            {
                ReferenceBoardSha256 =
                    ComputeContentSha256(referenceBoardJson)
            };
            VfxReferenceBoardParseResult boardParse =
                VfxReferenceContractParser.ParseBoard(referenceBoardJson);
            if (!boardParse.Success)
            {
                result.Results.Add(VfxValidationResult.Error(
                    boardParse.ErrorCode,
                    boardParse.Error));
            }
            else
            {
                result.Board = boardParse.Board;
                result.Results.AddRange(
                    VfxReferenceContractValidator.ValidateBoard(
                        result.Board));
            }

            VfxArtDirectionBriefParseResult briefParse =
                VfxReferenceContractParser.ParseBrief(
                    artDirectionBriefJson);
            if (!briefParse.Success)
            {
                result.Results.Add(VfxValidationResult.Error(
                    briefParse.ErrorCode,
                    briefParse.Error));
            }
            else
            {
                result.Brief = briefParse.Brief;
                result.Results.AddRange(
                    VfxReferenceContractValidator.ValidateBrief(
                        result.Brief));
            }

            if (result.Board != null && result.Brief != null)
            {
                bool identityMatches =
                    result.Board.id == result.Brief.referenceBoardId
                    && result.Board.taskId == result.Brief.taskId;
                result.Results.Add(identityMatches
                    ? VfxValidationResult.Pass(
                        "PREPROD-IDENTITY",
                        "Board and Brief identity match.")
                    : VfxValidationResult.Error(
                        "PREPROD-IDENTITY",
                        "Board and Brief id or taskId do not match."));

                bool hashMatches = string.Equals(
                    result.ReferenceBoardSha256,
                    result.Brief.referenceBoardSha256,
                    StringComparison.OrdinalIgnoreCase);
                result.Results.Add(hashMatches
                    ? VfxValidationResult.Pass(
                        "PREPROD-BOARD-HASH",
                        "Art Direction Brief targets the current Reference Board.")
                    : VfxValidationResult.Error(
                        "PREPROD-BOARD-HASH",
                        "Art Direction Brief is stale because the Reference Board hash changed."));
            }

            result.Status = VfxRecipeValidator.HasErrors(result.Results)
                ? VfxPreproductionStatus.Blocked
                : VfxPreproductionStatus.ReadyForConcepts;
            return result;
        }

        public static string ComputeContentSha256(string json)
        {
            string canonical = (json ?? string.Empty)
                .TrimStart('\uFEFF')
                .Replace("\r\n", "\n")
                .Replace("\r", "\n");
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(
                    Encoding.UTF8.GetBytes(canonical));
                var builder = new StringBuilder(hash.Length * 2);
                foreach (byte value in hash)
                {
                    builder.Append(value.ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
