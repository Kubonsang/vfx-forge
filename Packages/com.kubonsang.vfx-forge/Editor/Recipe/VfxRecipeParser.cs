using System;
using System.IO;
using UnityEngine;

namespace Kubonsang.VfxForge.Editor
{
    public sealed class VfxRecipeParseResult
    {
        public bool Success;
        public VfxRecipe Recipe;
        public string Error = string.Empty;
    }

    public static class VfxRecipeParser
    {
        public static VfxRecipeParseResult ParseFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return Failure("Recipe path is empty.");
            }

            if (!File.Exists(path))
            {
                return Failure($"Recipe file does not exist: {path}");
            }

            return ParseJson(File.ReadAllText(path));
        }

        public static VfxRecipeParseResult ParseJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return Failure("Recipe JSON is empty.");
            }

            try
            {
                VfxRecipe recipe = JsonUtility.FromJson<VfxRecipe>(json);
                if (recipe == null)
                {
                    return Failure("Recipe JSON produced a null object.");
                }

                VfxRecipeNormalizer.Normalize(recipe);
                return new VfxRecipeParseResult { Success = true, Recipe = recipe };
            }
            catch (Exception exception)
            {
                return Failure($"Recipe JSON parse failed: {exception.Message}");
            }
        }

        private static VfxRecipeParseResult Failure(string error)
        {
            return new VfxRecipeParseResult { Success = false, Error = error };
        }
    }
}
