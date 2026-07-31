using System;
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

            Require(
                results,
                "RECIPE-SCHEMA",
                recipe.schemaVersion == "1.0" || recipe.schemaVersion == "1.1",
                "Only schemaVersion 1.0 and 1.1 are supported.");
            Require(results, "RECIPE-ID", !string.IsNullOrWhiteSpace(recipe.id) && IdPattern.IsMatch(recipe.id), "Recipe id is invalid.");
            Require(results, "RECIPE-TEMPLATE", !string.IsNullOrWhiteSpace(recipe.template), "Template id is required.");
            Require(
                results,
                "RECIPE-OUTPUT",
                VfxRecipePath.TryNormalizePrefabAssetPath(recipe.outputPath, out _),
                "Output path must be a traversal-free .prefab path under Assets/.");
            Require(results, "RECIPE-DURATION", recipe.timing != null && recipe.timing.duration > 0f, "Duration must be greater than zero.");
            Require(results, "RECIPE-MAX-DURATION", recipe.budget != null && recipe.budget.maxDuration > 0f, "Max duration must be greater than zero.");

            if (recipe.timing != null)
            {
                Require(results, "RECIPE-ANTICIPATION", recipe.timing.anticipation >= 0f, "Anticipation cannot be negative.");
                Require(results, "RECIPE-IMPACT", recipe.timing.impact >= 0f, "Impact cannot be negative.");
                Require(results, "RECIPE-SUSTAIN", recipe.timing.sustain >= 0f, "Sustain cannot be negative.");
                Require(results, "RECIPE-DECAY", recipe.timing.decay >= 0f, "Decay cannot be negative.");
            }

            if (recipe.budget != null)
            {
                Require(results, "RECIPE-MAX-PARTICLES", recipe.budget.maxParticles > 0, "Max particles must be greater than zero.");
                Require(results, "RECIPE-MAX-BOUNDS", recipe.budget.maxBoundsRadius > 0f, "Max bounds radius must be greater than zero.");
            }

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
                Require(results, "RECIPE-SHARPNESS", recipe.style.sharpness >= 0f && recipe.style.sharpness <= 1f, "Sharpness must be between 0 and 1.");
                Require(results, "RECIPE-DISTORTION", recipe.style.distortionStrength >= 0f, "Distortion strength cannot be negative.");
            }

            if (recipe.motion != null)
            {
                Require(
                    results,
                    "RECIPE-MOTION-SPEED",
                    IsFinite(recipe.motion.speed) && recipe.motion.speed >= 0f,
                    "Motion speed must be finite and non-negative.");
                Require(
                    results,
                    "RECIPE-MOTION-DIRECTION",
                    IsFinite(recipe.motion.localDirection.x)
                        && IsFinite(recipe.motion.localDirection.y)
                        && IsFinite(recipe.motion.localDirection.z),
                    "Motion direction must be finite.");
            }

            if (recipe.schemaVersion == "1.0")
            {
                Require(
                    results,
                    "RECIPE-1.0-MOTION",
                    recipe.motion == null
                        || (recipe.motion.speed == 0f
                            && recipe.motion.localDirection == Vector3.forward),
                    "Recipe 1.0 cannot define motion overrides.");
                Require(
                    results,
                    "RECIPE-1.0-GEOMETRY",
                    recipe.geometry == null
                        || string.IsNullOrEmpty(recipe.geometry.variant),
                    "Recipe 1.0 cannot define geometry variants.");
                Require(
                    results,
                    "RECIPE-1.0-CONTEXTS",
                    recipe.capture == null
                        || recipe.capture.contexts == null
                        || recipe.capture.contexts.Length == 0,
                    "Recipe 1.0 cannot define review contexts.");
                Require(
                    results,
                    "RECIPE-1.0-QUALITY",
                    recipe.quality == null
                        || (recipe.quality.minimumForegroundRatio == 0.01f
                            && recipe.quality.maximumBorderForegroundRatio
                                == 0.005f
                            && !recipe.quality.requireHumanReview),
                    "Recipe 1.0 cannot override quality settings.");
            }

            ValidateUniqueStrings(results, "RECIPE-LAYER", recipe.layers, null);

            if (recipe.capture != null)
            {
                Require(results, "RECIPE-CAPTURE-DURATION", recipe.capture.duration > 0f, "Capture duration must be greater than zero.");
                Require(results, "RECIPE-CAPTURE-WIDTH", recipe.capture.width >= 64, "Capture width must be at least 64.");
                Require(results, "RECIPE-CAPTURE-HEIGHT", recipe.capture.height >= 64, "Capture height must be at least 64.");
                ValidateUniqueStrings(
                    results,
                    "RECIPE-CAPTURE-VIEW",
                    recipe.capture.views,
                    new HashSet<string>(new[] { "front", "side", "top" }, StringComparer.Ordinal));
                ValidateUniqueStrings(
                    results,
                    "RECIPE-CAPTURE-CONTEXT",
                    recipe.capture.contexts,
                    null);

                if (recipe.capture.frameTimes != null)
                {
                    foreach (float frameTime in recipe.capture.frameTimes)
                    {
                        Require(results, "RECIPE-FRAME-TIME", frameTime >= 0f && frameTime <= recipe.capture.duration, $"Frame time {frameTime} is outside capture duration.");
                    }
                }
            }

            if (recipe.quality != null)
            {
                Require(
                    results,
                    "RECIPE-QUALITY-FOREGROUND",
                    IsFinite(recipe.quality.minimumForegroundRatio)
                        && recipe.quality.minimumForegroundRatio >= 0f
                        && recipe.quality.minimumForegroundRatio <= 1f,
                    "Minimum foreground ratio must be between 0 and 1.");
                Require(
                    results,
                    "RECIPE-QUALITY-BORDER",
                    IsFinite(recipe.quality.maximumBorderForegroundRatio)
                        && recipe.quality.maximumBorderForegroundRatio >= 0f
                        && recipe.quality.maximumBorderForegroundRatio <= 1f,
                    "Maximum border foreground ratio must be between 0 and 1.");
                Require(
                    results,
                    "RECIPE-REVIEW-CONTEXT",
                    !recipe.quality.requireHumanReview
                        || (recipe.capture?.contexts != null
                            && recipe.capture.contexts.Length > 0),
                    "Human visual review requires at least one gameplay Context.");
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

        private static void ValidateUniqueStrings(
            List<VfxValidationResult> results,
            string id,
            IEnumerable<string> values,
            ISet<string> allowedValues)
        {
            if (values == null)
            {
                return;
            }

            var uniqueValues = new HashSet<string>(StringComparer.Ordinal);
            foreach (string value in values)
            {
                bool valid = !string.IsNullOrWhiteSpace(value)
                    && uniqueValues.Add(value)
                    && (allowedValues == null || allowedValues.Contains(value));
                Require(results, id, valid, $"Value is empty, duplicated, or unsupported: {value ?? "<null>"}.");
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
