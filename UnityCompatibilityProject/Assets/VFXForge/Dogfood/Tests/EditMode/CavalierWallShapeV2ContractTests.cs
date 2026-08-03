using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace VfxForge.Dogfood.Tests
{
    public sealed class CavalierWallShapeV2ContractTests
    {
        private const string V1PrefabPath =
            "Assets/VFXForge/Dogfood/HolyAegisV4/"
            + "Authoring/Shape/CavalierWallShape.prefab";
        private const string V2PrefabPath =
            "Assets/VFXForge/Dogfood/HolyAegisV4/"
            + "Authoring/ShapeV2/CavalierWallShapeV2.prefab";

        [Test]
        public void ShapeV2_UsesHighDensityProductionReadyTopology()
        {
            GameObject v1 = LoadPrefab(V1PrefabPath);
            GameObject v2 = LoadPrefab(V2PrefabPath);
            int v1Triangles = CountRenderedTriangles(v1);
            int v2Triangles = CountRenderedTriangles(v2);
            int v2Vertices = CountRenderedVertices(v2);

            Assert.That(v1Triangles, Is.EqualTo(876));
            Assert.That(v2Triangles, Is.EqualTo(8824));
            Assert.That(v2Vertices, Is.EqualTo(4430));
            Assert.That(v2Triangles, Is.GreaterThan(v1Triangles * 3));
        }

        [Test]
        public void ShapeV2_ReplacesTeethWithContinuousFrameAndFourAnchors()
        {
            GameObject prefab = LoadPrefab(V2PrefabPath);
            Transform assembly = prefab.transform
                .Find("Facing Pivot/Cavalier Wall Assembly V2");
            Assert.That(assembly, Is.Not.Null);
            Assert.That(
                assembly.Find("Sculpted Primary Barrier Surface"),
                Is.Not.Null);
            Transform frame = assembly.Find("Continuous Beveled Frame");
            Assert.That(frame, Is.Not.Null);
            Assert.That(frame.childCount, Is.EqualTo(4));
            Transform anchors = assembly.Find("Four Integrated Anchors");
            Assert.That(anchors, Is.Not.Null);
            Assert.That(anchors.childCount, Is.EqualTo(4));
            Assert.That(
                assembly.Find("Four Connected Braces"),
                Is.Null);
        }

        [Test]
        public void ShapeV2_PreservesGrayscaleGateAndFacingContract()
        {
            GameObject prefab = LoadPrefab(V2PrefabPath);
            Assert.That(
                prefab.GetComponent<CavalierWallFacing>(),
                Is.Not.Null);
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
        public void ShapeV2_SurfaceHasUvAndSculptedVerticalResolution()
        {
            GameObject prefab = LoadPrefab(V2PrefabPath);
            Transform surface = prefab.transform.Find(
                "Facing Pivot/Cavalier Wall Assembly V2/"
                + "Sculpted Primary Barrier Surface");
            Mesh mesh = surface.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh.vertexCount, Is.GreaterThanOrEqualTo(2000));
            Assert.That(mesh.uv.Length, Is.EqualTo(mesh.vertexCount));
            Assert.That(mesh.bounds.size.y, Is.GreaterThan(3.5f));
            Assert.That(mesh.bounds.size.z, Is.GreaterThan(0.7f));
        }

        private static GameObject LoadPrefab(string path)
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            return prefab;
        }

        private static int CountRenderedTriangles(GameObject prefab)
        {
            int total = 0;
            foreach (MeshFilter filter in
                prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                total += filter.sharedMesh.triangles.Length / 3;
            }
            return total;
        }

        private static int CountRenderedVertices(GameObject prefab)
        {
            int total = 0;
            foreach (MeshFilter filter in
                prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                total += filter.sharedMesh.vertexCount;
            }
            return total;
        }
    }
}
