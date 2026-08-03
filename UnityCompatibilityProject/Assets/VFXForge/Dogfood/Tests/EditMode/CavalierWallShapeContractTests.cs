using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace VfxForge.Dogfood.Tests
{
    public sealed class CavalierWallShapeContractTests
    {
        private const string PrefabPath =
            "Assets/VFXForge/Dogfood/HolyAegisV4/"
            + "Authoring/Shape/CavalierWallShape.prefab";

        [Test]
        public void Shape_HasApprovedConnectedLayerHierarchy()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrefabPath);
            Assert.That(prefab, Is.Not.Null);

            Transform pivot = prefab.transform.Find("Facing Pivot");
            Assert.That(pivot, Is.Not.Null);
            Transform assembly = pivot.Find(
                "Cavalier Wall Assembly");
            Assert.That(assembly, Is.Not.Null);
            Assert.That(
                assembly.Find("Primary Barrier Surface"),
                Is.Not.Null);
            Assert.That(
                assembly.Find("Structural Frame"),
                Is.Not.Null);

            Transform braces = assembly.Find(
                "Four Connected Braces");
            Assert.That(braces, Is.Not.Null);
            Assert.That(braces.childCount, Is.EqualTo(4));
            foreach (Transform brace in braces)
            {
                Assert.That(
                    brace.GetComponent<MeshRenderer>(),
                    Is.Not.Null);
            }
        }

        [Test]
        public void Shape_IsGrayscaleAndContainsNoFinishLayers()
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

            foreach (Renderer renderer in
                prefab.GetComponentsInChildren<Renderer>(true))
            {
                Material material = renderer.sharedMaterial;
                Assert.That(material, Is.Not.Null);
                Color color = material.HasProperty("_BaseColor")
                    ? material.GetColor("_BaseColor")
                    : material.GetColor("_Color");
                Assert.That(
                    color.r,
                    Is.EqualTo(color.g).Within(0.001f));
                Assert.That(
                    color.g,
                    Is.EqualTo(color.b).Within(0.001f));
            }
        }

        [Test]
        public void Shape_IsWideUprightAndPlayerFacingReady()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            CavalierWallFacing facing =
                prefab.GetComponent<CavalierWallFacing>();
            Assert.That(facing, Is.Not.Null);
            Assert.That(facing.FacingPivot, Is.Not.Null);

            Bounds bounds = new Bounds();
            bool initialized = false;
            foreach (Renderer renderer in
                prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (!initialized)
                {
                    bounds = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            Assert.That(initialized, Is.True);
            Assert.That(bounds.size.x, Is.GreaterThan(5.8f));
            Assert.That(bounds.size.y, Is.GreaterThan(3.0f));
            Assert.That(bounds.size.z, Is.LessThan(1.3f));
        }
    }
}
