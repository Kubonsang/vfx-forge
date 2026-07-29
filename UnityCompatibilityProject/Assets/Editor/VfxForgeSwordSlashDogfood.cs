using System;
using System.Collections.Generic;
using System.IO;
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

public static class VfxForgeSwordSlashDogfood
{
    private const string Root = "Assets/VFXForge/Dogfood/SwordSlash";
    private const string AuthoringRoot = Root + "/Authoring";
    private const string GraphPath = AuthoringRoot + "/SwordSlashBurst.vfx";
    private const string OuterMeshPath = AuthoringRoot + "/SwordSlashOuter.asset";
    private const string CoreMeshPath = AuthoringRoot + "/SwordSlashCore.asset";
    private const string OuterMaterialPath = AuthoringRoot + "/SwordSlashOuter.mat";
    private const string CoreMaterialPath = AuthoringRoot + "/SwordSlashCore.mat";
    private const string TemplatePath = AuthoringRoot + "/SwordSlashTemplate.prefab";
    private const string CatalogPath = Root + "/SwordSlashCatalog.asset";
    private const string OuterMeshV2Path = AuthoringRoot + "/SwordSlashOuterV2.asset";
    private const string CoreMeshV2Path = AuthoringRoot + "/SwordSlashCoreV2.asset";
    private const string OuterMaterialV2Path = AuthoringRoot + "/SwordSlashOuterV2.mat";
    private const string CoreMaterialV2Path = AuthoringRoot + "/SwordSlashCoreV2.mat";
    private const string TemplateV2Path = AuthoringRoot + "/SwordSlashTemplateV2.prefab";
    private const string TemplateV3Path = AuthoringRoot + "/SwordSlashTemplateV3.prefab";
    private const string OuterMaterialV4Path = AuthoringRoot + "/SwordSlashOuterV4.mat";
    private const string CoreMaterialV4Path = AuthoringRoot + "/SwordSlashCoreV4.mat";
    private const string TemplateV4Path = AuthoringRoot + "/SwordSlashTemplateV4.prefab";
    private const string TemplateV5Path = AuthoringRoot + "/SwordSlashTemplateV5.prefab";
    private const string TemplateV6Path = AuthoringRoot + "/SwordSlashTemplateV6.prefab";
    private const string TemplateV7Path = AuthoringRoot + "/SwordSlashTemplateV7.prefab";
    private const string TemplateV8Path = AuthoringRoot + "/SwordSlashTemplateV8.prefab";
    private const string GeneratedV2Path =
        Root + "/Generated/SwordSlashBasicV2.prefab";
    private const string GeneratedV4Path =
        Root + "/Generated/SwordSlashBasicV4.prefab";
    private const string GeneratedV8Path =
        Root + "/Generated/SwordSlashBasicV8.prefab";
    private const string DemoRoot = Root + "/Demo";
    private const string DemoScenePath = DemoRoot + "/SwordSlashDemo.unity";
    private const string DemoGroundMaterialPath = DemoRoot + "/DemoGround.mat";
    private const string RenderPipelineRoot =
        "Assets/VFXForge/Dogfood/RenderPipeline";
    private const string RendererDataPath =
        RenderPipelineRoot + "/DogfoodUniversalRenderer.asset";
    private const string PipelineAssetPath =
        RenderPipelineRoot + "/DogfoodUniversalRenderPipeline.asset";
    private const string RendererDataV2Path =
        RenderPipelineRoot + "/DogfoodUniversalRendererV2.asset";
    private const string PipelineAssetV2Path =
        RenderPipelineRoot + "/DogfoodUniversalRenderPipelineV2.asset";
    private const string TemporaryRendererDataPath =
        "Assets/UniversalRenderer.asset";
    private const string SourceGraphPath =
        "Packages/com.unity.visualeffectgraph/Editor/Templates/03_Simple_Burst.vfx";

    private static readonly string[] BuiltInTemplatePaths =
    {
        "Packages/com.unity.visualeffectgraph/Editor/Templates/01_Minimal_System.vfx",
        "Packages/com.unity.visualeffectgraph/Editor/Templates/02_Simple_Loop.vfx",
        "Packages/com.unity.visualeffectgraph/Editor/Templates/03_Simple_Burst.vfx",
        "Packages/com.unity.visualeffectgraph/Editor/Templates/04_Simple_Trail.vfx"
    };

    public static void InspectBuiltInTemplates()
    {
        foreach (string path in BuiltInTemplatePaths)
        {
            VisualEffectAsset asset =
                AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
            if (asset == null)
            {
                Debug.LogWarning($"[VFXForge Dogfood] Template missing: {path}");
                continue;
            }

            var properties = new List<VFXExposedProperty>();
            asset.GetExposedProperties(properties);
            Debug.Log(
                $"[VFXForge Dogfood] {path}: "
                + string.Join(
                    ", ",
                    properties.ConvertAll(
                        property => $"{property.name}:{property.type.Name}")));
        }
    }

    public static void BuildAuthoringAssets()
    {
        EnsureTargetsDoNotExist();
        EnsureFolder(AuthoringRoot);

        if (!AssetDatabase.CopyAsset(SourceGraphPath, GraphPath))
        {
            throw new InvalidOperationException(
                $"Could not copy built-in VFX Graph: {SourceGraphPath}");
        }
        AssetDatabase.ImportAsset(GraphPath, ImportAssetOptions.ForceSynchronousImport);

        Mesh outerMesh = CreateArcMesh(
            "Sword Slash Outer",
            0.88f,
            1.72f,
            -72f,
            72f,
            36);
        Mesh coreMesh = CreateArcMesh(
            "Sword Slash Core",
            1.18f,
            1.52f,
            -68f,
            68f,
            36);
        AssetDatabase.CreateAsset(outerMesh, OuterMeshPath);
        AssetDatabase.CreateAsset(coreMesh, CoreMeshPath);

        Material outerMaterial = CreateUnlitMaterial(
            "Sword Slash Outer",
            new Color(0.05f, 0.48f, 1f, 0.28f));
        Material coreMaterial = CreateUnlitMaterial(
            "Sword Slash Core",
            new Color(0.72f, 0.95f, 1f, 0.96f));
        AssetDatabase.CreateAsset(outerMaterial, OuterMaterialPath);
        AssetDatabase.CreateAsset(coreMaterial, CoreMaterialPath);

        VisualEffectAsset graph =
            AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(GraphPath);
        if (graph == null)
        {
            throw new InvalidOperationException(
                $"Copied VFX Graph could not be loaded: {GraphPath}");
        }

        GameObject source = new GameObject("Sword Slash Template");
        try
        {
            VfxPlayer player = source.AddComponent<VfxPlayer>();
            player.Configure("OnPlay");
            SwordSlashProjectile projectile =
                source.AddComponent<SwordSlashProjectile>();
            projectile.Configure(14f, 0.45f);

            AddArcChild(
                source.transform,
                "Outer Glow",
                outerMesh,
                outerMaterial,
                new Vector3(0f, 1f, 0.01f),
                Quaternion.Euler(0f, 0f, -12f));
            AddArcChild(
                source.transform,
                "White Core",
                coreMesh,
                coreMaterial,
                new Vector3(0f, 1f, 0f),
                Quaternion.Euler(0f, 0f, -12f));

            var particles = new GameObject("Burst Particles");
            particles.transform.SetParent(source.transform, false);
            particles.transform.localPosition = new Vector3(0.85f, 1f, 0.02f);
            particles.transform.localScale = Vector3.one * 0.55f;
            VisualEffect effect = particles.AddComponent<VisualEffect>();
            effect.visualEffectAsset = graph;
            effect.initialEventName = "OnPlay";
            effect.resetSeedOnPlay = false;
            effect.startSeed = 240719u;

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
            id = "sword_slash_basic",
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TemplatePath),
            playEventName = "OnPlay",
            supportedLayers = new[] { "slash_core", "burst_particles" },
            bindings = new List<VfxPropertyBinding>()
        });
        AssetDatabase.CreateAsset(catalog, CatalogPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[VFXForge Dogfood] Sword slash authoring assets created. "
            + $"Template={TemplatePath}, Catalog={CatalogPath}");
    }

    public static void BuildAuthoringAssetsV2()
    {
        EnsureAssetsDoNotExist(
            OuterMeshV2Path,
            CoreMeshV2Path,
            OuterMaterialV2Path,
            CoreMaterialV2Path,
            TemplateV2Path);

        VisualEffectAsset graph =
            AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(GraphPath);
        VfxTemplateCatalog catalog =
            AssetDatabase.LoadAssetAtPath<VfxTemplateCatalog>(CatalogPath);
        if (graph == null || catalog == null)
        {
            throw new InvalidOperationException(
                "V1 Graph and Catalog are required before building V2.");
        }
        if (catalog.TryGet("sword_slash_basic_v2", out _))
        {
            throw new InvalidOperationException(
                "Refusing to replace the V2 Catalog entry.");
        }

        Mesh outerMesh = CreateDoubleSidedArcMesh(
            "Sword Slash Outer V2",
            0.88f,
            1.72f,
            -72f,
            72f,
            36);
        Mesh coreMesh = CreateDoubleSidedArcMesh(
            "Sword Slash Core V2",
            1.18f,
            1.52f,
            -68f,
            68f,
            36);
        AssetDatabase.CreateAsset(outerMesh, OuterMeshV2Path);
        AssetDatabase.CreateAsset(coreMesh, CoreMeshV2Path);

        Material outerMaterial = CreateOpaqueUnlitMaterial(
            "Sword Slash Outer V2",
            new Color(0.03f, 0.42f, 1f, 1f));
        Material coreMaterial = CreateOpaqueUnlitMaterial(
            "Sword Slash Core V2",
            new Color(0.82f, 0.97f, 1f, 1f));
        AssetDatabase.CreateAsset(outerMaterial, OuterMaterialV2Path);
        AssetDatabase.CreateAsset(coreMaterial, CoreMaterialV2Path);

        GameObject source = new GameObject("Sword Slash Template V2");
        try
        {
            VfxPlayer player = source.AddComponent<VfxPlayer>();
            player.Configure("OnPlay");
            SwordSlashProjectile projectile =
                source.AddComponent<SwordSlashProjectile>();
            projectile.Configure(14f, 0.45f);

            AddArcChild(
                source.transform,
                "Outer Glow",
                outerMesh,
                outerMaterial,
                new Vector3(0f, 1f, 0.01f),
                Quaternion.Euler(0f, 0f, -12f));
            AddArcChild(
                source.transform,
                "White Core",
                coreMesh,
                coreMaterial,
                new Vector3(0f, 1f, -0.01f),
                Quaternion.Euler(0f, 0f, -12f));

            var particles = new GameObject("Burst Particles");
            particles.transform.SetParent(source.transform, false);
            particles.transform.localPosition = new Vector3(0.85f, 1f, 0.02f);
            particles.transform.localScale = Vector3.one * 0.55f;
            VisualEffect effect = particles.AddComponent<VisualEffect>();
            effect.visualEffectAsset = graph;
            effect.initialEventName = "OnPlay";
            effect.resetSeedOnPlay = false;
            effect.startSeed = 240719u;

            GameObject template =
                PrefabUtility.SaveAsPrefabAsset(source, TemplateV2Path);
            if (template == null)
            {
                throw new InvalidOperationException(
                    $"V2 Template Prefab could not be saved: {TemplateV2Path}");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
        }

        catalog.templates.Add(new VfxTemplateEntry
        {
            id = "sword_slash_basic_v2",
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TemplateV2Path),
            playEventName = "OnPlay",
            supportedLayers = new[] { "slash_core", "burst_particles" },
            bindings = new List<VfxPropertyBinding>()
        });
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[VFXForge Dogfood] Sword slash V2 authoring assets created. "
            + $"Template={TemplateV2Path}, Catalog={CatalogPath}");
    }

    public static void BuildAuthoringAssetsV3()
    {
        EnsureAssetsDoNotExist(TemplateV3Path);

        VisualEffectAsset graph =
            AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(GraphPath);
        Material outerMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(OuterMaterialV2Path);
        Material coreMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(CoreMaterialV2Path);
        VfxTemplateCatalog catalog =
            AssetDatabase.LoadAssetAtPath<VfxTemplateCatalog>(CatalogPath);
        if (graph == null
            || outerMaterial == null
            || coreMaterial == null
            || catalog == null)
        {
            throw new InvalidOperationException(
                "V2 Graph, Materials, and Catalog are required before building V3.");
        }
        if (catalog.TryGet("sword_slash_basic_v3", out _))
        {
            throw new InvalidOperationException(
                "Refusing to replace the V3 Catalog entry.");
        }

        GameObject source = new GameObject("Sword Slash Template V3");
        try
        {
            VfxPlayer player = source.AddComponent<VfxPlayer>();
            player.Configure("OnPlay");
            SwordSlashProjectile projectile =
                source.AddComponent<SwordSlashProjectile>();
            projectile.Configure(14f, 0.45f);

            AddSegmentedArc(
                source.transform,
                "Outer Glow",
                outerMaterial,
                1.3f,
                15,
                new Vector3(0.34f, 0.28f, 0.08f),
                0.02f);
            AddSegmentedArc(
                source.transform,
                "White Core",
                coreMaterial,
                1.36f,
                15,
                new Vector3(0.28f, 0.12f, 0.1f),
                -0.05f);

            var particles = new GameObject("Burst Particles");
            particles.transform.SetParent(source.transform, false);
            particles.transform.localPosition = new Vector3(0.85f, 1f, 0.02f);
            particles.transform.localScale = Vector3.one * 0.55f;
            VisualEffect effect = particles.AddComponent<VisualEffect>();
            effect.visualEffectAsset = graph;
            effect.initialEventName = "OnPlay";
            effect.resetSeedOnPlay = false;
            effect.startSeed = 240719u;

            GameObject template =
                PrefabUtility.SaveAsPrefabAsset(source, TemplateV3Path);
            if (template == null)
            {
                throw new InvalidOperationException(
                    $"V3 Template Prefab could not be saved: {TemplateV3Path}");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
        }

        catalog.templates.Add(new VfxTemplateEntry
        {
            id = "sword_slash_basic_v3",
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TemplateV3Path),
            playEventName = "OnPlay",
            supportedLayers = new[] { "slash_core", "burst_particles" },
            bindings = new List<VfxPropertyBinding>()
        });
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[VFXForge Dogfood] Sword slash V3 authoring assets created. "
            + $"Template={TemplateV3Path}, Catalog={CatalogPath}");
    }

    public static void BuildAuthoringAssetsV4()
    {
        EnsureAssetsDoNotExist(
            OuterMaterialV4Path,
            CoreMaterialV4Path,
            TemplateV4Path);

        VisualEffectAsset graph =
            AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(GraphPath);
        VfxTemplateCatalog catalog =
            AssetDatabase.LoadAssetAtPath<VfxTemplateCatalog>(CatalogPath);
        if (graph == null || catalog == null)
        {
            throw new InvalidOperationException(
                "V3 Graph and Catalog are required before building V4.");
        }
        if (catalog.TryGet("sword_slash_basic_v4", out _))
        {
            throw new InvalidOperationException(
                "Refusing to replace the V4 Catalog entry.");
        }

        Material outerMaterial = CreateDefaultUnlitMaterial(
            "Sword Slash Outer V4",
            new Color(0.03f, 0.42f, 1f, 1f));
        Material coreMaterial = CreateDefaultUnlitMaterial(
            "Sword Slash Core V4",
            new Color(0.82f, 0.97f, 1f, 1f));
        AssetDatabase.CreateAsset(outerMaterial, OuterMaterialV4Path);
        AssetDatabase.CreateAsset(coreMaterial, CoreMaterialV4Path);

        GameObject source = new GameObject("Sword Slash Template V4");
        try
        {
            VfxPlayer player = source.AddComponent<VfxPlayer>();
            player.Configure("OnPlay");
            SwordSlashProjectile projectile =
                source.AddComponent<SwordSlashProjectile>();
            projectile.Configure(14f, 0.45f);

            AddSegmentedArc(
                source.transform,
                "Outer Glow",
                outerMaterial,
                1.3f,
                15,
                new Vector3(0.34f, 0.28f, 0.08f),
                0.02f);
            AddSegmentedArc(
                source.transform,
                "White Core",
                coreMaterial,
                1.36f,
                15,
                new Vector3(0.28f, 0.12f, 0.1f),
                -0.05f);

            var particles = new GameObject("Burst Particles");
            particles.transform.SetParent(source.transform, false);
            particles.transform.localPosition = new Vector3(0.85f, 1f, 0.02f);
            particles.transform.localScale = Vector3.one * 0.55f;
            VisualEffect effect = particles.AddComponent<VisualEffect>();
            effect.visualEffectAsset = graph;
            effect.initialEventName = "OnPlay";
            effect.resetSeedOnPlay = false;
            effect.startSeed = 240719u;

            GameObject template =
                PrefabUtility.SaveAsPrefabAsset(source, TemplateV4Path);
            if (template == null)
            {
                throw new InvalidOperationException(
                    $"V4 Template Prefab could not be saved: {TemplateV4Path}");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
        }

        catalog.templates.Add(new VfxTemplateEntry
        {
            id = "sword_slash_basic_v4",
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TemplateV4Path),
            playEventName = "OnPlay",
            supportedLayers = new[] { "slash_core", "burst_particles" },
            bindings = new List<VfxPropertyBinding>()
        });
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[VFXForge Dogfood] Sword slash V4 authoring assets created. "
            + $"Template={TemplateV4Path}, Catalog={CatalogPath}");
    }

    public static void BuildAuthoringAssetsV5()
    {
        EnsureAssetsDoNotExist(TemplateV5Path);

        VisualEffectAsset graph =
            AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(GraphPath);
        Material outerMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(OuterMaterialV4Path);
        Material coreMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(CoreMaterialV4Path);
        VfxTemplateCatalog catalog =
            AssetDatabase.LoadAssetAtPath<VfxTemplateCatalog>(CatalogPath);
        if (graph == null
            || outerMaterial == null
            || coreMaterial == null
            || catalog == null)
        {
            throw new InvalidOperationException(
                "V4 Graph, Materials, and Catalog are required before building V5.");
        }
        if (catalog.TryGet("sword_slash_basic_v5", out _))
        {
            throw new InvalidOperationException(
                "Refusing to replace the V5 Catalog entry.");
        }

        GameObject source = new GameObject("Sword Slash Template V5");
        try
        {
            VfxPlayer player = source.AddComponent<VfxPlayer>();
            player.Configure("OnPlay");
            SwordSlashProjectile projectile =
                source.AddComponent<SwordSlashProjectile>();
            projectile.Configure(14f, 0.45f);

            AddDirectSegmentedArc(
                source.transform,
                "Outer",
                outerMaterial,
                1.28f,
                11,
                new Vector3(0.46f, 0.34f, 0.14f),
                0.04f);
            AddDirectSegmentedArc(
                source.transform,
                "Core",
                coreMaterial,
                1.34f,
                11,
                new Vector3(0.38f, 0.15f, 0.16f),
                -0.06f);

            var particles = new GameObject("Burst Particles");
            particles.transform.SetParent(source.transform, false);
            particles.transform.localPosition = new Vector3(0.85f, 1f, 0.35f);
            particles.transform.localScale = Vector3.one * 0.55f;
            VisualEffect effect = particles.AddComponent<VisualEffect>();
            effect.visualEffectAsset = graph;
            effect.initialEventName = "OnPlay";
            effect.resetSeedOnPlay = false;
            effect.startSeed = 240719u;

            GameObject template =
                PrefabUtility.SaveAsPrefabAsset(source, TemplateV5Path);
            if (template == null)
            {
                throw new InvalidOperationException(
                    $"V5 Template Prefab could not be saved: {TemplateV5Path}");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
        }

        catalog.templates.Add(new VfxTemplateEntry
        {
            id = "sword_slash_basic_v5",
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TemplateV5Path),
            playEventName = "OnPlay",
            supportedLayers = new[] { "slash_core", "burst_particles" },
            bindings = new List<VfxPropertyBinding>()
        });
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[VFXForge Dogfood] Sword slash V5 authoring assets created. "
            + $"Template={TemplateV5Path}, Catalog={CatalogPath}");
    }

    public static void BuildAuthoringAssetsV6()
    {
        EnsureAssetsDoNotExist(TemplateV6Path);

        VisualEffectAsset graph =
            AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(GraphPath);
        Material outerMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(OuterMaterialV4Path);
        Material coreMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(CoreMaterialV4Path);
        VfxTemplateCatalog catalog =
            AssetDatabase.LoadAssetAtPath<VfxTemplateCatalog>(CatalogPath);
        if (graph == null
            || outerMaterial == null
            || coreMaterial == null
            || catalog == null)
        {
            throw new InvalidOperationException(
                "V5 Graph, Materials, and Catalog are required before building V6.");
        }
        if (catalog.TryGet("sword_slash_basic_v6", out _))
        {
            throw new InvalidOperationException(
                "Refusing to replace the V6 Catalog entry.");
        }

        GameObject source = new GameObject("Sword Slash Template V6");
        try
        {
            AddDirectSegmentedArc(
                source.transform,
                "Outer",
                outerMaterial,
                1.28f,
                11,
                new Vector3(0.46f, 0.34f, 0.14f),
                0.04f);
            AddDirectSegmentedArc(
                source.transform,
                "Core",
                coreMaterial,
                1.34f,
                11,
                new Vector3(0.38f, 0.15f, 0.16f),
                -0.06f);

            var particles = new GameObject("Burst Particles");
            particles.transform.SetParent(source.transform, false);
            particles.transform.localPosition = new Vector3(0.85f, 1f, 0.35f);
            particles.transform.localScale = Vector3.one * 0.55f;
            VisualEffect effect = particles.AddComponent<VisualEffect>();
            effect.visualEffectAsset = graph;
            effect.initialEventName = "OnPlay";
            effect.resetSeedOnPlay = false;
            effect.startSeed = 240719u;

            GameObject template =
                PrefabUtility.SaveAsPrefabAsset(source, TemplateV6Path);
            if (template == null)
            {
                throw new InvalidOperationException(
                    $"V6 Template Prefab could not be saved: {TemplateV6Path}");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
        }

        catalog.templates.Add(new VfxTemplateEntry
        {
            id = "sword_slash_basic_v6",
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TemplateV6Path),
            playEventName = "OnPlay",
            supportedLayers = new[] { "slash_core", "burst_particles" },
            bindings = new List<VfxPropertyBinding>()
        });
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[VFXForge Dogfood] Sword slash V6 authoring assets created. "
            + $"Template={TemplateV6Path}, Catalog={CatalogPath}");
    }

    public static void BuildAuthoringAssetsV7()
    {
        EnsureAssetsDoNotExist(TemplateV7Path);

        VisualEffectAsset graph =
            AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(GraphPath);
        Material outerMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(OuterMaterialV4Path);
        Material coreMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(CoreMaterialV4Path);
        VfxTemplateCatalog catalog =
            AssetDatabase.LoadAssetAtPath<VfxTemplateCatalog>(CatalogPath);
        if (graph == null
            || outerMaterial == null
            || coreMaterial == null
            || catalog == null)
        {
            throw new InvalidOperationException(
                "V6 Graph, Materials, and Catalog are required before building V7.");
        }
        if (catalog.TryGet("sword_slash_basic_v7", out _))
        {
            throw new InvalidOperationException(
                "Refusing to replace the V7 Catalog entry.");
        }

        GameObject source = new GameObject("Sword Slash Template V7");
        try
        {
            AddDirectSegmentedArc(
                source.transform,
                "Outer",
                outerMaterial,
                1.28f,
                11,
                new Vector3(0.46f, 0.34f, 0.14f),
                0.04f);
            AddDirectSegmentedArc(
                source.transform,
                "Core",
                coreMaterial,
                1.34f,
                11,
                new Vector3(0.38f, 0.15f, 0.16f),
                -0.06f);

            var particles = new GameObject("Burst Particles");
            particles.transform.SetParent(source.transform, false);
            particles.transform.localPosition = new Vector3(0.85f, 1f, 0.35f);
            particles.transform.localScale = Vector3.one * 0.55f;
            VisualEffect effect = particles.AddComponent<VisualEffect>();
            effect.visualEffectAsset = graph;
            effect.initialEventName = "OnPlay";
            effect.resetSeedOnPlay = false;
            effect.startSeed = 240719u;
            effect.enabled = false;

            GameObject template =
                PrefabUtility.SaveAsPrefabAsset(source, TemplateV7Path);
            if (template == null)
            {
                throw new InvalidOperationException(
                    $"V7 Template Prefab could not be saved: {TemplateV7Path}");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
        }

        catalog.templates.Add(new VfxTemplateEntry
        {
            id = "sword_slash_basic_v7",
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TemplateV7Path),
            playEventName = "OnPlay",
            supportedLayers = new[] { "slash_core", "burst_particles" },
            bindings = new List<VfxPropertyBinding>()
        });
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[VFXForge Dogfood] Sword slash V7 authoring assets created. "
            + $"Template={TemplateV7Path}, Catalog={CatalogPath}");
    }

    public static void BuildAuthoringAssetsV8()
    {
        EnsureAssetsDoNotExist(TemplateV8Path);

        VisualEffectAsset graph =
            AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(GraphPath);
        Material outerMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(OuterMaterialV4Path);
        Material coreMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(CoreMaterialV4Path);
        VfxTemplateCatalog catalog =
            AssetDatabase.LoadAssetAtPath<VfxTemplateCatalog>(CatalogPath);
        if (graph == null
            || outerMaterial == null
            || coreMaterial == null
            || catalog == null)
        {
            throw new InvalidOperationException(
                "V7 Graph, Materials, and Catalog are required before building V8.");
        }
        if (catalog.TryGet("sword_slash_basic_v8", out _))
        {
            throw new InvalidOperationException(
                "Refusing to replace the V8 Catalog entry.");
        }

        GameObject source = new GameObject("Sword Slash Template V8");
        try
        {
            AddBlade(source.transform, "Outer Blade", outerMaterial, 2.6f, 0.46f, 0.14f);
            AddBlade(source.transform, "White Core", coreMaterial, 2.2f, 0.18f, 0.16f);

            var particles = new GameObject("Burst Particles");
            particles.transform.SetParent(source.transform, false);
            particles.SetActive(false);
            VisualEffect effect = particles.AddComponent<VisualEffect>();
            effect.visualEffectAsset = graph;
            effect.initialEventName = "OnPlay";
            effect.resetSeedOnPlay = false;
            effect.startSeed = 240719u;

            GameObject template =
                PrefabUtility.SaveAsPrefabAsset(source, TemplateV8Path);
            if (template == null)
            {
                throw new InvalidOperationException(
                    $"V8 Template Prefab could not be saved: {TemplateV8Path}");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
        }

        catalog.templates.Add(new VfxTemplateEntry
        {
            id = "sword_slash_basic_v8",
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TemplateV8Path),
            playEventName = "OnPlay",
            supportedLayers = new[] { "slash_core", "burst_particles" },
            bindings = new List<VfxPropertyBinding>()
        });
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[VFXForge Dogfood] Sword slash V8 authoring assets created. "
            + $"Template={TemplateV8Path}, Catalog={CatalogPath}");
    }

    public static void InspectGeneratedV2()
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(GeneratedV2Path);
        if (prefab == null)
        {
            throw new InvalidOperationException(
                $"Generated V2 Prefab is missing: {GeneratedV2Path}");
        }

        MeshRenderer[] renderers =
            prefab.GetComponentsInChildren<MeshRenderer>(true);
        Debug.Log(
            $"[VFXForge Dogfood] Generated V2 renderers={renderers.Length}, "
            + $"active={prefab.activeSelf}, scale={prefab.transform.localScale}");
        foreach (MeshRenderer renderer in renderers)
        {
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            Material material = renderer.sharedMaterial;
            string baseColor = material != null && material.HasProperty("_BaseColor")
                ? material.GetColor("_BaseColor").ToString()
                : "n/a";
            string surface = material != null && material.HasProperty("_Surface")
                ? material.GetFloat("_Surface").ToString()
                : "n/a";
            string cull = material != null && material.HasProperty("_Cull")
                ? material.GetFloat("_Cull").ToString()
                : "n/a";
            Debug.Log(
                $"[VFXForge Dogfood] Renderer={renderer.name}, "
                + $"enabled={renderer.enabled}, active={renderer.gameObject.activeInHierarchy}, "
                + $"position={renderer.transform.localPosition}, "
                + $"mesh={mesh?.name ?? "null"}, vertices={mesh?.vertexCount ?? 0}, "
                + $"triangles={(mesh?.triangles.Length ?? 0) / 3}, "
                + $"bounds={mesh?.bounds.ToString() ?? "null"}, "
                + $"material={material?.name ?? "null"}, "
                + $"shader={material?.shader?.name ?? "null"}, "
                + $"baseColor={baseColor}, surface={surface}, cull={cull}, "
                + $"queue={material?.renderQueue ?? -1}");
        }
    }

    public static void InspectPreviewV2()
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(GeneratedV2Path);
        VfxPreviewOpenResult open = VfxPreviewSession.Open(prefab);
        if (!open.Success || open.Session == null)
        {
            throw new InvalidOperationException(
                $"{open.ErrorCode}: {open.Message}");
        }

        using (open.Session)
        {
            MeshRenderer[] renderers =
                open.Session.PreviewInstance.GetComponentsInChildren<MeshRenderer>(
                    true);
            Camera camera = open.Session.PreviewCamera;
            Debug.Log(
                $"[VFXForge Dogfood] Preview root active="
                + $"{open.Session.PreviewInstance.activeInHierarchy}, "
                + $"renderers={renderers.Length}, cameraMask={camera.cullingMask}, "
                + $"pipeline={GraphicsSettings.currentRenderPipeline?.name ?? "built-in"}");
            foreach (MeshRenderer renderer in renderers)
            {
                Material material = renderer.sharedMaterial;
                Debug.Log(
                    $"[VFXForge Dogfood] Preview renderer={renderer.name}, "
                    + $"active={renderer.gameObject.activeInHierarchy}, "
                    + $"layer={renderer.gameObject.layer}, enabled={renderer.enabled}, "
                    + $"shaderSupported={material?.shader?.isSupported ?? false}, "
                    + $"worldBounds={renderer.bounds}");
            }
        }
    }

    public static void InspectPreviewV4()
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(GeneratedV4Path);
        VfxPreviewOpenResult open = VfxPreviewSession.Open(prefab);
        if (!open.Success || open.Session == null)
        {
            throw new InvalidOperationException(
                $"{open.ErrorCode}: {open.Message}");
        }

        using (open.Session)
        {
            open.Session.SetCameraView(VfxPreviewView.Front);
            Camera camera = open.Session.PreviewCamera;
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
            MeshRenderer[] renderers =
                open.Session.PreviewInstance.GetComponentsInChildren<MeshRenderer>(
                    true);
            int active = 0;
            int inFrustum = 0;
            foreach (MeshRenderer renderer in renderers)
            {
                active += renderer.gameObject.activeInHierarchy && renderer.enabled
                    ? 1
                    : 0;
                inFrustum += GeometryUtility.TestPlanesAABB(
                    planes,
                    renderer.bounds)
                    ? 1
                    : 0;
            }

            MeshRenderer sample = renderers.Length > 0 ? renderers[0] : null;
            Debug.Log(
                $"[VFXForge Dogfood] V4 preview renderers={renderers.Length}, "
                + $"active={active}, inFrustum={inFrustum}, "
                + $"camera={camera.transform.position}, "
                + $"sampleCenter={sample?.bounds.center.ToString() ?? "none"}, "
                + $"sampleViewport="
                + $"{(sample != null ? camera.WorldToViewportPoint(sample.bounds.center).ToString() : "none")}, "
                + $"pipeline={GraphicsSettings.currentRenderPipeline?.name ?? "built-in"}");
        }
    }

    public static void CapturePreviewV2WithControlCube()
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(GeneratedV2Path);
        VfxPreviewOpenResult open = VfxPreviewSession.Open(prefab);
        if (!open.Success || open.Session == null)
        {
            throw new InvalidOperationException(
                $"{open.ErrorCode}: {open.Message}");
        }

        Material controlMaterial = null;
        using (open.Session)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Control Cube";
            cube.transform.SetParent(
                open.Session.PreviewInstance.transform,
                false);
            cube.transform.localPosition = new Vector3(-0.75f, 1f, -0.1f);
            cube.transform.localScale = Vector3.one * 0.45f;
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());

            Shader shader =
                Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color");
            controlMaterial = new Material(shader);
            if (controlMaterial.HasProperty("_BaseColor"))
            {
                controlMaterial.SetColor("_BaseColor", Color.magenta);
            }
            if (controlMaterial.HasProperty("_Color"))
            {
                controlMaterial.SetColor("_Color", Color.magenta);
            }
            cube.GetComponent<Renderer>().sharedMaterial = controlMaterial;

            string outputDirectory = Path.GetFullPath(
                Path.Combine(
                    Application.dataPath,
                    "../Artifacts/df-001-mesh-diagnostic"));
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }

            var recipe = new VfxRecipe
            {
                id = "df001_mesh_diagnostic",
                capture = new VfxCaptureSettings
                {
                    duration = 0.1f,
                    frameTimes = new[] { 0f },
                    views = new[] { "front" },
                    width = 256,
                    height = 256
                }
            };
            VfxFrameCaptureResult capture =
                VfxFrameCapture.Capture(open.Session, recipe, outputDirectory);
            if (!capture.Success)
            {
                throw new InvalidOperationException(
                    $"{capture.ErrorCode}: {capture.Message}");
            }

            Debug.Log(
                $"[VFXForge Dogfood] Mesh diagnostic written: "
                + capture.FramePaths[0]);
        }

        if (controlMaterial != null)
        {
            UnityEngine.Object.DestroyImmediate(controlMaterial);
        }
    }

    public static void CapturePreviewV4WithoutGraph()
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(GeneratedV4Path);
        VfxPreviewOpenResult open = VfxPreviewSession.Open(prefab);
        if (!open.Success || open.Session == null)
        {
            throw new InvalidOperationException(
                $"{open.ErrorCode}: {open.Message}");
        }

        using (open.Session)
        {
            foreach (VisualEffect effect in
                open.Session.PreviewInstance.GetComponentsInChildren<VisualEffect>(
                    true))
            {
                effect.enabled = false;
            }

            string outputDirectory = Path.GetFullPath(
                Path.Combine(
                    Application.dataPath,
                    "../Artifacts/df-001-v4-graph-disabled"));
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }

            var recipe = new VfxRecipe
            {
                id = "df001_v4_graph_disabled",
                capture = new VfxCaptureSettings
                {
                    duration = 0.1f,
                    frameTimes = new[] { 0f },
                    views = new[] { "front" },
                    width = 256,
                    height = 256
                }
            };
            VfxFrameCaptureResult capture =
                VfxFrameCapture.Capture(open.Session, recipe, outputDirectory);
            if (!capture.Success)
            {
                throw new InvalidOperationException(
                    $"{capture.ErrorCode}: {capture.Message}");
            }

            Debug.Log(
                $"[VFXForge Dogfood] Graph-disabled diagnostic written: "
                + capture.FramePaths[0]);
        }
    }

    public static void CaptureV4MaterialControl()
    {
        const string assetRoot = "Assets/__VfxForgeV4MaterialControl";
        if (AssetDatabase.IsValidFolder(assetRoot))
        {
            AssetDatabase.DeleteAsset(assetRoot);
        }
        AssetDatabase.CreateFolder("Assets", "__VfxForgeV4MaterialControl");

        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(CoreMaterialV4Path);
        GameObject source = new GameObject("V4 Material Control");
        VfxPreviewSession session = null;
        try
        {
            source.AddComponent<VfxMetadata>();
            source.AddComponent<VisualEffect>();
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(source.transform, false);
            cube.transform.localPosition = Vector3.up;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
                source,
                $"{assetRoot}/Control.prefab");
            VfxPreviewOpenResult open = VfxPreviewSession.Open(prefab);
            if (!open.Success || open.Session == null)
            {
                throw new InvalidOperationException(
                    $"{open.ErrorCode}: {open.Message}");
            }
            session = open.Session;

            string outputDirectory = Path.GetFullPath(
                Path.Combine(
                    Application.dataPath,
                    "../Artifacts/df-001-v4-material-control"));
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }
            var recipe = new VfxRecipe
            {
                id = "df001_v4_material_control",
                capture = new VfxCaptureSettings
                {
                    duration = 0.1f,
                    frameTimes = new[] { 0f },
                    views = new[] { "front" },
                    width = 256,
                    height = 256
                }
            };
            VfxFrameCaptureResult capture =
                VfxFrameCapture.Capture(session, recipe, outputDirectory);
            if (!capture.Success)
            {
                throw new InvalidOperationException(
                    $"{capture.ErrorCode}: {capture.Message}");
            }
            Debug.Log(
                $"[VFXForge Dogfood] V4 material control written: "
                + capture.FramePaths[0]);
        }
        finally
        {
            session?.Dispose();
            UnityEngine.Object.DestroyImmediate(source);
            if (AssetDatabase.IsValidFolder(assetRoot))
            {
                AssetDatabase.DeleteAsset(assetRoot);
            }
        }
    }

    public static void ConfigureUrp()
    {
        EnsureAssetsDoNotExist(RendererDataPath, PipelineAssetPath);
        EnsureFolder(RenderPipelineRoot);

        var rendererData =
            ScriptableObject.CreateInstance<UniversalRendererData>();
        rendererData.name = "Dogfood Universal Renderer";
        AssetDatabase.CreateAsset(rendererData, RendererDataPath);

        UniversalRenderPipelineAsset pipelineAsset =
            UniversalRenderPipelineAsset.Create(rendererData);
        pipelineAsset.name = "Dogfood Universal Render Pipeline";
        AssetDatabase.CreateAsset(pipelineAsset, PipelineAssetPath);

        GraphicsSettings.defaultRenderPipeline = pipelineAsset;
        QualitySettings.renderPipeline = pipelineAsset;
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[VFXForge Dogfood] URP configured. "
            + $"Pipeline={PipelineAssetPath}, Renderer={RendererDataPath}");
    }

    public static void ConfigureUrpV2()
    {
        EnsureAssetsDoNotExist(
            RendererDataV2Path,
            PipelineAssetV2Path,
            TemporaryRendererDataPath);

        UniversalRenderPipelineAsset pipelineAsset =
            UniversalRenderPipelineAsset.Create();
        pipelineAsset.name = "Dogfood Universal Render Pipeline V2";
        AssetDatabase.CreateAsset(pipelineAsset, PipelineAssetV2Path);

        ScriptableRendererData rendererData =
            pipelineAsset.LoadBuiltinRendererData();
        if (rendererData == null)
        {
            throw new InvalidOperationException(
                "Unity did not create the built-in Universal Renderer Data.");
        }
        AssetDatabase.SaveAssets();

        string moveError = AssetDatabase.MoveAsset(
            TemporaryRendererDataPath,
            RendererDataV2Path);
        if (!string.IsNullOrEmpty(moveError))
        {
            throw new InvalidOperationException(
                $"Could not move Renderer Data: {moveError}");
        }

        rendererData.name = "Dogfood Universal Renderer V2";
        EditorUtility.SetDirty(rendererData);
        EditorUtility.SetDirty(pipelineAsset);
        GraphicsSettings.defaultRenderPipeline = pipelineAsset;
        QualitySettings.renderPipeline = pipelineAsset;
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[VFXForge Dogfood] URP V2 configured. "
            + $"Pipeline={PipelineAssetV2Path}, Renderer={RendererDataV2Path}");
    }

    public static void CreateDemoScene()
    {
        EnsureAssetsDoNotExist(DemoScenePath, DemoGroundMaterialPath);
        EnsureFolder(DemoRoot);

        GameObject slashPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(GeneratedV8Path);
        if (slashPrefab == null)
        {
            throw new InvalidOperationException(
                $"Generated V8 Prefab is missing: {GeneratedV8Path}");
        }

        Material groundMaterial = CreateDefaultUnlitMaterial(
            "Sword Slash Demo Ground",
            new Color(0.055f, 0.065f, 0.085f, 1f));
        AssetDatabase.CreateAsset(groundMaterial, DemoGroundMaterialPath);

        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single);

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.01f, 0.015f, 0.025f, 1f);
        camera.fieldOfView = 48f;
        cameraObject.transform.position = new Vector3(0f, 2.2f, -8f);
        cameraObject.transform.LookAt(new Vector3(0f, 1f, 3f));

        var lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightObject.transform.rotation = Quaternion.Euler(42f, -30f, 0f);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0f, -0.15f, 4f);
        ground.transform.localScale = new Vector3(12f, 0.2f, 20f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = groundMaterial;

        var controllerObject = new GameObject("Sword Slash Demo Controller");
        SwordSlashDemoController controller =
            controllerObject.AddComponent<SwordSlashDemoController>();
        controller.Configure(slashPrefab, 0.8f, true);

        if (!EditorSceneManager.SaveScene(scene, DemoScenePath))
        {
            throw new InvalidOperationException(
                $"Demo Scene could not be saved: {DemoScenePath}");
        }

        var scenes = new List<EditorBuildSettingsScene>(
            EditorBuildSettings.scenes);
        if (!scenes.Exists(item => item.path == DemoScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(DemoScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"[VFXForge Dogfood] Sword slash demo Scene created: "
            + DemoScenePath);
    }

    [MenuItem("Tools/VFX Forge/Dogfood/Open Sword Slash Demo")]
    public static void OpenDemoScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DemoScenePath) == null)
        {
            throw new InvalidOperationException(
                $"Demo Scene is missing: {DemoScenePath}");
        }
        EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
    }

    public static void CaptureDemoStill()
    {
        Scene scene = EditorSceneManager.OpenScene(
            DemoScenePath,
            OpenSceneMode.Single);
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(GeneratedV8Path);
        GameObject instance =
            PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (instance == null)
        {
            throw new InvalidOperationException(
                "Could not instantiate the generated V8 Prefab in the demo Scene.");
        }
        instance.name = "Basic Sword Slash (Still)";

        Camera camera = Camera.main;
        if (camera == null)
        {
            throw new InvalidOperationException(
                "Demo Scene has no Main Camera.");
        }

        string outputPath = Path.GetFullPath(
            Path.Combine(
                Application.dataPath,
                "../Artifacts/dogfood/DF-001-demo-still.png"));
        string evidencePath = Path.GetFullPath(
            Path.Combine(
                Application.dataPath,
                "../../Dogfooding/Evidence/DF-001-demo-still.png"));
        if (File.Exists(evidencePath))
        {
            throw new InvalidOperationException(
                $"Refusing to overwrite existing evidence: {evidencePath}");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        Directory.CreateDirectory(Path.GetDirectoryName(evidencePath));
        byte[] png = RenderCameraPng(camera, 512, 512);
        File.WriteAllBytes(outputPath, png);
        File.WriteAllBytes(evidencePath, png);
        UnityEngine.Object.DestroyImmediate(instance);

        Debug.Log(
            $"[VFXForge Dogfood] Demo still written: "
            + $"{outputPath}, evidence={evidencePath}");
    }

    private static void EnsureTargetsDoNotExist()
    {
        string[] targets =
        {
            GraphPath,
            OuterMeshPath,
            CoreMeshPath,
            OuterMaterialPath,
            CoreMaterialPath,
            TemplatePath,
            CatalogPath
        };
        foreach (string target in targets)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(target) != null)
            {
                throw new InvalidOperationException(
                    $"Refusing to overwrite existing Asset: {target}");
            }
        }
    }

    private static void EnsureAssetsDoNotExist(params string[] targets)
    {
        foreach (string target in targets)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(target) != null)
            {
                throw new InvalidOperationException(
                    $"Refusing to overwrite existing Asset: {target}");
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

    private static Mesh CreateArcMesh(
        string name,
        float innerRadius,
        float outerRadius,
        float startDegrees,
        float endDegrees,
        int segments)
    {
        var vertices = new Vector3[(segments + 1) * 2];
        var colors = new Color[vertices.Length];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[segments * 6];

        for (int index = 0; index <= segments; index++)
        {
            float progress = index / (float)segments;
            float radians =
                Mathf.Lerp(startDegrees, endDegrees, progress) * Mathf.Deg2Rad;
            var direction = new Vector3(
                Mathf.Cos(radians),
                Mathf.Sin(radians),
                0f);
            int vertex = index * 2;
            vertices[vertex] = direction * innerRadius;
            vertices[vertex + 1] = direction * outerRadius;
            float tipFade = Mathf.Sin(progress * Mathf.PI);
            colors[vertex] = new Color(1f, 1f, 1f, tipFade);
            colors[vertex + 1] = new Color(1f, 1f, 1f, tipFade);
            uv[vertex] = new Vector2(progress, 0f);
            uv[vertex + 1] = new Vector2(progress, 1f);

            if (index == segments)
            {
                continue;
            }

            int triangle = index * 6;
            triangles[triangle] = vertex;
            triangles[triangle + 1] = vertex + 3;
            triangles[triangle + 2] = vertex + 1;
            triangles[triangle + 3] = vertex;
            triangles[triangle + 4] = vertex + 2;
            triangles[triangle + 5] = vertex + 3;
        }

        var mesh = new Mesh
        {
            name = name,
            vertices = vertices,
            colors = colors,
            uv = uv,
            triangles = triangles
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CreateDoubleSidedArcMesh(
        string name,
        float innerRadius,
        float outerRadius,
        float startDegrees,
        float endDegrees,
        int segments)
    {
        var vertices = new Vector3[(segments + 1) * 2];
        var colors = new Color[vertices.Length];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[segments * 12];

        for (int index = 0; index <= segments; index++)
        {
            float progress = index / (float)segments;
            float radians =
                Mathf.Lerp(startDegrees, endDegrees, progress) * Mathf.Deg2Rad;
            var direction = new Vector3(
                Mathf.Cos(radians),
                Mathf.Sin(radians),
                0f);
            int vertex = index * 2;
            vertices[vertex] = direction * innerRadius;
            vertices[vertex + 1] = direction * outerRadius;
            float tipFade = Mathf.Sin(progress * Mathf.PI);
            colors[vertex] = new Color(1f, 1f, 1f, tipFade);
            colors[vertex + 1] = new Color(1f, 1f, 1f, tipFade);
            uv[vertex] = new Vector2(progress, 0f);
            uv[vertex + 1] = new Vector2(progress, 1f);

            if (index == segments)
            {
                continue;
            }

            int triangle = index * 12;
            triangles[triangle] = vertex;
            triangles[triangle + 1] = vertex + 3;
            triangles[triangle + 2] = vertex + 1;
            triangles[triangle + 3] = vertex;
            triangles[triangle + 4] = vertex + 2;
            triangles[triangle + 5] = vertex + 3;
            triangles[triangle + 6] = vertex;
            triangles[triangle + 7] = vertex + 1;
            triangles[triangle + 8] = vertex + 3;
            triangles[triangle + 9] = vertex;
            triangles[triangle + 10] = vertex + 3;
            triangles[triangle + 11] = vertex + 2;
        }

        var mesh = new Mesh
        {
            name = name,
            vertices = vertices,
            colors = colors,
            uv = uv,
            triangles = triangles
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Material CreateUnlitMaterial(string name, Color color)
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color");
        if (shader == null)
        {
            throw new InvalidOperationException(
                "No supported unlit Shader is available.");
        }

        var material = new Material(shader)
        {
            name = name,
            renderQueue = 3000
        };
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }
        if (material.HasProperty("_Cull"))
        {
            material.SetFloat("_Cull", 0f);
        }
        if (material.HasProperty("_ZWrite"))
        {
            material.SetFloat("_ZWrite", 0f);
        }
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetOverrideTag("RenderType", "Transparent");
        return material;
    }

    private static Material CreateOpaqueUnlitMaterial(string name, Color color)
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color");
        if (shader == null)
        {
            throw new InvalidOperationException(
                "No supported unlit Shader is available.");
        }

        var material = new Material(shader)
        {
            name = name,
            renderQueue = 2000
        };
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 0f);
        }
        if (material.HasProperty("_Cull"))
        {
            material.SetFloat("_Cull", 0f);
        }
        if (material.HasProperty("_ZWrite"))
        {
            material.SetFloat("_ZWrite", 1f);
        }
        material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetOverrideTag("RenderType", "Opaque");
        return material;
    }

    private static Material CreateDefaultUnlitMaterial(string name, Color color)
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color");
        if (shader == null)
        {
            throw new InvalidOperationException(
                "No supported unlit Shader is available.");
        }

        var material = new Material(shader)
        {
            name = name
        };
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
        return material;
    }

    private static void AddArcChild(
        Transform parent,
        string name,
        Mesh mesh,
        Material material,
        Vector3 position,
        Quaternion rotation)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = position;
        child.transform.localRotation = rotation;
        child.AddComponent<MeshFilter>().sharedMesh = mesh;
        child.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static void AddSegmentedArc(
        Transform parent,
        string name,
        Material material,
        float radius,
        int segmentCount,
        Vector3 segmentScale,
        float z)
    {
        var group = new GameObject(name);
        group.transform.SetParent(parent, false);
        group.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);

        for (int index = 0; index < segmentCount; index++)
        {
            float progress = index / (float)(segmentCount - 1);
            float degrees = Mathf.Lerp(-70f, 70f, progress);
            float radians = degrees * Mathf.Deg2Rad;
            var segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = $"Segment {index:00}";
            segment.transform.SetParent(group.transform, false);
            segment.transform.localPosition = new Vector3(
                Mathf.Cos(radians) * radius,
                1f + Mathf.Sin(radians) * radius,
                z);
            segment.transform.localRotation =
                Quaternion.Euler(0f, 0f, degrees);
            segment.transform.localScale = segmentScale;
            segment.GetComponent<MeshRenderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(segment.GetComponent<Collider>());
        }
    }

    private static void AddDirectSegmentedArc(
        Transform parent,
        string prefix,
        Material material,
        float radius,
        int segmentCount,
        Vector3 segmentScale,
        float z)
    {
        for (int index = 0; index < segmentCount; index++)
        {
            float progress = index / (float)(segmentCount - 1);
            float degrees = Mathf.Lerp(-68f, 68f, progress);
            float rotatedDegrees = degrees - 12f;
            float radians = rotatedDegrees * Mathf.Deg2Rad;
            var segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = $"{prefix} Segment {index:00}";
            segment.transform.SetParent(parent, false);
            segment.transform.localPosition = new Vector3(
                Mathf.Cos(radians) * radius,
                1f + Mathf.Sin(radians) * radius,
                z);
            segment.transform.localRotation =
                Quaternion.Euler(0f, 0f, rotatedDegrees);
            segment.transform.localScale = segmentScale;
            segment.GetComponent<MeshRenderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(segment.GetComponent<Collider>());
        }
    }

    private static void AddBlade(
        Transform parent,
        string name,
        Material material,
        float length,
        float width,
        float depth)
    {
        GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blade.name = name;
        blade.transform.SetParent(parent, false);
        blade.transform.localPosition = Vector3.up;
        blade.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
        blade.transform.localScale = new Vector3(length, width, depth);
        blade.GetComponent<MeshRenderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(blade.GetComponent<Collider>());
    }

    private static byte[] RenderCameraPng(Camera camera, int width, int height)
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
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            texture = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false);
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
}
