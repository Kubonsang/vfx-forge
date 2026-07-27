using System.Collections.Generic;
using NUnit.Framework;

namespace Kubonsang.VfxForge.Editor.Tests
{
    public sealed class VfxRecipeValidatorTests
    {
        [Test]
        public void Validate_DurationExceedsBudget_HasError()
        {
            VfxRecipe recipe = CreateValidRecipe();
            recipe.timing.duration = 2f;
            recipe.budget.maxDuration = 1f;

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

        [TestCase("../Escape.prefab")]
        [TestCase("Assets/../Escape.prefab")]
        [TestCase("Assets/VFXForge/Generated/../../Escape.prefab")]
        [TestCase("Assets//Impact.prefab")]
        [TestCase("Assets/VFXForge/Generated/Impact.asset")]
        [TestCase("Assets/VFXForge/Generated/.prefab")]
        [TestCase("C:/Project/Assets/Impact.prefab")]
        public void Validate_UnsafeOutputPath_ReturnsOutputError(string outputPath)
        {
            VfxRecipe recipe = CreateValidRecipe();
            recipe.outputPath = outputPath;

            List<VfxValidationResult> results = VfxRecipeValidator.Validate(recipe);

            Assert.That(HasError(results, "RECIPE-OUTPUT"), Is.True);
        }

        [Test]
        public void Normalize_WindowsOutputPath_ProducesSafeAssetPath()
        {
            VfxRecipe recipe = CreateValidRecipe();
            recipe.outputPath = " Assets\\VFXForge\\Generated\\Impact.prefab ";

            VfxRecipeNormalizer.Normalize(recipe);

            Assert.That(recipe.outputPath, Is.EqualTo("Assets/VFXForge/Generated/Impact.prefab"));
            Assert.That(VfxRecipePath.TryNormalizePrefabAssetPath(recipe.outputPath, out _), Is.True);
        }

        [Test]
        public void Validate_SchemaRangeViolations_ReturnStableRuleIds()
        {
            VfxRecipe recipe = CreateValidRecipe();
            recipe.style.sharpness = 1.1f;
            recipe.budget.maxParticles = 0;
            recipe.capture.width = 32;

            List<VfxValidationResult> results = VfxRecipeValidator.Validate(recipe);

            Assert.That(HasError(results, "RECIPE-SHARPNESS"), Is.True);
            Assert.That(HasError(results, "RECIPE-MAX-PARTICLES"), Is.True);
            Assert.That(HasError(results, "RECIPE-CAPTURE-WIDTH"), Is.True);
        }

        [Test]
        public void Validate_DuplicateLayer_ReturnsStableRuleId()
        {
            VfxRecipe recipe = CreateValidRecipe();
            recipe.layers = new[] { "core", "core" };

            List<VfxValidationResult> results = VfxRecipeValidator.Validate(recipe);

            Assert.That(HasError(results, "RECIPE-LAYER"), Is.True);
        }

        private static VfxRecipe CreateValidRecipe()
        {
            return new VfxRecipe
            {
                id = "impact_test",
                template = "impact_light",
                outputPath = "Assets/VFXForge/Generated/Impact.prefab",
                timing = new VfxTiming { duration = 0.5f },
                budget = new VfxBudget
                {
                    maxDuration = 1f,
                    maxParticles = 100,
                    maxBoundsRadius = 5f
                }
            };
        }

        private static bool HasError(IEnumerable<VfxValidationResult> results, string ruleId)
        {
            foreach (VfxValidationResult result in results)
            {
                if (result != null
                    && result.ruleId == ruleId
                    && result.severity == VfxValidationSeverity.Error
                    && !result.passed)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
