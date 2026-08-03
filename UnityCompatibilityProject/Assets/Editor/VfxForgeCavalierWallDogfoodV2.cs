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
    private const string ShapeV2Root =
        Root + "/Authoring/ShapeV2";
    private const string PrefabV2Path =
        ShapeV2Root + "/CavalierWallShapeV2.prefab";
    private const string DemoV2ScenePath =
        DemoRoot + "/CavalierWallShapeV2Demo.unity";
    private const string EvidenceV2RelativePath =
        "Dogfooding/Evidence/VF-022-cavalier-wall-shape-v2";

    private const float V2HalfWidth = 3.35f;
    private const float V2CurveDepth = 0.52f;
    private const int V2HorizontalSegments = 72;
    private const int V2VerticalSegments = 14;
    private const int V2RailSides = 12;

    [MenuItem(
        "Tools/VFX Forge/Dogfood/Build VF-022 Cavalier Wall Shape V2")]
    public static void BuildShapeV2()
    {
        RefuseExisting(PrefabV2Path);
        EnsureFolder(ShapeV2Root);

        Material surfaceMaterial = CreateMaterial(
            ShapeV2Root + "/ShapeV2Surface.mat",
            "Cavalier Wall V2 Surface",
            0.28f,
            0.30f);
        Material frameMaterial = CreateMaterial(
            ShapeV2Root + "/ShapeV2Frame.mat",
            "Cavalier Wall V2 Frame",
            0.58f,
            0.46f);
        Material anchorMaterial = CreateMaterial(
            ShapeV2Root + "/ShapeV2Anchor.mat",
            "Cavalier Wall V2 Anchors",
            0.76f,
            0.38f);

        Mesh surface = CreateSculptedSurfaceV2();
        Mesh topRail = CreateHorizontalRailV2(
            "Cavalier Wall V2 Top Rail",
            true);
        Mesh bottomRail = CreateHorizontalRailV2(
            "Cavalier Wall V2 Bottom Rail",
            false);
        Mesh leftRail = CreateSideRailV2(
            "Cavalier Wall V2 Left Rail",
            -1f);
        Mesh rightRail = CreateSideRailV2(
            "Cavalier Wall V2 Right Rail",
            1f);
        Mesh anchor = CreateFacetedAnchorV2();

        SaveMeshV2(surface, "CavalierWallSurfaceV2.asset");
        SaveMeshV2(topRail, "CavalierWallTopRailV2.asset");
        SaveMeshV2(bottomRail, "CavalierWallBottomRailV2.asset");
        SaveMeshV2(leftRail, "CavalierWallLeftRailV2.asset");
        SaveMeshV2(rightRail, "CavalierWallRightRailV2.asset");
        SaveMeshV2(anchor, "CavalierWallAnchorV2.asset");

        var root = new GameObject("Cavalier Wall Shape V2");
        try
        {
            Transform pivot = new GameObject("Facing Pivot").transform;
            pivot.SetParent(root.transform, false);

            Transform assembly =
                new GameObject("Cavalier Wall Assembly V2").transform;
            assembly.SetParent(pivot, false);
            assembly.localPosition =
                new Vector3(0f, 0f, ForwardOffset);

            AddMesh(
                assembly,
                "Sculpted Primary Barrier Surface",
                surface,
                surfaceMaterial,
                Vector3.zero);

            Transform frame =
                new GameObject("Continuous Beveled Frame").transform;
            frame.SetParent(assembly, false);
            AddMesh(
                frame,
                "Beveled Top Rail",
                topRail,
                frameMaterial,
                Vector3.zero);
            AddMesh(
                frame,
                "Beveled Bottom Rail",
                bottomRail,
                frameMaterial,
                Vector3.zero);
            AddMesh(
                frame,
                "Tapered Left Rail",
                leftRail,
                frameMaterial,
                Vector3.zero);
            AddMesh(
                frame,
                "Tapered Right Rail",
                rightRail,
                frameMaterial,
                Vector3.zero);

            Transform anchors =
                new GameObject("Four Integrated Anchors").transform;
            anchors.SetParent(assembly, false);
            AddAnchorV2(anchors, anchor, anchorMaterial, -1.72f, true);
            AddAnchorV2(anchors, anchor, anchorMaterial, 1.72f, true);
            AddSideAnchorV2(anchors, anchor, anchorMaterial, -1f);
            AddSideAnchorV2(anchors, anchor, anchorMaterial, 1f);

            CavalierWallFacing facing =
                root.AddComponent<CavalierWallFacing>();
            facing.Configure(null, pivot);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                root,
                PrefabV2Path);
            if (saved == null)
            {
                throw new InvalidOperationException(
                    "Cavalier Wall Shape V2 Prefab could not be saved.");
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
            + anchor.triangles.Length / 3 * 4;
        Debug.Log(
            "[VFXForge VF-022 V2] High-density grayscale shape created. "
            + $"RenderedTriangles={renderedTriangles}, "
            + $"SurfaceGrid={V2HorizontalSegments}x{V2VerticalSegments}, "
            + $"RailSides={V2RailSides}, Anchors=4, ShaderFinish=0.");
    }

    [MenuItem(
        "Tools/VFX Forge/Dogfood/Create VF-022 Cavalier Wall Demo V2")]
    public static void CreateDemoV2()
    {
        RefuseExisting(DemoV2ScenePath);
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabV2Path);
        if (prefab == null)
        {
            throw new InvalidOperationException(
                "Build the VF-022 Cavalier Wall Shape V2 first.");
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
                "Cavalier Wall Shape V2 could not be instantiated.");
        }
        instance.name = "Cavalier Wall Shape V2 (Candidate E)";
        CavalierWallFacing facing =
            instance.GetComponent<CavalierWallFacing>();
        facing.Configure(caster.transform, facing.FacingPivot);

        var controllerObject =
            new GameObject("VF-022 V2 Demo Controller");
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
        EditorSceneManager.SaveScene(scene, DemoV2ScenePath);
        AddSceneToBuildSettings(DemoV2ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            "[VFXForge VF-022 V2] High-density demo created.");
    }

    public static void BuildAllV2Batch()
    {
        BuildShapeV2();
        CreateDemoV2();
        CaptureEvidenceV2Batch();
    }

    public static void CaptureEvidenceV2Batch()
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabV2Path);
        if (prefab == null)
        {
            throw new InvalidOperationException(
                "Cavalier Wall Shape V2 Prefab is missing.");
        }

        string evidenceDirectory = GetRepositoryPath(
            EvidenceV2RelativePath);
        string isolatedPath = Path.Combine(
            evidenceDirectory,
            "isolated-grayscale-v2.png");
        string gameplayPath = Path.Combine(
            evidenceDirectory,
            "gameplay-forward-v2.png");
        string rotatedPath = Path.Combine(
            evidenceDirectory,
            "gameplay-facing-right-v2.png");
        string sheetPath = Path.Combine(
            evidenceDirectory,
            "shape-contact-sheet-v2.png");
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
            "[VFXForge VF-022 V2] Shape evidence captured: "
            + evidenceDirectory);
    }

    public static void CaptureConsoleCountsV2Batch()
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
            EvidenceV2RelativePath);
        Directory.CreateDirectory(evidenceDirectory);
        string reportPath = Path.Combine(
            evidenceDirectory,
            "console-counts-v2.json");
        RefuseExisting(reportPath);
        File.WriteAllText(
            reportPath,
            JsonUtility.ToJson(result, true));
        if (result.errors > 0)
        {
            throw new InvalidOperationException(
                "VF-022 V2 verification found Unity Console errors: "
                + result.errors);
        }
        Debug.Log(
            "[VFXForge VF-022 V2] Console evidence recorded: errors=0, "
            + $"warnings={result.warnings}, logs={result.logs}.");
    }

    private static Mesh CreateSculptedSurfaceV2()
    {
        int columns = V2HorizontalSegments + 1;
        int rows = V2VerticalSegments + 1;
        int layerVertexCount = columns * rows;
        var vertices = new List<Vector3>(layerVertexCount * 2);
        var uv = new List<Vector2>(layerVertexCount * 2);
        const float inset = 0.20f;
        const float halfThickness = 0.09f;

        for (int layer = 0; layer < 2; layer++)
        {
            float thicknessOffset = layer == 0
                ? -halfThickness
                : halfThickness;
            for (int yIndex = 0; yIndex <= V2VerticalSegments; yIndex++)
            {
                float v = yIndex / (float)V2VerticalSegments;
                for (int xIndex = 0;
                    xIndex <= V2HorizontalSegments;
                    xIndex++)
                {
                    float u = xIndex / (float)V2HorizontalSegments;
                    float x = Mathf.Lerp(
                        -V2HalfWidth + inset,
                        V2HalfWidth - inset,
                        u);
                    float bottom = BottomHeightV2(x) + inset;
                    float top = TopHeightV2(x) - inset;
                    float y = Mathf.Lerp(bottom, top, v);
                    float normalized = x / V2HalfWidth;
                    float centerBow = 0.13f
                        * Mathf.Sin(Mathf.PI * v)
                        * (1f - normalized * normalized);
                    float z = CurveZV2(x) + centerBow
                        + thicknessOffset;
                    vertices.Add(new Vector3(x, y, z));
                    uv.Add(new Vector2(u, v));
                }
            }
        }

        var triangles = new List<int>();
        AddGridLayerV2(
            triangles,
            0,
            columns,
            false);
        AddGridLayerV2(
            triangles,
            layerVertexCount,
            columns,
            true);

        for (int xIndex = 0;
            xIndex < V2HorizontalSegments;
            xIndex++)
        {
            AddSurfaceBoundaryQuadV2(
                triangles,
                xIndex,
                xIndex + 1,
                layerVertexCount);
            int top = V2VerticalSegments * columns + xIndex;
            AddSurfaceBoundaryQuadV2(
                triangles,
                top + 1,
                top,
                layerVertexCount);
        }
        for (int yIndex = 0;
            yIndex < V2VerticalSegments;
            yIndex++)
        {
            int left = yIndex * columns;
            int nextLeft = (yIndex + 1) * columns;
            AddSurfaceBoundaryQuadV2(
                triangles,
                nextLeft,
                left,
                layerVertexCount);
            int right = left + V2HorizontalSegments;
            int nextRight = nextLeft + V2HorizontalSegments;
            AddSurfaceBoundaryQuadV2(
                triangles,
                right,
                nextRight,
                layerVertexCount);
        }
        return BuildMeshV2(
            "Cavalier Wall V2 Sculpted Surface",
            vertices,
            triangles,
            uv);
    }

    private static void AddGridLayerV2(
        List<int> triangles,
        int offset,
        int columns,
        bool reverse)
    {
        for (int yIndex = 0;
            yIndex < V2VerticalSegments;
            yIndex++)
        {
            for (int xIndex = 0;
                xIndex < V2HorizontalSegments;
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

    private static void AddSurfaceBoundaryQuadV2(
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

    private static Mesh CreateHorizontalRailV2(
        string name,
        bool top)
    {
        var path = new List<Vector3>();
        for (int index = 0;
            index <= V2HorizontalSegments;
            index++)
        {
            float ratio = index / (float)V2HorizontalSegments;
            float x = Mathf.Lerp(-V2HalfWidth, V2HalfWidth, ratio);
            float y = top ? TopHeightV2(x) : BottomHeightV2(x);
            path.Add(new Vector3(x, y, CurveZV2(x)));
        }
        return CreateSweepMeshV2(name, path, 0.17f, 0.23f);
    }

    private static Mesh CreateSideRailV2(
        string name,
        float side)
    {
        var path = new List<Vector3>();
        float x = side * V2HalfWidth;
        for (int index = 0;
            index <= V2VerticalSegments;
            index++)
        {
            float ratio = index / (float)V2VerticalSegments;
            float eased = Mathf.SmoothStep(0f, 1f, ratio);
            float y = Mathf.Lerp(
                BottomHeightV2(x),
                TopHeightV2(x),
                eased);
            float rootFlare = (1f - ratio) * 0.12f;
            path.Add(new Vector3(
                x + side * rootFlare,
                y,
                CurveZV2(x)));
        }
        return CreateSweepMeshV2(name, path, 0.20f, 0.25f);
    }

    private static Mesh CreateSweepMeshV2(
        string name,
        IReadOnlyList<Vector3> path,
        float radiusA,
        float radiusB)
    {
        var vertices = new List<Vector3>(path.Count * V2RailSides);
        var uv = new List<Vector2>(path.Count * V2RailSides);
        var triangles = new List<int>();
        for (int ring = 0; ring < path.Count; ring++)
        {
            Vector3 previous = path[Mathf.Max(0, ring - 1)];
            Vector3 next = path[Mathf.Min(path.Count - 1, ring + 1)];
            Vector3 tangent = (next - previous).normalized;
            Vector3 reference = Mathf.Abs(
                Vector3.Dot(tangent, Vector3.up)) > 0.92f
                ? Vector3.right
                : Vector3.up;
            Vector3 axisA = Vector3.Cross(tangent, reference).normalized;
            Vector3 axisB = Vector3.Cross(tangent, axisA).normalized;
            for (int side = 0; side < V2RailSides; side++)
            {
                Vector2 profile = BeveledRectanglePointV2(
                    side,
                    radiusA,
                    radiusB);
                Vector3 radial = axisA * profile.x
                    + axisB * profile.y;
                vertices.Add(path[ring] + radial);
                uv.Add(new Vector2(
                    ring / (float)(path.Count - 1),
                    side / (float)V2RailSides));
            }
        }
        for (int ring = 0; ring < path.Count - 1; ring++)
        {
            for (int side = 0; side < V2RailSides; side++)
            {
                int nextSide = (side + 1) % V2RailSides;
                int a = ring * V2RailSides + side;
                int b = ring * V2RailSides + nextSide;
                int d = (ring + 1) * V2RailSides + side;
                int c = (ring + 1) * V2RailSides + nextSide;
                AddQuad(triangles, a, b, c, d);
            }
        }
        AddSweepCapV2(triangles, 0, true);
        AddSweepCapV2(
            triangles,
            (path.Count - 1) * V2RailSides,
            false);
        return BuildMeshV2(name, vertices, triangles, uv);
    }

    private static void AddSweepCapV2(
        List<int> triangles,
        int ringStart,
        bool reverse)
    {
        for (int side = 1; side < V2RailSides - 1; side++)
        {
            if (reverse)
            {
                triangles.Add(ringStart);
                triangles.Add(ringStart + side + 1);
                triangles.Add(ringStart + side);
            }
            else
            {
                triangles.Add(ringStart);
                triangles.Add(ringStart + side);
                triangles.Add(ringStart + side + 1);
            }
        }
    }

    private static Mesh CreateFacetedAnchorV2()
    {
        Vector2[] outline =
        {
            new Vector2(-0.38f, -0.46f),
            new Vector2(0.38f, -0.46f),
            new Vector2(0.52f, -0.30f),
            new Vector2(0.52f, 0.30f),
            new Vector2(0.38f, 0.46f),
            new Vector2(-0.38f, 0.46f),
            new Vector2(-0.52f, 0.30f),
            new Vector2(-0.52f, -0.30f)
        };
        float[] depths = { -0.34f, -0.23f, 0.23f, 0.34f };
        float[] scales = { 0.78f, 1f, 1f, 0.78f };
        int sides = outline.Length;
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        var triangles = new List<int>();
        for (int ring = 0; ring < depths.Length; ring++)
        {
            for (int side = 0; side < sides; side++)
            {
                Vector2 point = outline[side] * scales[ring];
                vertices.Add(new Vector3(
                    point.x,
                    point.y,
                    depths[ring]));
                uv.Add(new Vector2(
                    side / (float)sides,
                    ring / (float)(depths.Length - 1)));
            }
        }
        for (int ring = 0; ring < depths.Length - 1; ring++)
        {
            for (int side = 0; side < sides; side++)
            {
                int nextSide = (side + 1) % sides;
                int a = ring * sides + side;
                int b = ring * sides + nextSide;
                int d = (ring + 1) * sides + side;
                int c = (ring + 1) * sides + nextSide;
                AddQuad(triangles, a, b, c, d);
            }
        }
        AddAnchorCapV2(triangles, 0, sides, true);
        AddAnchorCapV2(
            triangles,
            (depths.Length - 1) * sides,
            sides,
            false);
        return BuildMeshV2(
            "Cavalier Wall V2 Integrated Anchor",
            vertices,
            triangles,
            uv);
    }

    private static void AddAnchorCapV2(
        List<int> triangles,
        int ringStart,
        int sides,
        bool reverse)
    {
        for (int side = 1; side < sides - 1; side++)
        {
            if (reverse)
            {
                triangles.Add(ringStart);
                triangles.Add(ringStart + side + 1);
                triangles.Add(ringStart + side);
            }
            else
            {
                triangles.Add(ringStart);
                triangles.Add(ringStart + side);
                triangles.Add(ringStart + side + 1);
            }
        }
    }

    private static void AddAnchorV2(
        Transform parent,
        Mesh mesh,
        Material material,
        float x,
        bool top)
    {
        float y = top ? TopHeightV2(x) : BottomHeightV2(x);
        GameObject anchor = AddMeshObjectV2(
            parent,
            x < 0f ? "Upper Left Anchor" : "Upper Right Anchor",
            mesh,
            material,
            new Vector3(x, y, CurveZV2(x)));
        float slope = TopSlopeDegreesV2(x);
        anchor.transform.localRotation = Quaternion.Euler(0f, 0f, slope);
        anchor.transform.localScale = new Vector3(1.08f, 1.08f, 1.12f);
    }

    private static void AddSideAnchorV2(
        Transform parent,
        Mesh mesh,
        Material material,
        float side)
    {
        float x = side * V2HalfWidth;
        float y = Mathf.Lerp(
            BottomHeightV2(x),
            TopHeightV2(x),
            0.18f);
        GameObject anchor = AddMeshObjectV2(
            parent,
            side < 0f ? "Lower Left Root Anchor" : "Lower Right Root Anchor",
            mesh,
            material,
            new Vector3(x, y, CurveZV2(x)));
        anchor.transform.localScale = new Vector3(1.18f, 1.65f, 1.28f);
    }

    private static Vector2 BeveledRectanglePointV2(
        int index,
        float halfWidth,
        float halfHeight)
    {
        Vector2[] normalized =
        {
            new Vector2(-0.62f, -1f),
            new Vector2(0.62f, -1f),
            new Vector2(0.88f, -0.82f),
            new Vector2(1f, -0.54f),
            new Vector2(1f, 0.54f),
            new Vector2(0.88f, 0.82f),
            new Vector2(0.62f, 1f),
            new Vector2(-0.62f, 1f),
            new Vector2(-0.88f, 0.82f),
            new Vector2(-1f, 0.54f),
            new Vector2(-1f, -0.54f),
            new Vector2(-0.88f, -0.82f)
        };
        Vector2 point = normalized[index % normalized.Length];
        return new Vector2(
            point.x * halfWidth,
            point.y * halfHeight);
    }

    private static GameObject AddMeshObjectV2(
        Transform parent,
        string name,
        Mesh mesh,
        Material material,
        Vector3 localPosition)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localPosition;
        MeshFilter filter = child.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = child.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.On;
        renderer.receiveShadows = true;
        return child;
    }

    private static Mesh BuildMeshV2(
        string name,
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector2> uv)
    {
        var mesh = new Mesh { name = name };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uv);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static float TopHeightV2(float x)
    {
        float normalized = Mathf.Clamp(x / V2HalfWidth, -1f, 1f);
        return 4.22f - 0.72f
            * Mathf.Pow(Mathf.Abs(normalized), 1.65f);
    }

    private static float BottomHeightV2(float x)
    {
        float normalized = Mathf.Clamp(x / V2HalfWidth, -1f, 1f);
        return 0.14f + 0.23f
            * Mathf.Pow(Mathf.Abs(normalized), 1.45f);
    }

    private static float CurveZV2(float x)
    {
        float normalized = Mathf.Clamp(x / V2HalfWidth, -1f, 1f);
        return V2CurveDepth * (1f - normalized * normalized);
    }

    private static float TopSlopeDegreesV2(float x)
    {
        const float sample = 0.02f;
        float previous = TopHeightV2(x - sample);
        float next = TopHeightV2(x + sample);
        return Mathf.Atan2(next - previous, sample * 2f)
            * Mathf.Rad2Deg;
    }

    private static void SaveMeshV2(Mesh mesh, string fileName)
    {
        AssetDatabase.CreateAsset(
            mesh,
            ShapeV2Root + "/" + fileName);
    }

    private static Material LoadDemoMaterial(string fileName)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(
            DemoRoot + "/" + fileName);
        if (material == null)
        {
            throw new InvalidOperationException(
                "VF-022 demo material is missing: " + fileName);
        }
        return material;
    }
}
