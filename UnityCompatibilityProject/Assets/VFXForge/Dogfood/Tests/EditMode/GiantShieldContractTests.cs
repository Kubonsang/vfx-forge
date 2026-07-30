using System.Collections.Generic;
using System.Linq;
using Kubonsang.VfxForge;
using Kubonsang.VfxForge.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

namespace VfxForge.Dogfood.Tests
{
    public sealed class GiantShieldContractTests
    {
        private const string Root =
            "Assets/VFXForge/Dogfood/GiantShield";

        private static readonly string[] RequiredProperties =
        {
            "RandomSeed",
            "Duration",
            "ImpactTime",
            "SustainTime",
            "DecayTime",
            "Radius",
            "SpreadAngle",
            "Directionality",
            "PrimaryColor",
            "SecondaryColor",
            "EmissionIntensity",
            "Sharpness"
        };

        [Test]
        public void Catalog_GiantShieldEntryBindsEveryRequiredProperty()
        {
            VfxTemplateCatalog catalog =
                AssetDatabase.LoadAssetAtPath<VfxTemplateCatalog>(
                    Root + "/GiantShieldCatalog.asset");

            Assert.That(catalog, Is.Not.Null);
            Assert.That(
                catalog.TryGet(
                    "giant_shield_deployment_v1",
                    out VfxTemplateEntry entry),
                Is.True);
            Assert.That(entry.prefab, Is.Not.Null);
            Assert.That(entry.playEventName, Is.EqualTo("OnPlay"));
            Assert.That(entry.bindings, Has.Count.EqualTo(12));
            Assert.That(
                entry.bindings.All(binding => binding.required),
                Is.True);
            Assert.That(
                entry.bindings.Select(
                    binding => binding.exposedPropertyName),
                Is.EquivalentTo(RequiredProperties));

            List<VfxValidationResult> results =
                VfxTemplateCatalogValidator.Validate(catalog);
            Assert.That(
                results.Any(
                    result => result.severity
                        == VfxValidationSeverity.Error),
                Is.False);
        }

        [Test]
        public void CompiledPrefabs_AreDistinctAndMeetShieldPolicies()
        {
            GameObject primary =
                LoadGenerated("GiantShieldDeployment.prefab");
            GameObject variant =
                LoadGenerated("GiantShieldDeploymentVariant.prefab");
            VisualEffect primaryEffect =
                primary.GetComponentInChildren<VisualEffect>(true);
            VisualEffect variantEffect =
                variant.GetComponentInChildren<VisualEffect>(true);

            Assert.That(primaryEffect, Is.Not.Null);
            Assert.That(variantEffect, Is.Not.Null);
            Assert.That(primaryEffect.enabled, Is.True);
            Assert.That(variantEffect.enabled, Is.True);
            Assert.That(primaryEffect.visualEffectAsset, Is.Not.Null);
            Assert.That(
                variantEffect.visualEffectAsset,
                Is.SameAs(primaryEffect.visualEffectAsset));
            AssertRequiredProperties(primaryEffect);
            AssertRequiredProperties(variantEffect);

            int differences = 0;
            differences += primaryEffect.GetInt("RandomSeed")
                != variantEffect.GetInt("RandomSeed") ? 1 : 0;
            foreach (string property in RequiredProperties
                .Skip(1)
                .Take(7)
                .Concat(RequiredProperties.Skip(10)))
            {
                differences += !Mathf.Approximately(
                    primaryEffect.GetFloat(property),
                    variantEffect.GetFloat(property)) ? 1 : 0;
            }
            differences += primaryEffect.GetVector4("PrimaryColor")
                != variantEffect.GetVector4("PrimaryColor") ? 1 : 0;
            differences += primaryEffect.GetVector4("SecondaryColor")
                != variantEffect.GetVector4("SecondaryColor") ? 1 : 0;
            Assert.That(differences, Is.GreaterThanOrEqualTo(10));

            Assert.That(
                primary.GetComponentsInChildren<Light>(true),
                Is.Empty);
            Assert.That(
                primary.GetComponentsInChildren<MeshRenderer>(true),
                Has.Length.EqualTo(10));
            Assert.That(
                primary.GetComponentsInChildren<MeshRenderer>(true)
                    .All(
                        renderer =>
                            renderer.sharedMaterial.renderQueue >= 3000),
                Is.True);
            Assert.That(
                primary.GetComponentsInChildren<ParticleSystem>(true)
                    .Sum(system => system.main.maxParticles),
                Is.LessThanOrEqualTo(128));

            Assert.That(
                primary.GetComponent<VfxMetadata>().recipeId,
                Is.EqualTo("giant_shield_deployment"));
            Assert.That(
                variant.GetComponent<VfxMetadata>().recipeId,
                Is.EqualTo("giant_shield_deployment_variant"));
        }

        private static GameObject LoadGenerated(string fileName)
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    Root + "/Generated/" + fileName);
            Assert.That(prefab, Is.Not.Null, fileName);
            return prefab;
        }

        private static void AssertRequiredProperties(
            VisualEffect effect)
        {
            Assert.That(effect.HasInt("RandomSeed"), Is.True);
            foreach (string property in RequiredProperties
                .Skip(1)
                .Take(7)
                .Concat(RequiredProperties.Skip(10)))
            {
                Assert.That(effect.HasFloat(property), Is.True, property);
            }
            Assert.That(effect.HasVector4("PrimaryColor"), Is.True);
            Assert.That(effect.HasVector4("SecondaryColor"), Is.True);
        }
    }
}
