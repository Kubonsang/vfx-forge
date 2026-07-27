using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Kubonsang.VfxForge.Editor
{
    public static class VfxRecipeValidator
    {
        private static readonly Regex IdPattern = new Regex("^[a-z0-9][a-z0-9_-]{2,63}$");

        public static List<VfxValidationResult> Validate(VfxRecipe recipe)
        {
            var results = new List<VfxValidationResult>();
            if (recipe == null)
            {
                results.Add(VfxValidationResult.Error("RECIPE-NULL", "Recipe is null."));
                return results;
            }

            Require(results, "RECIPE-SCHEMA", recipe.schemaVersion == "1.0", "Only schemaVersion 1.0 is supported.");
            Require(results, "RECIPE-ID", !string.IsNullOrWhiteSpace(recipe.id) && IdPattern.IsMatch(recipe.id), "Recipe id is invalid.");
            Require(results, "RECIPE-TEMPLATE", !string.IsNullOrWhiteSpace(recipe.template), "Template id is required.");
            Require(results, "RECIPE-OUTPUT", !string.IsNullOrWhiteSpace(recipe.outputPath) && recipe.outputPath.StartsWith("Assets/"), "Output path must be under Assets/.");
            Require(results, "RECIPE-DURATION", recipe.timing != null && recipe.timing.duration > 0f, "Duration must be greater than zero.");
            Require(results, "RECIPE-MAX-DURATION", recipe.budget != null && recipe.budget.maxDuration > 0f, "Max duration must be greater than zero.");

            if (recipe.timing != null && recipe.budget != null)
            {
                Require(results, "RECIPE-DURATION-BUDGET", recipe.timing.duration <= recipe.budget.maxDuration, "Duration exceeds the recipe budget.");
            }

            if (recipe.shape != null)
            {
                Require(results, "RECIPE-RADIUS", recipe.shape.radius >= 0f, "Radius cannot be negative.");
                Require(results, "RECIPE-DIRECTIONALITY", recipe.shape.directionality >= 0f && recipe.shape.directionality <= 1f, "Directionality must be between 0 and 1.");
                Require(results, "RECIPE-SPREAD", recipe.shape.spreadAngle >= 0f && recipe.shape.spreadAngle <= 360f, "Spread angle must be between 0 and 360.");
            }

            if (recipe.style != null)
            {
                Require(results, "RECIPE-PRIMARY-COLOR", ColorUtility.TryParseHtmlString(recipe.style.primaryColor, out _), "Primary color is invalid.");
                Require(results, "RECIPE-SECONDARY-COLOR", ColorUtility.TryParseHtmlString(recipe.style.secondaryColor, out _), "Secondary color is invalid.");
                Require(results, "RECIPE-EMISSION", recipe.style.emissionIntensity >= 0f, "Emission intensity cannot be negative.");
                Require(results, "RECIPE-DISTORTION", recipe.style.distortionStrength >= 0f, "Distortion strength cannot be negative.");
            }

            if (recipe.capture != null && recipe.capture.frameTimes != null)
            {
                foreach (float frameTime in recipe.capture.frameTimes)
                {
                    Require(results, "RECIPE-FRAME-TIME", frameTime >= 0f && frameTime <= recipe.capture.duration, $"Frame time {frameTime} is outside capture duration.");
                }
            }

            return results;
        }

        public static bool HasErrors(IEnumerable<VfxValidationResult> results)
        {
            if (results == null)
            {
                return false;
            }

            foreach (VfxValidationResult result in results)
            {
                if (result != null && result.severity == VfxValidationSeverity.Error && !result.passed)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Require(List<VfxValidationResult> results, string id, bool condition, string message)
        {
            results.Add(condition
                ? VfxValidationResult.Pass(id, message)
                : VfxValidationResult.Error(id, message));
        }
    }
}
