using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace VfxForge.Dogfood.Tests
{
    public sealed class CavalierWallShapeV3ContractTests
    {
        private const string V2PrefabPath =
            "Assets/VFXForge/Dogfood/HolyAegisV4/"
            + "Authoring/ShapeV2/CavalierWallShapeV2.prefab";
        private const string V3PrefabPath =
            "Assets/VFXForge/Dogfood/HolyAegisV4/"
            + "Authoring/ShapeV3/CavalierWallShapeV3.prefab";

        [Test]
        public void ShapeV3_UsesScreenContributionTopologyBudget()
        {
            GameObject v2 = LoadPrefab(V2PrefabPath);
            GameObject v3 = LoadPrefab(V3PrefabPath);
            int v2Triangles = CountRenderedTriangles(v2);
            int v3Triangles = CountRenderedTriangles(v3);
            int v3Vertices = CountRenderedVertices(v3);

            Assert.That(v2Triangles, Is.EqualTo(8824));
            Assert.That(v3Triangles, Is.EqualTo(4384));
            Assert.That(v3Triangles, Is.InRange(3000, 5000));
            Assert.That(v3Vertices, Is.EqualTo(3234));
        }

        [Test]
        public void ShapeV3_HasTaperedFrameAndDistinctAnchorRoles()
        {
            GameObject prefab = LoadPrefab(V3PrefabPath);
            Transform assembly = prefab.transform.Find(
                "Facing Pivot/Cavalier Wall Assembly V3");
            Assert.That(assembly, Is.Not.Null);
            Assert.That(
                assembly.Find("Tapered Primary Barrier Surface"),
                Is.Not.Null);
            Transform frame = assembly.Find("Tapered Hard Bevel Frame");
            Assert.That(frame, Is.Not.Null);
            Assert.That(frame.childCount, Is.EqualTo(4));
            Transform anchors = assembly.Find("Four Authored Anchors");
            Assert.That(anchors, Is.Not.Null);
            Assert.That(anchors.childCount, Is.EqualTo(4));
            Assert.That(
                anchors.Find("Left Crown Keystone"),
                Is.Not.Null);
            Assert.That(
                anchors.Find("Left Root Bastion"),
                Is.Not.Null);
        }

        [Test]
        public void ShapeV3_RailTopologySplitsBevelFaces()
        {
            GameObject prefab = LoadPrefab(V3PrefabPath);
            Transform rail = prefab.transform.Find(
                "Facing Pivot/Cavalier Wall Assembly V3/"
                + "Tapered Hard Bevel Frame/Weighted Crown Rail");
            Mesh mesh = rail.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh.vertexCount, Is.EqualTo(802));
            Assert.That(mesh.triangles.Length / 3, Is.EqualTo(784));
        }

        [Test]
        public void ShapeV3_PreservesGrayscaleAndFacingGate()
        {
            GameObject prefab = LoadPrefab(V3PrefabPath);
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
