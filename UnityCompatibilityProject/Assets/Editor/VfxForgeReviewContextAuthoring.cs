using System.IO;
using Kubonsang.VfxForge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VfxForgeReviewContextAuthoring
{
    public const string Root =
        "Assets/VFXForge/Dogfood/ReviewContexts";
    public const string PrefabPath =
        Root + "/TopDownThreeGrounds.prefab";

    [MenuItem(
        "Tools/VFX Forge/Dogfood/Create Default Review Context")]
    public static void CreateDefaultContext()
    {
        GameObject existing =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabPath);
        if (existing != null)
        {
            Selection.activeObject = existing;
            EditorGUIUtility.PingObject(existing);
            Debug.Log(
                $"[VFXForge] Review Context already exists: {PrefabPath}");
            return;
        }

        EnsureFolder(Root);
        Material lightGround = CreateMaterial(
            Root + "/GroundLight.mat",
            new Color(0.63f, 0.67f, 0.62f, 1f));
        Material mediumGround = CreateMaterial(
            Root + "/GroundMedium.mat",
            new Color(0.24f, 0.29f, 0.27f, 1f));
        Material darkGround = CreateMaterial(
            Root + "/GroundDark.mat",
            new Color(0.055f, 0.07f, 0.065f, 1f));
        Material casterMaterial = CreateMaterial(
            Root + "/Caster.mat",
            new Color(0.18f, 0.42f, 0.68f, 1f));
        Material targetMaterial = CreateMaterial(
            Root + "/Target.mat",
            new Color(0.62f, 0.18f, 0.16f, 1f));

        var root = new GameObject(
            "TopDown Three Grounds Review Context");
        try
        {
            var context =
                root.AddComponent<VfxReviewContext>();
            Camera camera = CreateCamera(root.transform);
            Transform anchor =
                new GameObject("Effect Anchor").transform;
            anchor.SetParent(root.transform, false);
            anchor.localPosition = Vector3.zero;

            CreateGround(
                root.transform,
                "Light Ground",
                new Vector3(-4f, -0.15f, 0f),
                lightGround);
            CreateGround(
                root.transform,
                "Medium Ground",
                new Vector3(0f, -0.15f, 0f),
                mediumGround);
            CreateGround(
                root.transform,
                "Dark Ground",
                new Vector3(4f, -0.15f, 0f),
                darkGround);

            Transform caster = CreateActor(
                root.transform,
                "Caster",
                new Vector3(0f, 0.65f, -2.4f),
                casterMaterial,
                true);
            Transform target = CreateActor(
                root.transform,
                "Target",
                new Vector3(0f, 0.65f, 3f),
                targetMaterial,
                false);

            context.reviewCamera = camera;
            context.effectAnchor = anchor;
            context.caster = caster;
            context.target = target;

            GameObject prefab =
                PrefabUtility.SaveAsPrefabAsset(
                    root,
                    PrefabPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log(
                $"[VFXForge] Created Review Context: {PrefabPath}");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    public static void CreateDefaultContextBatch()
    {
        CreateDefaultContext();
    }

    public static void CaptureDefaultContextBatch()
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabPath);
        if (prefab == null)
        {
            throw new FileNotFoundException(
                "Default Review Context is missing.",
                PrefabPath);
        }

        Scene scene =
            EditorSceneManager.NewPreviewScene();
        RenderTexture renderTexture = null;
        Texture2D texture = null;
        try
        {
            GameObject instance =
                PrefabUtility.InstantiatePrefab(prefab, scene)
                    as GameObject;
            VfxReviewContext context =
                instance.GetComponent<VfxReviewContext>();
            Camera camera = context.reviewCamera;
            camera.enabled = false;
            camera.cameraType = CameraType.Preview;
            camera.scene = scene;
            camera.aspect = 16f / 9f;

            renderTexture =
                RenderTexture.GetTemporary(
                    1280,
                    720,
                    24,
                    RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.sRGB);
            texture =
                new Texture2D(
                    1280,
                    720,
                    TextureFormat.RGBA32,
                    false);
            RenderTexture previousActive =
                RenderTexture.active;
            RenderTexture previousTarget =
                camera.targetTexture;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(
                    new Rect(0f, 0f, 1280f, 720f),
                    0,
                    0,
                    false);
                texture.Apply(false, false);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
            }

            string evidenceDirectory =
                Path.GetFullPath(
                    Path.Combine(
                        Application.dataPath,
                        "..",
                        "..",
                        "Dogfooding",
                        "Evidence",
                        "VF-017"));
            Directory.CreateDirectory(evidenceDirectory);
            string output =
                Path.Combine(
                    evidenceDirectory,
                    "default-review-context.png");
            File.WriteAllBytes(
                output,
                texture.EncodeToPNG());
            Debug.Log(
                $"[VFXForge] Captured Review Context: {output}");
        }
        finally
        {
            if (texture != null)
            {
                Object.DestroyImmediate(texture);
            }
            if (renderTexture != null)
            {
                RenderTexture.ReleaseTemporary(
                    renderTexture);
            }
            if (scene.IsValid()
                && EditorSceneManager.IsPreviewScene(scene))
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }

    private static Camera CreateCamera(Transform parent)
    {
        var cameraObject =
            new GameObject("TopDown Review Camera");
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.localPosition =
            new Vector3(0f, 12f, 0f);
        cameraObject.transform.localRotation =
            Quaternion.LookRotation(
                Vector3.down,
                Vector3.forward);
        Camera camera =
            cameraObject.AddComponent<Camera>();
        camera.enabled = false;
        camera.orthographic = true;
        camera.orthographicSize = 7.2f;
        camera.aspect = 16f / 9f;
        camera.clearFlags =
            CameraClearFlags.SolidColor;
        camera.backgroundColor =
            new Color(0.025f, 0.03f, 0.028f, 1f);
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 30f;
        return camera;
    }

    private static void CreateGround(
        Transform parent,
        string name,
        Vector3 position,
        Material material)
    {
        GameObject ground =
            GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = name;
        ground.transform.SetParent(parent, false);
        ground.transform.localPosition = position;
        ground.transform.localScale =
            new Vector3(3.9f, 0.2f, 13.5f);
        ground.GetComponent<Renderer>().sharedMaterial =
            material;
        Object.DestroyImmediate(
            ground.GetComponent<Collider>());
    }

    private static Transform CreateActor(
        Transform parent,
        string name,
        Vector3 position,
        Material material,
        bool addSword)
    {
        GameObject actor =
            GameObject.CreatePrimitive(PrimitiveType.Capsule);
        actor.name = name;
        actor.transform.SetParent(parent, false);
        actor.transform.localPosition = position;
        actor.transform.localScale =
            new Vector3(0.75f, 0.8f, 0.75f);
        actor.GetComponent<Renderer>().sharedMaterial =
            material;
        Object.DestroyImmediate(
            actor.GetComponent<Collider>());

        if (addSword)
        {
            GameObject sword =
                GameObject.CreatePrimitive(PrimitiveType.Cube);
            sword.name = "Sword";
            sword.transform.SetParent(
                actor.transform,
                false);
            sword.transform.localPosition =
                new Vector3(0.75f, 0f, 0.2f);
            sword.transform.localRotation =
                Quaternion.Euler(0f, 35f, 0f);
            sword.transform.localScale =
                new Vector3(0.12f, 0.12f, 1.3f);
            sword.GetComponent<Renderer>().sharedMaterial =
                material;
            Object.DestroyImmediate(
                sword.GetComponent<Collider>());
        }

        return actor.transform;
    }

    private static Material CreateMaterial(
        string path,
        Color color)
    {
        Material existing =
            AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            return existing;
        }

        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Sprites/Default");
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
}
