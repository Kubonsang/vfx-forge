using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using Kubonsang.VfxForge;
using Object = UnityEngine.Object;

namespace Kubonsang.VfxForge.Editor
{
    public sealed class VfxMeshRuntimeExportResult
    {
        public string SourceDependencyHash = string.Empty;
        public string RuntimeDependencyHash = string.Empty;
        public string RuntimePrefabPath = string.Empty;
        public string[] MeshAssetPaths = Array.Empty<string>();
        public int RenderedTriangles;
    }

    public static class VfxProBuilderRuntimeExporter
    {
        public static VfxMeshRuntimeExportResult Export(
            string sourcePrefabPath,
            string runtimePrefabPath,
            string runtimeMeshFolder)
        {
            ValidatePath(sourcePrefabPath, ".prefab");
            ValidatePath(runtimePrefabPath, ".prefab");
            ValidatePath(runtimeMeshFolder, string.Empty);
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(
                sourcePrefabPath);
            if (source == null)
            {
                throw new InvalidOperationException(
                    $"ProBuilder source Prefab is missing: {sourcePrefabPath}");
            }
            if (source.GetComponentsInChildren<ProBuilderMesh>(true).Length == 0)
            {
                throw new InvalidOperationException(
                    "Source Prefab must contain at least one ProBuilderMesh.");
            }

            Hash128 sourceHashBefore =
                AssetDatabase.GetAssetDependencyHash(sourcePrefabPath);
            string sourceGuid = AssetDatabase.AssetPathToGUID(sourcePrefabPath);
            ValidateExistingOutput(
                runtimePrefabPath,
                runtimeMeshFolder,
                sourceGuid);
            EnsureAssetFolder(runtimeMeshFolder);
            EnsureAssetFolder(Path.GetDirectoryName(runtimePrefabPath)
                .Replace('\\', '/'));

            GameObject root = PrefabUtility.LoadPrefabContents(sourcePrefabPath);
            var meshPaths = new List<string>();
            try
            {
                BakeProBuilderMeshes(root, runtimeMeshFolder, meshPaths);
                StripRemainingProBuilderComponents(root);
                VfxMeshRuntimeExportMetadata metadata =
                    root.GetComponent<VfxMeshRuntimeExportMetadata>()
                    ?? root.AddComponent<VfxMeshRuntimeExportMetadata>();
                metadata.schemaVersion = "mesh-runtime-export-1.0";
                metadata.sourcePrefabGuid = sourceGuid;
                metadata.sourceDependencyHash = sourceHashBefore.ToString();
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                    root,
                    runtimePrefabPath);
                if (saved == null)
                {
                    throw new InvalidOperationException(
                        "Runtime Prefab could not be saved.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Hash128 sourceHashAfter =
                AssetDatabase.GetAssetDependencyHash(sourcePrefabPath);
            if (sourceHashBefore != sourceHashAfter)
            {
                throw new InvalidOperationException(
                    "Runtime export modified the ProBuilder source Prefab.");
            }

            GameObject runtime = AssetDatabase.LoadAssetAtPath<GameObject>(
                runtimePrefabPath);
            if (runtime == null)
            {
                throw new InvalidOperationException(
                    "Runtime Prefab could not be reloaded.");
            }
            if (runtime.GetComponentsInChildren<ProBuilderMesh>(true).Length != 0)
            {
                throw new InvalidOperationException(
                    "Runtime Prefab still contains ProBuilder components.");
            }

            return new VfxMeshRuntimeExportResult
            {
                SourceDependencyHash = sourceHashAfter.ToString(),
                RuntimeDependencyHash = AssetDatabase
                    .GetAssetDependencyHash(runtimePrefabPath)
                    .ToString(),
                RuntimePrefabPath = runtimePrefabPath,
                MeshAssetPaths = meshPaths.ToArray(),
                RenderedTriangles = CountRenderedTriangles(runtime)
            };
        }

        private static void BakeProBuilderMeshes(
            GameObject root,
            string meshFolder,
            List<string> meshPaths)
        {
            ProBuilderMesh[] meshes =
                root.GetComponentsInChildren<ProBuilderMesh>(true);
            Array.Sort(
                meshes,
                (left, right) => string.CompareOrdinal(
                    GetHierarchyPath(left.transform),
                    GetHierarchyPath(right.transform)));

            for (int index = 0; index < meshes.Length; index++)
            {
                ProBuilderMesh proBuilderMesh = meshes[index];
                proBuilderMesh.ToMesh();
                proBuilderMesh.Refresh();
                MeshFilter filter = proBuilderMesh.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    throw new InvalidOperationException(
                        $"ProBuilder mesh has no compiled MeshFilter: "
                        + GetHierarchyPath(proBuilderMesh.transform));
                }

                Mesh runtimeMesh = Object.Instantiate(filter.sharedMesh);
                runtimeMesh.name = proBuilderMesh.gameObject.name + " Runtime";
                string fileName = $"{index:00}_"
                    + SanitizeFileName(GetHierarchyPath(proBuilderMesh.transform))
                    + ".asset";
                string meshPath = meshFolder + "/" + fileName;
                Object.DestroyImmediate(proBuilderMesh);
                Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (existing == null)
                {
                    AssetDatabase.CreateAsset(runtimeMesh, meshPath);
                    filter.sharedMesh = runtimeMesh;
                }
                else
                {
                    EditorUtility.CopySerialized(runtimeMesh, existing);
                    Object.DestroyImmediate(runtimeMesh);
                    filter.sharedMesh = existing;
                }
                meshPaths.Add(meshPath);
            }
        }

        private static void StripRemainingProBuilderComponents(GameObject root)
        {
            Component[] components = root.GetComponentsInChildren<Component>(true);
            foreach (Component component in components)
            {
                if (component == null)
                {
                    continue;
                }
                string namespaceName = component.GetType().Namespace ?? string.Empty;
                if (namespaceName.StartsWith(
                    "UnityEngine.ProBuilder",
                    StringComparison.Ordinal))
                {
                    Object.DestroyImmediate(component);
                }
            }
        }

        private static int CountRenderedTriangles(GameObject prefab)
        {
            return prefab.GetComponentsInChildren<MeshFilter>(true)
                .Where(filter => filter.sharedMesh != null)
                .Sum(filter => filter.sharedMesh.triangles.Length / 3);
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var names = new Stack<string>();
            while (transform != null)
            {
                names.Push(transform.name);
                transform = transform.parent;
            }
            return string.Join("/", names);
        }

        private static string SanitizeFileName(string value)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            return new string(value
                .Select(character => invalid.Contains(character)
                    || character == '/'
                    || character == '\\'
                    || char.IsWhiteSpace(character)
                    ? '_'
                    : character)
                .ToArray());
        }

        private static void ValidatePath(string path, string extension)
        {
            if (string.IsNullOrWhiteSpace(path)
                || !path.StartsWith("Assets/", StringComparison.Ordinal)
                || path.Split('/', '\\').Contains("..")
                || (!string.IsNullOrEmpty(extension)
                    && !path.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException($"Unsafe Asset path: {path}");
            }
        }

        private static void ValidateExistingOutput(
            string runtimePrefabPath,
            string meshFolder,
            string sourceGuid)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(
                runtimePrefabPath);
            if (existing == null)
            {
                if (AssetDatabase.IsValidFolder(meshFolder)
                    && AssetDatabase.FindAssets(
                        string.Empty,
                        new[] { meshFolder }).Length > 0)
                {
                    throw new IOException(
                        "Refusing to use a non-empty runtime mesh folder.");
                }
                return;
            }

            VfxMeshRuntimeExportMetadata metadata =
                existing.GetComponent<VfxMeshRuntimeExportMetadata>();
            if (metadata == null
                || metadata.schemaVersion != "mesh-runtime-export-1.0"
                || metadata.sourcePrefabGuid != sourceGuid)
            {
                throw new IOException(
                    $"Refusing to overwrite user Asset: {runtimePrefabPath}");
            }
        }

        private static void EnsureAssetFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder)
                || AssetDatabase.IsValidFolder(folder))
            {
                return;
            }
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }

    public sealed class VfxMeshTopologyReport
    {
        public int VertexCount;
        public int TriangleCount;
        public int InvalidVertexCount;
        public int InvalidIndexCount;
        public int DegenerateTriangleCount;
        public int NonManifoldEdgeCount;
        public bool Valid => InvalidVertexCount == 0
            && InvalidIndexCount == 0
            && DegenerateTriangleCount == 0
            && NonManifoldEdgeCount == 0;
    }

    public static class VfxMeshTopologyValidator
    {
        public static VfxMeshTopologyReport Evaluate(Mesh mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            var report = new VfxMeshTopologyReport
            {
                VertexCount = vertices.Length,
                TriangleCount = triangles.Length / 3
            };
            foreach (Vector3 vertex in vertices)
            {
                if (!IsFinite(vertex.x)
                    || !IsFinite(vertex.y)
                    || !IsFinite(vertex.z))
                {
                    report.InvalidVertexCount++;
                }
            }

            var edgeUse = new Dictionary<ulong, int>();
            for (int index = 0; index + 2 < triangles.Length; index += 3)
            {
                int a = triangles[index];
                int b = triangles[index + 1];
                int c = triangles[index + 2];
                if (!IsIndexValid(a, vertices.Length)
                    || !IsIndexValid(b, vertices.Length)
                    || !IsIndexValid(c, vertices.Length))
                {
                    report.InvalidIndexCount++;
                    continue;
                }
                if (Vector3.Cross(vertices[b] - vertices[a],
                    vertices[c] - vertices[a]).sqrMagnitude <= 0.00000001f)
                {
                    report.DegenerateTriangleCount++;
                    continue;
                }
                CountEdge(edgeUse, a, b);
                CountEdge(edgeUse, b, c);
                CountEdge(edgeUse, c, a);
            }
            report.NonManifoldEdgeCount = edgeUse.Count(pair => pair.Value > 2);
            return report;
        }

        private static void CountEdge(
            Dictionary<ulong, int> edgeUse,
            int first,
            int second)
        {
            uint minimum = (uint)System.Math.Min(first, second);
            uint maximum = (uint)System.Math.Max(first, second);
            ulong key = ((ulong)minimum << 32) | maximum;
            edgeUse.TryGetValue(key, out int count);
            edgeUse[key] = count + 1;
        }

        private static bool IsIndexValid(int value, int length)
        {
            return value >= 0 && value < length;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
