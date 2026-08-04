using System;
using System.Collections.Generic;
using System.IO;
using Kubonsang.VfxForge.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace VfxForge.Dogfood.Tests
{
    public sealed class SymmetricShieldProBuilderBlockoutTests
    {
        private const string SourcePrefabPath =
            "Assets/VFXForge/Dogfood/HolyAegisV4/Authoring/ProBuilder/"
            + "SymmetricShieldBlockoutV1.prefab";
        private const string RuntimePrefabPath =
            "Assets/VFXForge/Dogfood/HolyAegisV4/Runtime/"
            + "SymmetricShieldBlockoutV1.prefab";
        private const string EvidenceDirectory =
            "Dogfooding/Evidence/VF-022R-symmetric-shield-blockout-v1";

        [Test]
        public void SourceIsEditableAndRuntimeIsProBuilderFree()
        {
            GameObject source = LoadPrefab(SourcePrefabPath);
            GameObject runtime = LoadPrefab(RuntimePrefabPath);

            Assert.That(CountProBuilderComponents(source), Is.EqualTo(6));
            Assert.That(CountProBuilderComponents(runtime), Is.Zero);
            Assert.That(
                source.GetComponent<SymmetricShieldBlockoutMarker>()
                    .AuthoringRevision,
                Is.EqualTo("symmetric-shield-blockout-v5"));
            Assert.That(
                runtime.GetComponent<SymmetricShieldBlockoutMarker>(),
                Is.Not.Null);
        }

        [Test]
        public void RuntimeMeshesPassTopologyAndBudgetValidation()
        {
            GameObject runtime = LoadPrefab(RuntimePrefabPath);
            int triangles = 0;
            foreach (MeshFilter filter in
                runtime.GetComponentsInChildren<MeshFilter>(true))
            {
                Assert.That(filter.sharedMesh, Is.Not.Null, filter.name);
                VfxMeshTopologyReport report =
                    VfxMeshTopologyValidator.Evaluate(filter.sharedMesh);
                Assert.That(report.Valid, Is.True, filter.name);
                Assert.That(report.InvalidVertexCount, Is.Zero, filter.name);
                Assert.That(report.DegenerateTriangleCount, Is.Zero, filter.name);
                Assert.That(report.NonManifoldEdgeCount, Is.Zero, filter.name);
                triangles += filter.sharedMesh.triangles.Length / 3;
            }

            Assert.That(triangles, Is.GreaterThan(0));
            Assert.That(triangles, Is.LessThanOrEqualTo(12000));
        }

        [Test]
        public void RuntimeExportIsDeterministicAndPreservesSource()
        {
            string sourceBefore = AssetDatabase.GetAssetDependencyHash(
                SourcePrefabPath).ToString();
            VfxMeshRuntimeExportResult first =
                VfxProBuilderRuntimeExporter.Export(
                    SourcePrefabPath,
                    RuntimePrefabPath,
                    "Assets/VFXForge/Dogfood/HolyAegisV4/Runtime/Meshes");
            VfxMeshRuntimeExportResult second =
                VfxProBuilderRuntimeExporter.Export(
                    SourcePrefabPath,
                    RuntimePrefabPath,
                    "Assets/VFXForge/Dogfood/HolyAegisV4/Runtime/Meshes");

            Assert.That(first.SourceDependencyHash, Is.EqualTo(sourceBefore));
            Assert.That(second.SourceDependencyHash, Is.EqualTo(sourceBefore));
            Assert.That(
                second.RuntimeDependencyHash,
                Is.EqualTo(first.RuntimeDependencyHash));
            Assert.That(second.RenderedTriangles, Is.EqualTo(first.RenderedTriangles));
        }

        [Test]
        public void AuthoredGuardPairsAreGeometricallyMirrored()
        {
            GameObject runtime = LoadPrefab(RuntimePrefabPath);
            AssertMirrored(
                FindRenderer(runtime, "Editable Upper Left Shoulder Guard"),
                FindRenderer(runtime, "Editable Upper Right Shoulder Guard"));
            AssertMirrored(
                FindRenderer(runtime, "Editable Lower Left Flank Guard"),
                FindRenderer(runtime, "Editable Lower Right Flank Guard"));
        }

        [Test]
        public void AuthoringAndCaptureEvidenceMatchGeneratedAssets()
        {
            string repository = RepositoryRoot();
            VfxMeshAuthoringManifest authoring = ReadJson<
                VfxMeshAuthoringManifest>(Path.Combine(
                    repository,
                    EvidenceDirectory,
                    "mesh-authoring.json"));
            VfxMeshContractValidation validation =
                VfxMeshContractValidator.Validate(authoring);

            Assert.That(validation.Valid, Is.True);
            Assert.That(
                authoring.sourceDependencyHash,
                Is.EqualTo(AssetDatabase.GetAssetDependencyHash(
                    SourcePrefabPath).ToString()));
            Assert.That(
                authoring.runtimeDependencyHash,
                Is.EqualTo(AssetDatabase.GetAssetDependencyHash(
                    RuntimePrefabPath).ToString()));
            Assert.That(authoring.renderedTriangles, Is.GreaterThan(0));

            string capture = File.ReadAllText(Path.Combine(
                repository,
                EvidenceDirectory,
                "capture-manifest.json"));
            Assert.That(capture, Does.Contain("\"silhouetteIou\": 0.878"));
            Assert.That(capture, Does.Contain("\"landmarkMeanError\""));
            Assert.That(File.Exists(Path.Combine(
                repository,
                EvidenceDirectory,
                "blockout-contact-sheet.png")), Is.True);
        }

        [Test]
        public void BlockoutRemainsReviewRequiredUntilHumanDecision()
        {
            VfxMeshReviewRecord review = ReadJson<VfxMeshReviewRecord>(
                Path.Combine(
                    RepositoryRoot(),
                    EvidenceDirectory,
                    "blockout-review.json"));

            Assert.That(review.stage, Is.EqualTo(VfxMeshReviewStage.Blockout));
            Assert.That(
                review.status,
                Is.EqualTo(VfxMeshReviewStatus.ReviewRequired));
            Assert.That(review.accepted, Is.False);
            Assert.That(review.reviewer, Is.Empty);
        }

        private static void AssertMirrored(Renderer left, Renderer right)
        {
            Assert.That(left.bounds.center.x, Is.LessThan(0f));
            Assert.That(right.bounds.center.x, Is.GreaterThan(0f));
            Assert.That(
                left.bounds.center.x,
                Is.EqualTo(-right.bounds.center.x).Within(0.0001f));
            Assert.That(
                left.bounds.center.y,
                Is.EqualTo(right.bounds.center.y).Within(0.0001f));
            Assert.That(
                left.bounds.size,
                Is.EqualTo(right.bounds.size).Using(Vector3Comparer));
        }

        private static readonly IEqualityComparer<Vector3> Vector3Comparer =
            new ApproximateVector3Comparer();

        private sealed class ApproximateVector3Comparer :
            IEqualityComparer<Vector3>
        {
            public bool Equals(Vector3 left, Vector3 right)
            {
                return (left - right).sqrMagnitude < 0.000001f;
            }

            public int GetHashCode(Vector3 value)
            {
                return 0;
            }
        }

        private static int CountProBuilderComponents(GameObject root)
        {
            int count = 0;
            foreach (Component component in
                root.GetComponentsInChildren<Component>(true))
            {
                string typeNamespace = component.GetType().Namespace
                    ?? string.Empty;
                if (typeNamespace.StartsWith(
                    "UnityEngine.ProBuilder",
                    StringComparison.Ordinal))
                {
                    count++;
                }
            }
            return count;
        }

        private static Renderer FindRenderer(GameObject root, string name)
        {
            foreach (Renderer renderer in
                root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.name == name)
                {
                    return renderer;
                }
            }
            Assert.Fail($"Renderer is missing: {name}");
            return null;
        }

        private static GameObject LoadPrefab(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            return prefab;
        }

        private static T ReadJson<T>(string path)
        {
            Assert.That(File.Exists(path), Is.True, path);
            return JsonUtility.FromJson<T>(File.ReadAllText(path));
        }

        private static string RepositoryRoot()
        {
            return Directory.GetParent(Application.dataPath).Parent.FullName;
        }
    }
}
