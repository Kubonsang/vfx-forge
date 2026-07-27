using System;

namespace Kubonsang.VfxForge.Editor
{
    public static class VfxRecipeNormalizer
    {
        public static void Normalize(VfxRecipe recipe)
        {
            if (recipe == null)
            {
                return;
            }

            recipe.schemaVersion = string.IsNullOrWhiteSpace(recipe.schemaVersion) ? "1.0" : recipe.schemaVersion.Trim();
            recipe.id = recipe.id?.Trim() ?? string.Empty;
            recipe.template = recipe.template?.Trim() ?? string.Empty;
            recipe.outputPath = recipe.outputPath?.Trim().Replace('\\', '/') ?? string.Empty;
            recipe.seed = recipe.seed == 0 ? 1 : recipe.seed;
            if (recipe.timing == null) recipe.timing = new VfxTiming();
            if (recipe.shape == null) recipe.shape = new VfxShape();
            if (recipe.style == null) recipe.style = new VfxStyle();
            if (recipe.budget == null) recipe.budget = new VfxBudget();
            if (recipe.capture == null) recipe.capture = new VfxCaptureSettings();
            if (recipe.layers == null) recipe.layers = Array.Empty<string>();
            if (recipe.capture.frameTimes == null) recipe.capture.frameTimes = Array.Empty<float>();
            if (recipe.capture.views == null) recipe.capture.views = Array.Empty<string>();
            Array.Sort(recipe.capture.frameTimes);
        }
    }
}
