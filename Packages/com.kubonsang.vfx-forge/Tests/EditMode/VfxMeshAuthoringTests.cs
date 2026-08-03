using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace Kubonsang.VfxForge.Editor.Tests
{
    public sealed class VfxMeshAuthoringTests
    {
        private const string TestRoot = "Assets/VFXForgeMeshAuthoringTests";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TestRoot);
            AssetDatabase.Refresh();
        }

        [Test]
        public void ReferenceManifest_ValidatesFourLockedViewsAndLandmarks()
        {
            VfxMeshReferenceManifest manifest = CreateReferenceManifest();

            VfxMeshContractValidation result =
                VfxMeshContractValidator.Validate(manifest);

            Assert.That(result.Valid, Is.True);
        }

        [Test]
        public void ReferenceManifest_RejectsUnsafePathAndCameraMismatch()
        {
            VfxMeshReferenceManifest manifest = CreateReferenceManifest();
            manifest.modelSheetPath = "../outside.png";
            manifest.views[0].projection = "perspective";
            manifest.views[0].fieldOfView = 0f;

            VfxMeshContractValidation result =
                VfxMeshContractValidator.Validate(manifest);

            Assert.That(result.Valid, Is.False);
            Assert.That(
                result.Results.Exists(item =>
                    item.ruleId == "MESH-REF-SHEET-PATH"
                    && item.severity == VfxValidationSeverity.Error),
                Is.True);
            Assert.That(
                result.Results.Exists(item =>
                    item.ruleId == "MESH-REF-CAMERA"
                    && item.severity == VfxValidationSeverity.Error),
                Is.True);
        }

        [Test]
        public void MeshReview_RejectsStaleOrIncompleteAcceptance()
        {
            string hash = new string('a', 64);
            VfxMeshReviewRecord expected = VfxMeshReviewStore.CreateExpected(
                "VF-022",
                VfxMeshReviewStage.ModelSheet,
                hash);
            VfxMeshReviewRecord submitted = CreateAcceptedReview(hash);

            Assert.That(
                VfxMeshReviewStore.Evaluate(expected, submitted),
                Is.EqualTo(VfxMeshReviewStatus.Accepted));

            submitted.inputSha256 = new string('b', 64);
            Assert.That(
                VfxMeshReviewStore.Evaluate(expected, submitted),
                Is.EqualTo(VfxMeshReviewStatus.ReviewStale));

            submitted = CreateAcceptedReview(hash);
            submitted.criteria.connectedAnchors = false;
            Assert.That(
                VfxMeshReviewStore.Evaluate(expected, submitted),
                Is.EqualTo(VfxMeshReviewStatus.ReviewRequired));
        }

        [Test]
        public void TopologyValidator_FindsDegenerateAndNonManifoldGeometry()
        {
            var mesh = new Mesh
            {
                vertices = new[]
                {
                    Vector3.zero,
                    Vector3.right,
                    Vector3.up,
                    Vector3.forward,
                    Vector3.one
                },
                triangles = new[]
                {
                    0, 1, 2,
                    0, 1, 3,
                    0, 1, 4,
                    0, 0, 2
                }
            };

            VfxMeshTopologyReport report =
                VfxMeshTopologyValidator.Evaluate(mesh);

            Assert.That(report.Valid, Is.False);
            Assert.That(report.DegenerateTriangleCount, Is.EqualTo(1));
            Assert.That(report.NonManifoldEdgeCount, Is.EqualTo(1));
            UnityEngine.Object.DestroyImmediate(mesh);
        }

        [Test]
        public void RuntimeExporter_PreservesSourceAndStripsProBuilder()
        {
            EnsureFolder(TestRoot);
            string sourcePath = TestRoot + "/Source.prefab";
            string outputPath = TestRoot + "/Runtime/Runtime.prefab";
            string meshFolder = TestRoot + "/Runtime/Meshes";
            CreateProBuilderSource(sourcePath);
            Hash128 sourceHashBefore =
                AssetDatabase.GetAssetDependencyHash(sourcePath);

            VfxMeshRuntimeExportResult result =
                VfxProBuilderRuntimeExporter.Export(
                    sourcePath,
                    outputPath,
                    meshFolder);

            Assert.That(
                AssetDatabase.GetAssetDependencyHash(sourcePath),
                Is.EqualTo(sourceHashBefore));
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(
                sourcePath);
            GameObject runtime = AssetDatabase.LoadAssetAtPath<GameObject>(
                outputPath);
            Assert.That(
                source.GetComponentsInChildren<ProBuilderMesh>(true),
                Is.Not.Empty);
            Assert.That(
                runtime.GetComponentsInChildren<ProBuilderMesh>(true),
                Is.Empty);
            Assert.That(result.MeshAssetPaths, Has.Length.EqualTo(1));
            Assert.That(result.RenderedTriangles, Is.EqualTo(2));

            VfxMeshRuntimeExportResult repeated =
                VfxProBuilderRuntimeExporter.Export(
                    sourcePath,
                    outputPath,
                    meshFolder);
            Assert.That(
                repeated.RuntimeDependencyHash,
                Is.EqualTo(result.RuntimeDependencyHash));
        }

        private static VfxMeshReferenceManifest CreateReferenceManifest()
        {
            string hash = new string('a', 64);
            return new VfxMeshReferenceManifest
            {
                taskId = "VF-022",
                selectedCandidateId = "candidate_e",
                candidateBoardPath = "Dogfooding/CandidateE.png",
                candidateBoardSha256 = hash,
                modelSheetPath = "Dogfooding/ModelSheet.png",
                modelSheetSha256 = hash,
                views = new[]
                {
                    CreateView("front", "orthographic"),
                    CreateView("top", "orthographic"),
                    CreateView("right_side", "orthographic"),
                    CreateView("gameplay", "perspective")
                },
                landmarks = new[]
                {
                    new VfxMeshLandmark
                    {
                        id = "front_top_left",
                        viewId = "front",
                        partId = "frame",
                        normalizedPosition = new Vector2(0.2f, 0.8f),
                        depthMeters = 0.1f
                    }
                },
                parts = new[]
                {
                    CreatePart("surface"),
                    CreatePart("frame"),
                    CreatePart("anchor_front_left"),
                    CreatePart("anchor_front_right"),
                    CreatePart("anchor_root_left"),
                    CreatePart("anchor_root_right")
                }
            };
        }

        private static VfxMeshReferenceView CreateView(
            string id,
            string projection)
        {
            return new VfxMeshReferenceView
            {
                id = id,
                projection = projection,
                width = 1024,
                height = 1024,
                position = new Vector3(0f, 0f, -10f),
                target = Vector3.zero,
                fieldOfView = projection == "perspective" ? 38f : 0f,
                orthographicSize = projection == "orthographic" ? 4f : 0f,
                normalizedImageRect = new Rect(0f, 0f, 0.5f, 0.5f)
            };
        }

        private static VfxMeshPartContract CreatePart(string id)
        {
            return new VfxMeshPartContract
            {
                id = id,
                role = id,
                materialZone = id == "surface" ? "energy" : "frame",
                connectedTo = id == "surface"
                    ? new[] { "frame" }
                    : Array.Empty<string>()
            };
        }

        private static VfxMeshReviewRecord CreateAcceptedReview(string hash)
        {
            return new VfxMeshReviewRecord
            {
                taskId = "VF-022",
                stage = VfxMeshReviewStage.ModelSheet,
                status = VfxMeshReviewStatus.Accepted,
                inputSha256 = hash,
                reviewer = "Project owner",
                reviewTimeUtc = "2026-08-03T00:00:00Z",
                accepted = true,
                criteria = new VfxMeshReviewCriteria
                {
                    visibleShapeFidelity = true,
                    structuralFrameReadability = true,
                    connectedAnchors = true,
                    depthConsistency = true,
                    gameplayReadability = true
                }
            };
        }

        private static void CreateProBuilderSource(string path)
        {
            ProBuilderMesh mesh = ProBuilderMesh.Create(
                new[]
                {
                    new Vector3(-1f, -1f, 0f),
                    new Vector3(1f, -1f, 0f),
                    new Vector3(1f, 1f, 0f),
                    new Vector3(-1f, 1f, 0f)
                },
                new[] { new Face(new[] { 0, 1, 2, 2, 3, 0 }) });
            mesh.name = "Editable Surface";
            mesh.ToMesh();
            mesh.Refresh();
            PrefabUtility.SaveAsPrefabAsset(mesh.gameObject, path);
            UnityEngine.Object.DestroyImmediate(mesh.gameObject);
            AssetDatabase.SaveAssets();
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
