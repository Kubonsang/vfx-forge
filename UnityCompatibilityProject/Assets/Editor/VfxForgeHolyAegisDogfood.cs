using System;
using System.Collections.Generic;
using System.IO;
using Kubonsang.VfxForge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VfxForgeHolyAegisDogfood
{
    private const string Root =
        "Assets/VFXForge/Dogfood/HolyAegisV3";
    private const string AuthoringRoot =
        Root + "/Authoring";
    private const string SilhouetteRoot =
        AuthoringRoot + "/Silhouette";
    private const string PrefabPath =
        SilhouetteRoot + "/HolyAegisV3Silhouette.prefab";
    private const string ContextPath =
        "Assets/VFXForge/Dogfood/ReviewContexts/"
        + "TopDownThreeGrounds.prefab";
    private const float Radius = 2.6f;
    private const float TiltDegrees = -35f;

    [MenuItem(
        "Tools/VFX Forge/Dogfood/Build VF-019 Holy Aegis Silhouette")]
    public static void BuildSilhouette()
    {
        RefuseExisting(PrefabPath);
        EnsureFolder(SilhouetteRoot);

        Material plateMaterial = CreateMaterial(
            SilhouetteRoot + "/SilhouettePlate.mat",
            new Color(0.34f, 0.34f, 0.34f, 1f));
        Material rimMaterial = CreateMaterial(
            SilhouetteRoot + "/SilhouetteRim.mat",
            new Color(0.68f, 0.68f, 0.68f, 1f));
        Material crestMaterial = CreateMaterial(
            SilhouetteRoot + "/SilhouetteCrest.mat",
            new Color(0.16f, 0.16f, 0.16f, 1f));
        Material crestAccentMaterial = CreateMaterial(
            SilhouetteRoot + "/SilhouetteCrestAccent.mat",
            new Color(0.84f, 0.84f, 0.84f, 1f));
        Material ornamentMaterial = CreateMaterial(
            SilhouetteRoot + "/SilhouetteOrnament.mat",
            new Color(0.55f, 0.55f, 0.55f, 1f));

        Mesh plate = CreateDiscMesh(
            "Holy Aegis Circular Plate",
            Radius,
            72);
        Mesh rim = CreateRingMesh(
            "Holy Aegis Thick Rim",
            Radius,
            2.34f,
            72);
        Mesh crestBacking = CreatePolygonMesh(
            "Knight Crest Backing",
            new[]
            {
                new Vector2(-0.86f, 0.70f),
                new Vector2(0f, 1.28f),
                new Vector2(0.86f, 0.70f),
                new Vector2(0.72f, -0.58f),
                new Vector2(0f, -1.28f),
                new Vector2(-0.72f, -0.58f)
            });
        Mesh crestSword = CreateCompositeMesh(
            "Knight Crest Sword",
            new[]
            {
                new[]
                {
                    new Vector2(-0.13f, -0.83f),
                    new Vector2(0.13f, -0.83f),
                    new Vector2(0.13f, 0.87f),
                    new Vector2(0f, 1.12f),
                    new Vector2(-0.13f, 0.87f)
                },
                new[]
                {
                    new Vector2(-0.62f, 0.22f),
                    new Vector2(0.62f, 0.22f),
                    new Vector2(0.48f, 0.43f),
                    new Vector2(-0.48f, 0.43f)
                },
                new[]
                {
                    new Vector2(-0.25f, -0.82f),
                    new Vector2(0f, -1.10f),
                    new Vector2(0.25f, -0.82f)
                }
            });

        Vector2[] leftPoints =
        {
            new Vector2(-4.05f, 0f),
            new Vector2(-3.25f, -1.08f),
            new Vector2(-2.18f, -0.72f),
            new Vector2(-2.18f, 0.72f),
            new Vector2(-3.25f, 1.08f)
        };
        Mesh left = CreatePolygonMesh(
            "Left Connected Ornament",
            leftPoints);
        Mesh right = CreatePolygonMesh(
            "Right Connected Ornament",
            MirrorPolygon(leftPoints));
        Mesh front = CreatePolygonMesh(
            "Front Connected Ornament",
            new[]
            {
                new Vector2(-0.96f, 2.18f),
                new Vector2(0.96f, 2.18f),
                new Vector2(1.28f, 3.00f),
                new Vector2(0f, 4.15f),
                new Vector2(-1.28f, 3.00f)
            });
        Mesh rear = CreatePolygonMesh(
            "Rear Connected Ornament",
            new[]
            {
                new Vector2(-1.06f, -2.18f),
                new Vector2(-1.26f, -3.05f),
                new Vector2(0f, -3.82f),
                new Vector2(1.26f, -3.05f),
                new Vector2(1.06f, -2.18f)
            });

        SaveMesh(plate, "CircularPlate.asset");
        SaveMesh(rim, "ThickRim.asset");
        SaveMesh(crestBacking, "KnightCrestBacking.asset");
        SaveMesh(crestSword, "KnightCrestSword.asset");
        SaveMesh(left, "LeftOrnament.asset");
        SaveMesh(right, "RightOrnament.asset");
        SaveMesh(front, "FrontOrnament.asset");
        SaveMesh(rear, "RearOrnament.asset");

        var root = new GameObject(
            "Holy Aegis Shield V3 Silhouette");
        try
        {
            var assembly =
                new GameObject("Shield Assembly").transform;
            assembly.SetParent(root.transform, false);
            assembly.localPosition =
                new Vector3(0f, 2.28f, 0f);
            assembly.localRotation =
                Quaternion.Euler(TiltDegrees, 0f, 0f);

            AddMesh(
                assembly,
                "Circular Main Plate",
                plate,
                plateMaterial,
                0f);
            AddMesh(
                assembly,
                "Thick Connected Rim",
                rim,
                rimMaterial,
                0.015f);

            var crest =
                new GameObject("Central Knight Crest").transform;
            crest.SetParent(assembly, false);
            AddMesh(
                crest,
                "Crest Backing",
                crestBacking,
                crestMaterial,
                0.025f);
            AddMesh(
                crest,
                "Crest Sword",
                crestSword,
                crestAccentMaterial,
                0.040f);

            var ornaments =
                new GameObject("Four Rim Ornaments").transform;
            ornaments.SetParent(assembly, false);
            AddMesh(
                ornaments,
                "Left Connected Ornament",
                left,
                ornamentMaterial,
                0.020f);
            AddMesh(
                ornaments,
                "Right Connected Ornament",
                right,
                ornamentMaterial,
                0.020f);
            AddMesh(
                ornaments,
                "Front Connected Ornament",
                front,
                ornamentMaterial,
                0.020f);
            AddMesh(
                ornaments,
                "Rear Connected Ornament",
                rear,
                ornamentMaterial,
                0.020f);

            GameObject prefab =
                PrefabUtility.SaveAsPrefabAsset(
                    root,
                    PrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    "Holy Aegis silhouette Prefab could not be saved.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            "[VFXForge VF-019] Grayscale silhouette created. "
            + $"Radius={Radius:F1}m, Tilt={Mathf.Abs(TiltDegrees):F0}deg, "
            + "Ornaments=4, ParticleSystems=0, Lights=0.");
    }

    public static void BuildSilhouetteBatch()
    {
        BuildSilhouette();
    }

    public static void RefineSilhouetteBatch()
    {
        ReplaceMesh(
            SilhouetteRoot + "/LeftOrnament.asset",
            CreatePolygonMesh(
                "Left Connected Wing Ornament",
                LeftWingPoints()));
        ReplaceMesh(
            SilhouetteRoot + "/RightOrnament.asset",
            CreatePolygonMesh(
                "Right Connected Wing Ornament",
                MirrorPolygon(LeftWingPoints())));
        ReplaceMesh(
            SilhouetteRoot + "/FrontOrnament.asset",
            CreatePolygonMesh(
                "Front Connected Crown Ornament",
                FrontCrownPoints()));
        ReplaceMesh(
            SilhouetteRoot + "/RearOrnament.asset",
            CreatePolygonMesh(
                "Rear Connected Keel Ornament",
                RearKeelPoints()));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            "[VFXForge VF-019] Silhouette ornaments refined. "
            + "Changes=connected wing outline, crown outline, rear keel outline.");
    }

    public static void CaptureSilhouetteEvidenceBatch()
    {
        CaptureSilhouetteEvidence(
            "Dogfooding/Evidence/VF-019-silhouette");
    }

    public static void CaptureSilhouetteEvidenceV2Batch()
    {
        CaptureSilhouetteEvidence(
            "Dogfooding/Evidence/VF-019-silhouette-v2");
    }

    private static void CaptureSilhouetteEvidence(
        string evidenceRelativePath)
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabPath);
        GameObject contextPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                ContextPath);
        if (prefab == null || contextPrefab == null)
        {
            throw new InvalidOperationException(
                "Holy Aegis silhouette or default Review Context is missing.");
        }

        string evidenceDirectory =
            GetRepositoryPath(
                evidenceRelativePath);
        string isolatedPath =
            Path.Combine(evidenceDirectory, "isolated-top.png");
        string contextPath =
            Path.Combine(evidenceDirectory, "gameplay-top.png");
        string sheetPath =
            Path.Combine(evidenceDirectory, "silhouette-contact-sheet.png");
        RefuseExisting(isolatedPath, contextPath, sheetPath);
        Directory.CreateDirectory(evidenceDirectory);

        byte[] isolated = CaptureIsolated(prefab);
        byte[] gameplay =
            CaptureInContext(prefab, contextPrefab);
        File.WriteAllBytes(isolatedPath, isolated);
        File.WriteAllBytes(contextPath, gameplay);
        File.WriteAllBytes(
            sheetPath,
            BuildContactSheet(isolated, gameplay));
        Debug.Log(
            "[VFXForge VF-019] Silhouette evidence captured: "
            + evidenceDirectory);
    }

    private static byte[] CaptureIsolated(
        GameObject prefab)
    {
        Scene scene =
            EditorSceneManager.NewPreviewScene();
        try
        {
            PrefabUtility.InstantiatePrefab(prefab, scene);
            var cameraObject =
                new GameObject("Silhouette Top Camera");
            SceneManager.MoveGameObjectToScene(
                cameraObject,
                scene);
            Camera camera =
                cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.cameraType = CameraType.Preview;
            camera.scene = scene;
            camera.orthographic = true;
            camera.orthographicSize = 4.65f;
            camera.aspect = 16f / 9f;
            camera.clearFlags =
                CameraClearFlags.SolidColor;
            camera.backgroundColor =
                new Color(0.04f, 0.04f, 0.04f, 1f);
            camera.transform.position =
                new Vector3(0f, 12f, 0f);
            camera.transform.rotation =
                Quaternion.LookRotation(
                    Vector3.down,
                    Vector3.forward);
            return Render(camera, 1280, 720);
        }
        finally
        {
            if (scene.IsValid()
                && EditorSceneManager.IsPreviewScene(scene))
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }

    private static byte[] CaptureInContext(
        GameObject prefab,
        GameObject contextPrefab)
    {
        Scene scene =
            EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject contextInstance =
                PrefabUtility.InstantiatePrefab(
                    contextPrefab,
                    scene) as GameObject;
            VfxReviewContext context =
                contextInstance.GetComponent<VfxReviewContext>();
            GameObject shield =
                PrefabUtility.InstantiatePrefab(
                    prefab,
                    scene) as GameObject;
            shield.transform.SetParent(
                context.effectAnchor,
                false);
            Camera camera = context.reviewCamera;
            camera.enabled = false;
            camera.cameraType = CameraType.Preview;
            camera.scene = scene;
            camera.aspect = 16f / 9f;
            return Render(camera, 1280, 720);
        }
        finally
        {
            if (scene.IsValid()
                && EditorSceneManager.IsPreviewScene(scene))
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }

    private static byte[] Render(
        Camera camera,
        int width,
        int height)
    {
        RenderTexture previousActive =
            RenderTexture.active;
        RenderTexture previousTarget =
            camera.targetTexture;
        RenderTexture renderTexture = null;
        Texture2D texture = null;
        try
        {
            renderTexture =
                RenderTexture.GetTemporary(
                    width,
                    height,
                    24,
                    RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.sRGB);
            texture =
                new Texture2D(
                    width,
                    height,
                    TextureFormat.RGBA32,
                    false);
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;
            texture.ReadPixels(
                new Rect(0f, 0f, width, height),
                0,
                0,
                false);
            texture.Apply(false, false);
            return texture.EncodeToPNG();
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            if (texture != null)
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
            if (renderTexture != null)
            {
                RenderTexture.ReleaseTemporary(
                    renderTexture);
            }
        }
    }

    private static byte[] BuildContactSheet(
        byte[] isolated,
        byte[] gameplay)
    {
        var left =
            new Texture2D(2, 2, TextureFormat.RGBA32, false);
        var right =
            new Texture2D(2, 2, TextureFormat.RGBA32, false);
        var sheet =
            new Texture2D(
                2564,
                720,
                TextureFormat.RGBA32,
                false);
        try
        {
            left.LoadImage(isolated, false);
            right.LoadImage(gameplay, false);
            var background =
                new Color32[2564 * 720];
            for (int index = 0;
                index < background.Length;
                index++)
            {
                background[index] =
                    new Color32(12, 12, 12, 255);
            }
            sheet.SetPixels32(background);
            sheet.SetPixels32(
                0,
                0,
                1280,
                720,
                left.GetPixels32());
            sheet.SetPixels32(
                1284,
                0,
                1280,
                720,
                right.GetPixels32());
            sheet.Apply(false, false);
            return sheet.EncodeToPNG();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(left);
            UnityEngine.Object.DestroyImmediate(right);
            UnityEngine.Object.DestroyImmediate(sheet);
        }
    }

    private static void AddMesh(
        Transform parent,
        string name,
        Mesh mesh,
        Material material,
        float depth)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition =
            new Vector3(0f, 0f, depth);
        child.AddComponent<MeshFilter>().sharedMesh = mesh;
        child.AddComponent<MeshRenderer>()
            .sharedMaterial = material;
    }

    private static Mesh CreateDiscMesh(
        string name,
        float radius,
        int segments)
    {
        var vertices =
            new Vector3[segments + 1];
        var triangles =
            new int[segments * 3];
        vertices[0] = Vector3.zero;
        for (int index = 0;
            index < segments;
            index++)
        {
            float angle =
                index * Mathf.PI * 2f / segments;
            vertices[index + 1] =
                new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0f);
            triangles[index * 3] = 0;
            triangles[index * 3 + 1] =
                index + 1;
            triangles[index * 3 + 2] =
                (index + 1) % segments + 1;
        }
        return BuildMesh(
            name,
            vertices,
            triangles);
    }

    private static Mesh CreateRingMesh(
        string name,
        float outerRadius,
        float innerRadius,
        int segments)
    {
        var vertices =
            new Vector3[segments * 2];
        var triangles =
            new int[segments * 6];
        for (int index = 0;
            index < segments;
            index++)
        {
            float angle =
                index * Mathf.PI * 2f / segments;
            Vector3 direction =
                new Vector3(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle),
                    0f);
            vertices[index * 2] =
                direction * outerRadius;
            vertices[index * 2 + 1] =
                direction * innerRadius;
            int next =
                (index + 1) % segments;
            int offset = index * 6;
            triangles[offset] = index * 2;
            triangles[offset + 1] =
                next * 2;
            triangles[offset + 2] =
                next * 2 + 1;
            triangles[offset + 3] =
                index * 2;
            triangles[offset + 4] =
                next * 2 + 1;
            triangles[offset + 5] =
                index * 2 + 1;
        }
        return BuildMesh(
            name,
            vertices,
            triangles);
    }

    private static Mesh CreatePolygonMesh(
        string name,
        IReadOnlyList<Vector2> points)
    {
        var vertices =
            new Vector3[points.Count];
        for (int index = 0;
            index < points.Count;
            index++)
        {
            vertices[index] =
                new Vector3(
                    points[index].x,
                    points[index].y,
                    0f);
        }
        int[] triangles = Triangulate(points);
        return BuildMesh(
            name,
            vertices,
            triangles);
    }

    private static Mesh CreateCompositeMesh(
        string name,
        IReadOnlyList<Vector2[]> polygons)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        foreach (Vector2[] polygon in polygons)
        {
            int start = vertices.Count;
            foreach (Vector2 point in polygon)
            {
                vertices.Add(
                    new Vector3(point.x, point.y, 0f));
            }
            bool counterClockwise =
                IsCounterClockwise(polygon);
            for (int index = 0;
                index < polygon.Length - 2;
                index++)
            {
                triangles.Add(start);
                triangles.Add(
                    start
                    + (counterClockwise
                        ? index + 1
                        : index + 2));
                triangles.Add(
                    start
                    + (counterClockwise
                        ? index + 2
                        : index + 1));
            }
        }
        return BuildMesh(
            name,
            vertices.ToArray(),
            triangles.ToArray());
    }

    private static Mesh BuildMesh(
        string name,
        Vector3[] vertices,
        int[] triangles)
    {
        var mesh = new Mesh { name = name };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        var uv = new Vector2[vertices.Length];
        for (int index = 0;
            index < vertices.Length;
            index++)
        {
            uv[index] =
                new Vector2(
                    vertices[index].x / 8f + 0.5f,
                    vertices[index].y / 8f + 0.5f);
        }
        mesh.uv = uv;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Vector2[] MirrorPolygon(
        IReadOnlyList<Vector2> source)
    {
        var mirrored =
            new Vector2[source.Count];
        for (int index = 0;
            index < source.Count;
            index++)
        {
            Vector2 point =
                source[source.Count - 1 - index];
            mirrored[index] =
                new Vector2(-point.x, point.y);
        }
        return mirrored;
    }

    private static Vector2[] LeftWingPoints()
    {
        return new[]
        {
            new Vector2(-4.35f, 0.08f),
            new Vector2(-3.72f, -0.82f),
            new Vector2(-3.03f, -0.64f),
            new Vector2(-3.34f, -0.16f),
            new Vector2(-2.68f, -0.47f),
            new Vector2(-2.16f, -0.38f),
            new Vector2(-2.16f, 0.38f),
            new Vector2(-2.68f, 0.47f),
            new Vector2(-3.34f, 0.16f),
            new Vector2(-3.03f, 0.64f),
            new Vector2(-3.70f, 0.98f)
        };
    }

    private static Vector2[] FrontCrownPoints()
    {
        return new[]
        {
            new Vector2(-1.10f, 2.16f),
            new Vector2(1.10f, 2.16f),
            new Vector2(1.34f, 2.78f),
            new Vector2(0.68f, 2.64f),
            new Vector2(0.86f, 3.28f),
            new Vector2(0.32f, 3.02f),
            new Vector2(0f, 4.18f),
            new Vector2(-0.32f, 3.02f),
            new Vector2(-0.86f, 3.28f),
            new Vector2(-0.68f, 2.64f),
            new Vector2(-1.34f, 2.78f)
        };
    }

    private static Vector2[] RearKeelPoints()
    {
        return new[]
        {
            new Vector2(-1.04f, -2.16f),
            new Vector2(-1.34f, -2.80f),
            new Vector2(-0.70f, -2.70f),
            new Vector2(-0.92f, -3.35f),
            new Vector2(0f, -4.02f),
            new Vector2(0.92f, -3.35f),
            new Vector2(0.70f, -2.70f),
            new Vector2(1.34f, -2.80f),
            new Vector2(1.04f, -2.16f)
        };
    }

    private static int[] Triangulate(
        IReadOnlyList<Vector2> points)
    {
        var remaining = new List<int>();
        if (IsCounterClockwise(points))
        {
            for (int index = 0;
                index < points.Count;
                index++)
            {
                remaining.Add(index);
            }
        }
        else
        {
            for (int index = points.Count - 1;
                index >= 0;
                index--)
            {
                remaining.Add(index);
            }
        }

        var triangles = new List<int>();
        int guard = points.Count * points.Count;
        while (remaining.Count > 3 && guard-- > 0)
        {
            bool clipped = false;
            for (int index = 0;
                index < remaining.Count;
                index++)
            {
                int previous =
                    remaining[
                        (index - 1 + remaining.Count)
                        % remaining.Count];
                int current = remaining[index];
                int next =
                    remaining[
                        (index + 1)
                        % remaining.Count];
                if (!IsConvex(
                    points[previous],
                    points[current],
                    points[next]))
                {
                    continue;
                }

                bool containsPoint = false;
                foreach (int candidate in remaining)
                {
                    if (candidate == previous
                        || candidate == current
                        || candidate == next)
                    {
                        continue;
                    }
                    if (PointInTriangle(
                        points[candidate],
                        points[previous],
                        points[current],
                        points[next]))
                    {
                        containsPoint = true;
                        break;
                    }
                }
                if (containsPoint)
                {
                    continue;
                }

                triangles.Add(previous);
                triangles.Add(current);
                triangles.Add(next);
                remaining.RemoveAt(index);
                clipped = true;
                break;
            }
            if (!clipped)
            {
                break;
            }
        }

        if (remaining.Count == 3)
        {
            triangles.Add(remaining[0]);
            triangles.Add(remaining[1]);
            triangles.Add(remaining[2]);
        }
        if (triangles.Count
            != (points.Count - 2) * 3)
        {
            throw new InvalidOperationException(
                "Silhouette polygon triangulation failed.");
        }
        return triangles.ToArray();
    }

    private static bool IsConvex(
        Vector2 previous,
        Vector2 current,
        Vector2 next)
    {
        return Cross(
            current - previous,
            next - current) > 0.00001f;
    }

    private static bool PointInTriangle(
        Vector2 point,
        Vector2 a,
        Vector2 b,
        Vector2 c)
    {
        float ab = Cross(b - a, point - a);
        float bc = Cross(c - b, point - b);
        float ca = Cross(a - c, point - c);
        return ab >= 0f && bc >= 0f && ca >= 0f;
    }

    private static float Cross(
        Vector2 left,
        Vector2 right)
    {
        return left.x * right.y
            - left.y * right.x;
    }

    private static bool IsCounterClockwise(
        IReadOnlyList<Vector2> points)
    {
        float signedArea = 0f;
        for (int index = 0;
            index < points.Count;
            index++)
        {
            Vector2 current = points[index];
            Vector2 next =
                points[(index + 1) % points.Count];
            signedArea +=
                current.x * next.y
                - next.x * current.y;
        }
        return signedArea > 0f;
    }

    private static void SaveMesh(
        Mesh mesh,
        string fileName)
    {
        AssetDatabase.CreateAsset(
            mesh,
            SilhouetteRoot + "/" + fileName);
    }

    private static void ReplaceMesh(
        string path,
        Mesh replacement)
    {
        Mesh target =
            AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (target == null)
        {
            UnityEngine.Object.DestroyImmediate(
                replacement);
            throw new InvalidOperationException(
                $"Silhouette Mesh is missing: {path}");
        }
        target.Clear();
        target.vertices = replacement.vertices;
        target.triangles = replacement.triangles;
        target.uv = replacement.uv;
        target.normals = replacement.normals;
        target.bounds = replacement.bounds;
        EditorUtility.SetDirty(target);
        UnityEngine.Object.DestroyImmediate(
            replacement);
    }

    private static Material CreateMaterial(
        string path,
        Color color)
    {
        RefuseExisting(path);
        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Sprites/Default");
        if (shader == null)
        {
            throw new InvalidOperationException(
                "No compatible Unlit Shader was found.");
        }
        var material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        string[] segments = path.Split('/');
        string current = segments[0];
        for (int index = 1;
            index < segments.Length;
            index++)
        {
            string next =
                $"{current}/{segments[index]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(
                    current,
                    segments[index]);
            }
            current = next;
        }
    }

    private static void RefuseExisting(
        params string[] paths)
    {
        foreach (string path in paths)
        {
            if (File.Exists(path)
                || AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                    path) != null)
            {
                throw new InvalidOperationException(
                    $"Refusing to overwrite existing output: {path}");
            }
        }
    }

    private static string GetRepositoryPath(
        string relativePath)
    {
        return Path.GetFullPath(
            Path.Combine(
                Application.dataPath,
                "..",
                "..",
                relativePath));
    }
}
