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

public static class VfxForgeProductionCrescentDogfood
{
    private const string Root = "Assets/VFXForge/Dogfood/ProductionCrescentSlash";
    private const string AuthoringRoot = Root + "/Authoring";
    private const string DemoRoot = Root + "/Demo";
    private const string GeneratedRoot = Root + "/Generated";
    private const string GraphPath = AuthoringRoot + "/ProductionCrescentSlash.vfx";
    private const string ShaderGraphPath = AuthoringRoot + "/ProductionCrescentVfx.shadergraph";
    private const string OuterMeshPath = AuthoringRoot + "/ProductionCrescentOuter.asset";
    private const string CoreMeshPath = AuthoringRoot + "/ProductionCrescentCore.asset";
    private const string HighlightMeshPath = AuthoringRoot + "/ProductionCrescentHighlight.asset";
    private const string OuterMaterialPath = AuthoringRoot + "/ProductionCrescentOuter.mat";
    private const string CoreMaterialPath = AuthoringRoot + "/ProductionCrescentCore.mat";
    private const string HighlightMaterialPath = AuthoringRoot + "/ProductionCrescentHighlight.mat";
    private const string ParticleMaterialPath = AuthoringRoot + "/ProductionCrescentParticle.mat";
    private const string TemplatePath = AuthoringRoot + "/ProductionCrescentTemplate.prefab";
    private const string CatalogPath = Root + "/ProductionCrescentCatalog.asset";
    private const string GeneratedPrefabPath = GeneratedRoot + "/ProductionCrescentSlash.prefab";
    private const string DemoScenePath = DemoRoot + "/ProductionCrescentDemo.unity";
    private const string VolumeProfilePath = DemoRoot + "/ProductionCrescentVolume.asset";
    private const string SourceGraphPath =
        "Packages/com.unity.visualeffectgraph/Editor/Templates/03_Simple_Burst.vfx";
    private const string SourceShaderGraphPath =
        "Packages/com.unity.visualeffectgraph/ShaderGraph/0_VFXGraph Unlit.shadergraph";

    private static readonly ExposedParameterSpec[] ExposedParameters =
    {
        new ExposedParameterSpec("RandomSeed", typeof(int), 120729),
        new ExposedParameterSpec("Duration", typeof(float), 0.52f),
        new ExposedParameterSpec("ImpactTime", typeof(float), 0.08f),
        new ExposedParameterSpec("SustainTime", typeof(float), 0.24f),
        new ExposedParameterSpec("DecayTime", typeof(float), 0.20f),
        new ExposedParameterSpec("Radius", typeof(float), 1.65f),
        new ExposedParameterSpec("SpreadAngle", typeof(float), 140f),
        new ExposedParameterSpec("Directionality", typeof(float), 1f),
        new ExposedParameterSpec("PrimaryColor", typeof(Color), new Color(0.07f, 0.85f, 1f, 1f)),
        new ExposedParameterSpec("SecondaryColor", typeof(Color), new Color(0.91f, 1f, 1f, 1f)),
        new ExposedParameterSpec("EmissionIntensity", typeof(float), 5.5f),
        new ExposedParameterSpec("Sharpness", typeof(float), 0.82f)
    };

    [MenuItem("Tools/VFX Forge/Dogfood/Build VF-012 Production Crescent")]
    public static void BuildAuthoringAssets()
    {
        EnsureTargetsAbsent(
            GraphPath,
            ShaderGraphPath,
            OuterMeshPath,
            CoreMeshPath,
            HighlightMeshPath,
            OuterMaterialPath,
            CoreMaterialPath,
            HighlightMaterialPath,
            ParticleMaterialPath,
            TemplatePath,
            CatalogPath);
        EnsureFolder(AuthoringRoot);
        EnsureFolder(GeneratedRoot);

        CopyProjectOwnedAsset(SourceGraphPath, GraphPath, "VFX Graph");
        CopyProjectOwnedAsset(SourceShaderGraphPath, ShaderGraphPath, "VFX Shader Graph");
        AddExposedParameters(GraphPath, ExposedParameters);
        AttachShaderGraph(GraphPath, ShaderGraphPath);
        SetParticleCapacity(GraphPath, 64);
        AssetDatabase.ImportAsset(GraphPath, ImportAssetOptions.ForceSynchronousImport);

        Mesh outerMesh = CreateTaperedCrescentMesh("Production Crescent Outer", 0.49f, 1f, 70f, 42, 1.12f);
        Mesh coreMesh = CreateTaperedCrescentMesh("Production Crescent Core", 0.66f, 0.94f, 67f, 42, 1.35f);
        Mesh highlightMesh = CreateTaperedCrescentMesh("Production Crescent Highlight", 0.76f, 0.91f, 62f, 38, 1.65f);
        AssetDatabase.CreateAsset(outerMesh, OuterMeshPath);
        AssetDatabase.CreateAsset(coreMesh, CoreMeshPath);
        AssetDatabase.CreateAsset(highlightMesh, HighlightMeshPath);

        Shader bodyShader = Shader.Find("VFXForge/Dogfood/ProductionCrescentSlash");
        if (bodyShader == null)
        {
            throw new InvalidOperationException("Production Crescent Shader did not import.");
        }

        Material outerMaterial = CreateBodyMaterial(
            "Production Crescent Outer",
            bodyShader,
            new Color(0.02f, 0.42f, 0.88f, 0.72f),
            new Color(0.16f, 0.92f, 1f, 0.92f),
            3.2f,
            0.55f,
            0.32f);
        Material coreMaterial = CreateBodyMaterial(
            "Production Crescent Core",
            bodyShader,
            new Color(0.10f, 0.82f, 1f, 0.94f),
            new Color(0.92f, 1f, 1f, 1f),
            5.5f,
            0.80f,
            0.18f);
        Material highlightMaterial = CreateBodyMaterial(
            "Production Crescent Highlight",
            bodyShader,
            new Color(0.70f, 0.98f, 1f, 1f),
            Color.white,
            7.0f,
            0.92f,
            0.10f);
        Material particleMaterial = CreateParticleMaterial();
        AssetDatabase.CreateAsset(outerMaterial, OuterMaterialPath);
        AssetDatabase.CreateAsset(coreMaterial, CoreMaterialPath);
        AssetDatabase.CreateAsset(highlightMaterial, HighlightMaterialPath);
        AssetDatabase.CreateAsset(particleMaterial, ParticleMaterialPath);

        VisualEffectAsset graph = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(GraphPath);
        if (graph == null)
        {
            throw new InvalidOperationException($"Production VFX Graph failed to load: {GraphPath}");
        }

        GameObject source = new GameObject("Production Crescent Template");
        try
        {
            VfxPlayer player = source.AddComponent<VfxPlayer>();
            player.Configure("OnPlay");

            var bodyRoot = new GameObject("Animated Crescent Body");
            bodyRoot.transform.SetParent(source.transform, false);
            Renderer outer = AddMeshChild(bodyRoot.transform, "Outer Energy", outerMesh, outerMaterial, 0.28f);
            Renderer core = AddMeshChild(bodyRoot.transform, "White Hot Core", coreMesh, coreMaterial, 0.34f);
            Renderer highlight = AddMeshChild(bodyRoot.transform, "Leading Highlight", highlightMesh, highlightMaterial, 0.40f);

            var graphObject = new GameObject("VFX Graph Sparks");
            graphObject.transform.SetParent(source.transform, false);
            graphObject.transform.localPosition = new Vector3(0f, 0.44f, 0.62f);
            graphObject.transform.localScale = Vector3.one * 0.14f;
            VisualEffect effect = graphObject.AddComponent<VisualEffect>();
            effect.visualEffectAsset = graph;
            effect.initialEventName = "OnPlay";
            effect.startSeed = 120729u;
            effect.resetSeedOnPlay = false;
            effect.enabled = true;
            SetDefaultOverrides(effect);

            ParticleSystem leading = CreateLeadingSparks(source.transform, particleMaterial);
            ParticleSystem trailing = CreateTrailingWisps(source.transform, particleMaterial);
            ParticleSystem dissipate = CreateDissipateBurst(source.transform, particleMaterial);

            ProductionCrescentSlash controller = source.AddComponent<ProductionCrescentSlash>();
            controller.Configure(
                effect,
                bodyRoot.transform,
                new[] { outer, core, highlight },
                leading,
                trailing,
                dissipate,
                11f);

            GameObject template = PrefabUtility.SaveAsPrefabAsset(source, TemplatePath);
            if (template == null)
            {
                throw new InvalidOperationException($"Template Prefab could not be saved: {TemplatePath}");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
        }

        var catalog = ScriptableObject.CreateInstance<VfxTemplateCatalog>();
        catalog.templates.Add(new VfxTemplateEntry
        {
            id = "production_crescent_slash_v1",
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TemplatePath),
            playEventName = "OnPlay",
            supportedLayers = new[]
            {
                "slash_core", "slash_glow", "leading_sparks", "trailing_wisps", "dissipate_burst"
            },
            bindings = CreateBindings()
        });
        AssetDatabase.CreateAsset(catalog, CatalogPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        VerifyExposedProperties(graph);
        List<VfxValidationResult> catalogResults = VfxTemplateCatalogValidator.Validate(catalog);
        VfxValidationResult catalogError = catalogResults.FirstOrDefault(result => result.severity == VfxValidationSeverity.Error);
        if (catalogError != null)
        {
            throw new InvalidOperationException($"Production Catalog failed validation: {catalogError.ruleId} {catalogError.message}");
        }

        Debug.Log(
            $"[VFXForge VF-012] Production authoring assets created. "
            + $"Graph={GraphPath}, ShaderGraph={ShaderGraphPath}, Bindings={ExposedParameters.Length}.");
    }

    [MenuItem("Tools/VFX Forge/Dogfood/Create VF-012 Gameplay Demo")]
    public static void CreateDemoScene()
    {
        EnsureTargetsAbsent(DemoScenePath, VolumeProfilePath);
        EnsureFolder(DemoRoot);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GeneratedPrefabPath);
        if (prefab == null)
        {
            throw new InvalidOperationException($"Generated Prefab is missing: {GeneratedPrefabPath}");
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera camera = CreateGameplayCamera();
        CreateBloomVolume();

        float[] laneX = { -4f, 0f, 4f };
        Color[] groundColors =
        {
            new Color(0.025f, 0.035f, 0.06f, 1f),
            new Color(0.19f, 0.23f, 0.30f, 1f),
            new Color(0.66f, 0.70f, 0.72f, 1f)
        };
        var spawnPositions = new List<Vector3>();
        for (int index = 0; index < laneX.Length; index++)
        {
            CreateGameplayLane(laneX[index], groundColors[index], index);
            spawnPositions.Add(new Vector3(laneX[index], 0f, -3.4f));
        }

        var controllerObject = new GameObject("Production Crescent Demo Controller");
        ProductionCrescentDemoController demo = controllerObject.AddComponent<ProductionCrescentDemoController>();
        demo.Configure(prefab, spawnPositions, 1.1f);

        if (!EditorSceneManager.SaveScene(scene, DemoScenePath))
        {
            throw new InvalidOperationException($"Demo Scene could not be saved: {DemoScenePath}");
        }
        AddSceneToBuildSettings(DemoScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[VFXForge VF-012] Gameplay-context demo created: {DemoScenePath}, Camera={camera.name}.");
    }

    [MenuItem("Tools/VFX Forge/Dogfood/Open VF-012 Gameplay Demo")]
    public static void OpenDemoScene()
    {
        EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
    }

    public static void CaptureDemoEvidence()
    {
        EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
        Camera camera = Camera.main;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GeneratedPrefabPath);
        if (camera == null || prefab == null)
        {
            throw new InvalidOperationException("Gameplay Camera or generated Prefab is missing.");
        }

        string evidenceRoot = GetRepositoryPath("Dogfooding/Evidence/VF-012");
        string[] expectedFiles =
        {
            "peak-dark.png", "peak-mid.png", "peak-bright.png",
            "sequence-002.png", "sequence-008.png", "sequence-018.png", "sequence-032.png", "sequence-048.png"
        };
        RefuseExistingEvidence(evidenceRoot, expectedFiles);
        Directory.CreateDirectory(evidenceRoot);

        float[] laneX = { -4f, 0f, 4f };
        string[] laneNames = { "dark", "mid", "bright" };
        for (int index = 0; index < laneX.Length; index++)
        {
            GameObject instance = InstantiateForEvidence(prefab, laneX[index], 0.18f);
            WriteCameraPng(camera, Path.Combine(evidenceRoot, $"peak-{laneNames[index]}.png"), 1280, 720);
            UnityEngine.Object.DestroyImmediate(instance);
        }

        float[] times = { 0.02f, 0.08f, 0.18f, 0.32f, 0.48f };
        string[] suffixes = { "002", "008", "018", "032", "048" };
        for (int index = 0; index < times.Length; index++)
        {
            GameObject instance = InstantiateForEvidence(prefab, 0f, times[index]);
            WriteCameraPng(camera, Path.Combine(evidenceRoot, $"sequence-{suffixes[index]}.png"), 1280, 720);
            UnityEngine.Object.DestroyImmediate(instance);
        }

        Debug.Log($"[VFXForge VF-012] Gameplay evidence captured: {evidenceRoot}");
    }

    public static void ValidatePipelineCaptures()
    {
        string captureRoot = Path.GetFullPath(Path.Combine(
            Application.dataPath,
            "../Artifacts/dogfood/VF-012-production-final-v7/capture"));
        string[] frames = Directory.Exists(captureRoot)
            ? Directory.GetFiles(captureRoot, "*.png").OrderBy(path => path, StringComparer.Ordinal).ToArray()
            : Array.Empty<string>();
        if (frames.Length != 5)
        {
            throw new InvalidOperationException($"Expected five pipeline frames, found {frames.Length}: {captureRoot}");
        }

        foreach (string frame in frames)
        {
            float ratio = MeasureForegroundRatio(frame);
            Debug.Log($"[VFXForge VF-012] Foreground ratio {Path.GetFileName(frame)}={ratio:P2}");
            if (ratio < 0.01f)
            {
                throw new InvalidOperationException(
                    $"Capture foreground ratio is below 1%: {Path.GetFileName(frame)}={ratio:P2}");
            }
        }
    }

    private static void AddExposedParameters(string graphPath, IEnumerable<ExposedParameterSpec> specs)
    {
        Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(candidate => candidate.GetName().Name == "Unity.VisualEffectGraph.Editor");
        if (assembly == null)
        {
            throw new InvalidOperationException("Unity.VisualEffectGraph.Editor assembly is unavailable.");
        }

        Type resourceType = FindLoadedType("UnityEditor.VFX.VisualEffectResource");
        object resource = resourceType.GetMethod(
            "GetResourceAtPath",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?.Invoke(null, new object[] { graphPath });
        if (resource == null)
        {
            throw new InvalidOperationException($"VFX resource could not be resolved: {graphPath}");
        }

        Type extensionType = FindLoadedType("UnityEditor.VFX.VisualEffectResourceExtensions");
        MethodInfo getGraph = extensionType.GetMethod(
            "GetOrCreateGraph",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        object graph = getGraph?.Invoke(null, new[] { resource });
        if (graph == null)
        {
            throw new InvalidOperationException("VFX Graph object could not be created.");
        }

        Type parameterType = FindLoadedType("UnityEditor.VFX.VFXParameter");
        MethodInfo initialize = parameterType.GetMethod("Init", BindingFlags.Instance | BindingFlags.Public);
        PropertyInfo valueProperty = parameterType.GetProperty("value", BindingFlags.Instance | BindingFlags.Public);
        MethodInfo addChild = graph.GetType().GetMethod(
            "AddChild",
            BindingFlags.Instance | BindingFlags.Public,
            null,
            new[] { FindLoadedType("UnityEditor.VFX.VFXModel"), typeof(int), typeof(bool) },
            null);
        if (initialize == null || valueProperty == null || addChild == null)
        {
            throw new InvalidOperationException("Required VFX Graph authoring API is unavailable.");
        }

        int order = 0;
        foreach (ExposedParameterSpec spec in specs)
        {
            var parameter = ScriptableObject.CreateInstance(parameterType);
            parameter.name = spec.Name;
            initialize.Invoke(parameter, new object[] { spec.Type });
            valueProperty.SetValue(parameter, spec.Value);
            var serialized = new SerializedObject(parameter);
            serialized.FindProperty("m_ExposedName").stringValue = spec.Name;
            serialized.FindProperty("m_Exposed").boolValue = true;
            serialized.FindProperty("m_Order").intValue = order++;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            addChild.Invoke(graph, new object[] { parameter, -1, true });
        }

        graph.GetType().GetMethod("BuildParameterInfo", BindingFlags.Instance | BindingFlags.Public)
            ?.Invoke(graph, Array.Empty<object>());
        MethodInfo write = extensionType.GetMethod(
            "WriteAssetWithSubAssets",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        write?.Invoke(null, new[] { resource });
        AssetDatabase.ImportAsset(graphPath, ImportAssetOptions.ForceSynchronousImport);
    }

    private static void AttachShaderGraph(string graphPath, string shaderGraphPath)
    {
        UnityEngine.Object shaderGraph = AssetDatabase.LoadAllAssetsAtPath(shaderGraphPath)
            .FirstOrDefault(asset => asset != null && asset.GetType().Name == "ShaderGraphVfxAsset");
        if (shaderGraph == null)
        {
            throw new InvalidOperationException($"VFX Shader Graph import object is missing: {shaderGraphPath}");
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
            SerializedProperty property = serialized.FindProperty("shaderGraph");
            if (property != null && property.propertyType == SerializedPropertyType.ObjectReference)
            {
                property.objectReferenceValue = shaderGraph;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(graphObject);
                attached++;
            }

            PropertyInfo childrenProperty = model.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(candidate => candidate.Name == "children"
                    && candidate.GetIndexParameters().Length == 0);
            if (childrenProperty?.GetValue(model) is System.Collections.IEnumerable children)
            {
                foreach (object child in children)
                {
                    if (child != null)
                    {
                        pending.Push(child);
                    }
                }
            }
        }

        if (attached == 0)
        {
            throw new InvalidOperationException("No VFX output accepted the project-owned Shader Graph.");
        }
        AssetDatabase.SaveAssets();
    }

    private static void SetParticleCapacity(string graphPath, int capacity)
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
            SerializedProperty property = serialized.FindProperty("capacity");
            if (property != null
                && property.propertyType == SerializedPropertyType.Integer)
            {
                property.intValue = capacity;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(graphObject);
                updated++;
            }

            SerializedProperty dataReference = serialized.FindProperty("m_Data");
            if (dataReference != null
                && dataReference.propertyType
                    == SerializedPropertyType.ObjectReference
                && dataReference.objectReferenceValue != null)
            {
                pending.Push(dataReference.objectReferenceValue);
            }

            PropertyInfo childrenProperty = model.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(candidate => candidate.Name == "children"
                    && candidate.GetIndexParameters().Length == 0);
            if (childrenProperty?.GetValue(model) is System.Collections.IEnumerable children)
            {
                foreach (object child in children)
                {
                    if (child != null)
                    {
                        pending.Push(child);
                    }
                }
            }
        }

        if (updated != 1)
        {
            throw new InvalidOperationException(
                $"Expected one VFX particle capacity field, updated {updated}.");
        }
        AssetDatabase.SaveAssets();
    }

    private static object ResolveGraphObject(string graphPath)
    {
        Type resourceType = FindLoadedType("UnityEditor.VFX.VisualEffectResource");
        object resource = resourceType.GetMethod(
            "GetResourceAtPath",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?.Invoke(null, new object[] { graphPath });
        if (resource == null)
        {
            throw new InvalidOperationException($"VFX resource could not be resolved: {graphPath}");
        }
        Type extensionType = FindLoadedType("UnityEditor.VFX.VisualEffectResourceExtensions");
        object graph = extensionType.GetMethod(
            "GetOrCreateGraph",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?.Invoke(null, new[] { resource });
        return graph ?? throw new InvalidOperationException($"VFX Graph object could not be resolved: {graphPath}");
    }

    private static Type FindLoadedType(string fullName)
    {
        foreach (Assembly loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = loadedAssembly.GetType(fullName, false);
            if (type != null)
            {
                return type;
            }
        }
        throw new TypeLoadException($"Could not find loaded Unity type: {fullName}");
    }

    private static List<VfxPropertyBinding> CreateBindings()
    {
        return new List<VfxPropertyBinding>
        {
            Binding("seed", "RandomSeed", VfxPropertyType.Int),
            Binding("timing.duration", "Duration", VfxPropertyType.Float),
            Binding("timing.impact", "ImpactTime", VfxPropertyType.Float),
            Binding("timing.sustain", "SustainTime", VfxPropertyType.Float),
            Binding("timing.decay", "DecayTime", VfxPropertyType.Float),
            Binding("shape.radius", "Radius", VfxPropertyType.Float),
            Binding("shape.spreadAngle", "SpreadAngle", VfxPropertyType.Float),
            Binding("shape.directionality", "Directionality", VfxPropertyType.Float),
            Binding("style.primaryColor", "PrimaryColor", VfxPropertyType.Color),
            Binding("style.secondaryColor", "SecondaryColor", VfxPropertyType.Color),
            Binding("style.emissionIntensity", "EmissionIntensity", VfxPropertyType.Float),
            Binding("style.sharpness", "Sharpness", VfxPropertyType.Float)
        };
    }

    private static VfxPropertyBinding Binding(string path, string property, VfxPropertyType type)
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

    private static void VerifyExposedProperties(VisualEffectAsset graph)
    {
        var probeObject = new GameObject("VF-012 Property Probe");
        try
        {
            VisualEffect probe = probeObject.AddComponent<VisualEffect>();
            probe.visualEffectAsset = graph;
            foreach (ExposedParameterSpec spec in ExposedParameters)
            {
                bool found = spec.Type == typeof(float) ? probe.HasFloat(spec.Name)
                    : spec.Type == typeof(int) ? probe.HasInt(spec.Name)
                    : probe.HasVector4(spec.Name);
                if (!found)
                {
                    throw new InvalidOperationException($"Required exposed VFX property is missing: {spec.Name}");
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
        effect.SetInt("RandomSeed", 120729);
        effect.SetFloat("Duration", 0.52f);
        effect.SetFloat("ImpactTime", 0.08f);
        effect.SetFloat("SustainTime", 0.24f);
        effect.SetFloat("DecayTime", 0.20f);
        effect.SetFloat("Radius", 1.65f);
        effect.SetFloat("SpreadAngle", 140f);
        effect.SetFloat("Directionality", 1f);
        effect.SetVector4("PrimaryColor", new Color(0.07f, 0.85f, 1f, 1f));
        effect.SetVector4("SecondaryColor", new Color(0.91f, 1f, 1f, 1f));
        effect.SetFloat("EmissionIntensity", 5.5f);
        effect.SetFloat("Sharpness", 0.82f);
    }

    private static Mesh CreateTaperedCrescentMesh(
        string name,
        float innerRadius,
        float outerRadius,
        float halfAngle,
        int segments,
        float taperPower)
    {
        var vertices = new Vector3[(segments + 1) * 2];
        var uv = new Vector2[vertices.Length];
        var colors = new Color[vertices.Length];
        var triangles = new int[segments * 6];
        float width = outerRadius - innerRadius;

        for (int index = 0; index <= segments; index++)
        {
            float progress = index / (float)segments;
            float angleProgress = Mathf.Pow(progress, 0.92f);
            float angle = Mathf.Lerp(-halfAngle * 0.86f, halfAngle, angleProgress) * Mathf.Deg2Rad;
            float taperBase = Mathf.Max(0f, Mathf.Sin(progress * Mathf.PI));
            float taper = Mathf.Pow(taperBase, taperPower);
            float asymmetry = Mathf.Lerp(0.70f, 1.08f, progress);
            float localWidth = width * Mathf.Max(0.018f, taper) * asymmetry;
            float centerRadius = Mathf.Lerp(innerRadius + width * 0.42f, outerRadius - width * 0.28f, progress);
            float inner = centerRadius - localWidth * 0.5f;
            float outer = centerRadius + localWidth * 0.5f;
            Vector3 direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            int vertex = index * 2;
            vertices[vertex] = direction * inner;
            vertices[vertex + 1] = direction * outer;
            uv[vertex] = new Vector2(progress, 0f);
            uv[vertex + 1] = new Vector2(progress, 1f);
            colors[vertex] = Color.white;
            colors[vertex + 1] = Color.white;

            if (index == segments)
            {
                continue;
            }
            int triangle = index * 6;
            int next = vertex + 2;
            triangles[triangle] = vertex;
            triangles[triangle + 1] = next;
            triangles[triangle + 2] = vertex + 1;
            triangles[triangle + 3] = vertex + 1;
            triangles[triangle + 4] = next;
            triangles[triangle + 5] = next + 1;
        }

        var mesh = new Mesh { name = name, vertices = vertices, uv = uv, colors = colors, triangles = triangles };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Material CreateBodyMaterial(
        string name,
        Shader shader,
        Color primary,
        Color secondary,
        float emission,
        float sharpness,
        float noise)
    {
        var material = new Material(shader) { name = name };
        material.SetColor("_PrimaryColor", primary);
        material.SetColor("_SecondaryColor", secondary);
        material.SetFloat("_Emission", emission);
        material.SetFloat("_Sharpness", sharpness);
        material.SetFloat("_NoiseStrength", noise);
        material.SetFloat("_Age01", 0.22f);
        material.SetFloat("_LayerAlpha", 1f);
        return material;
    }

    private static Material CreateParticleMaterial()
    {
        Shader shader = Shader.Find("VFXForge/Dogfood/ProductionCrescentParticle");
        if (shader == null)
        {
            throw new InvalidOperationException(
                "Production crescent particle Shader is unavailable.");
        }
        var material = new Material(shader) { name = "Production Crescent Particle" };
        material.SetColor("_Tint", new Color(0.16f, 0.9f, 1f, 0.82f));
        material.SetFloat("_Softness", 3.4f);
        material.renderQueue = 3000;
        return material;
    }

    private static Renderer AddMeshChild(Transform parent, string name, Mesh mesh, Material material, float height)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = new Vector3(0f, height, 0f);
        child.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = child.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return renderer;
    }

    private static ParticleSystem CreateLeadingSparks(Transform parent, Material material)
    {
        ParticleSystem system = CreateParticleSystem(parent, "Leading Sparks", material, 0.16f, 0.06f, 1.3f, 18);
        var emission = system.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });
        var shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.18f;
        system.transform.localPosition = new Vector3(0f, 0.43f, 0.72f);
        return system;
    }

    private static ParticleSystem CreateTrailingWisps(Transform parent, Material material)
    {
        ParticleSystem system = CreateParticleSystem(parent, "Trailing Wisps", material, 0.28f, 0.11f, 0.22f, 24);
        var emission = system.emission;
        emission.rateOverTime = 44f;
        var shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(1.3f, 0.05f, 0.18f);
        system.transform.localPosition = new Vector3(0f, 0.32f, -0.12f);
        return system;
    }

    private static ParticleSystem CreateDissipateBurst(Transform parent, Material material)
    {
        ParticleSystem system = CreateParticleSystem(parent, "Dissipate Burst", material, 0.22f, 0.08f, 0.65f, 12);
        var emission = system.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });
        var shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.9f;
        system.transform.localPosition = new Vector3(0f, 0.35f, 0.2f);
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return system;
    }

    private static ParticleSystem CreateParticleSystem(
        Transform parent,
        string name,
        Material material,
        float lifetime,
        float size,
        float speed,
        int maxParticles)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        ParticleSystem system = child.AddComponent<ParticleSystem>();
        var main = system.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.52f;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = size;
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.05f, 0.52f, 1f, 0.25f),
            new Color(0.85f, 1f, 1f, 0.95f));
        var emission = system.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        var color = system.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.02f, 0.55f, 1f), 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
        ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 2.2f;
        renderer.velocityScale = 0.18f;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        return system;
    }

    private static Camera CreateGameplayCamera()
    {
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.2f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.008f, 0.012f, 0.02f, 1f);
        camera.allowHDR = true;
        UniversalAdditionalCameraData cameraData =
            camera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = true;
        cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        cameraObject.transform.position = new Vector3(0f, 14f, 1.2f);
        cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        return camera;
    }

    private static void CreateBloomVolume()
    {
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        Bloom bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(0.85f);
        bloom.threshold.Override(0.75f);
        bloom.scatter.Override(0.62f);
        AssetDatabase.CreateAsset(profile, VolumeProfilePath);
        var volumeObject = new GameObject("Global VFX Volume");
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.sharedProfile = profile;
    }

    private static void CreateGameplayLane(float x, Color groundColor, int index)
    {
        Material groundMaterial = CreateUnlitMaterial($"Lane {index} Ground", groundColor);
        string materialPath = $"{DemoRoot}/Lane{index}Ground.mat";
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(materialPath) != null)
        {
            throw new InvalidOperationException($"Demo material already exists: {materialPath}");
        }
        AssetDatabase.CreateAsset(groundMaterial, materialPath);
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = $"Lane {index} Ground";
        ground.transform.position = new Vector3(x, -0.18f, 0.5f);
        ground.transform.localScale = new Vector3(3.65f, 0.25f, 10f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
        UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());

        GameObject caster = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        caster.name = $"Caster {index}";
        caster.transform.position = new Vector3(x, 0.55f, -3.75f);
        caster.transform.localScale = new Vector3(0.38f, 0.62f, 0.38f);
        caster.GetComponent<Renderer>().sharedMaterial = CreateRuntimeMaterial(
            new Color(0.11f, 0.16f, 0.24f, 1f));
        UnityEngine.Object.DestroyImmediate(caster.GetComponent<Collider>());

        GameObject sword = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sword.name = $"Sword {index}";
        sword.transform.SetParent(caster.transform, false);
        sword.transform.localPosition = new Vector3(0.75f, 0f, 0.3f);
        sword.transform.localRotation = Quaternion.Euler(0f, 24f, -28f);
        sword.transform.localScale = new Vector3(0.10f, 0.12f, 1.15f);
        sword.GetComponent<Renderer>().sharedMaterial = CreateRuntimeMaterial(Color.white);
        UnityEngine.Object.DestroyImmediate(sword.GetComponent<Collider>());

        for (int targetIndex = 0; targetIndex < 2; targetIndex++)
        {
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            target.name = $"Target {index}-{targetIndex}";
            target.transform.position = new Vector3(x + (targetIndex == 0 ? -0.65f : 0.65f), 0.45f, 3.1f);
            target.transform.localScale = new Vector3(0.34f, 0.45f, 0.34f);
            target.GetComponent<Renderer>().sharedMaterial = CreateRuntimeMaterial(
                new Color(0.48f, 0.12f, 0.16f, 1f));
            UnityEngine.Object.DestroyImmediate(target.GetComponent<Collider>());
        }
    }

    private static Material CreateRuntimeMaterial(Color color)
    {
        return CreateUnlitMaterial("Demo Runtime Material", color);
    }

    private static Material CreateUnlitMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        var material = new Material(shader) { name = name };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        return material;
    }

    private static GameObject InstantiateForEvidence(GameObject prefab, float x, float time)
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            throw new InvalidOperationException("Could not instantiate production Prefab for evidence.");
        }
        ProductionCrescentSlash controller = instance.GetComponent<ProductionCrescentSlash>();
        instance.transform.position = new Vector3(x, 0f, -3.4f + 11f * time);
        controller?.EvaluatePreviewTime(time);
        return instance;
    }

    private static float MeasureForegroundRatio(string pngPath)
    {
        byte[] bytes = File.ReadAllBytes(pngPath);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!texture.LoadImage(bytes, false))
            {
                throw new InvalidOperationException($"Could not decode capture: {pngPath}");
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

    private static void WriteCameraPng(Camera camera, string path, int width, int height)
    {
        File.WriteAllBytes(path, RenderCameraPng(camera, width, height));
    }

    private static byte[] RenderCameraPng(Camera camera, int width, int height)
    {
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture target = null;
        Texture2D texture = null;
        try
        {
            target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.sRGB);
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            texture.Apply(false, false);
            return texture.EncodeToPNG();
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            if (target != null) RenderTexture.ReleaseTemporary(target);
            if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static void CopyProjectOwnedAsset(string source, string destination, string label)
    {
        if (!AssetDatabase.CopyAsset(source, destination))
        {
            throw new InvalidOperationException($"Could not copy project-owned {label}: {source}");
        }
        AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
    }

    private static void EnsureTargetsAbsent(params string[] paths)
    {
        foreach (string path in paths)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
            {
                throw new InvalidOperationException($"Refusing to overwrite existing Asset: {path}");
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
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!scenes.Exists(scene => scene.path == scenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }

    private static string GetRepositoryPath(string relativePath)
    {
        string repository = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
        return Path.Combine(repository, relativePath);
    }

    private static void RefuseExistingEvidence(string root, IEnumerable<string> files)
    {
        foreach (string file in files)
        {
            string path = Path.Combine(root, file);
            if (File.Exists(path) || Directory.Exists(path))
            {
                throw new InvalidOperationException($"Refusing to overwrite existing evidence: {path}");
            }
        }
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
