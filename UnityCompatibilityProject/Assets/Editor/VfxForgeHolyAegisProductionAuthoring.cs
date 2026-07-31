using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kubonsang.VfxForge;
using Kubonsang.VfxForge.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;
using VfxForge.Dogfood;

public static class VfxForgeHolyAegisProductionAuthoring
{
    private const string Root =
        "Assets/VFXForge/Dogfood/HolyAegisV3";
    private const string ProductionRoot =
        Root + "/Authoring/Production";
    private const string GeneratedRoot =
        Root + "/Generated";
    private const string SilhouetteRoot =
        Root + "/Authoring/Silhouette";
    private const string TemplatePath =
        ProductionRoot + "/HolyAegisV3Template.prefab";
    private const string GraphPath =
        ProductionRoot + "/HolyAegisPlayback.vfx";
    private const string ShaderGraphPath =
        ProductionRoot + "/HolyAegisPlayback.shadergraph";
    private const string EnergyMaterialPath =
        ProductionRoot + "/HolyAegisEmeraldEnergy.mat";
    private const string GoldMaterialPath =
        ProductionRoot + "/HolyAegisGoldRim.mat";
    private const string HeraldryMaterialPath =
        ProductionRoot + "/HolyAegisHeraldry.mat";
    private const string CatalogPath =
        Root + "/HolyAegisV3Catalog.asset";
    private const string DemoRoot =
        Root + "/Demo";
    private const string DemoScenePath =
        DemoRoot + "/HolyAegisV3Demo.unity";
    private const string SilhouettePrefabPath =
        SilhouetteRoot + "/HolyAegisV3Silhouette.prefab";
    private const string ReviewContextPath =
        "Assets/VFXForge/Dogfood/ReviewContexts/"
        + "TopDownThreeGrounds.prefab";
    private const string SourceGraphPath =
        "Packages/com.unity.visualeffectgraph/Editor/"
        + "Templates/03_Simple_Burst.vfx";
    private const string SourceShaderGraphPath =
        "Packages/com.unity.visualeffectgraph/ShaderGraph/"
        + "0_VFXGraph Unlit.shadergraph";

    [MenuItem(
        "Tools/VFX Forge/Dogfood/Build VF-019 Holy Aegis Production")]
    public static void BuildProductionAssets()
    {
        RefuseExisting(
            TemplatePath,
            GraphPath,
            ShaderGraphPath,
            EnergyMaterialPath,
            GoldMaterialPath,
            HeraldryMaterialPath,
            CatalogPath);
        EnsureFolder(ProductionRoot);
        EnsureFolder(GeneratedRoot);

        CopyProjectOwnedAsset(
            SourceGraphPath,
            GraphPath,
            "playback VFX Graph");
        CopyProjectOwnedAsset(
            SourceShaderGraphPath,
            ShaderGraphPath,
            "playback VFX Shader Graph");
        SetParticleCapacity(GraphPath, 1);
        AttachShaderGraph(GraphPath, ShaderGraphPath);
        AssetDatabase.ImportAsset(
            GraphPath,
            ImportAssetOptions.ForceSynchronousImport);

        Shader shader =
            Shader.Find("VFXForge/Dogfood/HolyAegisShield");
        if (shader == null)
        {
            throw new InvalidOperationException(
                "Holy Aegis transparent emissive Shader "
                + "did not import.");
        }

        Material energy = CreateMaterial(
            "Holy Aegis Emerald Energy",
            shader,
            new Color(0.015f, 0.84f, 0.44f, 1f),
            new Color(0.48f, 1f, 0.74f, 1f),
            5.8f,
            0.82f,
            0f);
        Material gold = CreateMaterial(
            "Holy Aegis Gold Rim",
            shader,
            new Color(1f, 0.68f, 0.12f, 1f),
            new Color(1f, 0.94f, 0.58f, 1f),
            5.2f,
            0.88f,
            1f);
        Material heraldry = CreateMaterial(
            "Holy Aegis Heraldry",
            shader,
            new Color(0.02f, 0.48f, 0.29f, 1f),
            new Color(1f, 0.74f, 0.18f, 1f),
            5.6f,
            0.90f,
            2f);
        AssetDatabase.CreateAsset(energy, EnergyMaterialPath);
        AssetDatabase.CreateAsset(gold, GoldMaterialPath);
        AssetDatabase.CreateAsset(
            heraldry,
            HeraldryMaterialPath);

        GameObject silhouette =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                SilhouettePrefabPath);
        VisualEffectAsset graph =
            AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(
                GraphPath);
        GameObject reviewContext =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                ReviewContextPath);
        if (silhouette == null
            || graph == null
            || reviewContext == null)
        {
            throw new InvalidOperationException(
                "Silhouette, playback graph, or Review Context "
                + "is missing.");
        }

        GameObject source =
            PrefabUtility.InstantiatePrefab(silhouette)
                as GameObject;
        if (source == null)
        {
            throw new InvalidOperationException(
                "Holy Aegis silhouette could not be instantiated.");
        }

        try
        {
            PrefabUtility.UnpackPrefabInstance(
                source,
                PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
            source.name = "Holy Aegis Shield V3 Template";

            Transform assembly = Require(
                source.transform,
                "Shield Assembly");
            Transform plate = Require(
                assembly,
                "Circular Main Plate");
            Transform rim = Require(
                assembly,
                "Thick Connected Rim");
            Transform crest = Require(
                assembly,
                "Central Knight Crest");
            Transform ornaments = Require(
                assembly,
                "Four Rim Ornaments");

            var plateRenderers = new List<Renderer>();
            var rimRenderers = new List<Renderer>();
            var crestRenderers = new List<Renderer>();
            var ornamentRenderers = new List<Renderer>();

            ReplaceMaterial(
                plate,
                energy,
                0,
                plateRenderers);
            ReplaceMaterial(
                rim,
                gold,
                3,
                rimRenderers);
            ReplaceMaterial(
                Require(crest, "Crest Backing"),
                heraldry,
                4,
                crestRenderers);
            ReplaceMaterial(
                Require(crest, "Crest Sword"),
                gold,
                6,
                crestRenderers);

            Mesh ringMesh =
                Require(rim).GetComponent<MeshFilter>().sharedMesh;
            AddLayer(
                plate,
                "Emerald Concentric Halo",
                ringMesh,
                energy,
                0.73f,
                0.018f,
                2,
                plateRenderers);
            AddLayer(
                rim,
                "Emerald Rim Inlay",
                ringMesh,
                energy,
                0.89f,
                0.020f,
                4,
                rimRenderers);

            foreach (Transform ornament in ornaments)
            {
                ReplaceMaterial(
                    ornament,
                    gold,
                    4,
                    ornamentRenderers);
                Mesh ornamentMesh =
                    Require(ornament)
                        .GetComponent<MeshFilter>()
                        .sharedMesh;
                AddLayer(
                    ornament,
                    "Emerald Connected Inlay",
                    ornamentMesh,
                    heraldry,
                    0.78f,
                    0.018f,
                    5,
                    ornamentRenderers);
            }

            var graphObject =
                new GameObject("VFX Playback Bridge");
            graphObject.transform.SetParent(
                source.transform,
                false);
            graphObject.transform.localScale =
                Vector3.one * 0.0001f;
            VisualEffect visualEffect =
                graphObject.AddComponent<VisualEffect>();
            visualEffect.visualEffectAsset = graph;
            visualEffect.initialEventName = "OnPlay";
            visualEffect.startSeed = 190731u;
            visualEffect.resetSeedOnPlay = false;
            visualEffect.enabled = true;

            VfxPlayer player =
                source.GetComponent<VfxPlayer>()
                ?? source.AddComponent<VfxPlayer>();
            player.Configure("OnPlay");

            var witness =
                new GameObject("Recipe Scale Witness");
            witness.transform.SetParent(
                source.transform,
                false);

            HolyAegisDeployment deployment =
                source.AddComponent<HolyAegisDeployment>();
            deployment.Configure(
                assembly,
                plate,
                rim,
                crest,
                ornaments,
                plateRenderers.ToArray(),
                rimRenderers.ToArray(),
                crestRenderers.ToArray(),
                ornamentRenderers.ToArray());
            deployment.EvaluatePreviewTime(0f);

            GameObject template =
                PrefabUtility.SaveAsPrefabAsset(
                    source,
                    TemplatePath);
            if (template == null)
            {
                throw new InvalidOperationException(
                    "Holy Aegis Production Template "
                    + "could not be saved.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
        }

        var catalog =
            ScriptableObject.CreateInstance<VfxTemplateCatalog>();
        catalog.templates.Add(
            new VfxTemplateEntry
            {
                id = "holy_aegis_shield_v3",
                prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        TemplatePath),
                playEventName = "OnPlay",
                supportedLayers = new[]
                {
                    "emerald_energy_plate",
                    "gold_connected_rim",
                    "central_knight_crest",
                    "four_connected_ornaments"
                },
                bindings = CreateBindings()
            });
        catalog.reviewContexts.Add(
            new VfxReviewContextEntry
            {
                id = "topdown_three_grounds",
                prefab = reviewContext
            });
        AssetDatabase.CreateAsset(catalog, CatalogPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        List<VfxValidationResult> results =
            VfxTemplateCatalogValidator.Validate(catalog);
        VfxValidationResult error =
            results.FirstOrDefault(
                result =>
                    result.severity
                    == VfxValidationSeverity.Error);
        if (error != null)
        {
            throw new InvalidOperationException(
                "Holy Aegis Catalog failed validation: "
                + $"{error.ruleId} {error.message}");
        }

        GameObject saved =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                TemplatePath);
        AssertProductionContract(saved);
        Debug.Log(
            "[VFXForge VF-019] Holy Aegis production assets "
            + "created. Radius=2.6m, Duration=1.8s, "
            + "VisibleShaderedRenderers=14, "
            + "ParticleSystems=0, Lights=0.");
    }

    public static void BuildProductionAssetsBatch()
    {
        BuildProductionAssets();
    }

    public static void CleanupFailedProductionBuildBatch()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(
                TemplatePath) != null
            || AssetDatabase.LoadAssetAtPath<VfxTemplateCatalog>(
                CatalogPath) != null)
        {
            throw new InvalidOperationException(
                "Refusing cleanup because a completed Production "
                + "Template or Catalog exists.");
        }
        AssetDatabase.DeleteAsset(ProductionRoot);
        AssetDatabase.DeleteAsset(GeneratedRoot);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            "[VFXForge VF-019] Removed failed-build partial "
            + "Production and Generated folders.");
    }

    [MenuItem(
        "Tools/VFX Forge/Dogfood/Create VF-019 Holy Aegis Demo")]
    public static void CreateProductionDemo()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(
                DemoScenePath) != null)
        {
            throw new InvalidOperationException(
                "Refusing to overwrite the Holy Aegis Demo Scene.");
        }
        GameObject contextPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                ReviewContextPath);
        GameObject effectPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                Root
                + "/Generated/HolyAegisShieldV3.prefab");
        if (contextPrefab == null || effectPrefab == null)
        {
            throw new InvalidOperationException(
                "Review Context or generated Holy Aegis "
                + "Prefab is missing.");
        }

        EnsureFolder(DemoRoot);
        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single);
        GameObject contextObject =
            PrefabUtility.InstantiatePrefab(
                contextPrefab,
                scene) as GameObject;
        VfxReviewContext context =
            contextObject?.GetComponent<VfxReviewContext>();
        GameObject effect =
            PrefabUtility.InstantiatePrefab(
                effectPrefab,
                scene) as GameObject;
        if (context == null
            || context.effectAnchor == null
            || effect == null)
        {
            throw new InvalidOperationException(
                "Holy Aegis Demo bootstrap failed.");
        }
        effect.transform.SetParent(
            context.effectAnchor,
            false);
        effect.name = "Holy Aegis V3 Gameplay Instance";

        if (!EditorSceneManager.SaveScene(
            scene,
            DemoScenePath))
        {
            throw new InvalidOperationException(
                "Holy Aegis Demo Scene could not be saved.");
        }
        AddSceneToBuildSettings(DemoScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log(
            "[VFXForge VF-019] Holy Aegis 16:9 top-down "
            + $"Demo created: {DemoScenePath}");
    }

    public static void CreateProductionDemoBatch()
    {
        CreateProductionDemo();
    }

    private static List<VfxPropertyBinding> CreateBindings()
    {
        return new List<VfxPropertyBinding>
        {
            Adapter(
                "seed",
                "RandomSeed",
                VfxPropertyType.Int),
            Adapter(
                "timing.duration",
                "Duration",
                VfxPropertyType.Float),
            Adapter(
                "timing.impact",
                "ImpactTime",
                VfxPropertyType.Float),
            Adapter(
                "timing.sustain",
                "SustainTime",
                VfxPropertyType.Float),
            Adapter(
                "timing.decay",
                "DecayTime",
                VfxPropertyType.Float),
            Adapter(
                "shape.radius",
                "Radius",
                VfxPropertyType.Float),
            Adapter(
                "shape.spreadAngle",
                "SpreadAngle",
                VfxPropertyType.Float),
            Adapter(
                "shape.directionality",
                "Directionality",
                VfxPropertyType.Float),
            Adapter(
                "style.primaryColor",
                "PrimaryColor",
                VfxPropertyType.Color),
            Adapter(
                "style.secondaryColor",
                "SecondaryColor",
                VfxPropertyType.Color),
            Adapter(
                "style.emissionIntensity",
                "EmissionIntensity",
                VfxPropertyType.Float),
            Adapter(
                "style.sharpness",
                "Sharpness",
                VfxPropertyType.Float),
            Adapter(
                "motion.speed",
                "PulseRate",
                VfxPropertyType.Float),
            Adapter(
                "motion.localDirection",
                "LocalDirection",
                VfxPropertyType.Vector3),
            MaterialBinding(
                "style.primaryColor",
                "Shield Assembly/Circular Main Plate",
                "_PrimaryColor",
                VfxPropertyType.Color),
            MaterialBinding(
                "style.secondaryColor",
                "Shield Assembly/Thick Connected Rim",
                "_SecondaryColor",
                VfxPropertyType.Color),
            MaterialBinding(
                "style.emissionIntensity",
                "Shield Assembly/Circular Main Plate",
                "_Emission",
                VfxPropertyType.Float),
            MaterialBinding(
                "style.sharpness",
                "Shield Assembly/Circular Main Plate",
                "_Sharpness",
                VfxPropertyType.Float),
            new VfxPropertyBinding
            {
                recipePath = "shape.radius",
                exposedPropertyName = "uniformScale",
                propertyType = VfxPropertyType.Float,
                required = true,
                targetKind =
                    VfxBindingTargetKind.TransformProperty,
                targetPath = "Recipe Scale Witness"
            }
        };
    }

    private static VfxPropertyBinding Adapter(
        string recipePath,
        string property,
        VfxPropertyType type)
    {
        return new VfxPropertyBinding
        {
            recipePath = recipePath,
            exposedPropertyName = property,
            propertyType = type,
            required = true,
            targetKind = VfxBindingTargetKind.AdapterProperty,
            targetPath = string.Empty,
            adapterId = HolyAegisDeployment.AdapterId
        };
    }

    private static VfxPropertyBinding MaterialBinding(
        string recipePath,
        string targetPath,
        string property,
        VfxPropertyType type)
    {
        return new VfxPropertyBinding
        {
            recipePath = recipePath,
            exposedPropertyName = property,
            propertyType = type,
            required = true,
            targetKind = VfxBindingTargetKind.MaterialProperty,
            targetPath = targetPath,
            materialIndex = 0
        };
    }

    private static void AssertProductionContract(
        GameObject prefab)
    {
        if (prefab == null)
        {
            throw new InvalidOperationException(
                "Holy Aegis Production Prefab is missing.");
        }
        if (prefab.GetComponentsInChildren<ParticleSystem>(
                true).Length != 0)
        {
            throw new InvalidOperationException(
                "Holy Aegis must not contain ParticleSystem.");
        }
        if (prefab.GetComponentsInChildren<Light>(
                true).Length != 0)
        {
            throw new InvalidOperationException(
                "Holy Aegis must not contain dynamic Light.");
        }
        MeshRenderer[] renderers =
            prefab.GetComponentsInChildren<MeshRenderer>(true);
        if (renderers.Length != 14
            || renderers.Any(
                renderer =>
                    renderer.sharedMaterial == null
                    || renderer.sharedMaterial.shader == null
                    || renderer.sharedMaterial.shader.name
                        != "VFXForge/Dogfood/HolyAegisShield"))
        {
            throw new InvalidOperationException(
                "Every visible Holy Aegis Mesh must use "
                + "the dedicated transparent emissive Shader.");
        }
    }

    private static void ReplaceMaterial(
        Transform target,
        Material material,
        int sortingOrder,
        ICollection<Renderer> renderers)
    {
        MeshRenderer renderer =
            Require(target).GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = sortingOrder;
        renderers.Add(renderer);
    }

    private static void AddLayer(
        Transform parent,
        string name,
        Mesh mesh,
        Material material,
        float scale,
        float depth,
        int sortingOrder,
        ICollection<Renderer> renderers)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition =
            new Vector3(0f, 0f, depth);
        child.transform.localScale =
            Vector3.one * scale;
        child.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer =
            child.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = sortingOrder;
        renderers.Add(renderer);
    }

    private static Material CreateMaterial(
        string name,
        Shader shader,
        Color primary,
        Color secondary,
        float emission,
        float sharpness,
        float layerMode)
    {
        var material =
            new Material(shader)
            {
                name = name,
                renderQueue = 3000
            };
        material.SetColor("_PrimaryColor", primary);
        material.SetColor("_SecondaryColor", secondary);
        material.SetFloat("_Emission", emission);
        material.SetFloat("_Sharpness", sharpness);
        material.SetFloat("_Age01", 0f);
        material.SetFloat("_LayerAlpha", 1f);
        material.SetFloat("_LayerMode", layerMode);
        material.SetFloat("_Seed", 0.19f);
        return material;
    }

    private static Transform Require(
        Transform root,
        string path)
    {
        Transform target = root.Find(path);
        if (target == null)
        {
            throw new InvalidOperationException(
                $"Required Holy Aegis node is missing: {path}");
        }
        return target;
    }

    private static Transform Require(Transform target)
    {
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
        if (target.GetComponent<MeshFilter>() == null
            || target.GetComponent<MeshRenderer>() == null)
        {
            throw new InvalidOperationException(
                $"Holy Aegis Mesh node is incomplete: "
                + target.name);
        }
        return target;
    }

    private static void CopyProjectOwnedAsset(
        string source,
        string destination,
        string label)
    {
        if (!AssetDatabase.CopyAsset(source, destination))
        {
            throw new InvalidOperationException(
                $"Could not copy {label}: {source}");
        }
        AssetDatabase.ImportAsset(
            destination,
            ImportAssetOptions.ForceSynchronousImport);
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
            PushChildren(model, pending);
        }
        if (updated == 0)
        {
            throw new InvalidOperationException(
                "No VFX particle capacity was updated.");
        }
        WriteGraph(graphPath);
    }

    private static void AttachShaderGraph(
        string graphPath,
        string shaderGraphPath)
    {
        UnityEngine.Object shaderGraph =
            AssetDatabase.LoadAllAssetsAtPath(shaderGraphPath)
                .FirstOrDefault(
                    asset =>
                        asset != null
                        && asset.GetType().Name
                            == "ShaderGraphVfxAsset");
        if (shaderGraph == null)
        {
            throw new InvalidOperationException(
                "Holy Aegis VFX Shader Graph import "
                + "object is missing.");
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
                "No Holy Aegis VFX output accepted "
                + "the project-owned Shader Graph.");
        }
        WriteGraph(graphPath);
    }

    private static object ResolveGraphObject(
        string graphPath)
    {
        Type resourceType = FindLoadedType(
            "UnityEditor.VFX.VisualEffectResource");
        object resource =
            resourceType.GetMethod(
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
        object graph =
            extensionType.GetMethod(
                "GetOrCreateGraph",
                BindingFlags.Static
                    | BindingFlags.Public
                    | BindingFlags.NonPublic)
                ?.Invoke(null, new[] { resource });
        return graph
            ?? throw new InvalidOperationException(
                "VFX Graph authoring model is unavailable.");
    }

    private static void PushChildren(
        object model,
        Stack<object> pending)
    {
        PropertyInfo childrenProperty =
            model.GetType()
                .GetProperties(
                    BindingFlags.Instance
                    | BindingFlags.Public)
                .FirstOrDefault(
                    candidate =>
                        candidate.Name == "children"
                        && candidate.GetIndexParameters().Length
                            == 0);
        if (!(childrenProperty?.GetValue(model)
            is IEnumerable children))
        {
            children = null;
        }
        if (children != null)
        {
            foreach (object child in children)
            {
                if (child != null)
                {
                    pending.Push(child);
                }
            }
        }

        if (model is UnityEngine.Object graphObject)
        {
            var serialized = new SerializedObject(graphObject);
            SerializedProperty data =
                serialized.FindProperty("m_Data");
            if (data != null
                && data.propertyType
                    == SerializedPropertyType.ObjectReference
                && data.objectReferenceValue != null)
            {
                pending.Push(data.objectReferenceValue);
            }
        }
    }

    private static void WriteGraph(string graphPath)
    {
        Type resourceType = FindLoadedType(
            "UnityEditor.VFX.VisualEffectResource");
        object resource =
            resourceType.GetMethod(
                "GetResourceAtPath",
                BindingFlags.Static
                    | BindingFlags.Public
                    | BindingFlags.NonPublic)
                ?.Invoke(null, new object[] { graphPath });
        Type extensionType = FindLoadedType(
            "UnityEditor.VFX.VisualEffectResourceExtensions");
        MethodInfo write =
            extensionType?.GetMethod(
                "WriteAssetWithSubAssets",
                BindingFlags.Static
                    | BindingFlags.Public
                    | BindingFlags.NonPublic);
        if (resource == null || write == null)
        {
            throw new InvalidOperationException(
                "VFX Graph write API is unavailable.");
        }
        write.Invoke(null, new[] { resource });
        AssetDatabase.ImportAsset(
            graphPath,
            ImportAssetOptions.ForceSynchronousImport);
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
            $"Unity type is unavailable: {fullName}");
    }

    private static void RefuseExisting(
        params string[] paths)
    {
        string existing =
            paths.FirstOrDefault(
                path =>
                    AssetDatabase.LoadAssetAtPath<
                        UnityEngine.Object>(path) != null);
        if (!string.IsNullOrEmpty(existing))
        {
            throw new InvalidOperationException(
                $"Refusing to overwrite existing Asset: {existing}");
        }
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int index = 1;
            index < parts.Length;
            index++)
        {
            string next = current + "/" + parts[index];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(
                    current,
                    parts[index]);
            }
            current = next;
        }
    }

    private static void AddSceneToBuildSettings(
        string scenePath)
    {
        List<EditorBuildSettingsScene> scenes =
            EditorBuildSettings.scenes.ToList();
        if (scenes.Any(
            scene =>
                scene.path == scenePath))
        {
            return;
        }
        scenes.Add(
            new EditorBuildSettingsScene(
                scenePath,
                true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
