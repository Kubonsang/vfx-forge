using UnityEngine;

namespace Kubonsang.VfxForge.Editor
{
    public static class VfxRecipeValueResolver
    {
        public static bool TryResolve(VfxRecipe recipe, string path, out object value)
        {
            value = null;
            if (recipe == null || string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            switch (path)
            {
                case "seed": value = recipe.seed; return true;
                case "timing.duration": value = recipe.timing.duration; return true;
                case "timing.anticipation": value = recipe.timing.anticipation; return true;
                case "timing.impact": value = recipe.timing.impact; return true;
                case "timing.sustain": value = recipe.timing.sustain; return true;
                case "timing.decay": value = recipe.timing.decay; return true;
                case "shape.radius": value = recipe.shape.radius; return true;
                case "shape.directionality": value = recipe.shape.directionality; return true;
                case "shape.spreadAngle": value = recipe.shape.spreadAngle; return true;
                case "style.emissionIntensity": value = recipe.style.emissionIntensity; return true;
                case "style.sharpness": value = recipe.style.sharpness; return true;
                case "style.distortionStrength": value = recipe.style.distortionStrength; return true;
                case "style.primaryColor":
                    if (ColorUtility.TryParseHtmlString(recipe.style.primaryColor, out Color primary)) { value = primary; return true; }
                    return false;
                case "style.secondaryColor":
                    if (ColorUtility.TryParseHtmlString(recipe.style.secondaryColor, out Color secondary)) { value = secondary; return true; }
                    return false;
                default:
                    return false;
            }
        }
    }
}
