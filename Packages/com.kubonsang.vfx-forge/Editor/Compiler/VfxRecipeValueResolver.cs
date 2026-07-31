using UnityEngine;

namespace Kubonsang.VfxForge.Editor
{
    public static class VfxRecipeValueResolver
    {
        public static bool TryGetPropertyType(string path, out VfxPropertyType propertyType)
        {
            switch (path)
            {
                case "seed":
                    propertyType = VfxPropertyType.Int;
                    return true;
                case "timing.duration":
                case "timing.anticipation":
                case "timing.impact":
                case "timing.sustain":
                case "timing.decay":
                case "shape.radius":
                case "shape.directionality":
                case "shape.spreadAngle":
                case "style.emissionIntensity":
                case "style.sharpness":
                case "style.distortionStrength":
                case "motion.speed":
                    propertyType = VfxPropertyType.Float;
                    return true;
                case "style.primaryColor":
                case "style.secondaryColor":
                    propertyType = VfxPropertyType.Color;
                    return true;
                case "motion.localDirection":
                    propertyType = VfxPropertyType.Vector3;
                    return true;
                case "geometry.variant":
                    propertyType = VfxPropertyType.String;
                    return true;
                default:
                    propertyType = default;
                    return false;
            }
        }

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
                case "motion.speed": value = recipe.motion.speed; return true;
                case "motion.localDirection": value = recipe.motion.localDirection; return true;
                case "geometry.variant": value = recipe.geometry.variant; return true;
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
