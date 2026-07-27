using NUnit.Framework;

namespace Kubonsang.VfxForge.Editor.Tests
{
    public sealed class VfxRecipeValidatorTests
    {
        [Test]
        public void Validate_DurationExceedsBudget_HasError()
        {
            var recipe = new VfxRecipe
            {
                id = "impact_test",
                template = "impact_light",
                outputPath = "Assets/VFXForge/Generated/Impact.prefab",
                timing = new VfxTiming { duration = 2f },
                budget = new VfxBudget { maxDuration = 1f, maxParticles = 100 }
            };

            Assert.That(VfxRecipeValidator.HasErrors(VfxRecipeValidator.Validate(recipe)), Is.True);
        }

        [Test]
        public void Normalize_UnsortedFrameTimes_SortsAscending()
        {
            var recipe = new VfxRecipe
            {
                capture = new VfxCaptureSettings { duration = 1f, frameTimes = new[] { 0.5f, 0.1f, 0.3f } }
            };

            VfxRecipeNormalizer.Normalize(recipe);

            Assert.That(recipe.capture.frameTimes, Is.EqualTo(new[] { 0.1f, 0.3f, 0.5f }));
        }
    }
}
