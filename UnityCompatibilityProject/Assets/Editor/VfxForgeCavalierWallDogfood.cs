using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VfxForge.Dogfood;

public static class VfxForgeCavalierWallDogfood
{
    [Serializable]
    private sealed class ConsoleCounts
    {
        public int errors;
        public int warnings;
        public int logs;
    }

    private const string Root =
        "Assets/VFXForge/Dogfood/HolyAegisV4";
    private const string ShapeRoot = Root + "/Authoring/Shape";
    private const string DemoRoot = Root + "/Demo";
    private const string PrefabPath =
        ShapeRoot + "/CavalierWallShape.prefab";
    private const string DemoScenePath =
        DemoRoot + "/CavalierWallShapeDemo.unity";
    private const string EvidenceRelativePath =
        "Dogfooding/Evidence/VF-022-cavalier-wall-shape";

    private const float HalfWidth = 3.25f;
    private const float ForwardOffset = 2.45f;
    private const float CurveDepth = 0.52f;
    private const float PanelThickness = 0.16f;
    private const int CurveSegments = 32;

    [MenuItem(
        "Tools/VFX Forge/Dogfood/Build VF-022 Cavalier Wall Shape")]
    public static void BuildShape()
    {
        RefuseExisting(PrefabPath);
        EnsureFolder(ShapeRoot);

        Material surfaceMaterial = CreateMaterial(
            ShapeRoot + "/ShapeSurface.mat",
            "Cavalier Wall Shape Surface",
            0.30f,
            0.16f);
        Material frameMaterial = CreateMaterial(
            ShapeRoot + "/ShapeFrame.mat",
            "Cavalier Wall Shape Frame",
            0.62f,
            0.28f);
        Material braceMaterial = CreateMaterial(
            ShapeRoot + "/ShapeBrace.mat",
            "Cavalier Wall Shape Braces",
            0.78f,
            0.34f);

        Mesh surface = CreateCurvedBandMesh(
            "Cavalier Wall Primary Surface",
            0.30f,
            -0.18f,
            PanelThickness);
        Mesh topRim = CreateCurvedBandMesh(
            "Cavalier Wall Top Rim",
            3.00f,
            0.10f,
            0.28f,
            true);
        Mesh bottomRim = CreateCurvedBandMesh(
            "Cavalier Wall Bottom Rim",
            0.10f,
            0.22f,
            0.26f,
            false,
            true);
        Mesh leftRoot = CreateExtrudedPolygonMesh(
            "Cavalier Wall Left Root",
            LeftRootPoints(),
            0.34f);
        Mesh rightRoot = CreateExtrudedPolygonMesh(
            "Cavalier Wall Right Root",
            MirrorPolygon(LeftRootPoints()),
            0.34f);
        Mesh brace = CreateExtrudedPolygonMesh(
            "Cavalier Wall Connected Brace",
            new[]
            {
                new Vector2(-0.38f, -0.32f),
                new Vector2(0.38f, -0.32f),
                new Vector2(0.29f, 0.30f),
                new Vector2(0f, 0.48f),
                new Vector2(-0.29f, 0.30f)
            },
            0.30f);

        SaveMesh(surface, "CavalierWallSurface.asset");
        SaveMesh(topRim, "CavalierWallTopRim.asset");
        SaveMesh(bottomRim, "CavalierWallBottomRim.asset");
        SaveMesh(leftRoot, "CavalierWallLeftRoot.asset");
        SaveMesh(rightRoot, "CavalierWallRightRoot.asset");
        SaveMesh(brace, "CavalierWallBrace.asset");

        var root = new GameObject("Cavalier Wall Shape");
        try
        {
            Transform pivot = new GameObject("Facing Pivot").transform;
            pivot.SetParent(root.transform, false);

            Transform assembly =
                new GameObject("Cavalier Wall Assembly").transform;
            assembly.SetParent(pivot, false);
            assembly.localPosition =
                new Vector3(0f, 0f, ForwardOffset);

            AddMesh(
                assembly,
                "Primary Barrier Surface",
                surface,
                surfaceMaterial,
                Vector3.zero);

            Transform frame =
                new GameObject("Structural Frame").transform;
            frame.SetParent(assembly, false);
            AddMesh(
                frame,
                "Continuous Top Rim",
                topRim,
                frameMaterial,
                Vector3.zero);
            AddMesh(
                frame,
                "Continuous Bottom Rim",
                bottomRim,
                frameMaterial,
                Vector3.zero);
            AddMesh(
                frame,
                "Left Integrated Root",
                leftRoot,
                frameMaterial,
                Vector3.zero);
            AddMesh(
                frame,
                "Right Integrated Root",
                rightRoot,
                frameMaterial,
                Vector3.zero);

            Transform braces =
                new GameObject("Four Connected Braces").transform;
            braces.SetParent(assembly, false);
            float[] braceX = { -2.35f, -0.78f, 0.78f, 2.35f };
            for (int index = 0; index < braceX.Length; index++)
            {
                float x = braceX[index];
                float normalized = x / HalfWidth;
                float z = CurveDepth
                    * (1f - normalized * normalized)
                    - 0.18f;
                float y = TopHeight(x) - 0.16f;
                AddMesh(
                    braces,
                    $"Connected Brace {index + 1:00}",
                    brace,
                    braceMaterial,
                    new Vector3(x, y, z));
            }

            CavalierWallFacing facing =
                root.AddComponent<CavalierWallFacing>();
            facing.Configure(null, pivot);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                root,
                PrefabPath);
            if (saved == null)
            {
                throw new InvalidOperationException(
                    "Cavalier Wall Shape Prefab could not be saved.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            "[VFXForge VF-022] Cavalier Wall grayscale shape created. "
            + "PrimarySurface=1, ContinuousFrame=1, ConnectedBraces=4, "
            + "ParticleSystems=0, Lights=0, ShaderFinish=0.");
    }

    [MenuItem(
        "Tools/VFX Forge/Dogfood/Create VF-022 Cavalier Wall Demo")]
    public static void CreateDemo()
    {
        RefuseExisting(DemoScenePath);
        EnsureFolder(DemoRoot);

        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            throw new InvalidOperationException(
                "Build the VF-022 Cavalier Wall Shape first.");
        }

        Material lightGround = CreateMaterial(
            DemoRoot + "/GroundLight.mat",
            "VF-022 Light Ground",
            0.70f,
            0.02f);
        Material mediumGround = CreateMaterial(
            DemoRoot + "/GroundMedium.mat",
            "VF-022 Medium Ground",
            0.34f,
            0.02f);
        Material darkGround = CreateMaterial(
            DemoRoot + "/GroundDark.mat",
            "VF-022 Dark Ground",
            0.10f,
            0.02f);
        Material casterMaterial = CreateMaterial(
            DemoRoot + "/Caster.mat",
            "VF-022 Caster",
            0.82f,
            0.18f);
        Material targetMaterial = CreateMaterial(
            DemoRoot + "/Target.mat",
            "VF-022 Target",
            0.18f,
            0.18f);

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
                "Cavalier Wall Shape could not be instantiated.");
        }
        instance.name = "Cavalier Wall Shape (Approved E)";
        CavalierWallFacing facing =
            instance.GetComponent<CavalierWallFacing>();
        facing.Configure(caster.transform, facing.FacingPivot);

        var controllerObject =
            new GameObject("VF-022 Demo Controller");
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
        EditorSceneManager.SaveScene(scene, DemoScenePath);
        AddSceneToBuildSettings(DemoScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            "[VFXForge VF-022] Cavalier Wall demo created. "
            + "Play Mode sweeps the target while the wall follows caster facing.");
    }

    public static void BuildAllBatch()
    {
        BuildShape();
        CreateDemo();
        CaptureEvidenceBatch();
    }

    public static void CaptureEvidenceBatch()
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            throw new InvalidOperationException(
                "Cavalier Wall Shape Prefab is missing.");
        }

        string evidenceDirectory = GetRepositoryPath(
            EvidenceRelativePath);
        string isolatedPath = Path.Combine(
            evidenceDirectory,
            "isolated-grayscale.png");
        string gameplayPath = Path.Combine(
            evidenceDirectory,
            "gameplay-forward.png");
        string rotatedPath = Path.Combine(
            evidenceDirectory,
            "gameplay-facing-right.png");
        string sheetPath = Path.Combine(
            evidenceDirectory,
            "shape-contact-sheet.png");
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
            "[VFXForge VF-022] Shape evidence captured: "
            + evidenceDirectory);
    }

    public static void CaptureConsoleCountsBatch()
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
            EvidenceRelativePath);
        Directory.CreateDirectory(evidenceDirectory);
        string reportPath = Path.Combine(
            evidenceDirectory,
            "console-counts.json");
        RefuseExisting(reportPath);
        File.WriteAllText(
            reportPath,
            JsonUtility.ToJson(result, true));

        if (result.errors > 0)
        {
            throw new InvalidOperationException(
                "VF-022 verification found Unity Console errors: "
                + result.errors);
        }
        Debug.Log(
            "[VFXForge VF-022] Console evidence recorded: errors=0, "
            + $"warnings={result.warnings}, logs={result.logs}.");
    }

    private static byte[] Capture(
        GameObject prefab,
        bool gameplayContext,
        Vector3 direction)
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        try
        {
            Material casterMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(
                    DemoRoot + "/Caster.mat");
            Material targetMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(
                    DemoRoot + "/Target.mat");
            Material lightGround =
                AssetDatabase.LoadAssetAtPath<Material>(
                    DemoRoot + "/GroundLight.mat");
            Material mediumGround =
                AssetDatabase.LoadAssetAtPath<Material>(
                    DemoRoot + "/GroundMedium.mat");
            Material darkGround =
                AssetDatabase.LoadAssetAtPath<Material>(
                    DemoRoot + "/GroundDark.mat");

            GameObject instance = PrefabUtility.InstantiatePrefab(
                prefab,
                scene) as GameObject;
            CavalierWallFacing facing =
                instance.GetComponent<CavalierWallFacing>();
            facing.TrySetAimDirection(direction);

            if (gameplayContext)
            {
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
                caster.transform.rotation =
                    Quaternion.LookRotation(direction, Vector3.up);
                AddSword(caster.transform, casterMaterial);
                CreateCapsule(
                    scene,
                    "Target",
                    direction * 7f + Vector3.up * 0.9f,
                    targetMaterial);
            }

            Camera camera = CreateCamera(scene, false);
            CreateLight(scene);
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

    private static Mesh CreateCurvedBandMesh(
        string name,
        float lowerOffset,
        float upperOffset,
        float thickness,
        bool topBand = false,
        bool bottomBand = false)
    {
        var lower = new float[CurveSegments + 1];
        var upper = new float[CurveSegments + 1];
        for (int index = 0; index <= CurveSegments; index++)
        {
            float x = Mathf.Lerp(
                -HalfWidth,
                HalfWidth,
                index / (float)CurveSegments);
            if (topBand)
            {
                lower[index] = TopHeight(x) - 0.18f;
                upper[index] = TopHeight(x) + upperOffset;
            }
            else if (bottomBand)
            {
                lower[index] = BottomHeight(x) - lowerOffset;
                upper[index] = BottomHeight(x) + upperOffset;
            }
            else
            {
                lower[index] = BottomHeight(x) + lowerOffset;
                upper[index] = TopHeight(x) + upperOffset;
            }
        }

        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        float halfThickness = thickness * 0.5f;
        for (int index = 0; index <= CurveSegments; index++)
        {
            float ratio = index / (float)CurveSegments;
            float x = Mathf.Lerp(-HalfWidth, HalfWidth, ratio);
            float normalized = x / HalfWidth;
            float z = CurveDepth
                * (1f - normalized * normalized);
            vertices.Add(new Vector3(
                x,
                lower[index],
                z - halfThickness));
            vertices.Add(new Vector3(
                x,
                upper[index],
                z - halfThickness));
            vertices.Add(new Vector3(
                x,
                lower[index],
                z + halfThickness));
            vertices.Add(new Vector3(
                x,
                upper[index],
                z + halfThickness));
        }

        for (int index = 0; index < CurveSegments; index++)
        {
            int current = index * 4;
            int next = (index + 1) * 4;
            AddQuad(triangles, current, next, next + 1, current + 1);
            AddQuad(
                triangles,
                current + 2,
                current + 3,
                next + 3,
                next + 2);
            AddQuad(
                triangles,
                current,
                current + 2,
                next + 2,
                next);
            AddQuad(
                triangles,
                current + 1,
                next + 1,
                next + 3,
                current + 3);
        }

        AddQuad(triangles, 0, 1, 3, 2);
        int end = CurveSegments * 4;
        AddQuad(
            triangles,
            end,
            end + 2,
            end + 3,
            end + 1);
        return BuildMesh(name, vertices, triangles);
    }

    private static Mesh CreateExtrudedPolygonMesh(
        string name,
        Vector2[] points,
        float thickness)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        float half = thickness * 0.5f;
        foreach (Vector2 point in points)
        {
            vertices.Add(new Vector3(point.x, point.y, -half));
        }
        foreach (Vector2 point in points)
        {
            vertices.Add(new Vector3(point.x, point.y, half));
        }

        for (int index = 1; index < points.Length - 1; index++)
        {
            triangles.Add(0);
            triangles.Add(index + 1);
            triangles.Add(index);
            triangles.Add(points.Length);
            triangles.Add(points.Length + index);
            triangles.Add(points.Length + index + 1);
        }

        for (int index = 0; index < points.Length; index++)
        {
            int next = (index + 1) % points.Length;
            AddQuad(
                triangles,
                index,
                next,
                points.Length + next,
                points.Length + index);
        }
        return BuildMesh(name, vertices, triangles);
    }

    private static Mesh BuildMesh(
        string name,
        List<Vector3> vertices,
        List<int> triangles)
    {
        var mesh = new Mesh { name = name };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void AddQuad(
        List<int> triangles,
        int a,
        int b,
        int c,
        int d)
    {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
        triangles.Add(a);
        triangles.Add(c);
        triangles.Add(d);
    }

    private static Vector2[] LeftRootPoints()
    {
        return new[]
        {
            new Vector2(-3.62f, 0.02f),
            new Vector2(-2.90f, 0.22f),
            new Vector2(-2.90f, 2.92f),
            new Vector2(-3.18f, 3.42f),
            new Vector2(-3.52f, 3.08f)
        };
    }

    private static Vector2[] MirrorPolygon(Vector2[] points)
    {
        var mirrored = new Vector2[points.Length];
        for (int index = 0; index < points.Length; index++)
        {
            Vector2 source = points[points.Length - 1 - index];
            mirrored[index] = new Vector2(-source.x, source.y);
        }
        return mirrored;
    }

    private static float TopHeight(float x)
    {
        float normalized = x / HalfWidth;
        return 3.08f + 0.38f
            * (1f - normalized * normalized);
    }

    private static float BottomHeight(float x)
    {
        float normalized = x / HalfWidth;
        return 0.18f + 0.14f
            * (normalized * normalized);
    }

    private static Material CreateMaterial(
        string path,
        string name,
        float grayscale,
        float smoothness)
    {
        RefuseExisting(path);
        Shader shader = Shader.Find(
            "Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }
        if (shader == null)
        {
            throw new InvalidOperationException(
                "A grayscale shape Shader is unavailable.");
        }

        var material = new Material(shader) { name = name };
        Color color = new Color(
            grayscale,
            grayscale,
            grayscale,
            1f);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void AddMesh(
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
    }

    private static void SaveMesh(Mesh mesh, string fileName)
    {
        AssetDatabase.CreateAsset(
            mesh,
            ShapeRoot + "/" + fileName);
    }

    private static void CreateGroundStrip(
        Scene scene,
        string name,
        Vector3 position,
        Material material)
    {
        GameObject ground = GameObject.CreatePrimitive(
            PrimitiveType.Cube);
        ground.name = name;
        ground.transform.position = position;
        ground.transform.localScale = new Vector3(6f, 0.3f, 10f);
        ground.GetComponent<Renderer>().sharedMaterial = material;
        SceneManager.MoveGameObjectToScene(ground, scene);
    }

    private static GameObject CreateCapsule(
        Scene scene,
        string name,
        Vector3 position,
        Material material)
    {
        GameObject capsule = GameObject.CreatePrimitive(
            PrimitiveType.Capsule);
        capsule.name = name;
        capsule.transform.position = position;
        capsule.transform.localScale = new Vector3(0.75f, 0.9f, 0.75f);
        capsule.GetComponent<Renderer>().sharedMaterial = material;
        SceneManager.MoveGameObjectToScene(capsule, scene);
        return capsule;
    }

    private static void AddSword(
        Transform caster,
        Material material)
    {
        GameObject sword = GameObject.CreatePrimitive(
            PrimitiveType.Cube);
        sword.name = "Facing Sword";
        sword.transform.SetParent(caster, false);
        sword.transform.localPosition = new Vector3(0.55f, 0.35f, 0.65f);
        sword.transform.localRotation = Quaternion.Euler(28f, 0f, 0f);
        sword.transform.localScale = new Vector3(0.10f, 0.10f, 1.25f);
        sword.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static Camera CreateCamera(
        Scene scene,
        bool enabled)
    {
        var cameraObject = new GameObject("VF-022 Gameplay Camera");
        SceneManager.MoveGameObjectToScene(cameraObject, scene);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.scene = scene;
        camera.enabled = enabled;
        camera.cameraType = enabled
            ? CameraType.Game
            : CameraType.Preview;
        if (enabled)
        {
            cameraObject.tag = "MainCamera";
        }
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.035f, 0.04f, 1f);
        camera.fieldOfView = 38f;
        camera.aspect = 16f / 9f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.transform.position = new Vector3(8.7f, 10.8f, -10.5f);
        camera.transform.rotation = Quaternion.LookRotation(
            new Vector3(0f, 1.25f, 3.0f)
                - camera.transform.position,
            Vector3.up);
        return camera;
    }

    private static void CreateLight(Scene scene)
    {
        var lightObject = new GameObject("Demo Directional Light");
        SceneManager.MoveGameObjectToScene(lightObject, scene);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        light.color = new Color(1f, 0.96f, 0.90f);
        light.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
    }

    private static byte[] Render(
        Camera camera,
        int width,
        int height)
    {
        var target = new RenderTexture(
            width,
            height,
            24,
            RenderTextureFormat.ARGB32);
        var texture = new Texture2D(
            width,
            height,
            TextureFormat.RGBA32,
            false);
        RenderTexture previous = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(
                new Rect(0f, 0f, width, height),
                0,
                0);
            texture.Apply();
            return texture.EncodeToPNG();
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static byte[] BuildContactSheet(
        byte[] first,
        byte[] second,
        byte[] third)
    {
        Texture2D a = LoadPng(first);
        Texture2D b = LoadPng(second);
        Texture2D c = LoadPng(third);
        var sheet = new Texture2D(
            a.width * 3,
            a.height,
            TextureFormat.RGBA32,
            false);
        try
        {
            sheet.SetPixels(0, 0, a.width, a.height, a.GetPixels());
            sheet.SetPixels(
                a.width,
                0,
                b.width,
                b.height,
                b.GetPixels());
            sheet.SetPixels(
                a.width * 2,
                0,
                c.width,
                c.height,
                c.GetPixels());
            sheet.Apply();
            return sheet.EncodeToPNG();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(a);
            UnityEngine.Object.DestroyImmediate(b);
            UnityEngine.Object.DestroyImmediate(c);
            UnityEngine.Object.DestroyImmediate(sheet);
        }
    }

    private static Texture2D LoadPng(byte[] bytes)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(bytes);
        return texture;
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = new List<EditorBuildSettingsScene>(
            EditorBuildSettings.scenes);
        if (scenes.Exists(scene => scene.path == scenePath))
        {
            return;
        }
        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static string GetRepositoryPath(string relativePath)
    {
        string projectRoot = Path.GetFullPath(
            Path.Combine(Application.dataPath, ".."));
        string repositoryRoot = Path.GetFullPath(
            Path.Combine(projectRoot, ".."));
        return Path.Combine(repositoryRoot, relativePath);
    }

    private static void EnsureFolder(string assetPath)
    {
        string[] parts = assetPath.Split('/');
        string current = parts[0];
        for (int index = 1; index < parts.Length; index++)
        {
            string next = current + "/" + parts[index];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[index]);
            }
            current = next;
        }
    }

    private static void RefuseExisting(params string[] paths)
    {
        foreach (string path in paths)
        {
            bool exists = path.StartsWith("Assets/", StringComparison.Ordinal)
                ? AssetDatabase.LoadMainAssetAtPath(path) != null
                : File.Exists(path);
            if (exists)
            {
                throw new InvalidOperationException(
                    "Refusing to overwrite existing VF-022 output: "
                    + path);
            }
        }
    }
}
