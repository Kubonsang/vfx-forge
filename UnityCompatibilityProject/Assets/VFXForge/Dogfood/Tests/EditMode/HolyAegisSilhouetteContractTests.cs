using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace VfxForge.Dogfood.Tests
{
    public sealed class HolyAegisSilhouetteContractTests
    {
        private const string PrefabPath =
            "Assets/VFXForge/Dogfood/HolyAegisV3/"
            + "Authoring/Silhouette/"
            + "HolyAegisV3Silhouette.prefab";

        [Test]
        public void Silhouette_HasOnePlateOneCrestAndFourConnectedOrnaments()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            Transform assembly =
                prefab.transform.Find("Shield Assembly");
            Assert.That(assembly, Is.Not.Null);
            Assert.That(
                Mathf.DeltaAngle(
                    assembly.localEulerAngles.x,
                    325f),
                Is.EqualTo(0f).Within(0.1f));
            Assert.That(
                assembly.Find("Circular Main Plate"),
                Is.Not.Null);
            Assert.That(
                assembly.Find("Central Knight Crest"),
                Is.Not.Null);

            Transform ornaments =
                assembly.Find("Four Rim Ornaments");
            Assert.That(ornaments, Is.Not.Null);
            Assert.That(
                ornaments.childCount,
                Is.EqualTo(4));
            foreach (Transform ornament in ornaments)
            {
                Assert.That(
                    ornament.name,
                    Does.Contain("Connected Ornament"));
                Assert.That(
                    ornament.GetComponent<MeshRenderer>(),
                    Is.Not.Null);
            }
        }

        [Test]
        public void Silhouette_ContainsNoForbiddenParticleOrLightComponents()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(
                prefab.GetComponentsInChildren<ParticleSystem>(true),
                Is.Empty);
            Assert.That(
                prefab.GetComponentsInChildren<Light>(true),
                Is.Empty);
        }

        [Test]
        public void Silhouette_AllMaterialsAreGrayscale()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrefabPath);
            foreach (Renderer renderer in
                prefab.GetComponentsInChildren<Renderer>(true))
            {
                Material material = renderer.sharedMaterial;
                Assert.That(material, Is.Not.Null);
                Color color = material.HasProperty("_BaseColor")
                    ? material.GetColor("_BaseColor")
                    : material.GetColor("_Color");
                Assert.That(color.r, Is.EqualTo(color.g).Within(0.001f));
                Assert.That(color.g, Is.EqualTo(color.b).Within(0.001f));
            }
        }
    }
}
