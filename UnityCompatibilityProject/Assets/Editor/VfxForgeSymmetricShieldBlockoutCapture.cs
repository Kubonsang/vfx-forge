using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Kubonsang.VfxForge.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VfxForge.Dogfood;
using Object = UnityEngine.Object;

public static class VfxForgeSymmetricShieldBlockoutCapture
{
    private const int Width = 1280;
    private const int Height = 720;
    private const string EvidenceDirectory =
        "Dogfooding/Evidence/VF-022R-symmetric-shield-blockout-v1";
    private const string ReferenceImage =
        "Dogfooding/Evidence/VF-022R-model-sheet/"
        + "candidate-e-model-sheet-v3.png";
    private static readonly Color Background =
        new Color(0.035f, 0.04f, 0.055f, 1f);

    [Serializable]
    private sealed class CaptureManifest
    {
        public string schemaVersion = "mesh-capture-1.0";
        public string taskId = "VF-022";
        public string sourceDependencyHash = string.Empty;
        public string runtimeDependencyHash = string.Empty;
        public Vector3 boundsCenter;
        public Vector3 boundsSize;
        public float silhouetteIou;
        public float landmarkMeanError;
        public float landmarkMaximumError;
        public CaptureFrame[] frames = Array.Empty<CaptureFrame>();
    }

    [Serializable]
    private sealed class CaptureFrame
    {
        public string id = string.Empty;
        public string path = string.Empty;
        public string sha256 = string.Empty;
        public int width;
        public int height;
        public float foregroundRatio;
        public float borderForegroundRatio;
    }

    [Serializable]
    private sealed class ConsoleCounts
    {
        public int errors;
        public int warnings;
        public int logs;
    }

    [MenuItem("Tools/VFX Forge/Dogfood/Capture VF-022R Symmetric Shield Blockout")]
    public static void Capture()
    {
        string repository = RepositoryRoot();
        string outputDirectory = Path.Combine(repository, EvidenceDirectory);
        Directory.CreateDirectory(outputDirectory);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            VfxForgeSymmetricShieldProBuilderBlockout.RuntimePrefabPath);
        if (prefab == null)
        {
            throw new InvalidOperationException("Runtime blockout Prefab is missing.");
        }

        Scene scene = EditorSceneManager.NewPreviewScene();
        var textures = new Dictionary<string, Texture2D>();
        try
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene)
                as GameObject;
            Transform assembly = instance.transform.Find(
                "Facing Pivot/Symmetric Shield Assembly");
            if (assembly == null)
            {
                throw new InvalidOperationException(
                    "Runtime blockout is missing its shield assembly.");
            }
            assembly.localRotation = Quaternion.identity;
            Bounds bounds = RendererBounds(instance);
            Camera camera = CreateCamera(scene);
            CreateLight(scene);

            var frames = new List<CaptureFrame>();
            ConfigureCamera(camera, bounds, "front");
            CaptureFrameAndKeep(
                camera, "front-clay", outputDirectory, false, null, frames, textures);
            CaptureFrameAndKeep(
                camera, "front-wireframe", outputDirectory, true, null, frames, textures);
            Material normals = CreateReplacementMaterial(
                "Hidden/VFXForge/MeshReviewNormals");
            try
            {
                CaptureFrameAndKeep(
                    camera, "front-normal", outputDirectory, false, normals, frames, textures);
            }
            finally
            {
                Object.DestroyImmediate(normals);
            }

            LandmarkMetrics landmarks = MeasureLandmarks(
                camera,
                instance,
                repository,
                textures["front-clay"]);
            ConfigureCamera(camera, bounds, "top");
            CaptureFrameAndKeep(
                camera, "top-clay", outputDirectory, false, null, frames, textures);
            CaptureFrameAndKeep(
                camera, "top-wireframe", outputDirectory, true, null, frames, textures);
            ConfigureCamera(camera, bounds, "right-side");
            CaptureFrameAndKeep(
                camera, "right-side-clay", outputDirectory, false, null, frames, textures);
            CaptureFrameAndKeep(
                camera, "right-side-wireframe", outputDirectory, true, null, frames, textures);
            assembly.localRotation = Quaternion.Euler(18f, 0f, 0f);
            Bounds gameplayBounds = RendererBounds(instance);
            ConfigureCamera(camera, gameplayBounds, "gameplay");
            CaptureFrameAndKeep(
                camera, "gameplay-clay", outputDirectory, false, null, frames, textures);

            Texture2D reference = LoadTexture(Path.Combine(repository, ReferenceImage));
            Texture2D referenceFront = CropFront(reference);
            string referencePath = Path.Combine(outputDirectory, "reference-front.png");
            WriteTexture(referencePath, referenceFront);
            frames.Add(AnalyzeFrame(
                "reference-front",
                referencePath,
                referenceFront));
            textures.Add("reference-front", referenceFront);
            Object.DestroyImmediate(reference);

            Texture2D targetSilhouette = BuildTargetSilhouette();
            string targetPath = Path.Combine(
                outputDirectory,
                "target-silhouette.png");
            WriteTexture(targetPath, targetSilhouette);
            frames.Add(AnalyzeFrame(
                "target-silhouette",
                targetPath,
                targetSilhouette));
            textures.Add("target-silhouette", targetSilhouette);

            Texture2D overlay = CreateOverlay(
                textures["front-clay"],
                referenceFront);
            string overlayPath = Path.Combine(outputDirectory, "front-overlay.png");
            WriteTexture(overlayPath, overlay);
            frames.Add(AnalyzeFrame("front-overlay", overlayPath, overlay));
            textures.Add("front-overlay", overlay);

            float iou = CalculateNormalizedIou(
                targetSilhouette,
                textures["front-clay"]);
            if (iou < 0.85f)
            {
                throw new InvalidOperationException(
                    $"Blockout silhouette IoU {iou:F4} is below 0.85.");
            }
            if (landmarks.Mean > 0.03f || landmarks.Maximum > 0.06f)
            {
                throw new InvalidOperationException(
                    $"Blockout landmark error exceeds limits: "
                    + $"mean={landmarks.Mean:F4}, max={landmarks.Maximum:F4}.");
            }

            string contactSheetPath = Path.Combine(
                outputDirectory,
                "blockout-contact-sheet.png");
            Texture2D contactSheet = BuildContactSheet(textures);
            WriteTexture(contactSheetPath, contactSheet);
            Object.DestroyImmediate(contactSheet);

            var manifest = new CaptureManifest
            {
                sourceDependencyHash = AssetDatabase.GetAssetDependencyHash(
                    VfxForgeSymmetricShieldProBuilderBlockout.SourcePrefabPath)
                    .ToString(),
                runtimeDependencyHash = AssetDatabase.GetAssetDependencyHash(
                    VfxForgeSymmetricShieldProBuilderBlockout.RuntimePrefabPath)
                    .ToString(),
                boundsCenter = bounds.center,
                boundsSize = bounds.size,
                silhouetteIou = iou,
                landmarkMeanError = landmarks.Mean,
                landmarkMaximumError = landmarks.Maximum,
                frames = frames.ToArray()
            };
            File.WriteAllText(
                Path.Combine(outputDirectory, "capture-manifest.json"),
                JsonUtility.ToJson(manifest, true) + "\n");
            WriteConsoleCounts(outputDirectory);
            Debug.Log(
                $"VF-022R captures passed: IoU={iou:F4}, "
                + $"landmarkMean={landmarks.Mean:F4}, "
                + $"landmarkMax={landmarks.Maximum:F4}.");
        }
        finally
        {
            foreach (Texture2D texture in textures.Values)
            {
                if (texture != null)
                {
                    Object.DestroyImmediate(texture);
                }
            }
            if (scene.IsValid() && EditorSceneManager.IsPreviewScene(scene))
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }

    private static void CaptureFrameAndKeep(
        Camera camera,
        string id,
        string outputDirectory,
        bool wireframe,
        Material replacement,
        List<CaptureFrame> frames,
        Dictionary<string, Texture2D> textures)
    {
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        var materials = new Dictionary<Renderer, Material[]>();
        try
        {
            if (replacement != null)
            {
                foreach (Renderer renderer in renderers)
                {
                    if (renderer.gameObject.scene != camera.gameObject.scene)
                    {
                        continue;
                    }
                    materials[renderer] = renderer.sharedMaterials;
                    Material[] assigned = new Material[renderer.sharedMaterials.Length];
                    for (int index = 0; index < assigned.Length; index++)
                    {
                        assigned[index] = replacement;
                    }
                    renderer.sharedMaterials = assigned;
                }
            }

            Texture2D texture = Render(camera, wireframe);
            string path = Path.Combine(outputDirectory, id + ".png");
            WriteTexture(path, texture);
            CaptureFrame frame = AnalyzeFrame(id, path, texture);
            if (frame.foregroundRatio < 0.01f)
            {
                throw new InvalidOperationException(
                    $"Capture foreground is below 1%: {id}.");
            }
            if (frame.borderForegroundRatio > 0.005f)
            {
                throw new InvalidOperationException(
                    $"Capture clips the 2% border: {id}="
                    + $"{frame.borderForegroundRatio:P2}.");
            }
            frames.Add(frame);
            textures.Add(id, texture);
        }
        finally
        {
            GL.wireframe = false;
            foreach (KeyValuePair<Renderer, Material[]> pair in materials)
            {
                if (pair.Key != null)
                {
                    pair.Key.sharedMaterials = pair.Value;
                }
            }
        }
    }

    private static Texture2D Render(Camera camera, bool wireframe)
    {
        var target = new RenderTexture(
            Width, Height, 24, RenderTextureFormat.ARGB32);
        var texture = new Texture2D(
            Width, Height, TextureFormat.RGBA32, false);
        RenderTexture previous = RenderTexture.active;
        bool previousWireframe = GL.wireframe;
        try
        {
            camera.targetTexture = target;
            GL.wireframe = wireframe;
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
            texture.Apply();
            return texture;
        }
        finally
        {
            GL.wireframe = previousWireframe;
            camera.targetTexture = null;
            RenderTexture.active = previous;
            Object.DestroyImmediate(target);
        }
    }

    private static Camera CreateCamera(Scene scene)
    {
        var gameObject = new GameObject("VF-022R Blockout Camera");
        SceneManager.MoveGameObjectToScene(gameObject, scene);
        Camera camera = gameObject.AddComponent<Camera>();
        camera.scene = scene;
        camera.enabled = false;
        camera.cameraType = CameraType.Preview;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Background;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 100f;
        camera.aspect = Width / (float)Height;
        return camera;
    }

    private static void ConfigureCamera(
        Camera camera,
        Bounds bounds,
        string view)
    {
        Vector3 center = bounds.center;
        if (view == "gameplay")
        {
            camera.orthographic = false;
            camera.fieldOfView = 38f;
            Vector3 direction = new Vector3(-0.58f, -0.62f, 0.53f).normalized;
            float distance = bounds.extents.magnitude
                / Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad)
                * 0.96f;
            camera.transform.position = center - direction * distance;
            camera.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            return;
        }

        camera.orthographic = true;
        Vector3 forward;
        Vector3 up = Vector3.up;
        if (view == "front")
        {
            forward = Vector3.forward;
            camera.transform.position = center + Vector3.back * 10f;
        }
        else if (view == "top")
        {
            forward = Vector3.down;
            up = Vector3.forward;
            camera.transform.position = center + Vector3.up * 10f;
        }
        else
        {
            forward = Vector3.left;
            camera.transform.position = center + Vector3.right * 10f;
        }
        camera.transform.rotation = Quaternion.LookRotation(forward, up);
        float verticalExtent = view == "top"
            ? Mathf.Max(bounds.extents.z, bounds.extents.x / camera.aspect)
            : view == "right-side"
                ? Mathf.Max(bounds.extents.y, bounds.extents.z / camera.aspect)
                : Mathf.Max(bounds.extents.y, bounds.extents.x / camera.aspect);
        camera.orthographicSize = verticalExtent * 1.18f;
    }

    private static void CreateLight(Scene scene)
    {
        var lightObject = new GameObject("Blockout Key Light");
        SceneManager.MoveGameObjectToScene(lightObject, scene);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.25f;
        light.color = new Color(1f, 0.96f, 0.90f);
        light.transform.rotation = Quaternion.Euler(44f, -34f, 0f);
    }

    private static Bounds RendererBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            throw new InvalidOperationException("Blockout has no renderers.");
        }
        Bounds bounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
        {
            bounds.Encapsulate(renderers[index].bounds);
        }
        return bounds;
    }

    private static Material CreateReplacementMaterial(string shaderName)
    {
        Shader shader = Shader.Find(shaderName);
        if (shader == null)
        {
            throw new InvalidOperationException(
                $"Capture replacement Shader is missing: {shaderName}");
        }
        return new Material(shader);
    }

    private static CaptureFrame AnalyzeFrame(
        string id,
        string path,
        Texture2D texture)
    {
        Color[] pixels = texture.GetPixels();
        float background = CornerLuminance(texture);
        int foreground = 0;
        int borderForeground = 0;
        int borderPixels = 0;
        int border = Mathf.Max(1, Mathf.RoundToInt(
            Mathf.Min(texture.width, texture.height) * 0.02f));
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                bool isForeground = IsForeground(
                    pixels[y * texture.width + x],
                    background);
                if (isForeground)
                {
                    foreground++;
                }
                bool inBorder = x < border
                    || x >= texture.width - border
                    || y < border
                    || y >= texture.height - border;
                if (inBorder)
                {
                    borderPixels++;
                    if (isForeground)
                    {
                        borderForeground++;
                    }
                }
            }
        }
        return new CaptureFrame
        {
            id = id,
            path = RelativeToRepository(path),
            sha256 = VfxMeshReviewStore.ComputeFileSha256(path),
            width = texture.width,
            height = texture.height,
            foregroundRatio = foreground / (float)pixels.Length,
            borderForegroundRatio = borderForeground / (float)borderPixels
        };
    }

    private static bool IsForeground(Color color, float backgroundLuminance)
    {
        float luminance = color.r * 0.2126f
            + color.g * 0.7152f
            + color.b * 0.0722f;
        return luminance > backgroundLuminance + 0.055f;
    }

    private static float CornerLuminance(Texture2D texture)
    {
        Color a = texture.GetPixel(2, 2);
        Color b = texture.GetPixel(texture.width - 3, 2);
        Color c = texture.GetPixel(2, texture.height - 3);
        Color d = texture.GetPixel(texture.width - 3, texture.height - 3);
        Color average = (a + b + c + d) * 0.25f;
        return average.r * 0.2126f
            + average.g * 0.7152f
            + average.b * 0.0722f;
    }

    private static Texture2D LoadTexture(string path)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(File.ReadAllBytes(path)))
        {
            Object.DestroyImmediate(texture);
            throw new InvalidOperationException($"PNG could not be loaded: {path}");
        }
        return texture;
    }

    private static Texture2D CropFront(Texture2D sheet)
    {
        int width = sheet.width / 2;
        int height = sheet.height / 2;
        var crop = new Texture2D(width, height, TextureFormat.RGBA32, false);
        crop.SetPixels(sheet.GetPixels(0, height, width, height));
        crop.Apply();
        return crop;
    }

    private static Texture2D CreateOverlay(Texture2D capture, Texture2D reference)
    {
        Texture2D fitted = FitTexture(reference, capture.width, capture.height);
        var overlay = new Texture2D(
            capture.width, capture.height, TextureFormat.RGBA32, false);
        Color[] basePixels = capture.GetPixels();
        Color[] referencePixels = fitted.GetPixels();
        for (int index = 0; index < basePixels.Length; index++)
        {
            basePixels[index] = Color.Lerp(
                basePixels[index],
                referencePixels[index],
                0.42f);
        }
        overlay.SetPixels(basePixels);
        overlay.Apply();
        Object.DestroyImmediate(fitted);
        return overlay;
    }

    private static Texture2D FitTexture(Texture2D source, int width, int height)
    {
        var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        for (int index = 0; index < pixels.Length; index++)
        {
            pixels[index] = new Color(0.22f, 0.22f, 0.22f, 1f);
        }
        float scale = Mathf.Min(width / (float)source.width,
            height / (float)source.height);
        int drawWidth = Mathf.RoundToInt(source.width * scale);
        int drawHeight = Mathf.RoundToInt(source.height * scale);
        int offsetX = (width - drawWidth) / 2;
        int offsetY = (height - drawHeight) / 2;
        for (int y = 0; y < drawHeight; y++)
        {
            float v = drawHeight > 1 ? y / (float)(drawHeight - 1) : 0f;
            for (int x = 0; x < drawWidth; x++)
            {
                float u = drawWidth > 1 ? x / (float)(drawWidth - 1) : 0f;
                pixels[(offsetY + y) * width + offsetX + x] =
                    source.GetPixelBilinear(u, v);
            }
        }
        result.SetPixels(pixels);
        result.Apply();
        return result;
    }

    private static float CalculateNormalizedIou(Texture2D first, Texture2D second)
    {
        MaskBounds a = FindMaskBounds(first);
        MaskBounds b = FindMaskBounds(second);
        const int resolution = 256;
        int intersection = 0;
        int union = 0;
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                bool firstValue = SampleMask(first, a, x, y, resolution);
                bool secondValue = SampleMask(second, b, x, y, resolution);
                if (firstValue || secondValue)
                {
                    union++;
                    if (firstValue && secondValue)
                    {
                        intersection++;
                    }
                }
            }
        }
        return union > 0 ? intersection / (float)union : 0f;
    }

    private readonly struct MaskBounds
    {
        public readonly int MinX;
        public readonly int MinY;
        public readonly int MaxX;
        public readonly int MaxY;

        public MaskBounds(int minX, int minY, int maxX, int maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }
    }

    private static MaskBounds FindMaskBounds(Texture2D texture)
    {
        float background = CornerLuminance(texture);
        int minX = texture.width;
        int minY = texture.height;
        int maxX = -1;
        int maxY = -1;
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                if (!IsForeground(texture.GetPixel(x, y), background))
                {
                    continue;
                }
                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
        }
        if (maxX < minX || maxY < minY)
        {
            throw new InvalidOperationException("Foreground mask is empty.");
        }
        return new MaskBounds(minX, minY, maxX, maxY);
    }

    private static bool SampleMask(
        Texture2D texture,
        MaskBounds bounds,
        int x,
        int y,
        int resolution)
    {
        int sourceX = Mathf.RoundToInt(Mathf.Lerp(
            bounds.MinX, bounds.MaxX, x / (float)(resolution - 1)));
        int sourceY = Mathf.RoundToInt(Mathf.Lerp(
            bounds.MinY, bounds.MaxY, y / (float)(resolution - 1)));
        return IsForeground(
            texture.GetPixel(sourceX, sourceY),
            CornerLuminance(texture));
    }

    private readonly struct LandmarkMetrics
    {
        public readonly float Mean;
        public readonly float Maximum;

        public LandmarkMetrics(float mean, float maximum)
        {
            Mean = mean;
            Maximum = maximum;
        }
    }

    private static LandmarkMetrics MeasureLandmarks(
        Camera camera,
        GameObject instance,
        string repository,
        Texture2D frontCapture)
    {
        VfxMeshReferenceManifest manifest = JsonUtility.FromJson<
            VfxMeshReferenceManifest>(File.ReadAllText(Path.Combine(
                repository,
                "Dogfooding/Evidence/VF-022R-model-sheet/mesh-reference-v3.json")));
        string[] ids =
        {
            "front_upper_left",
            "front_upper_right",
            "front_lower_left",
            "front_lower_right"
        };
        string[] rendererNames =
        {
            "Editable Upper Left Shoulder Guard",
            "Editable Upper Right Shoulder Guard",
            "Editable Lower Left Flank Guard",
            "Editable Lower Right Flank Guard"
        };
        float sum = 0f;
        float maximum = 0f;
        MaskBounds captureBounds = FindMaskBounds(frontCapture);
        const float targetMinX = 0.15f;
        const float targetMaxX = 0.85f;
        const float targetMinY = 0.06f;
        const float targetMaxY = 0.96f;
        for (int index = 0; index < ids.Length; index++)
        {
            VfxMeshLandmark landmark = Array.Find(
                manifest.landmarks,
                item => item.id == ids[index]);
            Renderer renderer = Array.Find(
                instance.GetComponentsInChildren<Renderer>(true),
                item => item.name == rendererNames[index]);
            if (landmark == null || renderer == null)
            {
                throw new InvalidOperationException(
                    $"Landmark binding is missing: {ids[index]}");
            }
            Vector3 viewport = camera.WorldToViewportPoint(renderer.bounds.center);
            float captureX = Mathf.InverseLerp(
                captureBounds.MinX,
                captureBounds.MaxX,
                viewport.x * frontCapture.width);
            float captureY = Mathf.InverseLerp(
                captureBounds.MinY,
                captureBounds.MaxY,
                viewport.y * frontCapture.height);
            float targetX = Mathf.InverseLerp(
                targetMinX,
                targetMaxX,
                landmark.normalizedPosition.x);
            float targetY = Mathf.InverseLerp(
                targetMinY,
                targetMaxY,
                landmark.normalizedPosition.y);
            float error = Vector2.Distance(
                new Vector2(targetX, targetY),
                new Vector2(captureX, captureY));
            sum += error;
            maximum = Mathf.Max(maximum, error);
        }
        return new LandmarkMetrics(sum / ids.Length, maximum);
    }

    private static Texture2D BuildContactSheet(
        Dictionary<string, Texture2D> textures)
    {
        string[] order =
        {
            "reference-front", "target-silhouette", "front-clay", "front-wireframe",
            "front-normal", "front-overlay", "top-clay", "top-wireframe",
            "right-side-clay", "right-side-wireframe", "gameplay-clay"
        };
        const int cellWidth = 640;
        const int cellHeight = 360;
        const int columns = 5;
        int rows = Mathf.CeilToInt(order.Length / (float)columns);
        var sheet = new Texture2D(
            cellWidth * columns,
            cellHeight * rows,
            TextureFormat.RGBA32,
            false);
        Color[] background = new Color[sheet.width * sheet.height];
        for (int index = 0; index < background.Length; index++)
        {
            background[index] = Background;
        }
        sheet.SetPixels(background);
        for (int index = 0; index < order.Length; index++)
        {
            Texture2D fitted = FitTexture(
                textures[order[index]], cellWidth, cellHeight);
            int column = index % columns;
            int rowFromTop = index / columns;
            int y = (rows - 1 - rowFromTop) * cellHeight;
            sheet.SetPixels(
                column * cellWidth,
                y,
                cellWidth,
                cellHeight,
                fitted.GetPixels());
            Object.DestroyImmediate(fitted);
        }
        sheet.Apply();
        return sheet;
    }

    private static Texture2D BuildTargetSilhouette()
    {
        const int size = 627;
        Vector2[] polygon =
        {
            new Vector2(0.50f, 0.96f),
            new Vector2(0.58f, 0.90f),
            new Vector2(0.68f, 0.84f),
            new Vector2(0.79f, 0.79f),
            new Vector2(0.84f, 0.70f),
            new Vector2(0.84f, 0.61f),
            new Vector2(0.82f, 0.50f),
            new Vector2(0.80f, 0.42f),
            new Vector2(0.76f, 0.33f),
            new Vector2(0.70f, 0.25f),
            new Vector2(0.61f, 0.17f),
            new Vector2(0.50f, 0.06f),
            new Vector2(0.39f, 0.17f),
            new Vector2(0.30f, 0.25f),
            new Vector2(0.24f, 0.33f),
            new Vector2(0.20f, 0.42f),
            new Vector2(0.18f, 0.50f),
            new Vector2(0.16f, 0.61f),
            new Vector2(0.16f, 0.70f),
            new Vector2(0.21f, 0.79f),
            new Vector2(0.32f, 0.84f),
            new Vector2(0.42f, 0.90f)
        };
        var texture = new Texture2D(
            size,
            size,
            TextureFormat.RGBA32,
            false);
        Color[] pixels = new Color[size * size];
        Color foreground = new Color(0.84f, 0.86f, 0.88f, 1f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2(
                    (x + 0.5f) / size,
                    (y + 0.5f) / size);
                pixels[y * size + x] = PointInPolygon(point, polygon)
                    ? foreground
                    : Background;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        int previous = polygon.Length - 1;
        for (int current = 0; current < polygon.Length; current++)
        {
            Vector2 a = polygon[current];
            Vector2 b = polygon[previous];
            bool crosses = (a.y > point.y) != (b.y > point.y)
                && point.x < (b.x - a.x) * (point.y - a.y)
                / (b.y - a.y) + a.x;
            if (crosses)
            {
                inside = !inside;
            }
            previous = current;
        }
        return inside;
    }

    private static void WriteTexture(string path, Texture2D texture)
    {
        File.WriteAllBytes(path, texture.EncodeToPNG());
    }

    private static void WriteConsoleCounts(string outputDirectory)
    {
        Type logEntriesType = typeof(Editor).Assembly.GetType(
            "UnityEditor.LogEntries",
            true);
        MethodInfo getCountsMethod = logEntriesType.GetMethod(
            "GetCountsByType",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (getCountsMethod == null)
        {
            throw new MissingMethodException(
                logEntriesType.FullName,
                "GetCountsByType");
        }
        object[] arguments = { 0, 0, 0 };
        getCountsMethod.Invoke(null, arguments);
        var counts = new ConsoleCounts
        {
            errors = (int)arguments[0],
            warnings = (int)arguments[1],
            logs = (int)arguments[2]
        };
        File.WriteAllText(
            Path.Combine(outputDirectory, "console-counts.json"),
            JsonUtility.ToJson(counts, true) + "\n");
        if (counts.errors > 0)
        {
            throw new InvalidOperationException(
                $"VF-022R capture found {counts.errors} Console error(s).");
        }
    }

    private static string RelativeToRepository(string absolutePath)
    {
        string repository = RepositoryRoot();
        return absolutePath.Substring(repository.Length + 1)
            .Replace('\\', '/');
    }

    private static string RepositoryRoot()
    {
        return Directory.GetParent(Application.dataPath).Parent.FullName;
    }
}
