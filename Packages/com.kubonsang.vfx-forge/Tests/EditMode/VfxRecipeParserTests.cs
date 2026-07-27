using NUnit.Framework;

namespace Kubonsang.VfxForge.Editor.Tests
{
    public sealed class VfxRecipeParserTests
    {
        [Test]
        public void ParseJson_ValidRecipe_ReturnsRecipe()
        {
            const string json = "{\"schemaVersion\":\"1.0\",\"id\":\"impact_test\",\"template\":\"impact_light\",\"outputPath\":\"Assets/VFXForge/Generated/Impact.prefab\",\"timing\":{\"duration\":0.2},\"budget\":{\"maxDuration\":1.0,\"maxParticles\":100}}";

            VfxRecipeParseResult result = VfxRecipeParser.ParseJson(json);

            Assert.That(result.Success, Is.True, result.Error);
            Assert.That(result.Recipe.id, Is.EqualTo("impact_test"));
        }

        [Test]
        public void ParseJson_EmptyJson_ReturnsFailure()
        {
            VfxRecipeParseResult result = VfxRecipeParser.ParseJson(string.Empty);
            Assert.That(result.Success, Is.False);
        }
    }
}
