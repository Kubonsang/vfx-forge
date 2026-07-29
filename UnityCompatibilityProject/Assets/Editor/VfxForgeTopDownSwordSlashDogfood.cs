using System;
using System.Collections.Generic;
using System.IO;
using Kubonsang.VfxForge;
using Kubonsang.VfxForge.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;
using VfxForge.Dogfood;

public static class VfxForgeTopDownSwordSlashDogfood
{
    private const string Root = "Assets/VFXForge/Dogfood/TopDownSwordSlash";
    private const string AuthoringRoot = Root + "/Authoring";
    private const string DemoRoot = Root + "/Demo";
    private const string GraphPath = AuthoringRoot + "/TopDownSwordSlashBurst.vfx";
    private const string OuterMeshPath = AuthoringRoot + "/TopDownCrescentOuter.asset";
    private const string CoreMeshPath = AuthoringRoot + "/TopDownCrescentCore.asset";
    private const string OuterMaterialPath = AuthoringRoot + "/TopDownCrescentOuter.mat";
    private const string CoreMaterialPath = AuthoringRoot + "/TopDownCrescentCore.mat";
    private const string TemplatePath = AuthoringRoot + "/TopDownCrescentTemplate.prefab";
    private const string CatalogPath = Root + "/TopDownCrescentCatalog.asset";
    private const string GeneratedPrefabPath = Root + "/Generated/TopDownCrescentSlash.prefab";
    private const string DemoScenePath = DemoRoot + "/TopDownSwordSlashDemo.unity";
    private const string GroundMaterialPath = DemoRoot + "/TopDownDemoGround.mat";
    private const string SourceGraphPath =
        "Packages/com.unity.visualeffectgraph/Editor/Templates/03_Simple_Burst.vfx";

    public static void BuildAuthoringAssets()
    {
        EnsureTargetsAbsent(
            GraphPath,
            OuterMeshPath,
            CoreMeshPath,
            OuterMaterialPath,
            CoreMaterialPath,
            TemplatePath,
            CatalogPath);
        EnsureFolder(AuthoringRoot);

        if (!AssetDatabase.CopyAsset(SourceGraphPath, GraphPath))
        {
            throw new InvalidOperationException(
                $"Could not copy built-in VFX Graph: {SourceGraphPath}");
        }
        AssetDatabase.ImportAsset(GraphPath, ImportAssetOptions.ForceSynchronousImport);

        Mesh outerMesh = CreateCrescentMesh(
            "Top Down Crescent Outer",
            0.88f,
            1.72f,
            0.16f,
            112f,
            28);
        Mesh coreMesh = CreateCrescentMesh(
            "Top Down Crescent Core",
            1.14f,
            1.48f,
            0.18f,
            106f,
            28);
        AssetDatabase.CreateAsset(outerMesh, OuterMeshPath);
        AssetDatabase.CreateAsset(coreMesh, CoreMeshPath);

        Material outerMaterial = CreateUnlitMaterial(
            "Top Down Crescent Outer",
            new Color(0.04f, 0.38f, 1f, 1f));
        Material coreMaterial = CreateUnlitMaterial(
            "Top Down Crescent Core",
            new Color(0.78f, 0.96f, 1f, 1f));
        AssetDatabase.CreateAsset(outerMaterial, OuterMaterialPath);
        AssetDatabase.CreateAsset(coreMaterial, CoreMaterialPath);

        VisualEffectAsset graph = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(GraphPath);
        GameObject source = new GameObject("Top Down Crescent Template");
        try
        {
            VfxPlayer player = source.AddComponent<VfxPlayer>();
            player.Configure("OnPlay");
            SwordSlashProjectile projectile = source.AddComponent<SwordSlashProjectile>();
            projectile.Configure(12f, 0.55f);

            AddMeshChild(
                source.transform,
                "Outer Glow",
                outerMesh,
                outerMaterial,
                new Vector3(0f, 0.34f, 0f));
            AddMeshChild(
                source.transform,
                "White Core",
                coreMesh,
                coreMaterial,
                new Vector3(0f, 0.44f, 0f));

            var particles = new GameObject("Burst Particles");
            particles.transform.SetParent(source.transform, false);
            particles.transform.localPosition = new Vector3(0f, 0.45f, 0.85f);
            VisualEffect effect = particles.AddComponent<VisualEffect>();
            effect.visualEffectAsset = graph;
            effect.initialEventName = "OnPlay";
            effect.startSeed = 240729u;
            effect.resetSeedOnPlay = false;
            effect.enabled = false;

            GameObject template = PrefabUtility.SaveAsPrefabAsset(source, TemplatePath);
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
            id = "topdown_crescent_slash",
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TemplatePath),
            playEventName = "OnPlay",
            supportedLayers = new[] { "slash_core", "slash_glow" },
            bindings = new List<VfxPropertyBinding>()
        });
        AssetDatabase.CreateAsset(catalog, CatalogPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[VFXForge Dogfood] Top-down crescent authoring assets created. "
            + $"Template={TemplatePath}, Catalog={CatalogPath}");
    }

    public static void CreateDemoScene()
    {
        EnsureTargetsAbsent(DemoScenePath, GroundMaterialPath);
        EnsureFolder(DemoRoot);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GeneratedPrefabPath);
        if (prefab == null)
        {
            throw new InvalidOperationException(
                $"Generated Prefab is missing: {GeneratedPrefabPath}");
        }

        Material groundMaterial = CreateUnlitMaterial(
            "Top Down Demo Ground",
            new Color(0.045f, 0.055f, 0.08f, 1f));
        AssetDatabase.CreateAsset(groundMaterial, GroundMaterialPath);

        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single);

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.6f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.006f, 0.01f, 0.02f, 1f);
        cameraObject.transform.position = new Vector3(0f, 12f, 2.5f);
        cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0f, -0.15f, 2.5f);
        ground.transform.localScale = new Vector3(12f, 0.2f, 17f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = groundMaterial;
        UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());

        var controllerObject = new GameObject("Top Down Sword Slash Demo Controller");
        TopDownSwordSlashDemoController controller =
            controllerObject.AddComponent<TopDownSwordSlashDemoController>();
        controller.Configure(prefab, new Vector3(0f, 0f, -3f), 0.9f);

        if (!EditorSceneManager.SaveScene(scene, DemoScenePath))
        {
            throw new InvalidOperationException(
                $"Demo Scene could not be saved: {DemoScenePath}");
        }

        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!scenes.Exists(item => item.path == DemoScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(DemoScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"[VFXForge Dogfood] Top-down crescent demo Scene created: {DemoScenePath}");
    }

    [MenuItem("Tools/VFX Forge/Dogfood/Open Top Down Crescent Demo")]
    public static void OpenDemoScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DemoScenePath) == null)
        {
            throw new InvalidOperationException($"Demo Scene is missing: {DemoScenePath}");
        }
        EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
    }

    public static void CaptureDemoStill()
    {
        Scene scene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GeneratedPrefabPath);
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (instance == null || Camera.main == null)
        {
            throw new InvalidOperationException("Demo Prefab or Main Camera is missing.");
        }
        instance.transform.position = new Vector3(0f, 0f, 2.5f);

        string repositoryRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
        string evidencePath = Path.Combine(
            repositoryRoot,
            "Dogfooding/Evidence/DF-002-topdown-crescent-still.png");
        if (File.Exists(evidencePath))
        {
            throw new InvalidOperationException(
                $"Refusing to overwrite existing evidence: {evidencePath}");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(evidencePath));
        File.WriteAllBytes(evidencePath, RenderCameraPng(Camera.main, 512, 512));
        UnityEngine.Object.DestroyImmediate(instance);

        Debug.Log($"[VFXForge Dogfood] Top-down crescent evidence written: {evidencePath}");
    }

    private static void EnsureTargetsAbsent(params string[] paths)
    {
        foreach (string path in paths)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
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

    private static Mesh CreateCrescentMesh(
        string name,
        float innerRadius,
        float outerRadius,
        float thickness,
        float halfAngleDegrees,
        int segments)
    {
        int ringCount = segments + 1;
        var vertices = new Vector3[ringCount * 4];
        var triangles = new List<int>(segments * 24);

        for (int index = 0; index <= segments; index++)
        {
            float progress = index / (float)segments;
            float angle = Mathf.Lerp(-halfAngleDegrees, halfAngleDegrees, progress)
                * Mathf.Deg2Rad;
            float taper = Mathf.Lerp(0.55f, 1f, Mathf.Sin(progress * Mathf.PI));
            float inner = Mathf.Lerp(outerRadius, innerRadius, taper);
            float outer = outerRadius;
            Vector3 innerPoint = new Vector3(
                Mathf.Sin(angle) * inner,
                0f,
                Mathf.Cos(angle) * inner);
            Vector3 outerPoint = new Vector3(
                Mathf.Sin(angle) * outer,
                0f,
                Mathf.Cos(angle) * outer);
            int vertex = index * 4;
            vertices[vertex] = innerPoint + Vector3.up * thickness;
            vertices[vertex + 1] = outerPoint + Vector3.up * thickness;
            vertices[vertex + 2] = innerPoint;
            vertices[vertex + 3] = outerPoint;

            if (index == segments)
            {
                continue;
            }

            int next = vertex + 4;
            AddQuad(triangles, vertex, next, vertex + 1, next + 1);
            AddQuad(triangles, vertex + 2, vertex + 3, next + 2, next + 3);
            AddQuad(triangles, vertex, vertex + 2, next, next + 2);
            AddQuad(triangles, vertex + 1, next + 1, vertex + 3, next + 3);
        }

        AddQuad(triangles, 0, 1, 2, 3);
        int end = segments * 4;
        AddQuad(triangles, end, end + 2, end + 1, end + 3);

        var mesh = new Mesh
        {
            name = name,
            vertices = vertices,
            triangles = triangles.ToArray()
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void AddQuad(List<int> triangles, int a, int b, int c, int d)
    {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
        triangles.Add(c);
        triangles.Add(b);
        triangles.Add(d);
    }

    private static Material CreateUnlitMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color");
        if (shader == null)
        {
            throw new InvalidOperationException("No supported unlit Shader is available.");
        }

        var material = new Material(shader) { name = name };
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

    private static void AddMeshChild(
        Transform parent,
        string name,
        Mesh mesh,
        Material material,
        Vector3 position)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = position;
        child.AddComponent<MeshFilter>().sharedMesh = mesh;
        child.AddComponent<MeshRenderer>().sharedMaterial = material;
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
