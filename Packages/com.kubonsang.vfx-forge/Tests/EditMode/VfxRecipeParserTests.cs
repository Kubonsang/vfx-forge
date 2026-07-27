using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor.PackageManager;

namespace Kubonsang.VfxForge.Editor.Tests
{
    public sealed class VfxRecipeParserTests
    {
        private const string MinimalValidJson =
            "{\"schemaVersion\":\"1.0\",\"id\":\"impact_test\",\"template\":\"impact_light\","
            + "\"outputPath\":\"Assets/VFXForge/Generated/Impact.prefab\","
            + "\"timing\":{\"duration\":0.2},"
            + "\"budget\":{\"maxParticles\":100,\"maxDuration\":1.0}}";

        [Test]
        public void ParseJson_ValidRecipe_ReturnsRecipe()
        {
            VfxRecipeParseResult result = VfxRecipeParser.ParseJson(MinimalValidJson);

            Assert.That(result.Success, Is.True, result.Error);
            Assert.That(result.Recipe.id, Is.EqualTo("impact_test"));
            Assert.That(result.ErrorCode, Is.Empty);
        }

        [Test]
        public void ParseJson_EmptyJson_ReturnsFailure()
        {
            VfxRecipeParseResult result = VfxRecipeParser.ParseJson(string.Empty);

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(VfxRecipeErrorCodes.JsonEmpty));
        }

        [Test]
        public void ParseJson_MinimalRecipe_AppliesOptionalDefaults()
        {
            VfxRecipeParseResult result = VfxRecipeParser.ParseJson(MinimalValidJson);

            Assert.That(result.Success, Is.True, result.Error);
            Assert.That(result.Recipe.seed, Is.EqualTo(1));
            Assert.That(result.Recipe.timing.impact, Is.EqualTo(0.1f));
            Assert.That(result.Recipe.shape.radius, Is.EqualTo(1f));
            Assert.That(result.Recipe.style.primaryColor, Is.EqualTo("#FFFFFFFF"));
            Assert.That(result.Recipe.budget.maxBoundsRadius, Is.EqualTo(5f));
            Assert.That(result.Recipe.capture.width, Is.EqualTo(1024));
            Assert.That(result.Recipe.capture.views, Is.EqualTo(new[] { "front" }));
        }

        [Test]
        public void ParseFile_PackageSample_ReturnsRecipe()
        {
            PackageInfo package = PackageInfo.FindForAssembly(typeof(VfxRecipeParser).Assembly);
            Assert.That(package, Is.Not.Null);
            string samplePath = Path.Combine(
                package.resolvedPath,
                "Samples~",
                "BasicRecipe",
                "sample_arcane_impact.json");

            VfxRecipeParseResult result = VfxRecipeParser.ParseFile(samplePath);

            Assert.That(result.Success, Is.True, result.Error);
            Assert.That(result.Recipe.id, Is.EqualTo("sample_arcane_impact"));
        }

        [TestCaseSource(nameof(MissingRequiredFieldCases))]
        public void ParseJson_MissingRequiredField_ReturnsStableCode(string json, string fieldPath)
        {
            VfxRecipeParseResult result = VfxRecipeParser.ParseJson(json);

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(VfxRecipeErrorCodes.SchemaMissingField));
            StringAssert.Contains(fieldPath, result.Error);
        }

        [Test]
        public void ParseJson_UnknownField_ReturnsStableCode()
        {
            string json = AppendRootProperty("\"unexpected\":true");

            VfxRecipeParseResult result = VfxRecipeParser.ParseJson(json);

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(VfxRecipeErrorCodes.SchemaUnknownField));
            StringAssert.Contains("unexpected", result.Error);
        }

        [Test]
        public void ParseJson_DuplicateField_ReturnsStableCode()
        {
            string json = MinimalValidJson.Replace(
                "\"id\":\"impact_test\",",
                "\"id\":\"impact_test\",\"id\":\"duplicate\",");

            VfxRecipeParseResult result = VfxRecipeParser.ParseJson(json);

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(VfxRecipeErrorCodes.SchemaDuplicateField));
            StringAssert.Contains("id", result.Error);
        }

        [Test]
        public void ParseJson_WrongFieldType_ReturnsStableCode()
        {
            string json = MinimalValidJson.Replace("\"id\":\"impact_test\"", "\"id\":42");

            VfxRecipeParseResult result = VfxRecipeParser.ParseJson(json);

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(VfxRecipeErrorCodes.SchemaTypeMismatch));
            StringAssert.Contains("id", result.Error);
        }

        [Test]
        public void ParseJson_WrongArrayItemType_ReturnsStableCode()
        {
            string json = AppendRootProperty("\"layers\":[1]");

            VfxRecipeParseResult result = VfxRecipeParser.ParseJson(json);

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(VfxRecipeErrorCodes.SchemaTypeMismatch));
            StringAssert.Contains("layers", result.Error);
        }

        [Test]
        public void ParseJson_MalformedJson_ReturnsStableCode()
        {
            VfxRecipeParseResult result = VfxRecipeParser.ParseJson("{\"schemaVersion\":");

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(VfxRecipeErrorCodes.JsonMalformed));
        }

        [Test]
        public void ParseFile_EmptyPath_ReturnsStableCode()
        {
            VfxRecipeParseResult result = VfxRecipeParser.ParseFile(string.Empty);

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(VfxRecipeErrorCodes.FilePathEmpty));
        }

        [Test]
        public void ParseFile_MissingFile_ReturnsStableCode()
        {
            string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");

            VfxRecipeParseResult result = VfxRecipeParser.ParseFile(path);

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(VfxRecipeErrorCodes.FileNotFound));
        }

        private static string AppendRootProperty(string propertyJson)
        {
            return MinimalValidJson.Substring(0, MinimalValidJson.Length - 1)
                + ","
                + propertyJson
                + "}";
        }

        private static IEnumerable MissingRequiredFieldCases()
        {
            yield return new TestCaseData(
                MinimalValidJson.Replace("\"schemaVersion\":\"1.0\",", string.Empty),
                "schemaVersion");
            yield return new TestCaseData(
                MinimalValidJson.Replace("\"id\":\"impact_test\",", string.Empty),
                "id");
            yield return new TestCaseData(
                MinimalValidJson.Replace("\"template\":\"impact_light\",", string.Empty),
                "template");
            yield return new TestCaseData(
                MinimalValidJson.Replace(
                    "\"outputPath\":\"Assets/VFXForge/Generated/Impact.prefab\",",
                    string.Empty),
                "outputPath");
            yield return new TestCaseData(
                MinimalValidJson.Replace("\"timing\":{\"duration\":0.2},", string.Empty),
                "timing");
            yield return new TestCaseData(
                MinimalValidJson.Replace(
                    ",\"budget\":{\"maxParticles\":100,\"maxDuration\":1.0}",
                    string.Empty),
                "budget");
            yield return new TestCaseData(
                MinimalValidJson.Replace("\"duration\":0.2", string.Empty),
                "timing.duration");
            yield return new TestCaseData(
                MinimalValidJson.Replace("\"maxParticles\":100,", string.Empty),
                "budget.maxParticles");
            yield return new TestCaseData(
                MinimalValidJson.Replace(",\"maxDuration\":1.0", string.Empty),
                "budget.maxDuration");
        }
    }
}
