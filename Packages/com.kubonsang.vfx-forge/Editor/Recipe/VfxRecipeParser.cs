using System;
using System.IO;
using UnityEngine;

namespace Kubonsang.VfxForge.Editor
{
    public sealed class VfxRecipeParseResult
    {
        public bool Success;
        public VfxRecipe Recipe;
        public string ErrorCode = string.Empty;
        public string Error = string.Empty;
    }

    public static class VfxRecipeParser
    {
        public static VfxRecipeParseResult ParseFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return Failure(VfxRecipeErrorCodes.FilePathEmpty, "Recipe path is empty.");
            }

            if (!File.Exists(path))
            {
                return Failure(VfxRecipeErrorCodes.FileNotFound, $"Recipe file does not exist: {path}");
            }

            try
            {
                return ParseJson(File.ReadAllText(path));
            }
            catch (Exception)
            {
                return Failure(VfxRecipeErrorCodes.FileReadFailed, $"Recipe file could not be read: {path}");
            }
        }

        public static VfxRecipeParseResult ParseJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return Failure(VfxRecipeErrorCodes.JsonEmpty, "Recipe JSON is empty.");
            }

            VfxRecipeJsonContract.ContractResult contract = VfxRecipeJsonContract.Validate(json);
            if (!contract.Success)
            {
                return Failure(contract.ErrorCode, contract.ErrorMessage);
            }

            try
            {
                var recipe = new VfxRecipe();
                JsonUtility.FromJsonOverwrite(json, recipe);
                VfxRecipeNormalizer.Normalize(recipe);
                return new VfxRecipeParseResult { Success = true, Recipe = recipe };
            }
            catch (Exception)
            {
                return Failure(
                    VfxRecipeErrorCodes.JsonDeserialize,
                    "Recipe JSON could not be deserialized.");
            }
        }

        private static VfxRecipeParseResult Failure(string errorCode, string error)
        {
            return new VfxRecipeParseResult
            {
                Success = false,
                ErrorCode = errorCode,
                Error = error
            };
        }
    }
}
