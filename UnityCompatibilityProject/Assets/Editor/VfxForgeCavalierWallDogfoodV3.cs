using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VfxForge.Dogfood;

public static partial class VfxForgeCavalierWallDogfood
{
    private const string ShapeV3Root =
        Root + "/Authoring/ShapeV3";
    private const string PrefabV3Path =
        ShapeV3Root + "/CavalierWallShapeV3.prefab";
    private const string DemoV3ScenePath =
        DemoRoot + "/CavalierWallShapeV3Demo.unity";
    private const string EvidenceV3RelativePath =
        "Dogfooding/Evidence/VF-022-cavalier-wall-shape-v3";

    private const float V3CurveDepth = 0.58f;
    private const int V3HorizontalSegments = 48;
    private const int V3VerticalSegments = 10;
    private const int V3RailSides = 8;

    [MenuItem(
        "Tools/VFX Forge/Dogfood/Build VF-022 Cavalier Wall Shape V3")]
    public static void BuildShapeV3()
    {
        RefuseExisting(PrefabV3Path);
        EnsureFolder(ShapeV3Root);

        Material surfaceMaterial = CreateMaterial(
            ShapeV3Root + "/ShapeV3Surface.mat",
            "Cavalier Wall V3 Surface",
            0.27f,
            0.28f);
        Material frameMaterial = CreateMaterial(
            ShapeV3Root + "/ShapeV3Frame.mat",
            "Cavalier Wall V3 Frame",
            0.57f,
            0.38f);
        Material anchorMaterial = CreateMaterial(
            ShapeV3Root + "/ShapeV3Anchor.mat",
            "Cavalier Wall V3 Anchors",
            0.74f,
            0.32f);

        Mesh surface = CreateTaperedSurfaceV3();
        Mesh topRail = CreateHorizontalRailV3(
            "Cavalier Wall V3 Crown Rail",
            true);
        Mesh bottomRail = CreateHorizontalRailV3(
            "Cavalier Wall V3 Root Rail",
            false);
        Mesh leftRail = CreateSideRailV3(
            "Cavalier Wall V3 Left Rail",
            -1f);
        Mesh rightRail = CreateSideRailV3(
            "Cavalier Wall V3 Right Rail",
            1f);
        Mesh upperAnchor = CreateUpperAnchorV3();
        Mesh rootAnchor = CreateRootAnchorV3();

        SaveMeshV3(surface, "CavalierWallSurfaceV3.asset");
        SaveMeshV3(topRail, "CavalierWallTopRailV3.asset");
        SaveMeshV3(bottomRail, "CavalierWallBottomRailV3.asset");
        SaveMeshV3(leftRail, "CavalierWallLeftRailV3.asset");
        SaveMeshV3(rightRail, "CavalierWallRightRailV3.asset");
        SaveMeshV3(upperAnchor, "CavalierWallUpperAnchorV3.asset");
        SaveMeshV3(rootAnchor, "CavalierWallRootAnchorV3.asset");

        var root = new GameObject("Cavalier Wall Shape V3");
        try
        {
            Transform pivot = new GameObject("Facing Pivot").transform;
            pivot.SetParent(root.transform, false);

            Transform assembly =
                new GameObject("Cavalier Wall Assembly V3").transform;
            assembly.SetParent(pivot, false);
            assembly.localPosition =
                new Vector3(0f, 0f, ForwardOffset);

            AddMesh(
                assembly,
                "Tapered Primary Barrier Surface",
                surface,
                surfaceMaterial,
                Vector3.zero);

            Transform frame =
                new GameObject("Tapered Hard Bevel Frame").transform;
            frame.SetParent(assembly, false);
            AddMesh(
                frame,
                "Weighted Crown Rail",
                topRail,
                frameMaterial,
                Vector3.zero);
            AddMesh(
                frame,
                "Weighted Root Rail",
                bottomRail,
                frameMaterial,
                Vector3.zero);
            AddMesh(
                frame,
                "Flaring Left Rail",
                leftRail,
                frameMaterial,
                Vector3.zero);
            AddMesh(
                frame,
                "Flaring Right Rail",
                rightRail,
                frameMaterial,
                Vector3.zero);

            Transform anchors =
                new GameObject("Four Authored Anchors").transform;
            anchors.SetParent(assembly, false);
            AddUpperAnchorV3(
                anchors,
                upperAnchor,
                anchorMaterial,
                -1.58f);
            AddUpperAnchorV3(
                anchors,
                upperAnchor,
                anchorMaterial,
                1.58f);
            AddRootAnchorV3(
                anchors,
                rootAnchor,
                anchorMaterial,
                -1f);
            AddRootAnchorV3(
                anchors,
                rootAnchor,
                anchorMaterial,
                1f);

            CavalierWallFacing facing =
                root.AddComponent<CavalierWallFacing>();
            facing.Configure(null, pivot);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                root,
                PrefabV3Path);
            if (saved == null)
            {
                throw new InvalidOperationException(
                    "Cavalier Wall Shape V3 Prefab could not be saved.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        int renderedTriangles = surface.triangles.Length / 3
            + topRail.triangles.Length / 3
            + bottomRail.triangles.Length / 3
            + leftRail.triangles.Length / 3
            + rightRail.triangles.Length / 3
            + upperAnchor.triangles.Length / 3 * 2
            + rootAnchor.triangles.Length / 3 * 2;
        Debug.Log(
            "[VFXForge VF-022 V3] Shape-first topology created. "
            + $"RenderedTriangles={renderedTriangles}, "
            + $"SurfaceGrid={V3HorizontalSegments}x{V3VerticalSegments}, "
            + $"RailSides={V3RailSides}, ShaderFinish=0.");
    }

    [MenuItem(
        "Tools/VFX Forge/Dogfood/Create VF-022 Cavalier Wall Demo V3")]
    public static void CreateDemoV3()
    {
        RefuseExisting(DemoV3ScenePath);
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabV3Path);
        if (prefab == null)
        {
            throw new InvalidOperationException(
                "Build the VF-022 Cavalier Wall Shape V3 first.");
        }

        Material lightGround = LoadDemoMaterial("GroundLight.mat");
        Material mediumGround = LoadDemoMaterial("GroundMedium.mat");
        Material darkGround = LoadDemoMaterial("GroundDark.mat");
        Material casterMaterial = LoadDemoMaterial("Caster.mat");
        Material targetMaterial = LoadDemoMaterial("Target.mat");

        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single);
        CreateGroundStrip(
            scene,
            "Light Ground",
            new Vector3(-6f, -0.18f, 3.2f),
            lightGround);
        CreateGroundStrip(
            scene,
            "Medium Ground",
            new Vector3(0f, -0.18f, 3.2f),
            mediumGround);
        CreateGroundStrip(
            scene,
            "Dark Ground",
            new Vector3(6f, -0.18f, 3.2f),
            darkGround);

        GameObject caster = CreateCapsule(
            scene,
            "Caster",
            new Vector3(0f, 0.9f, 0f),
            casterMaterial);
        AddSword(caster.transform, casterMaterial);
        GameObject target = CreateCapsule(
            scene,
            "Target",
            new Vector3(0f, 0.9f, 7f),
            targetMaterial);

        GameObject instance = PrefabUtility.InstantiatePrefab(
            prefab,
            scene) as GameObject;
        if (instance == null)
        {
            throw new InvalidOperationException(
                "Cavalier Wall Shape V3 could not be instantiated.");
        }
        instance.name = "Cavalier Wall Shape V3 (Candidate E)";
        CavalierWallFacing facing =
            instance.GetComponent<CavalierWallFacing>();
        facing.Configure(caster.transform, facing.FacingPivot);

        var controllerObject =
            new GameObject("VF-022 V3 Demo Controller");
        SceneManager.MoveGameObjectToScene(controllerObject, scene);
        CavalierWallShapeDemoController controller =
            controllerObject.AddComponent<
                CavalierWallShapeDemoController>();
        controller.Configure(
            caster.transform,
            target.transform,
            facing);

        CreateCamera(scene, true);
        CreateLight(scene);
        EditorSceneManager.SaveScene(scene, DemoV3ScenePath);
        AddSceneToBuildSettings(DemoV3ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            "[VFXForge VF-022 V3] Shape-first demo created.");
    }

    public static void BuildAllV3Batch()
    {
        BuildShapeV3();
        CreateDemoV3();
        CaptureEvidenceV3Batch();
    }

    public static void CaptureEvidenceV3Batch()
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabV3Path);
        if (prefab == null)
        {
            throw new InvalidOperationException(
                "Cavalier Wall Shape V3 Prefab is missing.");
        }

        string evidenceDirectory = GetRepositoryPath(
            EvidenceV3RelativePath);
        string isolatedPath = Path.Combine(
            evidenceDirectory,
            "isolated-grayscale-v3.png");
        string gameplayPath = Path.Combine(
            evidenceDirectory,
            "gameplay-forward-v3.png");
        string rotatedPath = Path.Combine(
            evidenceDirectory,
            "gameplay-facing-right-v3.png");
        string sheetPath = Path.Combine(
            evidenceDirectory,
            "shape-contact-sheet-v3.png");
        RefuseExisting(
            isolatedPath,
            gameplayPath,
            rotatedPath,
            sheetPath);
        Directory.CreateDirectory(evidenceDirectory);

        byte[] isolated = Capture(prefab, false, Vector3.forward);
        byte[] gameplay = Capture(prefab, true, Vector3.forward);
        Vector3 rotated = Quaternion.Euler(0f, 38f, 0f)
            * Vector3.forward;
        byte[] facingRight = Capture(prefab, true, rotated);
        File.WriteAllBytes(isolatedPath, isolated);
        File.WriteAllBytes(gameplayPath, gameplay);
        File.WriteAllBytes(rotatedPath, facingRight);
        File.WriteAllBytes(
            sheetPath,
            BuildContactSheet(isolated, gameplay, facingRight));
        Debug.Log(
            "[VFXForge VF-022 V3] Shape evidence captured: "
            + evidenceDirectory);
    }

    public static void CaptureConsoleCountsV3Batch()
    {
        Type logEntries = typeof(Editor).Assembly.GetType(
            "UnityEditor.LogEntries");
        MethodInfo getCounts = logEntries?.GetMethod(
            "GetCountsByType",
            BindingFlags.Static | BindingFlags.Public);
        if (getCounts == null)
        {
            throw new InvalidOperationException(
                "Unity Console count API is unavailable.");
        }

        object[] counts = { 0, 0, 0 };
        getCounts.Invoke(null, counts);
        var result = new ConsoleCounts
        {
            errors = (int)counts[0],
            warnings = (int)counts[1],
            logs = (int)counts[2]
        };
        string evidenceDirectory = GetRepositoryPath(
            EvidenceV3RelativePath);
        Directory.CreateDirectory(evidenceDirectory);
        string reportPath = Path.Combine(
            evidenceDirectory,
            "console-counts-v3.json");
        RefuseExisting(reportPath);
        File.WriteAllText(
            reportPath,
            JsonUtility.ToJson(result, true));
        if (result.errors > 0)
        {
            throw new InvalidOperationException(
                "VF-022 V3 verification found Unity Console errors: "
                + result.errors);
        }
        Debug.Log(
            "[VFXForge VF-022 V3] Console evidence recorded: errors=0, "
            + $"warnings={result.warnings}, logs={result.logs}.");
    }

    private static Mesh CreateTaperedSurfaceV3()
    {
        int columns = V3HorizontalSegments + 1;
        int rows = V3VerticalSegments + 1;
        int layerVertexCount = columns * rows;
        var vertices = new List<Vector3>(layerVertexCount * 2);
        var uv = new List<Vector2>(layerVertexCount * 2);
        const float inset = 0.19f;
        const float halfThickness = 0.08f;

        for (int layer = 0; layer < 2; layer++)
        {
            float thicknessOffset = layer == 0
                ? -halfThickness
                : halfThickness;
            for (int yIndex = 0; yIndex <= V3VerticalSegments; yIndex++)
            {
                float v = yIndex / (float)V3VerticalSegments;
                float halfWidth = WidthAtV3(v) - inset;
                for (int xIndex = 0;
                    xIndex <= V3HorizontalSegments;
                    xIndex++)
                {
                    float u = xIndex / (float)V3HorizontalSegments;
                    float normalized = Mathf.Lerp(-1f, 1f, u);
                    float x = normalized * halfWidth;
                    float bottom = BottomHeightV3(normalized) + inset;
                    float top = TopHeightV3(normalized) - inset;
                    float y = Mathf.Lerp(bottom, top, v);
                    float verticalBow = 0.10f
                        * Mathf.Sin(Mathf.PI * v)
                        * (1f - normalized * normalized);
                    float z = CurveZV3(normalized)
                        * (0.92f + 0.08f * Mathf.Sin(Mathf.PI * v))
                        + verticalBow
                        + thicknessOffset;
                    vertices.Add(new Vector3(x, y, z));
                    uv.Add(new Vector2(u, v));
                }
            }
        }

        var triangles = new List<int>();
        AddGridLayerV3(
            triangles,
            0,
            columns,
            false);
        AddGridLayerV3(
            triangles,
            layerVertexCount,
            columns,
            true);
        for (int xIndex = 0;
            xIndex < V3HorizontalSegments;
            xIndex++)
        {
            AddBoundaryQuadV3(
                triangles,
                xIndex,
                xIndex + 1,
                layerVertexCount);
            int top = V3VerticalSegments * columns + xIndex;
            AddBoundaryQuadV3(
                triangles,
                top + 1,
                top,
                layerVertexCount);
        }
        for (int yIndex = 0;
            yIndex < V3VerticalSegments;
            yIndex++)
        {
            int left = yIndex * columns;
            int nextLeft = (yIndex + 1) * columns;
            AddBoundaryQuadV3(
                triangles,
                nextLeft,
                left,
                layerVertexCount);
            int right = left + V3HorizontalSegments;
            int nextRight = nextLeft + V3HorizontalSegments;
            AddBoundaryQuadV3(
                triangles,
                right,
                nextRight,
                layerVertexCount);
        }
        return BuildMeshV2(
            "Cavalier Wall V3 Tapered Surface",
            vertices,
            triangles,
            uv);
    }

    private static void AddGridLayerV3(
        List<int> triangles,
        int offset,
        int columns,
        bool reverse)
    {
        for (int yIndex = 0;
            yIndex < V3VerticalSegments;
            yIndex++)
        {
            for (int xIndex = 0;
                xIndex < V3HorizontalSegments;
                xIndex++)
            {
                int a = offset + yIndex * columns + xIndex;
                int b = a + 1;
                int d = a + columns;
                int c = d + 1;
                if (reverse)
                {
                    AddQuad(triangles, a, d, c, b);
                }
                else
                {
                    AddQuad(triangles, a, b, c, d);
                }
            }
        }
    }

    private static void AddBoundaryQuadV3(
        List<int> triangles,
        int a,
        int b,
        int layerVertexCount)
    {
        AddQuad(
            triangles,
            a,
            b,
            b + layerVertexCount,
            a + layerVertexCount);
    }

    private static Mesh CreateHorizontalRailV3(
        string name,
        bool top)
    {
        var path = new List<Vector3>();
        float halfWidth = WidthAtV3(top ? 1f : 0f);
        for (int index = 0;
            index <= V3HorizontalSegments;
            index++)
        {
            float ratio = index / (float)V3HorizontalSegments;
            float normalized = Mathf.Lerp(-1f, 1f, ratio);
            float x = normalized * halfWidth;
            float y = top
                ? TopHeightV3(normalized)
                : BottomHeightV3(normalized);
            path.Add(new Vector3(x, y, CurveZV3(normalized)));
        }
        return CreateHardEdgeSweepV3(
            name,
            path,
            ratio => HorizontalRailSizeV3(ratio, top));
    }

    private static Mesh CreateSideRailV3(
        string name,
        float side)
    {
        var path = new List<Vector3>();
        for (int index = 0;
            index <= V3VerticalSegments;
            index++)
        {
            float ratio = index / (float)V3VerticalSegments;
            float halfWidth = WidthAtV3(ratio);
            float bottom = BottomHeightV3(side);
            float top = TopHeightV3(side);
            float y = Mathf.Lerp(bottom, top, ratio);
            float x = side * halfWidth;
            float z = CurveZV3(side)
                + 0.04f * Mathf.Sin(Mathf.PI * ratio);
            path.Add(new Vector3(x, y, z));
        }
        return CreateHardEdgeSweepV3(
            name,
            path,
            ratio => new Vector2(
                Mathf.Lerp(0.22f, 0.15f, ratio),
                Mathf.Lerp(0.27f, 0.20f, ratio)));
    }

    private static Vector2 HorizontalRailSizeV3(
        float ratio,
        bool top)
    {
        float edgeWeight = Mathf.Abs(ratio - 0.5f) * 2f;
        if (top)
        {
            return new Vector2(
                Mathf.Lerp(0.15f, 0.19f, edgeWeight),
                Mathf.Lerp(0.20f, 0.24f, edgeWeight));
        }
        return new Vector2(
            Mathf.Lerp(0.12f, 0.21f, edgeWeight),
            Mathf.Lerp(0.17f, 0.25f, edgeWeight));
    }

    private static Mesh CreateHardEdgeSweepV3(
        string name,
        IReadOnlyList<Vector3> path,
        Func<float, Vector2> sizeAtRatio)
    {
        Vector2[] profile = RailProfileV3();
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        var triangles = new List<int>();
        for (int face = 0; face < profile.Length; face++)
        {
            int nextFace = (face + 1) % profile.Length;
            int faceStart = vertices.Count;
            for (int ring = 0; ring < path.Count; ring++)
            {
                float ratio = ring / (float)(path.Count - 1);
                GetSweepFrameV3(
                    path,
                    ring,
                    out Vector3 axisA,
                    out Vector3 axisB);
                Vector2 size = sizeAtRatio(ratio);
                vertices.Add(
                    path[ring]
                    + axisA * (profile[face].x * size.x)
                    + axisB * (profile[face].y * size.y));
                vertices.Add(
                    path[ring]
                    + axisA * (profile[nextFace].x * size.x)
                    + axisB * (profile[nextFace].y * size.y));
                uv.Add(new Vector2(ratio, 0f));
                uv.Add(new Vector2(ratio, 1f));
            }
            for (int ring = 0; ring < path.Count - 1; ring++)
            {
                int a = faceStart + ring * 2;
                int b = a + 1;
                int d = a + 2;
                int c = a + 3;
                AddQuad(triangles, a, b, c, d);
            }
        }
        AddSweepCapV3(
            vertices,
            uv,
            triangles,
            path,
            profile,
            sizeAtRatio,
            0,
            true);
        AddSweepCapV3(
            vertices,
            uv,
            triangles,
            path,
            profile,
            sizeAtRatio,
            path.Count - 1,
            false);
        return BuildMeshV2(name, vertices, triangles, uv);
    }

    private static void GetSweepFrameV3(
        IReadOnlyList<Vector3> path,
        int ring,
        out Vector3 axisA,
        out Vector3 axisB)
    {
        Vector3 previous = path[Mathf.Max(0, ring - 1)];
        Vector3 next = path[Mathf.Min(path.Count - 1, ring + 1)];
        Vector3 tangent = (next - previous).normalized;
        Vector3 reference = Mathf.Abs(
            Vector3.Dot(tangent, Vector3.up)) > 0.92f
            ? Vector3.right
            : Vector3.up;
        axisA = Vector3.Cross(tangent, reference).normalized;
        axisB = Vector3.Cross(tangent, axisA).normalized;
    }

    private static void AddSweepCapV3(
        List<Vector3> vertices,
        List<Vector2> uv,
        List<int> triangles,
        IReadOnlyList<Vector3> path,
        IReadOnlyList<Vector2> profile,
        Func<float, Vector2> sizeAtRatio,
        int ring,
        bool reverse)
    {
        float ratio = ring / (float)(path.Count - 1);
        GetSweepFrameV3(
            path,
            ring,
            out Vector3 axisA,
            out Vector3 axisB);
        Vector2 size = sizeAtRatio(ratio);
        int center = vertices.Count;
        vertices.Add(path[ring]);
        uv.Add(new Vector2(0.5f, 0.5f));
        int first = vertices.Count;
        for (int index = 0; index < profile.Count; index++)
        {
            Vector2 point = profile[index];
            vertices.Add(
                path[ring]
                + axisA * (point.x * size.x)
                + axisB * (point.y * size.y));
            uv.Add(point * 0.5f + Vector2.one * 0.5f);
        }
        for (int index = 0; index < profile.Count; index++)
        {
            int next = (index + 1) % profile.Count;
            triangles.Add(center);
            triangles.Add(first + (reverse ? next : index));
            triangles.Add(first + (reverse ? index : next));
        }
    }

    private static Vector2[] RailProfileV3()
    {
        return new[]
        {
            new Vector2(-0.62f, -1f),
            new Vector2(0.62f, -1f),
            new Vector2(1f, -0.62f),
            new Vector2(1f, 0.62f),
            new Vector2(0.62f, 1f),
            new Vector2(-0.62f, 1f),
            new Vector2(-1f, 0.62f),
            new Vector2(-1f, -0.62f)
        };
    }

    private static Mesh CreateUpperAnchorV3()
    {
        Vector2[] outline =
        {
            new Vector2(-0.52f, -0.34f),
            new Vector2(-0.18f, -0.50f),
            new Vector2(0.34f, -0.43f),
            new Vector2(0.58f, -0.12f),
            new Vector2(0.43f, 0.26f),
            new Vector2(0.12f, 0.49f),
            new Vector2(-0.36f, 0.42f),
            new Vector2(-0.58f, 0.10f)
        };
        return CreateBeveledPrismV3(
            "Cavalier Wall V3 Upper Keystone",
            outline,
            new[] { -0.34f, -0.22f, 0.22f, 0.34f },
            new[] { 0.76f, 1f, 1f, 0.76f });
    }

    private static Mesh CreateRootAnchorV3()
    {
        Vector2[] outline =
        {
            new Vector2(-0.58f, -0.58f),
            new Vector2(0.58f, -0.58f),
            new Vector2(0.72f, -0.30f),
            new Vector2(0.61f, 0.12f),
            new Vector2(0.38f, 0.50f),
            new Vector2(0f, 0.64f),
            new Vector2(-0.38f, 0.50f),
            new Vector2(-0.61f, 0.12f),
            new Vector2(-0.72f, -0.30f),
            new Vector2(-0.66f, -0.48f)
        };
        return CreateBeveledPrismV3(
            "Cavalier Wall V3 Root Bastion",
            outline,
            new[] { -0.40f, -0.29f, 0f, 0.29f, 0.40f },
            new[] { 0.72f, 1f, 1.04f, 1f, 0.72f });
    }

    private static Mesh CreateBeveledPrismV3(
        string name,
        IReadOnlyList<Vector2> outline,
        IReadOnlyList<float> depths,
        IReadOnlyList<float> scales)
    {
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        var triangles = new List<int>();
        for (int ring = 0; ring < depths.Count; ring++)
        {
            for (int side = 0; side < outline.Count; side++)
            {
                Vector2 point = outline[side] * scales[ring];
                vertices.Add(new Vector3(
                    point.x,
                    point.y,
                    depths[ring]));
                uv.Add(new Vector2(
                    side / (float)outline.Count,
                    ring / (float)(depths.Count - 1)));
            }
        }
        for (int ring = 0; ring < depths.Count - 1; ring++)
        {
            for (int side = 0; side < outline.Count; side++)
            {
                int nextSide = (side + 1) % outline.Count;
                int a = ring * outline.Count + side;
                int b = ring * outline.Count + nextSide;
                int d = (ring + 1) * outline.Count + side;
                int c = (ring + 1) * outline.Count + nextSide;
                AddQuad(triangles, a, b, c, d);
            }
        }
        AddAnchorCapV2(
            triangles,
            0,
            outline.Count,
            true);
        AddAnchorCapV2(
            triangles,
            (depths.Count - 1) * outline.Count,
            outline.Count,
            false);
        return BuildMeshV2(name, vertices, triangles, uv);
    }

    private static void AddUpperAnchorV3(
        Transform parent,
        Mesh mesh,
        Material material,
        float x)
    {
        float normalized = x / WidthAtV3(1f);
        GameObject anchor = AddMeshObjectV2(
            parent,
            x < 0f ? "Left Crown Keystone" : "Right Crown Keystone",
            mesh,
            material,
            new Vector3(
                x,
                TopHeightV3(normalized),
                CurveZV3(normalized)));
        anchor.transform.localRotation = Quaternion.Euler(
            0f,
            0f,
            TopSlopeDegreesV3(normalized));
        anchor.transform.localScale = new Vector3(1.05f, 1.08f, 1.08f);
    }

    private static void AddRootAnchorV3(
        Transform parent,
        Mesh mesh,
        Material material,
        float side)
    {
        float x = side * WidthAtV3(0.12f);
        float y = Mathf.Lerp(
            BottomHeightV3(side),
            TopHeightV3(side),
            0.15f);
        GameObject anchor = AddMeshObjectV2(
            parent,
            side < 0f ? "Left Root Bastion" : "Right Root Bastion",
            mesh,
            material,
            new Vector3(x, y, CurveZV3(side)));
        anchor.transform.localScale = new Vector3(1.0f, 1.28f, 1.18f);
    }

    private static float WidthAtV3(float verticalRatio)
    {
        return 3.05f
            + 0.31f * Mathf.Sin(Mathf.PI * verticalRatio)
            + 0.04f * Mathf.Sin(Mathf.PI * 2f * verticalRatio);
    }

    private static float TopHeightV3(float normalized)
    {
        return 4.20f
            - 0.70f * Mathf.Pow(Mathf.Abs(normalized), 1.65f)
            + 0.05f * normalized;
    }

    private static float BottomHeightV3(float normalized)
    {
        return 0.14f
            + 0.27f * Mathf.Pow(Mathf.Abs(normalized), 1.45f)
            - 0.025f * normalized;
    }

    private static float CurveZV3(float normalized)
    {
        return V3CurveDepth
            * (1f - normalized * normalized);
    }

    private static float TopSlopeDegreesV3(float normalized)
    {
        const float sample = 0.01f;
        float previous = TopHeightV3(normalized - sample);
        float next = TopHeightV3(normalized + sample);
        float xDistance = WidthAtV3(1f) * sample * 2f;
        return Mathf.Atan2(next - previous, xDistance)
            * Mathf.Rad2Deg;
    }

    private static void SaveMeshV3(Mesh mesh, string fileName)
    {
        AssetDatabase.CreateAsset(
            mesh,
            ShapeV3Root + "/" + fileName);
    }
}
