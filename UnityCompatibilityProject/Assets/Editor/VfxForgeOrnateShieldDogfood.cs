using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Kubonsang.VfxForge;
using Kubonsang.VfxForge.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;
using VfxForge.Dogfood;

public static partial class VfxForgeGiantShieldDogfood
{
    private const string OrnateRoot =
        "Assets/VFXForge/Dogfood/OrnateGiantShield";
    private const string OrnateAuthoringRoot =
        OrnateRoot + "/Authoring";
    private const string OrnateGeneratedRoot =
        OrnateRoot + "/Generated";
    private const string OrnateDemoRoot =
        OrnateRoot + "/Demo";
    private const string OrnateGraphPath =
        OrnateAuthoringRoot + "/OrnateGiantShield.vfx";
    private const string OrnateShaderGraphPath =
        OrnateAuthoringRoot + "/OrnateGiantShieldVfx.shadergraph";
    private const string OrnatePanelMeshPath =
        OrnateAuthoringRoot + "/OrnateShieldPanel.asset";
    private const string LeftWingMeshPath =
        OrnateAuthoringRoot + "/LeftWing.asset";
    private const string RightWingMeshPath =
        OrnateAuthoringRoot + "/RightWing.asset";
    private const string FrontSpireMeshPath =
        OrnateAuthoringRoot + "/FrontSpire.asset";
    private const string RearCrestMeshPath =
        OrnateAuthoringRoot + "/RearCrest.asset";
    private const string RuneRingMeshPath =
        OrnateAuthoringRoot + "/RuneRing.asset";
    private const string OrnateFieldMaterialPath =
        OrnateAuthoringRoot + "/OrnateField.mat";
    private const string OrnateRimMaterialPath =
        OrnateAuthoringRoot + "/OrnateRim.mat";
    private const string OrnamentMaterialPath =
        OrnateAuthoringRoot + "/OrnamentFiligree.mat";
    private const string OrnateTemplatePath =
        OrnateAuthoringRoot + "/OrnateGiantShieldTemplate.prefab";
    private const string OrnateCatalogPath =
        OrnateRoot + "/OrnateGiantShieldCatalog.asset";
    private const string OrnateGeneratedPrefabPath =
        OrnateGeneratedRoot + "/OrnateGiantShield.prefab";
    private const string OrnateDemoScenePath =
        OrnateDemoRoot + "/OrnateGiantShieldDemo.unity";
    private const string OrnateVolumePath =
        OrnateDemoRoot + "/OrnateShieldVolume.asset";
    private const string V1GraphPath =
        "Assets/VFXForge/Dogfood/GiantShield/Authoring/GiantShieldDeployment.vfx";
    private const string V1ShaderGraphPath =
        "Assets/VFXForge/Dogfood/GiantShield/Authoring/GiantShieldVfx.shadergraph";

    [MenuItem("Tools/VFX Forge/Dogfood/Build VF-014 Ornate Shield")]
    public static void BuildOrnateAuthoringAssets()
    {
        EnsureTargetsAbsent(
            OrnateGraphPath,
            OrnateShaderGraphPath,
            OrnatePanelMeshPath,
            LeftWingMeshPath,
            RightWingMeshPath,
            FrontSpireMeshPath,
            RearCrestMeshPath,
            RuneRingMeshPath,
            OrnateFieldMaterialPath,
            OrnateRimMaterialPath,
            OrnamentMaterialPath,
            OrnateTemplatePath,
            OrnateCatalogPath);
        EnsureFolder(OrnateAuthoringRoot);
        EnsureFolder(OrnateGeneratedRoot);

        CopyProjectOwnedAsset(
            V1GraphPath,
            OrnateGraphPath,
            "ornate VFX Graph");
        CopyProjectOwnedAsset(
            V1ShaderGraphPath,
            OrnateShaderGraphPath,
            "ornate VFX Shader Graph");
        AttachShaderGraph(OrnateGraphPath, OrnateShaderGraphPath);
        SetParticleCapacity(OrnateGraphPath, 64);
        AssetDatabase.ImportAsset(
            OrnateGraphPath,
            ImportAssetOptions.ForceSynchronousImport);

        Mesh panel = CreateOrnatePanelMesh();
        Mesh leftWing = CreateSideWingMesh(false);
        Mesh rightWing = CreateSideWingMesh(true);
        Mesh frontSpire = CreateFrontSpireMesh();
        Mesh rearCrest = CreateRearCrestMesh();
        Mesh runeRing = CreateRuneRingMesh();
        AssetDatabase.CreateAsset(panel, OrnatePanelMeshPath);
        AssetDatabase.CreateAsset(leftWing, LeftWingMeshPath);
        AssetDatabase.CreateAsset(rightWing, RightWingMeshPath);
        AssetDatabase.CreateAsset(frontSpire, FrontSpireMeshPath);
        AssetDatabase.CreateAsset(rearCrest, RearCrestMeshPath);
        AssetDatabase.CreateAsset(runeRing, RuneRingMeshPath);

        Shader shader = Shader.Find("VFXForge/Dogfood/OrnateShield");
        if (shader == null)
        {
            throw new InvalidOperationException(
                "Ornate Shield shader did not import.");
        }
        Material fieldMaterial = CreateOrnateMaterial(
            "Ornate Shield Field",
            shader,
            0f);
        Material rimMaterial = CreateOrnateMaterial(
            "Ornate Platinum Rim",
            shader,
            1f);
        Material ornamentMaterial = CreateOrnateMaterial(
            "Ornate Gold-Cyan Filigree",
            shader,
            2f);
        AssetDatabase.CreateAsset(
            fieldMaterial,
            OrnateFieldMaterialPath);
        AssetDatabase.CreateAsset(
            rimMaterial,
            OrnateRimMaterialPath);
        AssetDatabase.CreateAsset(
            ornamentMaterial,
            OrnamentMaterialPath);

        VisualEffectAsset graph =
            AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(
                OrnateGraphPath);
        if (graph == null)
        {
            throw new InvalidOperationException(
                $"Ornate VFX Graph failed to load: {OrnateGraphPath}");
        }

        GameObject source =
            new GameObject("Ornate Giant Shield V2 Template");
        try
        {
            VfxPlayer player = source.AddComponent<VfxPlayer>();
            player.Configure("OnPlay");

            var visualRoot = new GameObject("Ornate Visual Root");
            visualRoot.transform.SetParent(source.transform, false);
            var panels = new List<Transform>();
            var panelRenderers = new List<Renderer>();
            for (int index = 0; index < 5; index++)
            {
                panels.Add(AddOrnatePanel(
                    visualRoot.transform,
                    index,
                    panel,
                    fieldMaterial,
                    rimMaterial,
                    panelRenderers));
            }

            var ornamentRoots = new List<Transform>();
            var ornamentRenderers = new List<Renderer>();
            ornamentRoots.Add(AddOrnament(
                visualRoot.transform,
                "Rune Ring",
                runeRing,
                ornamentMaterial,
                ornamentRenderers));
            ornamentRoots.Add(AddOrnament(
                visualRoot.transform,
                "Left Triple Wing",
                leftWing,
                ornamentMaterial,
                ornamentRenderers));
            ornamentRoots.Add(AddOrnament(
                visualRoot.transform,
                "Right Triple Wing",
                rightWing,
                ornamentMaterial,
                ornamentRenderers));
            ornamentRoots.Add(AddOrnament(
                visualRoot.transform,
                "Front Crown Spire",
                frontSpire,
                ornamentMaterial,
                ornamentRenderers));
            ornamentRoots.Add(AddOrnament(
                visualRoot.transform,
                "Rear Heraldic Crest",
                rearCrest,
                ornamentMaterial,
                ornamentRenderers));

            var graphObject = new GameObject("VFX Graph Jewels");
            graphObject.transform.SetParent(visualRoot.transform, false);
            graphObject.transform.localPosition =
                new Vector3(0f, 0.18f, 2.7f);
            graphObject.transform.localScale = Vector3.one * 0.04f;
            VisualEffect effect =
                graphObject.AddComponent<VisualEffect>();
            effect.visualEffectAsset = graph;
            effect.initialEventName = "OnPlay";
            effect.startSeed = 140730u;
            effect.resetSeedOnPlay = false;
            effect.enabled = true;
            SetOrnateDefaultOverrides(effect);

            OrnateShieldDeployment controller =
                source.AddComponent<OrnateShieldDeployment>();
            controller.Configure(
                effect,
                visualRoot.transform,
                panels.ToArray(),
                panelRenderers.ToArray(),
                ornamentRoots.ToArray(),
                ornamentRenderers.ToArray());

            GameObject template =
                PrefabUtility.SaveAsPrefabAsset(
                    source,
                    OrnateTemplatePath);
            if (template == null)
            {
                throw new InvalidOperationException(
                    "Ornate Template Prefab could not be saved.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
        }

        var catalog = ScriptableObject.CreateInstance<VfxTemplateCatalog>();
        catalog.templates.Add(new VfxTemplateEntry
        {
            id = "ornate_giant_shield_v2",
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                OrnateTemplatePath),
            playEventName = "OnPlay",
            supportedLayers = new[]
            {
                "shield_field",
                "platinum_rim",
                "side_wings",
                "front_spire",
                "rear_crest",
                "rune_ring"
            },
            bindings = CreateBindings()
        });
        AssetDatabase.CreateAsset(catalog, OrnateCatalogPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        VerifyExposedProperties(graph);
        List<VfxValidationResult> results =
            VfxTemplateCatalogValidator.Validate(catalog);
        VfxValidationResult error = results.FirstOrDefault(
            result =>
                result.severity == VfxValidationSeverity.Error);
        if (error != null)
        {
            throw new InvalidOperationException(
                $"Ornate Catalog failed validation: "
                + $"{error.ruleId} {error.message}");
        }

        GameObject saved =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                OrnateTemplatePath);
        if (saved.GetComponentsInChildren<ParticleSystem>(true).Length != 0)
        {
            throw new InvalidOperationException(
                "Ornate Template must not contain ParticleSystem.");
        }
        Debug.Log(
            "[VFXForge VF-014] Ornate authoring assets created. "
            + $"Graph={OrnateGraphPath}, Ornaments=5, "
            + "ParticleSystems=0, Bindings=12.");
    }

    [MenuItem("Tools/VFX Forge/Dogfood/Create VF-014 Demo")]
    public static void CreateOrnateDemoScene()
    {
        EnsureTargetsAbsent(
            OrnateDemoScenePath,
            OrnateVolumePath,
            OrnateDemoRoot + "/Arena.mat",
            OrnateDemoRoot + "/Caster.mat",
            OrnateDemoRoot + "/Projectile.mat",
            OrnateDemoRoot + "/Border.mat");
        EnsureFolder(OrnateDemoRoot);
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                OrnateGeneratedPrefabPath);
        if (prefab == null)
        {
            throw new InvalidOperationException(
                $"Generated Ornate Prefab is missing: "
                + OrnateGeneratedPrefabPath);
        }

        Material arena = CreateSavedUnlitMaterial(
            "Ornate Arena",
            new Color(0.018f, 0.03f, 0.055f, 1f),
            OrnateDemoRoot + "/Arena.mat");
        Material caster = CreateSavedUnlitMaterial(
            "Ornate Shield Caster",
            new Color(0.20f, 0.27f, 0.40f, 1f),
            OrnateDemoRoot + "/Caster.mat");
        Material projectile = CreateSavedUnlitMaterial(
            "Incoming Threat",
            new Color(1f, 0.08f, 0.04f, 1f),
            OrnateDemoRoot + "/Projectile.mat");
        Material border = CreateSavedUnlitMaterial(
            "Arena Border",
            new Color(0.055f, 0.12f, 0.19f, 1f),
            OrnateDemoRoot + "/Border.mat");

        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single);
        Camera camera = CreateOrnateTopDownCamera();
        CreateOrnateBloom();
        CreateOrnateArena(arena, caster, projectile, border);

        var spawnObject = new GameObject("Ornate Shield Spawn");
        spawnObject.transform.position =
            new Vector3(0f, 0f, -1.1f);
        var controllerObject =
            new GameObject("Ornate Shield Demo Controller");
        OrnateShieldDemoController demo =
            controllerObject.AddComponent<OrnateShieldDemoController>();
        demo.Configure(prefab, spawnObject.transform, 2.7f);

        if (!EditorSceneManager.SaveScene(
            scene,
            OrnateDemoScenePath))
        {
            throw new InvalidOperationException(
                "Ornate Demo Scene could not be saved.");
        }
        AddSceneToBuildSettings(OrnateDemoScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log(
            $"[VFXForge VF-014] Top-down demo created: "
            + OrnateDemoScenePath);
    }

    [MenuItem("Tools/VFX Forge/Dogfood/Open VF-014 Demo")]
    public static void OpenOrnateDemoScene()
    {
        EditorSceneManager.OpenScene(
            OrnateDemoScenePath,
            OpenSceneMode.Single);
    }

    public static void CaptureOrnateEvidence()
    {
        EditorSceneManager.OpenScene(
            OrnateDemoScenePath,
            OpenSceneMode.Single);
        Camera camera = Camera.main;
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                OrnateGeneratedPrefabPath);
        Transform spawn =
            GameObject.Find("Ornate Shield Spawn")?.transform;
        if (camera == null || prefab == null || spawn == null)
        {
            throw new InvalidOperationException(
                "Ornate demo Camera, Prefab, or spawn is missing.");
        }

        string root = GetRepositoryPath(
            "Dogfooding/Evidence/VF-014");
        float[] times =
        {
            0.12f, 0.30f, 0.55f, 0.95f, 1.48f, 1.88f
        };
        string[] suffixes =
        {
            "012", "030", "055", "095", "148", "188"
        };
        var expectedFiles = new List<string>();
        foreach (string suffix in suffixes)
        {
            expectedFiles.Add($"top-{suffix}.png");
            expectedFiles.Add($"threequarter-{suffix}.png");
        }
        RefuseExistingEvidence(root, expectedFiles);
        Directory.CreateDirectory(root);

        Vector3 originalPosition = camera.transform.position;
        Quaternion originalRotation = camera.transform.rotation;
        bool originalOrthographic = camera.orthographic;
        float originalSize = camera.orthographicSize;
        float originalFov = camera.fieldOfView;

        for (int index = 0; index < times.Length; index++)
        {
            GameObject instance =
                PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException(
                    "Could not instantiate Ornate evidence Prefab.");
            }
            instance.transform.SetPositionAndRotation(
                spawn.position,
                spawn.rotation);
            instance.GetComponent<OrnateShieldDeployment>()
                ?.EvaluatePreviewTime(times[index]);

            camera.orthographic = true;
            camera.orthographicSize = originalSize;
            camera.transform.SetPositionAndRotation(
                originalPosition,
                originalRotation);
            WriteCameraPng(
                camera,
                Path.Combine(root, $"top-{suffixes[index]}.png"),
                1280,
                720);

            camera.orthographic = false;
            camera.fieldOfView = 50f;
            camera.transform.position =
                new Vector3(0f, 7.8f, -8.2f);
            Vector3 target = new Vector3(0f, 0.25f, 1.2f);
            camera.transform.rotation = Quaternion.LookRotation(
                target - camera.transform.position,
                Vector3.up);
            WriteCameraPng(
                camera,
                Path.Combine(
                    root,
                    $"threequarter-{suffixes[index]}.png"),
                1280,
                720);
            UnityEngine.Object.DestroyImmediate(instance);
        }

        camera.orthographic = originalOrthographic;
        camera.orthographicSize = originalSize;
        camera.fieldOfView = originalFov;
        camera.transform.SetPositionAndRotation(
            originalPosition,
            originalRotation);
        Debug.Log(
            $"[VFXForge VF-014] Gameplay evidence captured: {root}");
    }

    public static void ValidateOrnateCaptures()
    {
        ValidateOrnatePngDirectory(
            Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "../Artifacts/dogfood/VF-014-primary-final/capture")),
            6);
        ValidateOrnatePngDirectory(
            Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "../Artifacts/dogfood/VF-014-variant-final/capture")),
            6);
        ValidateOrnatePngDirectory(
            GetRepositoryPath("Dogfooding/Evidence/VF-014"),
            12);
    }

    public static void CaptureOrnateConsoleCounts()
    {
        Type logEntriesType = typeof(Editor).Assembly.GetType(
            "UnityEditor.LogEntries",
            true);
        MethodInfo getCounts = logEntriesType.GetMethod(
            "GetCountsByType",
            BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic);
        if (getCounts == null)
        {
            throw new MissingMethodException(
                logEntriesType.FullName,
                "GetCountsByType");
        }

        object[] arguments = { 0, 0, 0 };
        getCounts.Invoke(null, arguments);
        int errors = (int)arguments[0];
        int warnings = (int)arguments[1];
        int logs = (int)arguments[2];
        string output = Path.GetFullPath(Path.Combine(
            Application.dataPath,
            "../Artifacts/vf-014-console.json"));
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        File.WriteAllText(
            output,
            JsonUtility.ToJson(
                new OrnateConsoleCounts
                {
                    errors = errors,
                    warnings = warnings,
                    logs = logs
                },
                true));
        Debug.Log(
            $"[VFXForge VF-014] Console counts: errors={errors}, "
            + $"warnings={warnings}, logs={logs}.");
        if (errors != 0)
        {
            throw new InvalidOperationException(
                $"VF-014 Console contains {errors} error(s).");
        }
    }

    private static void ValidateOrnatePngDirectory(
        string root,
        int expectedCount)
    {
        string[] frames = Directory.Exists(root)
            ? Directory.GetFiles(root, "*.png")
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray()
            : Array.Empty<string>();
        if (frames.Length != expectedCount)
        {
            throw new InvalidOperationException(
                $"Expected {expectedCount} Ornate frames, "
                + $"found {frames.Length}: {root}");
        }

        foreach (string frame in frames)
        {
            float ratio = MeasureForegroundRatio(frame);
            Debug.Log(
                "[VFXForge VF-014] Foreground ratio "
                + $"{Path.GetFileName(frame)}={ratio:P2}");
            if (ratio < 0.01f)
            {
                throw new InvalidOperationException(
                    "Ornate capture foreground ratio below 1%: "
                    + $"{Path.GetFileName(frame)}={ratio:P2}");
            }
        }
    }

    private static Mesh CreateOrnatePanelMesh()
    {
        const int Columns = 8;
        const int Rows = 12;
        const float HalfWidth = 0.70f;
        const float Height = 3.75f;
        const float Forward = 2.70f;
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var colors = new List<Color>();
        var triangles = new List<int>();

        for (int row = 0; row <= Rows; row++)
        {
            float v = row / (float)Rows;
            for (int column = 0; column <= Columns; column++)
            {
                float u = column / (float)Columns;
                float signed = u * 2f - 1f;
                float taper = Mathf.Lerp(1f, 0.78f, v);
                float x = signed * HalfWidth * taper;
                float crown = v * 0.30f
                    * (1f - Mathf.Pow(Mathf.Abs(signed), 1.6f));
                float y = v * Height + crown;
                float z = Forward
                    + 0.14f * (1f - signed * signed);
                vertices.Add(new Vector3(x, y, z));
                uvs.Add(new Vector2(u, v));
                colors.Add(Color.white);
            }
        }

        int stride = Columns + 1;
        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                AddQuad(
                    triangles,
                    row * stride + column,
                    row * stride + column + 1,
                    (row + 1) * stride + column,
                    (row + 1) * stride + column + 1);
            }
        }
        return FinalizeMesh(
            "Ornate Shield Crown Panel",
            vertices,
            uvs,
            colors,
            triangles);
    }

    private static Mesh CreateSideWingMesh(bool right)
    {
        float sign = right ? 1f : -1f;
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var colors = new List<Color>();
        var triangles = new List<int>();

        AddRibbon(
            vertices,
            uvs,
            colors,
            triangles,
            new[]
            {
                new Vector3(sign * 1.65f, 0.19f, 2.25f),
                new Vector3(sign * 2.45f, 0.21f, 2.42f),
                new Vector3(sign * 3.30f, 0.23f, 2.18f),
                new Vector3(sign * 4.10f, 0.25f, 1.55f),
                new Vector3(sign * 4.58f, 0.27f, 0.70f)
            },
            new[] { 0.16f, 0.28f, 0.31f, 0.23f, 0.035f });
        AddRibbon(
            vertices,
            uvs,
            colors,
            triangles,
            new[]
            {
                new Vector3(sign * 1.55f, 0.17f, 1.82f),
                new Vector3(sign * 2.30f, 0.19f, 1.72f),
                new Vector3(sign * 3.05f, 0.21f, 1.32f),
                new Vector3(sign * 3.72f, 0.23f, 0.58f),
                new Vector3(sign * 3.98f, 0.25f, -0.16f)
            },
            new[] { 0.14f, 0.25f, 0.28f, 0.19f, 0.03f });
        AddRibbon(
            vertices,
            uvs,
            colors,
            triangles,
            new[]
            {
                new Vector3(sign * 1.42f, 0.15f, 1.37f),
                new Vector3(sign * 2.05f, 0.17f, 1.10f),
                new Vector3(sign * 2.68f, 0.19f, 0.58f),
                new Vector3(sign * 3.18f, 0.21f, -0.10f),
                new Vector3(sign * 3.30f, 0.23f, -0.72f)
            },
            new[] { 0.12f, 0.22f, 0.23f, 0.15f, 0.025f });
        return FinalizeMesh(
            right ? "Right Triple Filigree Wing"
                : "Left Triple Filigree Wing",
            vertices,
            uvs,
            colors,
            triangles);
    }

    private static Mesh CreateFrontSpireMesh()
    {
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var colors = new List<Color>();
        var triangles = new List<int>();
        AddRibbon(
            vertices,
            uvs,
            colors,
            triangles,
            new[]
            {
                new Vector3(0f, 0.22f, 2.10f),
                new Vector3(0f, 0.25f, 3.10f),
                new Vector3(0f, 0.28f, 4.08f),
                new Vector3(0f, 0.31f, 5.18f)
            },
            new[] { 0.22f, 0.32f, 0.20f, 0.025f });
        AddRibbon(
            vertices,
            uvs,
            colors,
            triangles,
            new[]
            {
                new Vector3(-0.10f, 0.20f, 2.65f),
                new Vector3(-0.58f, 0.23f, 3.25f),
                new Vector3(-1.12f, 0.26f, 4.02f)
            },
            new[] { 0.13f, 0.20f, 0.025f });
        AddRibbon(
            vertices,
            uvs,
            colors,
            triangles,
            new[]
            {
                new Vector3(0.10f, 0.20f, 2.65f),
                new Vector3(0.58f, 0.23f, 3.25f),
                new Vector3(1.12f, 0.26f, 4.02f)
            },
            new[] { 0.13f, 0.20f, 0.025f });
        return FinalizeMesh(
            "Front Crown Spire",
            vertices,
            uvs,
            colors,
            triangles);
    }

    private static Mesh CreateRearCrestMesh()
    {
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var colors = new List<Color>();
        var triangles = new List<int>();
        AddRibbon(
            vertices,
            uvs,
            colors,
            triangles,
            new[]
            {
                new Vector3(0f, 0.14f, 0.72f),
                new Vector3(0f, 0.16f, -0.08f),
                new Vector3(0f, 0.18f, -1.08f),
                new Vector3(0f, 0.20f, -2.28f)
            },
            new[] { 0.18f, 0.28f, 0.20f, 0.025f });
        AddRibbon(
            vertices,
            uvs,
            colors,
            triangles,
            new[]
            {
                new Vector3(-0.08f, 0.13f, 0.18f),
                new Vector3(-0.56f, 0.15f, -0.34f),
                new Vector3(-1.22f, 0.17f, -0.72f),
                new Vector3(-1.72f, 0.19f, -1.30f)
            },
            new[] { 0.12f, 0.19f, 0.15f, 0.025f });
        AddRibbon(
            vertices,
            uvs,
            colors,
            triangles,
            new[]
            {
                new Vector3(0.08f, 0.13f, 0.18f),
                new Vector3(0.56f, 0.15f, -0.34f),
                new Vector3(1.22f, 0.17f, -0.72f),
                new Vector3(1.72f, 0.19f, -1.30f)
            },
            new[] { 0.12f, 0.19f, 0.15f, 0.025f });
        return FinalizeMesh(
            "Rear Heraldic Crest",
            vertices,
            uvs,
            colors,
            triangles);
    }

    private static Mesh CreateRuneRingMesh()
    {
        const int Segments = 64;
        const float InnerRadius = 1.02f;
        const float OuterRadius = 1.40f;
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var colors = new List<Color>();
        var triangles = new List<int>();
        for (int index = 0; index <= Segments; index++)
        {
            float u = index / (float)Segments;
            float angle = u * Mathf.PI * 2f;
            Vector3 direction =
                new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            vertices.Add(direction * InnerRadius + Vector3.up * 0.10f);
            vertices.Add(direction * OuterRadius + Vector3.up * 0.10f);
            uvs.Add(new Vector2(0f, u));
            uvs.Add(new Vector2(1f, u));
            colors.Add(Color.white);
            colors.Add(Color.white);
        }

        for (int index = 0; index < Segments; index++)
        {
            AddQuad(
                triangles,
                index * 2,
                index * 2 + 1,
                index * 2 + 2,
                index * 2 + 3);
        }
        return FinalizeMesh(
            "Runic Crown Ring",
            vertices,
            uvs,
            colors,
            triangles);
    }

    private static void AddRibbon(
        ICollection<Vector3> vertices,
        ICollection<Vector2> uvs,
        ICollection<Color> colors,
        ICollection<int> triangles,
        IReadOnlyList<Vector3> centers,
        IReadOnlyList<float> halfWidths)
    {
        int start = vertices.Count;
        for (int index = 0; index < centers.Count; index++)
        {
            Vector3 previous = centers[Mathf.Max(0, index - 1)];
            Vector3 next =
                centers[Mathf.Min(centers.Count - 1, index + 1)];
            Vector3 tangent = next - previous;
            tangent.y = 0f;
            tangent.Normalize();
            Vector3 normal =
                new Vector3(-tangent.z, 0f, tangent.x);
            float width = halfWidths[index];
            vertices.Add(centers[index] - normal * width);
            vertices.Add(centers[index] + normal * width);
            float v = index / Mathf.Max(1f, centers.Count - 1f);
            uvs.Add(new Vector2(0f, v));
            uvs.Add(new Vector2(1f, v));
            colors.Add(Color.white);
            colors.Add(Color.white);
        }

        for (int index = 0; index < centers.Count - 1; index++)
        {
            AddQuad(
                triangles,
                start + index * 2,
                start + index * 2 + 1,
                start + index * 2 + 2,
                start + index * 2 + 3);
        }
    }

    private static void AddQuad(
        ICollection<int> triangles,
        int lowerLeft,
        int lowerRight,
        int upperLeft,
        int upperRight)
    {
        triangles.Add(lowerLeft);
        triangles.Add(upperLeft);
        triangles.Add(upperRight);
        triangles.Add(lowerLeft);
        triangles.Add(upperRight);
        triangles.Add(lowerRight);
    }

    private static Mesh FinalizeMesh(
        string name,
        List<Vector3> vertices,
        List<Vector2> uvs,
        List<Color> colors,
        List<int> triangles)
    {
        var mesh = new Mesh { name = name };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        if (!IsFinite(mesh.bounds.min) || !IsFinite(mesh.bounds.max))
        {
            UnityEngine.Object.DestroyImmediate(mesh);
            throw new InvalidOperationException(
                $"{name} bounds are not finite.");
        }
        return mesh;
    }

    private static Material CreateOrnateMaterial(
        string name,
        Shader shader,
        float layerMode)
    {
        var material = new Material(shader) { name = name };
        material.SetColor(
            "_PrimaryColor",
            new Color(0.094f, 0.875f, 1f, 1f));
        material.SetColor(
            "_SecondaryColor",
            new Color(0.97f, 0.99f, 1f, 1f));
        material.SetColor(
            "_AccentColor",
            new Color(1f, 0.64f, 0.16f, 1f));
        material.SetFloat("_Emission", 5.2f);
        material.SetFloat("_Sharpness", 0.84f);
        material.SetFloat("_LayerMode", layerMode);
        return material;
    }

    private static Transform AddOrnatePanel(
        Transform parent,
        int index,
        Mesh mesh,
        Material field,
        Material rim,
        ICollection<Renderer> renderers)
    {
        var panel = new GameObject($"Ornate Panel {index}");
        panel.transform.SetParent(parent, false);
        panel.transform.localRotation =
            Quaternion.Euler(0f, (index - 2f) * 27f, 0f);
        renderers.Add(AddOrnateMesh(
            panel.transform,
            "Circuit Field",
            mesh,
            field,
            Vector3.one,
            0));
        renderers.Add(AddOrnateMesh(
            panel.transform,
            "Platinum Crown Rim",
            mesh,
            rim,
            Vector3.one * 1.012f,
            1));
        return panel.transform;
    }

    private static Transform AddOrnament(
        Transform parent,
        string name,
        Mesh mesh,
        Material material,
        ICollection<Renderer> renderers)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        renderers.Add(AddOrnateMesh(
            root.transform,
            "Shadered Filigree",
            mesh,
            material,
            Vector3.one,
            2));
        return root.transform;
    }

    private static MeshRenderer AddOrnateMesh(
        Transform parent,
        string name,
        Mesh mesh,
        Material material,
        Vector3 scale,
        int sortingOrder)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localScale = scale;
        child.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = child.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private static void SetOrnateDefaultOverrides(
        VisualEffect effect)
    {
        effect.SetInt("RandomSeed", 140730);
        effect.SetFloat("Duration", 2f);
        effect.SetFloat("ImpactTime", 0.38f);
        effect.SetFloat("SustainTime", 1.15f);
        effect.SetFloat("DecayTime", 0.47f);
        effect.SetFloat("Radius", 3.4f);
        effect.SetFloat("SpreadAngle", 135f);
        effect.SetFloat("Directionality", 1f);
        effect.SetVector4(
            "PrimaryColor",
            new Color(0.094f, 0.875f, 1f, 1f));
        effect.SetVector4(
            "SecondaryColor",
            new Color(0.97f, 0.99f, 1f, 1f));
        effect.SetFloat("EmissionIntensity", 5.2f);
        effect.SetFloat("Sharpness", 0.84f);
    }

    private static Camera CreateOrnateTopDownCamera()
    {
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 6.8f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor =
            new Color(0.004f, 0.008f, 0.018f, 1f);
        camera.allowHDR = true;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 100f;
        cameraObject.transform.position =
            new Vector3(0f, 13f, 1.2f);
        cameraObject.transform.rotation =
            Quaternion.Euler(90f, 0f, 0f);
        UniversalAdditionalCameraData cameraData =
            camera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = true;
        cameraData.antialiasing =
            AntialiasingMode.FastApproximateAntialiasing;
        return camera;
    }

    private static void CreateOrnateBloom()
    {
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        Bloom bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(0.62f);
        bloom.threshold.Override(0.92f);
        bloom.scatter.Override(0.54f);
        AssetDatabase.CreateAsset(profile, OrnateVolumePath);
        var volumeObject = new GameObject("Ornate VFX Volume");
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.sharedProfile = profile;
    }

    private static void CreateOrnateArena(
        Material arenaMaterial,
        Material casterMaterial,
        Material projectileMaterial,
        Material borderMaterial)
    {
        GameObject ground =
            GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Top-Down Arena";
        ground.transform.position =
            new Vector3(0f, -0.20f, 1.2f);
        ground.transform.localScale =
            new Vector3(12f, 0.25f, 14f);
        ground.GetComponent<Renderer>().sharedMaterial =
            arenaMaterial;
        UnityEngine.Object.DestroyImmediate(
            ground.GetComponent<Collider>());

        GameObject caster =
            GameObject.CreatePrimitive(PrimitiveType.Capsule);
        caster.name = "Shield Caster";
        caster.transform.position =
            new Vector3(0f, 0.62f, -1.45f);
        caster.transform.localScale =
            new Vector3(0.44f, 0.62f, 0.44f);
        caster.GetComponent<Renderer>().sharedMaterial =
            casterMaterial;
        UnityEngine.Object.DestroyImmediate(
            caster.GetComponent<Collider>());

        GameObject focus =
            GameObject.CreatePrimitive(PrimitiveType.Cube);
        focus.name = "Caster Focus";
        focus.transform.SetParent(caster.transform, false);
        focus.transform.localPosition =
            new Vector3(0f, 0f, 0.78f);
        focus.transform.localScale =
            new Vector3(0.14f, 0.14f, 0.78f);
        focus.GetComponent<Renderer>().sharedMaterial =
            casterMaterial;
        UnityEngine.Object.DestroyImmediate(
            focus.GetComponent<Collider>());

        for (int index = 0; index < 3; index++)
        {
            GameObject threat =
                GameObject.CreatePrimitive(PrimitiveType.Sphere);
            threat.name = $"Incoming Threat {index}";
            threat.transform.position = new Vector3(
                (index - 1) * 2.4f,
                0.35f,
                5.8f + index * 0.3f);
            threat.transform.localScale =
                new Vector3(0.25f, 0.25f, 0.75f);
            threat.GetComponent<Renderer>().sharedMaterial =
                projectileMaterial;
            UnityEngine.Object.DestroyImmediate(
                threat.GetComponent<Collider>());
        }

        for (int index = 0; index < 8; index++)
        {
            float angle = index / 8f * Mathf.PI * 2f;
            GameObject marker =
                GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = $"Arena Rune Marker {index}";
            marker.transform.position = new Vector3(
                Mathf.Sin(angle) * 5.3f,
                0.08f,
                1.2f + Mathf.Cos(angle) * 5.3f);
            marker.transform.localScale =
                new Vector3(0.18f, 0.08f, 0.48f);
            marker.transform.rotation =
                Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f);
            marker.GetComponent<Renderer>().sharedMaterial =
                borderMaterial;
            UnityEngine.Object.DestroyImmediate(
                marker.GetComponent<Collider>());
        }
    }

    [Serializable]
    private sealed class OrnateConsoleCounts
    {
        public int errors;
        public int warnings;
        public int logs;
    }
}
