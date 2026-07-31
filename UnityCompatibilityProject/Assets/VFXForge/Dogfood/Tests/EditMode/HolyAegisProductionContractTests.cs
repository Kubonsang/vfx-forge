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
    public sealed class HolyAegisProductionContractTests
    {
        private const string Root =
            "Assets/VFXForge/Dogfood/HolyAegisV3";
        private const string PrimaryPath =
            Root + "/Generated/HolyAegisShieldV3.prefab";
        private const string VariantPath =
            Root + "/Generated/HolyAegisShieldV3Variant.prefab";

        [Test]
        public void Catalog_UsesTypedMultiTargetBindingsAndReviewContext()
        {
            VfxTemplateCatalog catalog =
                AssetDatabase.LoadAssetAtPath<VfxTemplateCatalog>(
                    Root + "/HolyAegisV3Catalog.asset");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(
                catalog.TryGet(
                    "holy_aegis_shield_v3",
                    out VfxTemplateEntry entry),
                Is.True);
            Assert.That(entry.bindings, Has.Count.EqualTo(19));
            Assert.That(
                entry.bindings.All(binding => binding.required),
                Is.True);
            Assert.That(
                entry.bindings.Select(binding => binding.targetKind),
                Does.Contain(VfxBindingTargetKind.AdapterProperty));
            Assert.That(
                entry.bindings.Select(binding => binding.targetKind),
                Does.Contain(VfxBindingTargetKind.MaterialProperty));
            Assert.That(
                entry.bindings.Select(binding => binding.targetKind),
                Does.Contain(VfxBindingTargetKind.TransformProperty));
            Assert.That(
                catalog.TryGetReviewContext(
                    "topdown_three_grounds",
                    out VfxReviewContextEntry context),
                Is.True);
            Assert.That(context.prefab, Is.Not.Null);

            List<VfxValidationResult> results =
                VfxTemplateCatalogValidator.Validate(catalog);
            Assert.That(
                results.Any(
                    result =>
                        result.severity
                        == VfxValidationSeverity.Error),
                Is.False);
        }

        [Test]
        public void GeneratedPrimaryAndVariant_DifferAcrossTypedTargets()
        {
            GameObject primary = Load(PrimaryPath);
            GameObject variant = Load(VariantPath);
            HolyAegisDeployment primaryController =
                primary.GetComponent<HolyAegisDeployment>();
            HolyAegisDeployment variantController =
                variant.GetComponent<HolyAegisDeployment>();

            Assert.That(primaryController.Duration,
                Is.EqualTo(1.8f).Within(0.001f));
            Assert.That(variantController.Duration,
                Is.EqualTo(1.62f).Within(0.001f));
            Assert.That(primaryController.Radius,
                Is.EqualTo(2.6f).Within(0.001f));
            Assert.That(variantController.Radius,
                Is.EqualTo(2.3f).Within(0.001f));

            SerializedObject primarySerialized =
                new SerializedObject(primaryController);
            SerializedObject variantSerialized =
                new SerializedObject(variantController);
            Assert.That(
                primarySerialized.FindProperty("primaryColor")
                    .colorValue,
                Is.Not.EqualTo(
                    variantSerialized.FindProperty(
                        "primaryColor").colorValue));
            Assert.That(
                primarySerialized.FindProperty("pulseRate")
                    .floatValue,
                Is.Not.EqualTo(
                    variantSerialized.FindProperty(
                        "pulseRate").floatValue));
            Assert.That(
                primarySerialized.FindProperty("localDirection")
                    .vector3Value,
                Is.Not.EqualTo(
                    variantSerialized.FindProperty(
                        "localDirection").vector3Value));

            Transform primaryWitness =
                primary.transform.Find("Recipe Scale Witness");
            Transform variantWitness =
                variant.transform.Find("Recipe Scale Witness");
            Assert.That(primaryWitness.localScale.x,
                Is.EqualTo(2.6f).Within(0.001f));
            Assert.That(variantWitness.localScale.x,
                Is.EqualTo(2.3f).Within(0.001f));

            VfxMaterialPropertyOverride primaryColor =
                FindOverride(
                    primary,
                    "Shield Assembly/Circular Main Plate",
                    "_PrimaryColor");
            VfxMaterialPropertyOverride variantColor =
                FindOverride(
                    variant,
                    "Shield Assembly/Circular Main Plate",
                    "_PrimaryColor");
            Assert.That(
                primaryColor.colorValue,
                Is.Not.EqualTo(variantColor.colorValue));
        }

        [Test]
        public void GeneratedPrimary_UsesOnlyDedicatedVisibleShader()
        {
            GameObject primary = Load(PrimaryPath);
            Assert.That(
                primary.GetComponentsInChildren<ParticleSystem>(
                    true),
                Is.Empty);
            Assert.That(
                primary.GetComponentsInChildren<Light>(true),
                Is.Empty);
            VisualEffect effect =
                primary.GetComponentInChildren<VisualEffect>(true);
            Assert.That(effect, Is.Not.Null);
            Assert.That(effect.enabled, Is.True);

            MeshRenderer[] renderers =
                primary.GetComponentsInChildren<MeshRenderer>(
                    true);
            Assert.That(renderers, Has.Length.EqualTo(14));
            Assert.That(
                renderers.All(
                    renderer =>
                        renderer.sharedMaterial != null
                        && renderer.sharedMaterial.shader.name
                            == "VFXForge/Dogfood/HolyAegisShield"),
                Is.True);

            Transform ornaments =
                primary.transform.Find(
                    "Shield Assembly/Four Rim Ornaments");
            Assert.That(ornaments, Is.Not.Null);
            Assert.That(ornaments.childCount, Is.EqualTo(4));
        }

        [Test]
        public void Decay_RemovesOuterLayersBeforeCentralCrest()
        {
            GameObject instance =
                Object.Instantiate(Load(PrimaryPath));
            try
            {
                HolyAegisDeployment controller =
                    instance.GetComponent<HolyAegisDeployment>();
                controller.EvaluatePreviewTime(1.56f);
                float ornament = ReadAlpha(
                    instance.transform.Find(
                        "Shield Assembly/Four Rim Ornaments/"
                        + "Left Connected Ornament")
                        .GetComponent<Renderer>());
                float rim = ReadAlpha(
                    instance.transform.Find(
                        "Shield Assembly/Thick Connected Rim")
                        .GetComponent<Renderer>());
                float plate = ReadAlpha(
                    instance.transform.Find(
                        "Shield Assembly/Circular Main Plate")
                        .GetComponent<Renderer>());
                float crest = ReadAlpha(
                    instance.transform.Find(
                        "Shield Assembly/Central Knight Crest/"
                        + "Crest Sword")
                        .GetComponent<Renderer>());

                Assert.That(ornament, Is.LessThan(rim));
                Assert.That(rim, Is.LessThan(plate));
                Assert.That(plate, Is.LessThan(crest));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static GameObject Load(string path)
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            return prefab;
        }

        private static VfxMaterialPropertyOverride FindOverride(
            GameObject prefab,
            string path,
            string property)
        {
            VfxMaterialPropertyOverrides overrides =
                prefab.transform.Find(path)
                    .GetComponent<VfxMaterialPropertyOverrides>();
            Assert.That(overrides, Is.Not.Null, path);
            VfxMaterialPropertyOverride entry =
                overrides.Overrides.FirstOrDefault(
                    candidate =>
                        candidate.propertyName == property);
            Assert.That(entry, Is.Not.Null, property);
            return entry;
        }

        private static float ReadAlpha(Renderer renderer)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            return block.GetFloat("_LayerAlpha");
        }
    }
}
