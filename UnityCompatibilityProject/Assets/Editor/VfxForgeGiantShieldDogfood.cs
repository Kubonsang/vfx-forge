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
    private const string Root = "Assets/VFXForge/Dogfood/GiantShield";
    private const string AuthoringRoot = Root + "/Authoring";
    private const string GeneratedRoot = Root + "/Generated";
    private const string DemoRoot = Root + "/Demo";
    private const string GraphPath =
        AuthoringRoot + "/GiantShieldDeployment.vfx";
    private const string ShaderGraphPath =
        AuthoringRoot + "/GiantShieldVfx.shadergraph";
    private const string PanelMeshPath =
        AuthoringRoot + "/GiantShieldPanel.asset";
    private const string FieldMaterialPath =
        AuthoringRoot + "/GiantShieldField.mat";
    private const string RimMaterialPath =
        AuthoringRoot + "/GiantShieldRim.mat";
    private const string ParticleMaterialPath =
        AuthoringRoot + "/GiantShieldParticle.mat";
    private const string TemplatePath =
        AuthoringRoot + "/GiantShieldTemplate.prefab";
    private const string CatalogPath =
        Root + "/GiantShieldCatalog.asset";
    private const string GeneratedPrefabPath =
        GeneratedRoot + "/GiantShieldDeployment.prefab";
    private const string DemoScenePath =
        DemoRoot + "/GiantShieldDemo.unity";
    private const string VolumeProfilePath =
        DemoRoot + "/GiantShieldVolume.asset";
    private const string SourceGraphPath =
        "Packages/com.unity.visualeffectgraph/Editor/Templates/03_Simple_Burst.vfx";
    private const string SourceShaderGraphPath =
        "Packages/com.unity.visualeffectgraph/ShaderGraph/0_VFXGraph Unlit.shadergraph";

    private static readonly ExposedParameterSpec[] ExposedParameters =
    {
        new ExposedParameterSpec("RandomSeed", typeof(int), 130730),
        new ExposedParameterSpec("Duration", typeof(float), 1.8f),
        new ExposedParameterSpec("ImpactTime", typeof(float), 0.32f),
        new ExposedParameterSpec("SustainTime", typeof(float), 1.08f),
        new ExposedParameterSpec("DecayTime", typeof(float), 0.40f),
        new ExposedParameterSpec("Radius", typeof(float), 3.2f),
        new ExposedParameterSpec("SpreadAngle", typeof(float), 120f),
        new ExposedParameterSpec("Directionality", typeof(float), 1f),
        new ExposedParameterSpec(
            "PrimaryColor",
            typeof(Color),
            new Color(0.086f, 0.85f, 1f, 1f)),
        new ExposedParameterSpec(
            "SecondaryColor",
            typeof(Color),
            new Color(0.957f, 1f, 1f, 1f)),
        new ExposedParameterSpec(
            "EmissionIntensity",
            typeof(float),
            6.5f),
        new ExposedParameterSpec("Sharpness", typeof(float), 0.78f)
    };

    [MenuItem("Tools/VFX Forge/Dogfood/Build VF-013 Giant Shield")]
    public static void BuildAuthoringAssets()
    {
        EnsureTargetsAbsent(
            GraphPath,
            ShaderGraphPath,
            PanelMeshPath,
            FieldMaterialPath,
            RimMaterialPath,
            ParticleMaterialPath,
            TemplatePath,
            CatalogPath);
        EnsureFolder(AuthoringRoot);
        EnsureFolder(GeneratedRoot);

        CopyProjectOwnedAsset(SourceGraphPath, GraphPath, "VFX Graph");
        CopyProjectOwnedAsset(
            SourceShaderGraphPath,
            ShaderGraphPath,
            "VFX Shader Graph");
        AddExposedParameters(GraphPath, ExposedParameters);
        AttachShaderGraph(GraphPath, ShaderGraphPath);
        SetParticleCapacity(GraphPath, 64);
        AssetDatabase.ImportAsset(
            GraphPath,
            ImportAssetOptions.ForceSynchronousImport);

        Mesh panelMesh = CreateShieldPanelMesh();
        AssetDatabase.CreateAsset(panelMesh, PanelMeshPath);
        Shader barrierShader =
            Shader.Find("VFXForge/Dogfood/GiantShieldBarrier");
        Shader particleShader =
            Shader.Find("VFXForge/Dogfood/GiantShieldParticle");
        if (barrierShader == null || particleShader == null)
        {
            throw new InvalidOperationException(
                "Giant Shield shaders did not import.");
        }

        Material fieldMaterial = CreateBarrierMaterial(
            "Giant Shield Field",
            barrierShader,
            0f);
        Material rimMaterial = CreateBarrierMaterial(
            "Giant Shield Platinum Rim",
            barrierShader,
            1f);
        Material particleMaterial = new Material(particleShader)
        {
            name = "Giant Shield Particles"
        };
        particleMaterial.SetColor(
            "_Tint",
            new Color(0.12f, 0.84f, 1f, 0.92f));
        particleMaterial.SetFloat("_Softness", 3.8f);
        AssetDatabase.CreateAsset(fieldMaterial, FieldMaterialPath);
        AssetDatabase.CreateAsset(rimMaterial, RimMaterialPath);
        AssetDatabase.CreateAsset(
            particleMaterial,
            ParticleMaterialPath);

        VisualEffectAsset graph =
            AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(GraphPath);
        if (graph == null)
        {
            throw new InvalidOperationException(
                $"Giant Shield VFX Graph failed to load: {GraphPath}");
        }

        GameObject source =
            new GameObject("Giant Shield Deployment Template");
        try
        {
            VfxPlayer player = source.AddComponent<VfxPlayer>();
            player.Configure("OnPlay");

            var shieldRoot = new GameObject("Shield Root");
            shieldRoot.transform.SetParent(source.transform, false);
            var panels = new List<Transform>();
            var renderers = new List<Renderer>();
            for (int index = 0; index < 5; index++)
            {
                panels.Add(AddShieldPanel(
                    shieldRoot.transform,
                    index,
                    panelMesh,
                    fieldMaterial,
                    rimMaterial,
                    renderers));
            }

            var graphObject = new GameObject("VFX Graph Energy");
            graphObject.transform.SetParent(source.transform, false);
            graphObject.transform.localPosition =
                new Vector3(0f, 1.8f, 2.5f);
            graphObject.transform.localScale = Vector3.one * 0.18f;
            VisualEffect effect =
                graphObject.AddComponent<VisualEffect>();
            effect.visualEffectAsset = graph;
            effect.initialEventName = "OnPlay";
            effect.startSeed = 130730u;
            effect.resetSeedOnPlay = false;
            effect.enabled = true;
            SetDefaultOverrides(effect);

            ParticleSystem anchorBurst =
                CreateAnchorBurst(source.transform, particleMaterial);
            ParticleSystem edgeMotes =
                CreateEdgeMotes(source.transform, particleMaterial);
            ParticleSystem dissolveShards = CreateDissolveShards(
                source.transform,
                particleMaterial);

            GiantShieldDeployment controller =
                source.AddComponent<GiantShieldDeployment>();
            controller.Configure(
                effect,
                shieldRoot.transform,
                panels.ToArray(),
                renderers.ToArray(),
                anchorBurst,
                edgeMotes,
                dissolveShards);

            GameObject template =
                PrefabUtility.SaveAsPrefabAsset(source, TemplatePath);
            if (template == null)
            {
                throw new InvalidOperationException(
                    $"Template Prefab could not be saved: {TemplatePath}");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
        }

        var catalog = ScriptableObject.CreateInstance<VfxTemplateCatalog>();
        catalog.templates.Add(new VfxTemplateEntry
        {
            id = "giant_shield_deployment_v1",
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                TemplatePath),
            playEventName = "OnPlay",
            supportedLayers = new[]
            {
                "shield_field",
                "shield_rim",
                "anchor_burst",
                "edge_motes",
                "dissolve_shards"
            },
            bindings = CreateBindings()
        });
        AssetDatabase.CreateAsset(catalog, CatalogPath);
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
                $"Giant Shield Catalog failed validation: "
                + $"{error.ruleId} {error.message}");
        }

        Debug.Log(
            "[VFXForge VF-013] Authoring assets created. "
            + $"Graph={GraphPath}, ShaderGraph={ShaderGraphPath}, "
            + $"Bindings={ExposedParameters.Length}.");
    }

    [MenuItem("Tools/VFX Forge/Dogfood/Create VF-013 Demo")]
    public static void CreateDemoScene()
    {
        EnsureTargetsAbsent(
            DemoScenePath,
            VolumeProfilePath,
            DemoRoot + "/ArenaGround.mat",
            DemoRoot + "/Caster.mat",
            DemoRoot + "/Projectile.mat",
            DemoRoot + "/Accent.mat");
        EnsureFolder(DemoRoot);
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                GeneratedPrefabPath);
        if (prefab == null)
        {
            throw new InvalidOperationException(
                $"Generated Prefab is missing: {GeneratedPrefabPath}");
        }

        Material ground = CreateSavedUnlitMaterial(
            "Arena Ground",
            new Color(0.025f, 0.038f, 0.065f, 1f),
            DemoRoot + "/ArenaGround.mat");
        Material caster = CreateSavedUnlitMaterial(
            "Shield Caster",
            new Color(0.18f, 0.24f, 0.34f, 1f),
            DemoRoot + "/Caster.mat");
        Material projectile = CreateSavedUnlitMaterial(
            "Incoming Projectiles",
            new Color(1f, 0.12f, 0.055f, 1f),
            DemoRoot + "/Projectile.mat");
        Material accent = CreateSavedUnlitMaterial(
            "Arena Accent",
            new Color(0.07f, 0.16f, 0.24f, 1f),
            DemoRoot + "/Accent.mat");

        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single);
        Camera camera = CreateGameplayCamera();
        CreateBloomVolume();
        CreateArena(ground, caster, projectile, accent);

        var spawnObject = new GameObject("Shield Spawn");
        spawnObject.transform.position = new Vector3(0f, 0f, -1.25f);
        var controllerObject =
            new GameObject("Giant Shield Demo Controller");
        GiantShieldDemoController demo =
            controllerObject.AddComponent<GiantShieldDemoController>();
        demo.Configure(prefab, spawnObject.transform, 2.4f);

        if (!EditorSceneManager.SaveScene(scene, DemoScenePath))
        {
            throw new InvalidOperationException(
                $"Demo Scene could not be saved: {DemoScenePath}");
        }
        AddSceneToBuildSettings(DemoScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log(
            $"[VFXForge VF-013] Demo created: {DemoScenePath}, "
            + $"Camera={camera.name}.");
    }

    [MenuItem("Tools/VFX Forge/Dogfood/Open VF-013 Demo")]
    public static void OpenDemoScene()
    {
        EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
    }

    public static void CaptureDemoEvidence()
    {
        EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
        Camera camera = Camera.main;
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                GeneratedPrefabPath);
        Transform spawn = GameObject.Find("Shield Spawn")?.transform;
        if (camera == null || prefab == null || spawn == null)
        {
            throw new InvalidOperationException(
                "Demo Camera, generated Prefab, or spawn is missing.");
        }

        string root = GetRepositoryPath("Dogfooding/Evidence/VF-013");
        float[] times =
        {
            0.12f, 0.28f, 0.48f, 0.90f, 1.35f, 1.70f
        };
        string[] suffixes =
        {
            "012", "028", "048", "090", "135", "170"
        };
        var expectedFiles = new List<string>();
        foreach (string suffix in suffixes)
        {
            expectedFiles.Add($"front-{suffix}.png");
            expectedFiles.Add($"top-{suffix}.png");
        }
        RefuseExistingEvidence(root, expectedFiles);
        Directory.CreateDirectory(root);

        Vector3 frontPosition = camera.transform.position;
        Quaternion frontRotation = camera.transform.rotation;
        for (int index = 0; index < times.Length; index++)
        {
            GameObject instance =
                PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException(
                    "Could not instantiate Giant Shield evidence Prefab.");
            }
            instance.transform.SetPositionAndRotation(
                spawn.position,
                spawn.rotation);
            instance.GetComponent<GiantShieldDeployment>()
                ?.EvaluatePreviewTime(times[index]);

            camera.transform.SetPositionAndRotation(
                frontPosition,
                frontRotation);
            WriteCameraPng(
                camera,
                Path.Combine(root, $"front-{suffixes[index]}.png"),
                1280,
                720);

            Vector3 target = new Vector3(0f, 0.5f, 1.5f);
            camera.transform.position =
                new Vector3(0f, 10.5f, 1.5f);
            camera.transform.rotation = Quaternion.LookRotation(
                target - camera.transform.position,
                Vector3.forward);
            WriteCameraPng(
                camera,
                Path.Combine(root, $"top-{suffixes[index]}.png"),
                1280,
                720);
            UnityEngine.Object.DestroyImmediate(instance);
        }
        camera.transform.SetPositionAndRotation(
            frontPosition,
            frontRotation);
        Debug.Log(
            $"[VFXForge VF-013] Gameplay evidence captured: {root}");
    }

    public static void ValidatePipelineCaptures()
    {
        ValidateCaptureDirectory(
            "Artifacts/dogfood/VF-013-primary/capture",
            6);
        ValidateCaptureDirectory(
            "Artifacts/dogfood/VF-013-variant/capture",
            6);
        ValidatePngDirectory(
            GetRepositoryPath("Dogfooding/Evidence/VF-013"),
            12);
    }

    private static void ValidateCaptureDirectory(
        string projectRelativePath,
        int expectedCount)
    {
        string root = Path.GetFullPath(Path.Combine(
            Application.dataPath,
            "..",
            projectRelativePath));
        ValidatePngDirectory(root, expectedCount);
    }

    private static void ValidatePngDirectory(
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
                $"Expected {expectedCount} pipeline frames, "
                + $"found {frames.Length}: {root}");
        }

        foreach (string frame in frames)
        {
            float ratio = MeasureForegroundRatio(frame);
            Debug.Log(
                "[VFXForge VF-013] Foreground ratio "
                + $"{Path.GetFileName(frame)}={ratio:P2}");
            if (ratio < 0.01f)
            {
                throw new InvalidOperationException(
                    "Capture foreground ratio is below 1%: "
                    + $"{Path.GetFileName(frame)}={ratio:P2}");
            }
        }
    }

    private static Mesh CreateShieldPanelMesh()
    {
        const int Columns = 8;
        const int Rows = 12;
        const float HalfWidth = 0.68f;
        const float Height = 3.6f;
        const float Forward = 2.55f;
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
                float taper = Mathf.Lerp(1f, 0.84f, v);
                float x = signed * HalfWidth * taper;
                float crown = v * 0.22f
                    * (1f - Mathf.Pow(Mathf.Abs(signed), 1.7f));
                float y = v * Height + crown;
                float z = Forward
                    + 0.12f * (1f - signed * signed);
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
                int lowerLeft = row * stride + column;
                int lowerRight = lowerLeft + 1;
                int upperLeft = lowerLeft + stride;
                int upperRight = upperLeft + 1;
                triangles.Add(lowerLeft);
                triangles.Add(upperLeft);
                triangles.Add(upperRight);
                triangles.Add(lowerLeft);
                triangles.Add(upperRight);
                triangles.Add(lowerRight);
            }
        }

        var mesh = new Mesh { name = "Giant Shield Curved Panel" };
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
                "Giant Shield panel bounds are not finite.");
        }
        return mesh;
    }

    private static Transform AddShieldPanel(
        Transform parent,
        int index,
        Mesh mesh,
        Material field,
        Material rim,
        ICollection<Renderer> renderers)
    {
        var panelObject = new GameObject($"Shield Panel {index}");
        panelObject.transform.SetParent(parent, false);
        panelObject.transform.localRotation =
            Quaternion.Euler(0f, (index - 2f) * 24f, 0f);
        renderers.Add(AddMeshRenderer(
            panelObject.transform,
            "Energy Field",
            mesh,
            field,
            Vector3.one,
            0));
        renderers.Add(AddMeshRenderer(
            panelObject.transform,
            "Platinum Rim",
            mesh,
            rim,
            new Vector3(1.012f, 1.012f, 1.012f),
            1));
        return panelObject.transform;
    }

    private static MeshRenderer AddMeshRenderer(
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

    private static Material CreateBarrierMaterial(
        string name,
        Shader shader,
        float rimMode)
    {
        var material = new Material(shader) { name = name };
        material.SetColor(
            "_PrimaryColor",
            new Color(0.086f, 0.85f, 1f, 1f));
        material.SetColor(
            "_SecondaryColor",
            new Color(0.957f, 1f, 1f, 1f));
        material.SetFloat("_Emission", 6.5f);
        material.SetFloat("_Sharpness", 0.78f);
        material.SetFloat("_RimMode", rimMode);
        return material;
    }

    private static ParticleSystem CreateAnchorBurst(
        Transform parent,
        Material material)
    {
        ParticleSystem system = CreateParticleSystem(
            parent,
            "Ground Anchor Burst",
            material,
            false,
            1.8f,
            0.52f,
            0.16f,
            1.35f,
            30);
        var emission = system.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });
        var shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 2.7f;
        shape.radiusThickness = 0.12f;
        system.transform.localPosition =
            new Vector3(0f, 0.05f, 2.15f);
        return system;
    }

    private static ParticleSystem CreateEdgeMotes(
        Transform parent,
        Material material)
    {
        ParticleSystem system = CreateParticleSystem(
            parent,
            "Shield Edge Motes",
            material,
            true,
            1.8f,
            0.66f,
            0.10f,
            0.18f,
            30);
        var emission = system.emission;
        emission.rateOverTime = 18f;
        var shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(5.5f, 3.4f, 0.28f);
        system.transform.localPosition =
            new Vector3(0f, 1.75f, 2.35f);
        return system;
    }

    private static ParticleSystem CreateDissolveShards(
        Transform parent,
        Material material)
    {
        ParticleSystem system = CreateParticleSystem(
            parent,
            "Dissolve Shards",
            material,
            false,
            0.45f,
            0.38f,
            0.14f,
            0.72f,
            36);
        var emission = system.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 36) });
        var shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(5.4f, 3.4f, 0.35f);
        system.transform.localPosition =
            new Vector3(0f, 1.7f, 2.35f);
        system.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear);
        return system;
    }

    private static ParticleSystem CreateParticleSystem(
        Transform parent,
        string name,
        Material material,
        bool loop,
        float duration,
        float lifetime,
        float startSize,
        float speed,
        int maxParticles)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        ParticleSystem system = child.AddComponent<ParticleSystem>();
        var main = system.main;
        main.loop = loop;
        main.playOnAwake = false;
        main.duration = duration;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = startSize;
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.03f, 0.5f, 1f, 0.12f),
            new Color(0.90f, 1f, 1f, 0.96f));
        var emission = system.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        var color = system.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(
                    new Color(0.04f, 0.58f, 1f),
                    1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.12f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = gradient;

        var size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(
            1f,
            AnimationCurve.EaseInOut(0f, 0.3f, 1f, 0f));

        ParticleSystemRenderer renderer =
            child.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 2.4f;
        renderer.velocityScale = 0.18f;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        return system;
    }

    private static Camera CreateGameplayCamera()
    {
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor =
            new Color(0.006f, 0.01f, 0.02f, 1f);
        camera.fieldOfView = 48f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 100f;
        camera.allowHDR = true;
        cameraObject.transform.position =
            new Vector3(0f, 4.8f, -9.5f);
        Vector3 target = new Vector3(0f, 1.45f, 1.25f);
        cameraObject.transform.rotation = Quaternion.LookRotation(
            target - cameraObject.transform.position,
            Vector3.up);
        UniversalAdditionalCameraData cameraData =
            camera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = true;
        cameraData.antialiasing =
            AntialiasingMode.FastApproximateAntialiasing;
        return camera;
    }

    private static void CreateBloomVolume()
    {
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        Bloom bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(0.9f);
        bloom.threshold.Override(0.72f);
        bloom.scatter.Override(0.65f);
        AssetDatabase.CreateAsset(profile, VolumeProfilePath);

        var volumeObject = new GameObject("Global VFX Volume");
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.sharedProfile = profile;
    }

    private static void CreateArena(
        Material groundMaterial,
        Material casterMaterial,
        Material projectileMaterial,
        Material accentMaterial)
    {
        GameObject ground =
            GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Arena Ground";
        ground.transform.position = new Vector3(0f, -0.2f, 1.6f);
        ground.transform.localScale =
            new Vector3(12f, 0.3f, 15f);
        ground.GetComponent<Renderer>().sharedMaterial =
            groundMaterial;
        UnityEngine.Object.DestroyImmediate(
            ground.GetComponent<Collider>());

        GameObject caster =
            GameObject.CreatePrimitive(PrimitiveType.Capsule);
        caster.name = "Shield Caster";
        caster.transform.position =
            new Vector3(0f, 0.75f, -1.65f);
        caster.transform.localScale =
            new Vector3(0.48f, 0.75f, 0.48f);
        caster.GetComponent<Renderer>().sharedMaterial =
            casterMaterial;
        UnityEngine.Object.DestroyImmediate(
            caster.GetComponent<Collider>());

        GameObject arm =
            GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.name = "Caster Focus Arm";
        arm.transform.SetParent(caster.transform, false);
        arm.transform.localPosition =
            new Vector3(0f, 0.22f, 0.78f);
        arm.transform.localRotation =
            Quaternion.Euler(70f, 0f, 0f);
        arm.transform.localScale =
            new Vector3(0.18f, 0.18f, 0.72f);
        arm.GetComponent<Renderer>().sharedMaterial =
            casterMaterial;
        UnityEngine.Object.DestroyImmediate(
            arm.GetComponent<Collider>());

        for (int index = 0; index < 3; index++)
        {
            float x = (index - 1) * 2.5f;
            GameObject projectile =
                GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = $"Incoming Projectile {index}";
            projectile.transform.position =
                new Vector3(x, 1.25f + index * 0.28f, 6.2f);
            projectile.transform.localScale =
                new Vector3(0.24f, 0.24f, 1.25f);
            projectile.GetComponent<Renderer>().sharedMaterial =
                projectileMaterial;
            UnityEngine.Object.DestroyImmediate(
                projectile.GetComponent<Collider>());
        }

        for (int index = 0; index < 6; index++)
        {
            float x = index < 3 ? -5.2f : 5.2f;
            float z = -1f + (index % 3) * 3.8f;
            GameObject pillar =
                GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = $"Arena Pillar {index}";
            pillar.transform.position = new Vector3(x, 0.65f, z);
            pillar.transform.localScale =
                new Vector3(0.38f, 0.65f, 0.38f);
            pillar.GetComponent<Renderer>().sharedMaterial =
                accentMaterial;
            UnityEngine.Object.DestroyImmediate(
                pillar.GetComponent<Collider>());
        }
    }

    private static Material CreateSavedUnlitMaterial(
        string name,
        Color color,
        string path)
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color");
        var material = new Material(shader) { name = name };
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static float MeasureForegroundRatio(string pngPath)
    {
        byte[] bytes = File.ReadAllBytes(pngPath);
        var texture = new Texture2D(
            2,
            2,
            TextureFormat.RGBA32,
            false);
        try
        {
            if (!texture.LoadImage(bytes, false))
            {
                throw new InvalidOperationException(
                    $"Could not decode capture: {pngPath}");
            }
            Color32[] pixels = texture.GetPixels32();
            Color32 background = pixels[0];
            int foreground = 0;
            foreach (Color32 pixel in pixels)
            {
                int difference = Math.Abs(pixel.r - background.r)
                    + Math.Abs(pixel.g - background.g)
                    + Math.Abs(pixel.b - background.b);
                if (difference >= 18)
                {
                    foreground++;
                }
            }
            return foreground / (float)pixels.Length;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static void WriteCameraPng(
        Camera camera,
        string path,
        int width,
        int height)
    {
        File.WriteAllBytes(
            path,
            RenderCameraPng(camera, width, height));
    }

    private static byte[] RenderCameraPng(
        Camera camera,
        int width,
        int height)
    {
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture target = null;
        Texture2D texture = null;
        try
        {
            target = RenderTexture.GetTemporary(
                width,
                height,
                24,
                RenderTextureFormat.ARGBHalf,
                RenderTextureReadWrite.sRGB);
            texture = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(
                new Rect(0f, 0f, width, height),
                0,
                0);
            texture.Apply(false, false);
            return texture.EncodeToPNG();
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            if (target != null)
            {
                RenderTexture.ReleaseTemporary(target);
            }
            if (texture != null)
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }

    private static void AddExposedParameters(
        string graphPath,
        IEnumerable<ExposedParameterSpec> specs)
    {
        Type resourceType =
            FindLoadedType("UnityEditor.VFX.VisualEffectResource");
        object resource = resourceType.GetMethod(
            "GetResourceAtPath",
            BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic)
            ?.Invoke(null, new object[] { graphPath });
        if (resource == null)
        {
            throw new InvalidOperationException(
                $"VFX resource could not be resolved: {graphPath}");
        }

        Type extensionType = FindLoadedType(
            "UnityEditor.VFX.VisualEffectResourceExtensions");
        object graph = extensionType.GetMethod(
            "GetOrCreateGraph",
            BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic)
            ?.Invoke(null, new[] { resource });
        if (graph == null)
        {
            throw new InvalidOperationException(
                "VFX Graph object could not be created.");
        }

        Type parameterType =
            FindLoadedType("UnityEditor.VFX.VFXParameter");
        MethodInfo initialize = parameterType.GetMethod(
            "Init",
            BindingFlags.Instance | BindingFlags.Public);
        PropertyInfo valueProperty = parameterType.GetProperty(
            "value",
            BindingFlags.Instance | BindingFlags.Public);
        MethodInfo addChild = graph.GetType().GetMethod(
            "AddChild",
            BindingFlags.Instance | BindingFlags.Public,
            null,
            new[]
            {
                FindLoadedType("UnityEditor.VFX.VFXModel"),
                typeof(int),
                typeof(bool)
            },
            null);
        if (initialize == null
            || valueProperty == null
            || addChild == null)
        {
            throw new InvalidOperationException(
                "Required VFX Graph authoring API is unavailable.");
        }

        int order = 0;
        foreach (ExposedParameterSpec spec in specs)
        {
            var parameter =
                ScriptableObject.CreateInstance(parameterType);
            parameter.name = spec.Name;
            initialize.Invoke(parameter, new object[] { spec.Type });
            valueProperty.SetValue(parameter, spec.Value);
            var serialized = new SerializedObject(parameter);
            serialized.FindProperty("m_ExposedName").stringValue =
                spec.Name;
            serialized.FindProperty("m_Exposed").boolValue = true;
            serialized.FindProperty("m_Order").intValue = order++;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            addChild.Invoke(
                graph,
                new object[] { parameter, -1, true });
        }

        graph.GetType().GetMethod(
                "BuildParameterInfo",
                BindingFlags.Instance | BindingFlags.Public)
            ?.Invoke(graph, Array.Empty<object>());
        MethodInfo write = extensionType.GetMethod(
            "WriteAssetWithSubAssets",
            BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic);
        write?.Invoke(null, new[] { resource });
        AssetDatabase.ImportAsset(
            graphPath,
            ImportAssetOptions.ForceSynchronousImport);
    }

    private static void AttachShaderGraph(
        string graphPath,
        string shaderGraphPath)
    {
        UnityEngine.Object shaderGraph =
            AssetDatabase.LoadAllAssetsAtPath(shaderGraphPath)
                .FirstOrDefault(
                    asset => asset != null
                        && asset.GetType().Name
                            == "ShaderGraphVfxAsset");
        if (shaderGraph == null)
        {
            throw new InvalidOperationException(
                "VFX Shader Graph import object is missing: "
                + shaderGraphPath);
        }

        int attached = 0;
        var pending = new Stack<object>();
        var visited = new HashSet<int>();
        pending.Push(ResolveGraphObject(graphPath));
        while (pending.Count > 0)
        {
            object model = pending.Pop();
            if (!(model is UnityEngine.Object graphObject)
                || graphObject == null
                || !visited.Add(graphObject.GetInstanceID()))
            {
                continue;
            }

            var serialized = new SerializedObject(graphObject);
            SerializedProperty property =
                serialized.FindProperty("shaderGraph");
            if (property != null
                && property.propertyType
                    == SerializedPropertyType.ObjectReference)
            {
                property.objectReferenceValue = shaderGraph;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(graphObject);
                attached++;
            }
            PushChildren(model, pending);
        }

        if (attached == 0)
        {
            throw new InvalidOperationException(
                "No VFX output accepted the project-owned Shader Graph.");
        }
        AssetDatabase.SaveAssets();
    }

    private static void SetParticleCapacity(
        string graphPath,
        int capacity)
    {
        int updated = 0;
        var pending = new Stack<object>();
        var visited = new HashSet<int>();
        pending.Push(ResolveGraphObject(graphPath));
        while (pending.Count > 0)
        {
            object model = pending.Pop();
            if (!(model is UnityEngine.Object graphObject)
                || graphObject == null
                || !visited.Add(graphObject.GetInstanceID()))
            {
                continue;
            }

            var serialized = new SerializedObject(graphObject);
            SerializedProperty property =
                serialized.FindProperty("capacity");
            if (property != null
                && property.propertyType
                    == SerializedPropertyType.Integer)
            {
                property.intValue = capacity;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(graphObject);
                updated++;
            }

            SerializedProperty dataReference =
                serialized.FindProperty("m_Data");
            if (dataReference != null
                && dataReference.propertyType
                    == SerializedPropertyType.ObjectReference
                && dataReference.objectReferenceValue != null)
            {
                pending.Push(dataReference.objectReferenceValue);
            }
            PushChildren(model, pending);
        }

        if (updated != 1)
        {
            throw new InvalidOperationException(
                "Expected one VFX particle capacity field, "
                + $"updated {updated}.");
        }
        AssetDatabase.SaveAssets();
    }

    private static void PushChildren(
        object model,
        Stack<object> pending)
    {
        PropertyInfo childrenProperty = model.GetType()
            .GetProperties(
                BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(
                candidate => candidate.Name == "children"
                    && candidate.GetIndexParameters().Length == 0);
        if (!(childrenProperty?.GetValue(model)
            is System.Collections.IEnumerable children))
        {
            return;
        }

        foreach (object child in children)
        {
            if (child != null)
            {
                pending.Push(child);
            }
        }
    }

    private static object ResolveGraphObject(string graphPath)
    {
        Type resourceType =
            FindLoadedType("UnityEditor.VFX.VisualEffectResource");
        object resource = resourceType.GetMethod(
            "GetResourceAtPath",
            BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic)
            ?.Invoke(null, new object[] { graphPath });
        if (resource == null)
        {
            throw new InvalidOperationException(
                $"VFX resource could not be resolved: {graphPath}");
        }

        Type extensionType = FindLoadedType(
            "UnityEditor.VFX.VisualEffectResourceExtensions");
        object graph = extensionType.GetMethod(
            "GetOrCreateGraph",
            BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic)
            ?.Invoke(null, new[] { resource });
        return graph ?? throw new InvalidOperationException(
            $"VFX Graph object could not be resolved: {graphPath}");
    }

    private static Type FindLoadedType(string fullName)
    {
        foreach (Assembly assembly
            in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(fullName, false);
            if (type != null)
            {
                return type;
            }
        }
        throw new TypeLoadException(
            $"Could not find loaded Unity type: {fullName}");
    }

    private static List<VfxPropertyBinding> CreateBindings()
    {
        return new List<VfxPropertyBinding>
        {
            Binding("seed", "RandomSeed", VfxPropertyType.Int),
            Binding(
                "timing.duration",
                "Duration",
                VfxPropertyType.Float),
            Binding(
                "timing.impact",
                "ImpactTime",
                VfxPropertyType.Float),
            Binding(
                "timing.sustain",
                "SustainTime",
                VfxPropertyType.Float),
            Binding(
                "timing.decay",
                "DecayTime",
                VfxPropertyType.Float),
            Binding(
                "shape.radius",
                "Radius",
                VfxPropertyType.Float),
            Binding(
                "shape.spreadAngle",
                "SpreadAngle",
                VfxPropertyType.Float),
            Binding(
                "shape.directionality",
                "Directionality",
                VfxPropertyType.Float),
            Binding(
                "style.primaryColor",
                "PrimaryColor",
                VfxPropertyType.Color),
            Binding(
                "style.secondaryColor",
                "SecondaryColor",
                VfxPropertyType.Color),
            Binding(
                "style.emissionIntensity",
                "EmissionIntensity",
                VfxPropertyType.Float),
            Binding(
                "style.sharpness",
                "Sharpness",
                VfxPropertyType.Float)
        };
    }

    private static VfxPropertyBinding Binding(
        string path,
        string property,
        VfxPropertyType type)
    {
        return new VfxPropertyBinding
        {
            recipePath = path,
            exposedPropertyName = property,
            propertyType = type,
            required = true,
            componentIndex = 0
        };
    }

    private static void VerifyExposedProperties(
        VisualEffectAsset graph)
    {
        var probeObject = new GameObject("VF-013 Property Probe");
        try
        {
            VisualEffect probe =
                probeObject.AddComponent<VisualEffect>();
            probe.visualEffectAsset = graph;
            foreach (ExposedParameterSpec spec in ExposedParameters)
            {
                bool found = spec.Type == typeof(float)
                    ? probe.HasFloat(spec.Name)
                    : spec.Type == typeof(int)
                        ? probe.HasInt(spec.Name)
                        : probe.HasVector4(spec.Name);
                if (!found)
                {
                    throw new InvalidOperationException(
                        "Required exposed VFX property is missing: "
                        + spec.Name);
                }
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(probeObject);
        }
    }

    private static void SetDefaultOverrides(VisualEffect effect)
    {
        effect.SetInt("RandomSeed", 130730);
        effect.SetFloat("Duration", 1.8f);
        effect.SetFloat("ImpactTime", 0.32f);
        effect.SetFloat("SustainTime", 1.08f);
        effect.SetFloat("DecayTime", 0.40f);
        effect.SetFloat("Radius", 3.2f);
        effect.SetFloat("SpreadAngle", 120f);
        effect.SetFloat("Directionality", 1f);
        effect.SetVector4(
            "PrimaryColor",
            new Color(0.086f, 0.85f, 1f, 1f));
        effect.SetVector4(
            "SecondaryColor",
            new Color(0.957f, 1f, 1f, 1f));
        effect.SetFloat("EmissionIntensity", 6.5f);
        effect.SetFloat("Sharpness", 0.78f);
    }

    private static void CopyProjectOwnedAsset(
        string source,
        string destination,
        string label)
    {
        if (!AssetDatabase.CopyAsset(source, destination))
        {
            throw new InvalidOperationException(
                $"Could not copy project-owned {label}: {source}");
        }
        AssetDatabase.ImportAsset(
            destination,
            ImportAssetOptions.ForceSynchronousImport);
    }

    private static void EnsureTargetsAbsent(params string[] paths)
    {
        foreach (string path in paths)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                path) != null)
            {
                throw new InvalidOperationException(
                    $"Refusing to overwrite existing Asset: {path}");
            }
        }
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int index = 1; index < parts.Length; index++)
        {
            string next = $"{current}/{parts[index]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[index]);
            }
            current = next;
        }
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes =
            new List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);
        if (!scenes.Exists(scene => scene.path == scenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }

    private static string GetRepositoryPath(string relativePath)
    {
        string repository = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../.."));
        return Path.Combine(repository, relativePath);
    }

    private static void RefuseExistingEvidence(
        string root,
        IEnumerable<string> files)
    {
        foreach (string file in files)
        {
            string path = Path.Combine(root, file);
            if (File.Exists(path) || Directory.Exists(path))
            {
                throw new InvalidOperationException(
                    $"Refusing to overwrite existing evidence: {path}");
            }
        }
    }

    private static bool IsFinite(Vector3 value)
    {
        return IsFinite(value.x)
            && IsFinite(value.y)
            && IsFinite(value.z);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private readonly struct ExposedParameterSpec
    {
        public ExposedParameterSpec(string name, Type type, object value)
        {
            Name = name;
            Type = type;
            Value = value;
        }

        public string Name { get; }
        public Type Type { get; }
        public object Value { get; }
    }
}
