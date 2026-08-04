using System;
using System.Collections.Generic;
using System.IO;
using Kubonsang.VfxForge.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using VfxForge.Dogfood;
using Object = UnityEngine.Object;

public static class VfxForgeSymmetricShieldProBuilderBlockout
{
    public const string SourcePrefabPath =
        "Assets/VFXForge/Dogfood/HolyAegisV4/Authoring/ProBuilder/"
        + "SymmetricShieldBlockoutV1.prefab";
    public const string RuntimePrefabPath =
        "Assets/VFXForge/Dogfood/HolyAegisV4/Runtime/"
        + "SymmetricShieldBlockoutV1.prefab";
    public const string RuntimeMeshFolder =
        "Assets/VFXForge/Dogfood/HolyAegisV4/Runtime/Meshes";
    public const string ModelSheetHash =
        "ff61ba884c8a94252ccb4a3eecbdc451668dd612f86936ae82a19609f4a36173";
    public const string AuthoringRevision = "symmetric-shield-blockout-v5";

    private const string SourceFolder =
        "Assets/VFXForge/Dogfood/HolyAegisV4/Authoring/ProBuilder";
    private const string MaterialFolder = SourceFolder + "/Materials";
    private const string ReferenceManifest =
        "Dogfooding/Evidence/VF-022R-model-sheet/mesh-reference-v3.json";
    private const string ModelReview =
        "Dogfooding/Evidence/VF-022R-model-sheet/model-sheet-review-v3.json";
    private const string AuthoringManifest =
        "Dogfooding/Evidence/VF-022R-symmetric-shield-blockout-v1/"
        + "mesh-authoring.json";
    private const string BlockoutReview =
        "Dogfooding/Evidence/VF-022R-symmetric-shield-blockout-v1/"
        + "blockout-review.json";

    private static readonly float[] RowY =
    {
        2.85f, 2.70f, 2.52f, 2.28f,
        1.96f, 1.56f, 1.10f, 0.60f,
        0.10f, -0.45f, -0.95f, -1.42f,
        -1.85f, -2.22f, -2.55f, -2.82f
    };

    private static readonly float[] RowHalfWidth =
    {
        0.06f, 0.48f, 0.98f, 1.52f,
        1.98f, 2.28f, 2.42f, 2.44f,
        2.36f, 2.18f, 1.94f, 1.64f,
        1.30f, 0.92f, 0.49f, 0.05f
    };

    private static readonly float[] ColumnFraction =
    {
        -1f, -0.833333f, -0.666667f, -0.5f,
        -0.333333f, -0.166667f, 0f, 0.166667f,
        0.333333f, 0.5f, 0.666667f, 0.833333f, 1f
    };

    [MenuItem("Tools/VFX Forge/Dogfood/Build VF-022R Symmetric Shield Blockout")]
    public static void Build()
    {
        string repository = RepositoryRoot();
        VfxMeshReferenceManifest reference = ReadJson<VfxMeshReferenceManifest>(
            Path.Combine(repository, ReferenceManifest));
        VfxMeshContractValidation referenceValidation =
            VfxMeshContractValidator.Validate(reference);
        if (!referenceValidation.Valid)
        {
            throw new InvalidOperationException(
                "The approved mesh reference manifest is invalid.");
        }

        string referenceHash = VfxMeshReviewStore.ComputeFileSha256(
            Path.Combine(repository, ReferenceManifest));
        RequireApprovedModelSheet(repository, reference);
        EnsureFolder(SourceFolder);
        EnsureFolder(MaterialFolder);
        EnsureFolder(Path.GetDirectoryName(RuntimePrefabPath).Replace('\\', '/'));

        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(
            SourcePrefabPath);
        if (existing == null)
        {
            CreateSourcePrefab(referenceHash);
        }
        else
        {
            ValidateOwnedSource(existing, referenceHash);
            SymmetricShieldBlockoutMarker marker =
                existing.GetComponent<SymmetricShieldBlockoutMarker>();
            if (marker.AuthoringRevision != AuthoringRevision)
            {
                CreateSourcePrefab(referenceHash);
            }
            else
            {
                EnsureLockedAssemblyTransform();
            }
        }

        VfxMeshRuntimeExportResult export =
            VfxProBuilderRuntimeExporter.Export(
                SourcePrefabPath,
                RuntimePrefabPath,
                RuntimeMeshFolder);
        ValidateRuntimeMeshes(export);
        WriteAuthoringArtifacts(repository, referenceHash, export);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            $"VF-022R blockout built: {export.RenderedTriangles} triangles, "
            + $"source={export.SourceDependencyHash}, "
            + $"runtime={export.RuntimeDependencyHash}");
    }

    private static void CreateSourcePrefab(string referenceHash)
    {
        Material surface = CreateClayMaterial(
            MaterialFolder + "/ShieldSurfaceClay.mat",
            new Color(0.48f, 0.53f, 0.57f, 1f));
        Material frame = CreateClayMaterial(
            MaterialFolder + "/ShieldFrameClay.mat",
            new Color(0.68f, 0.70f, 0.72f, 1f));
        Material guard = CreateClayMaterial(
            MaterialFolder + "/ShieldGuardClay.mat",
            new Color(0.59f, 0.62f, 0.65f, 1f));

        var root = new GameObject("VF-022R Symmetric Shield Blockout V1");
        try
        {
            SymmetricShieldBlockoutMarker marker =
                root.AddComponent<SymmetricShieldBlockoutMarker>();
            marker.Configure(ModelSheetHash, referenceHash, AuthoringRevision);
            CavalierWallFacing facing = root.AddComponent<CavalierWallFacing>();

            var pivot = new GameObject("Facing Pivot");
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 3.05f, 2.65f);
            facing.Configure(root.transform, pivot.transform);

            var assembly = new GameObject("Symmetric Shield Assembly");
            assembly.transform.SetParent(pivot.transform, false);
            assembly.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
            assembly.transform.localScale = Vector3.one;

            CreatePlateCage(assembly.transform, surface);
            CreateRimCage(assembly.transform, frame);
            CreateUpperGuardPair(assembly.transform, guard);
            CreateLowerGuardPair(assembly.transform, guard);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                root,
                SourcePrefabPath);
            if (saved == null)
            {
                throw new InvalidOperationException(
                    "The ProBuilder source Prefab could not be saved.");
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void CreatePlateCage(Transform parent, Material material)
    {
        int rows = RowY.Length;
        int columns = ColumnFraction.Length;
        int surfaceVertexCount = rows * columns;
        var positions = new List<Vector3>(surfaceVertexCount * 2);
        var faces = new List<Face>();

        for (int side = 0; side < 2; side++)
        {
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    float fraction = ColumnFraction[column];
                    float x = RowHalfWidth[row] * fraction;
                    float z = side == 0
                        ? -FrontPlateDepth(fraction)
                        : 0.12f;
                    positions.Add(new Vector3(x, RowY[row], z));
                }
            }
        }

        for (int row = 0; row < rows - 1; row++)
        {
            for (int column = 0; column < columns - 1; column++)
            {
                int a = row * columns + column;
                int b = a + 1;
                int c = (row + 1) * columns + column;
                int d = c + 1;
                faces.Add(NewFace(new[] { a, b, c, b, d, c }, 1));

                int offset = surfaceVertexCount;
                faces.Add(NewFace(new[]
                {
                    offset + a, offset + c, offset + b,
                    offset + b, offset + c, offset + d
                }, 2));
            }
        }

        AddPlateBoundaryFaces(faces, rows, columns, surfaceVertexCount);
        ProBuilderMesh mesh = CreateMesh(
            "Editable Convex Shield Plate",
            parent,
            positions,
            faces,
            material);
        mesh.GetComponent<MeshRenderer>().receiveShadows = true;
    }

    private static void AddPlateBoundaryFaces(
        List<Face> faces,
        int rows,
        int columns,
        int offset)
    {
        for (int row = 0; row < rows - 1; row++)
        {
            int leftTop = row * columns;
            int leftBottom = (row + 1) * columns;
            faces.Add(NewFace(new[]
            {
                leftTop, offset + leftTop, leftBottom,
                leftBottom, offset + leftTop, offset + leftBottom
            }, 0));

            int rightTop = row * columns + columns - 1;
            int rightBottom = (row + 1) * columns + columns - 1;
            faces.Add(NewFace(new[]
            {
                rightTop, rightBottom, offset + rightTop,
                rightBottom, offset + rightBottom, offset + rightTop
            }, 0));
        }

        for (int column = 0; column < columns - 1; column++)
        {
            int topLeft = column;
            int topRight = column + 1;
            faces.Add(NewFace(new[]
            {
                topLeft, topRight, offset + topLeft,
                topRight, offset + topRight, offset + topLeft
            }, 0));

            int bottomLeft = (rows - 1) * columns + column;
            int bottomRight = bottomLeft + 1;
            faces.Add(NewFace(new[]
            {
                bottomLeft, offset + bottomLeft, bottomRight,
                bottomRight, offset + bottomLeft, offset + bottomRight
            }, 0));
        }
    }

    private static float FrontPlateDepth(float fraction)
    {
        float absolute = Mathf.Abs(fraction);
        float bowl = 0.20f * (1f - Mathf.Pow(absolute, 1.45f));
        float keel = 0.08f * Mathf.Clamp01(1f - absolute / 0.17f);
        return 0.08f + bowl + keel;
    }

    private static void CreateRimCage(Transform parent, Material material)
    {
        Vector2[] outline = BuildOutline();
        float[] scales = { 1.04f, 1.04f, 1.01f, 0.97f, 0.91f, 0.91f };
        float[] depth = { 0.14f, -0.06f, -0.23f, -0.30f, -0.18f, 0.10f };
        var positions = new List<Vector3>(outline.Length * scales.Length);
        var faces = new List<Face>();
        for (int ring = 0; ring < scales.Length; ring++)
        {
            foreach (Vector2 point in outline)
            {
                positions.Add(new Vector3(
                    point.x * scales[ring],
                    point.y * scales[ring],
                    depth[ring]));
            }
        }

        for (int band = 0; band < scales.Length; band++)
        {
            int nextBand = (band + 1) % scales.Length;
            for (int index = 0; index < outline.Length; index++)
            {
                int next = (index + 1) % outline.Length;
                int a = band * outline.Length + index;
                int b = band * outline.Length + next;
                int c = nextBand * outline.Length + index;
                int d = nextBand * outline.Length + next;
                faces.Add(NewFace(new[] { a, b, c, b, d, c },
                    band == 2 || band == 3 ? 3 : 0));
            }
        }

        CreateMesh(
            "Editable Continuous Shield Rim",
            parent,
            positions,
            faces,
            material);
    }

    private static Vector2[] BuildOutline()
    {
        var points = new List<Vector2>(RowY.Length * 2);
        for (int index = 0; index < RowY.Length; index++)
        {
            points.Add(new Vector2(-RowHalfWidth[index], RowY[index]));
        }
        for (int index = RowY.Length - 1; index >= 0; index--)
        {
            points.Add(new Vector2(RowHalfWidth[index], RowY[index]));
        }
        return points.ToArray();
    }

    private static void CreateUpperGuardPair(
        Transform parent,
        Material material)
    {
        Vector2[] leftPath =
        {
            new Vector2(-2.02f, 2.02f),
            new Vector2(-2.40f, 1.82f),
            new Vector2(-2.58f, 1.47f),
            new Vector2(-2.48f, 1.12f),
            new Vector2(-2.16f, 0.86f)
        };
        CreateGuardSweep(
            "Editable Upper Left Shoulder Guard",
            parent,
            leftPath,
            0.48f,
            material);
        CreateGuardSweep(
            "Editable Upper Right Shoulder Guard",
            parent,
            MirrorPath(leftPath),
            0.48f,
            material);
    }

    private static void CreateLowerGuardPair(
        Transform parent,
        Material material)
    {
        Vector2[] leftPath =
        {
            new Vector2(-2.25f, -0.52f),
            new Vector2(-2.30f, -0.88f),
            new Vector2(-2.16f, -1.24f),
            new Vector2(-1.91f, -1.55f),
            new Vector2(-1.56f, -1.78f)
        };
        CreateGuardSweep(
            "Editable Lower Left Flank Guard",
            parent,
            leftPath,
            0.46f,
            material);
        CreateGuardSweep(
            "Editable Lower Right Flank Guard",
            parent,
            MirrorPath(leftPath),
            0.46f,
            material);
    }

    private static Vector2[] MirrorPath(Vector2[] source)
    {
        var mirrored = new Vector2[source.Length];
        for (int index = 0; index < source.Length; index++)
        {
            Vector2 point = source[index];
            mirrored[index] = new Vector2(-point.x, point.y);
        }
        return mirrored;
    }

    private static void CreateGuardSweep(
        string name,
        Transform parent,
        Vector2[] path,
        float width,
        Material material)
    {
        Vector2[] section =
        {
            new Vector2(-0.50f, 0.10f),
            new Vector2(-0.50f, -0.14f),
            new Vector2(-0.36f, -0.30f),
            new Vector2(0.36f, -0.30f),
            new Vector2(0.50f, -0.14f),
            new Vector2(0.50f, 0.10f)
        };
        var positions = new List<Vector3>(path.Length * section.Length + 2);
        for (int pathIndex = 0; pathIndex < path.Length; pathIndex++)
        {
            Vector2 tangent;
            if (pathIndex == 0)
            {
                tangent = (path[1] - path[0]).normalized;
            }
            else if (pathIndex == path.Length - 1)
            {
                tangent = (path[pathIndex] - path[pathIndex - 1]).normalized;
            }
            else
            {
                tangent = (path[pathIndex + 1] - path[pathIndex - 1]).normalized;
            }
            Vector2 normal = new Vector2(-tangent.y, tangent.x);
            foreach (Vector2 crossSection in section)
            {
                Vector2 planar = path[pathIndex]
                    + normal * (crossSection.x * width);
                positions.Add(new Vector3(planar.x, planar.y, crossSection.y));
            }
        }

        var faces = new List<Face>();
        int sectionCount = section.Length;
        for (int pathIndex = 0; pathIndex < path.Length - 1; pathIndex++)
        {
            for (int sectionIndex = 0; sectionIndex < sectionCount; sectionIndex++)
            {
                int nextSection = (sectionIndex + 1) % sectionCount;
                int a = pathIndex * sectionCount + sectionIndex;
                int b = pathIndex * sectionCount + nextSection;
                int c = (pathIndex + 1) * sectionCount + sectionIndex;
                int d = (pathIndex + 1) * sectionCount + nextSection;
                faces.Add(NewFace(
                    new[] { a, b, c, b, d, c },
                    sectionIndex == 2 ? 4 : 0));
            }
        }

        int startCenter = positions.Count;
        positions.Add(new Vector3(path[0].x, path[0].y, -0.10f));
        int endCenter = positions.Count;
        positions.Add(new Vector3(
            path[path.Length - 1].x,
            path[path.Length - 1].y,
            -0.10f));
        for (int sectionIndex = 0; sectionIndex < sectionCount; sectionIndex++)
        {
            int nextSection = (sectionIndex + 1) % sectionCount;
            faces.Add(NewFace(
                new[] { nextSection, sectionIndex, startCenter },
                0));
            int endOffset = (path.Length - 1) * sectionCount;
            faces.Add(NewFace(
                new[]
                {
                    endOffset + sectionIndex,
                    endOffset + nextSection,
                    endCenter
                },
                0));
        }

        CreateMesh(name, parent, positions, faces, material);
    }

    private static ProBuilderMesh CreateMesh(
        string name,
        Transform parent,
        IList<Vector3> positions,
        IList<Face> faces,
        Material material)
    {
        ProBuilderMesh mesh = ProBuilderMesh.Create(positions, faces);
        mesh.name = name;
        mesh.transform.SetParent(parent, false);
        mesh.GetComponent<MeshRenderer>().sharedMaterial = material;
        mesh.ToMesh();
        mesh.Refresh();
        return mesh;
    }

    private static Face NewFace(int[] indexes, int smoothingGroup)
    {
        var face = new Face(indexes)
        {
            smoothingGroup = smoothingGroup
        };
        return face;
    }

    private static Material CreateClayMaterial(string path, Color color)
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            return existing;
        }
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Standard");
        if (shader == null)
        {
            throw new InvalidOperationException("No clay-compatible Shader is available.");
        }
        var material = new Material(shader)
        {
            name = Path.GetFileNameWithoutExtension(path),
            color = color
        };
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0f);
        }
        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.28f);
        }
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void RequireApprovedModelSheet(
        string repository,
        VfxMeshReferenceManifest reference)
    {
        VfxMeshReviewRecord submitted = ReadJson<VfxMeshReviewRecord>(
            Path.Combine(repository, ModelReview));
        string inputHash = VfxMeshReviewStore.ComputeCombinedSha256(
            reference.candidateBoardSha256,
            reference.modelSheetSha256);
        VfxMeshReviewRecord expected = VfxMeshReviewStore.CreateExpected(
            "VF-022",
            VfxMeshReviewStage.ModelSheet,
            inputHash);
        if (VfxMeshReviewStore.Evaluate(expected, submitted)
            != VfxMeshReviewStatus.Accepted)
        {
            throw new InvalidOperationException(
                "A matching human model-sheet approval is required.");
        }
    }

    private static void ValidateOwnedSource(
        GameObject source,
        string referenceHash)
    {
        SymmetricShieldBlockoutMarker marker =
            source.GetComponent<SymmetricShieldBlockoutMarker>();
        if (marker == null
            || marker.SchemaVersion != "symmetric-shield-blockout-1.0"
            || marker.ModelSheetSha256 != ModelSheetHash
            || marker.MeshReferenceSha256 != referenceHash)
        {
            throw new IOException(
                $"Refusing to use an unowned source Prefab: {SourcePrefabPath}");
        }
    }

    private static void EnsureLockedAssemblyTransform()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(SourcePrefabPath);
        try
        {
            Transform assembly = root.transform.Find(
                "Facing Pivot/Symmetric Shield Assembly");
            if (assembly == null)
            {
                throw new InvalidOperationException(
                    "The owned ProBuilder source is missing its assembly.");
            }
            bool changed = false;
            if ((assembly.localScale - Vector3.one).sqrMagnitude > 0.000001f)
            {
                assembly.localScale = Vector3.one;
                changed = true;
            }
            Quaternion expectedRotation = Quaternion.Euler(18f, 0f, 0f);
            if (Quaternion.Angle(assembly.localRotation, expectedRotation) > 0.001f)
            {
                assembly.localRotation = expectedRotation;
                changed = true;
            }
            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, SourcePrefabPath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ValidateRuntimeMeshes(VfxMeshRuntimeExportResult export)
    {
        if (export.RenderedTriangles <= 0 || export.RenderedTriangles > 12000)
        {
            throw new InvalidOperationException(
                $"Rendered triangle budget is invalid: {export.RenderedTriangles}");
        }
        foreach (string path in export.MeshAssetPaths)
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            VfxMeshTopologyReport report = VfxMeshTopologyValidator.Evaluate(mesh);
            if (!report.Valid)
            {
                throw new InvalidOperationException(
                    $"Topology validation failed for {path}: "
                    + $"invalidVertices={report.InvalidVertexCount}, "
                    + $"invalidIndices={report.InvalidIndexCount}, "
                    + $"degenerate={report.DegenerateTriangleCount}, "
                    + $"nonManifold={report.NonManifoldEdgeCount}");
            }
        }
    }

    private static void WriteAuthoringArtifacts(
        string repository,
        string referenceHash,
        VfxMeshRuntimeExportResult export)
    {
        var manifest = new VfxMeshAuthoringManifest
        {
            taskId = "VF-022",
            meshReferenceSha256 = referenceHash,
            sourcePrefabPath = SourcePrefabPath,
            sourceDependencyHash = export.SourceDependencyHash,
            runtimePrefabPath = RuntimePrefabPath,
            runtimeDependencyHash = export.RuntimeDependencyHash,
            runtimeMeshFolder = RuntimeMeshFolder,
            renderedTriangles = export.RenderedTriangles,
            maximumRenderedTriangles = 12000,
            materialZones = new[] { "surface", "frame", "anchor" }
        };
        VfxMeshContractValidation validation =
            VfxMeshContractValidator.Validate(manifest);
        if (!validation.Valid)
        {
            throw new InvalidOperationException(
                "Generated mesh-authoring manifest is invalid.");
        }

        string authoringPath = Path.Combine(repository, AuthoringManifest);
        EnsureFileDirectory(authoringPath);
        File.WriteAllText(
            authoringPath,
            JsonUtility.ToJson(manifest, true) + "\n");

        string inputHash = VfxMeshReviewStore.ComputeCombinedSha256(
            export.SourceDependencyHash,
            export.RuntimeDependencyHash,
            ModelSheetHash);
        VfxMeshReviewRecord review = VfxMeshReviewStore.CreateExpected(
            "VF-022",
            VfxMeshReviewStage.Blockout,
            inputHash);
        string reviewPath = Path.Combine(repository, BlockoutReview);
        EnsureFileDirectory(reviewPath);
        File.WriteAllText(
            reviewPath,
            JsonUtility.ToJson(review, true) + "\n");
    }

    private static T ReadJson<T>(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(path);
        }
        return JsonUtility.FromJson<T>(File.ReadAllText(path));
    }

    private static void EnsureFileDirectory(string path)
    {
        string directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static string RepositoryRoot()
    {
        return Directory.GetParent(Application.dataPath).Parent.FullName;
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
