using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;

namespace Kubonsang.VfxForge.Editor
{
    [Serializable]
    public sealed class VfxReviewFrame
    {
        public int order;
        public float timeSeconds;
        public string sourceKind = string.Empty;
        public string sourceId = string.Empty;
        public string fileName = string.Empty;
        public float foregroundRatio;
        public float borderForegroundRatio;
        public string sha256 = string.Empty;
    }

    [Serializable]
    public sealed class VfxReviewManifest
    {
        public string schemaVersion = "review-manifest-1.0";
        public string recipeId = string.Empty;
        public string status = "failed";
        public string isolatedCaptureManifest = string.Empty;
        public string isolatedCaptureManifestSha256 = string.Empty;
        public string contactSheet = string.Empty;
        public string contactSheetSha256 = string.Empty;
        public List<VfxReviewFrame> frames = new List<VfxReviewFrame>();
    }

    public sealed class VfxGameplayReviewResult
    {
        public bool Success;
        public string ErrorCode = string.Empty;
        public string Message = string.Empty;
        public string ManifestPath = string.Empty;
        public string ContactSheetPath = string.Empty;
        public List<string> ContextFramePaths = new List<string>();
    }

    public static class VfxGameplayReviewCapture
    {
        public const string ManifestFileName = "review-manifest.json";
        public const string ContactSheetFileName = "contact-sheet.png";

        public static VfxGameplayReviewResult Capture(
            VfxRecipe recipe,
            GameObject generatedPrefab,
            VfxTemplateCatalog catalog,
            string playEventName,
            string isolatedManifestPath,
            string outputDirectory)
        {
            if (recipe?.capture?.contexts == null
                || recipe.capture.contexts.Length == 0)
            {
                return Failure(
                    "REVIEW-CONTEXTS",
                    "At least one Review Context is required.");
            }
            if (generatedPrefab == null
                || catalog == null
                || string.IsNullOrWhiteSpace(isolatedManifestPath)
                || !File.Exists(isolatedManifestPath)
                || string.IsNullOrWhiteSpace(outputDirectory))
            {
                return Failure(
                    "REVIEW-INPUT",
                    "Recipe, generated Prefab, Catalog, isolated manifest, and output path are required.");
            }

            string manifestPath =
                Path.Combine(outputDirectory, ManifestFileName);
            string contactSheetPath =
                Path.Combine(outputDirectory, ContactSheetFileName);
            if (Directory.Exists(outputDirectory)
                || File.Exists(manifestPath)
                || File.Exists(contactSheetPath))
            {
                return Failure(
                    "REVIEW-OUTPUT-EXISTS",
                    $"Review output already exists: {outputDirectory}");
            }

            var contextIds =
                new List<string>(recipe.capture.contexts);
            contextIds.Sort(StringComparer.Ordinal);
            var resolvedContexts = new List<VfxReviewContextEntry>();
            foreach (string contextId in contextIds)
            {
                if (!catalog.TryGetReviewContext(
                    contextId,
                    out VfxReviewContextEntry entry)
                    || entry?.prefab == null)
                {
                    return Failure(
                        "CATALOG-CONTEXT-REFERENCE",
                        $"Review Context is not registered: {contextId}.");
                }
                resolvedContexts.Add(entry);
            }

            VfxCaptureManifest isolatedManifest;
            try
            {
                isolatedManifest = JsonUtility.FromJson<VfxCaptureManifest>(
                    File.ReadAllText(isolatedManifestPath));
            }
            catch (Exception exception)
            {
                return Failure("REVIEW-MANIFEST", exception.Message);
            }
            if (isolatedManifest == null
                || isolatedManifest.frames == null
                || isolatedManifest.frames.Count == 0)
            {
                return Failure(
                    "REVIEW-MANIFEST",
                    "Isolated Capture manifest contains no frames.");
            }

            var createdPaths = new List<string>();
            var contextFrames = new List<VfxReviewFrame>();
            try
            {
                Directory.CreateDirectory(outputDirectory);
                string contextDirectory =
                    Path.Combine(outputDirectory, "contexts");
                Directory.CreateDirectory(contextDirectory);

                float[] sortedTimes =
                    (float[])recipe.capture.frameTimes.Clone();
                Array.Sort(sortedTimes);
                for (int contextIndex = 0;
                    contextIndex < resolvedContexts.Count;
                    contextIndex++)
                {
                    CaptureContext(
                        recipe,
                        generatedPrefab,
                        playEventName,
                        resolvedContexts[contextIndex],
                        sortedTimes,
                        contextDirectory,
                        createdPaths,
                        contextFrames);
                }

                List<VfxReviewFrame> allFrames = BuildOrderedFrames(
                    isolatedManifest,
                    isolatedManifestPath,
                    contextFrames);
                VfxContactSheetBuilder.Build(
                    allFrames,
                    sortedTimes.Length,
                    contactSheetPath);
                createdPaths.Add(contactSheetPath);

                var manifest = new VfxReviewManifest
                {
                    recipeId = recipe.id,
                    status = "passed",
                    isolatedCaptureManifest =
                        Path.Combine(
                            "..",
                            "capture",
                            Path.GetFileName(isolatedManifestPath))
                        .Replace('\\', '/'),
                    isolatedCaptureManifestSha256 =
                        ComputeSha256(isolatedManifestPath),
                    contactSheet = ContactSheetFileName,
                    contactSheetSha256 = ComputeSha256(contactSheetPath),
                    frames = allFrames
                };
                File.WriteAllText(
                    manifestPath,
                    JsonUtility.ToJson(manifest, true));
                createdPaths.Add(manifestPath);

                return new VfxGameplayReviewResult
                {
                    Success = true,
                    Message =
                        $"Captured {contextFrames.Count} gameplay frame(s).",
                    ManifestPath = manifestPath,
                    ContactSheetPath = contactSheetPath,
                    ContextFramePaths = CollectContextPaths(
                        contextFrames,
                        outputDirectory)
                };
            }
            catch (VfxGameplayReviewException exception)
            {
                Cleanup(outputDirectory);
                return Failure(exception.ErrorCode, exception.Message);
            }
            catch (Exception exception)
            {
                Cleanup(outputDirectory);
                return Failure("REVIEW-CAPTURE", exception.Message);
            }
        }

        private static void CaptureContext(
            VfxRecipe recipe,
            GameObject generatedPrefab,
            string playEventName,
            VfxReviewContextEntry entry,
            float[] sortedTimes,
            string outputDirectory,
            List<string> createdPaths,
            List<VfxReviewFrame> frames)
        {
            Scene scene = default;
            try
            {
                scene = EditorSceneManager.NewPreviewScene();
                GameObject contextInstance =
                    PrefabUtility.InstantiatePrefab(entry.prefab, scene)
                        as GameObject;
                if (contextInstance == null)
                {
                    throw new VfxGameplayReviewException(
                        "REVIEW-CONTEXT-INSTANTIATE",
                        $"Review Context could not be instantiated: {entry.id}.");
                }

                VfxReviewContext context =
                    contextInstance.GetComponentInChildren<VfxReviewContext>(true);
                if (context == null
                    || context.reviewCamera == null
                    || context.effectAnchor == null)
                {
                    throw new VfxGameplayReviewException(
                        "REVIEW-CONTEXT-REFERENCES",
                        $"Review Context references are incomplete: {entry.id}.");
                }

                Camera camera = context.reviewCamera;
                camera.enabled = false;
                camera.cameraType = CameraType.Preview;
                camera.scene = scene;
                camera.aspect =
                    (float)recipe.capture.width / recipe.capture.height;
                VfxFrameCapture.VfxRenderedFrame baseline =
                    VfxFrameCapture.RenderFrame(
                        camera,
                        recipe.capture.width,
                        recipe.capture.height);

                GameObject effectInstance =
                    PrefabUtility.InstantiatePrefab(generatedPrefab, scene)
                        as GameObject;
                if (effectInstance == null)
                {
                    throw new VfxGameplayReviewException(
                        "REVIEW-EFFECT-INSTANTIATE",
                        "Generated Prefab could not be instantiated in Review Context.");
                }
                effectInstance.transform.SetParent(
                    context.effectAnchor,
                    false);

                VfxPlayer player =
                    effectInstance.GetComponent<VfxPlayer>()
                    ?? effectInstance.AddComponent<VfxPlayer>();
                player.Configure(playEventName);
                foreach (float time in sortedTimes)
                {
                    SimulateTo(effectInstance, player, time);
                    VfxFrameCapture.VfxRenderedFrame rendered =
                        VfxFrameCapture.RenderFrame(
                            camera,
                            recipe.capture.width,
                            recipe.capture.height);
                    VfxCaptureContentMetrics metrics =
                        VfxCaptureContentGate.MeasureDifference(
                            rendered.Pixels,
                            baseline.Pixels,
                            recipe.capture.width,
                            recipe.capture.height);
                    string fileName = BuildContextFileName(
                        recipe.id,
                        entry.id,
                        time);
                    var captured = new VfxCapturedFrame
                    {
                        fileName = fileName,
                        foregroundRatio = metrics.ForegroundRatio,
                        borderForegroundRatio =
                            metrics.BorderForegroundRatio
                    };
                    VfxValidationResult quality =
                        VfxCaptureContentGate.Evaluate(recipe, captured);
                    if (!quality.passed)
                    {
                        throw new VfxGameplayReviewException(
                            VfxCaptureContentGate.RuleId,
                            quality.message);
                    }

                    string path = Path.Combine(outputDirectory, fileName);
                    File.WriteAllBytes(path, rendered.Png);
                    createdPaths.Add(path);
                    frames.Add(new VfxReviewFrame
                    {
                        timeSeconds = time,
                        sourceKind = "context",
                        sourceId = entry.id,
                        fileName = Path.Combine("contexts", fileName)
                            .Replace('\\', '/'),
                        foregroundRatio = metrics.ForegroundRatio,
                        borderForegroundRatio =
                            metrics.BorderForegroundRatio,
                        sha256 = ComputeSha256(path)
                    });
                }
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

        private static void SimulateTo(
            GameObject effectInstance,
            VfxPlayer player,
            float time)
        {
            player.StopAndReinitialize();
            player.PlayAll();
            foreach (VisualEffect effect in
                effectInstance.GetComponentsInChildren<VisualEffect>(true))
            {
                effect.pause = false;
                if (time > 0f)
                {
                    effect.Simulate(time, 1);
                }
                effect.pause = true;
            }
            foreach (MonoBehaviour behaviour in
                effectInstance.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is IVfxPreviewTimeEvaluable evaluable)
                {
                    evaluable.EvaluatePreviewTime(time);
                }
            }
        }

        private static List<VfxReviewFrame> BuildOrderedFrames(
            VfxCaptureManifest isolatedManifest,
            string isolatedManifestPath,
            List<VfxReviewFrame> contextFrames)
        {
            string isolatedDirectory =
                Path.GetDirectoryName(isolatedManifestPath);
            var frames = new List<VfxReviewFrame>();
            foreach (VfxCapturedFrame isolated in isolatedManifest.frames)
            {
                string path =
                    Path.Combine(isolatedDirectory, isolated.fileName);
                if (!File.Exists(path))
                {
                    throw new VfxGameplayReviewException(
                        "REVIEW-ISOLATED-FRAME",
                        $"Isolated frame is missing: {isolated.fileName}.");
                }
                frames.Add(new VfxReviewFrame
                {
                    timeSeconds = isolated.timeSeconds,
                    sourceKind = "isolated",
                    sourceId = isolated.view,
                    fileName = Path.Combine(
                        "..",
                        "capture",
                        isolated.fileName).Replace('\\', '/'),
                    foregroundRatio = isolated.foregroundRatio,
                    borderForegroundRatio =
                        isolated.borderForegroundRatio,
                    sha256 = ComputeSha256(path)
                });
            }
            frames.AddRange(contextFrames);
            frames.Sort(CompareFrames);
            for (int index = 0; index < frames.Count; index++)
            {
                frames[index].order = index;
            }
            return frames;
        }

        private static int CompareFrames(
            VfxReviewFrame left,
            VfxReviewFrame right)
        {
            int time = left.timeSeconds.CompareTo(right.timeSeconds);
            if (time != 0)
            {
                return time;
            }
            int kind = SourceKindOrder(left.sourceKind)
                .CompareTo(SourceKindOrder(right.sourceKind));
            if (kind != 0)
            {
                return kind;
            }
            if (left.sourceKind == "isolated")
            {
                int view = ViewOrder(left.sourceId)
                    .CompareTo(ViewOrder(right.sourceId));
                if (view != 0)
                {
                    return view;
                }
            }
            return string.CompareOrdinal(left.sourceId, right.sourceId);
        }

        private static int SourceKindOrder(string kind)
        {
            return kind == "isolated" ? 0 : 1;
        }

        private static int ViewOrder(string view)
        {
            switch (view)
            {
                case "front":
                    return 0;
                case "side":
                    return 1;
                case "top":
                    return 2;
                default:
                    return 3;
            }
        }

        private static string BuildContextFileName(
            string recipeId,
            string contextId,
            float time)
        {
            long microseconds = (long)Math.Round(
                time * 1000000d,
                MidpointRounding.AwayFromZero);
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}_context_{1}_t{2:D8}.png",
                recipeId,
                contextId,
                microseconds);
        }

        private static List<string> CollectContextPaths(
            IEnumerable<VfxReviewFrame> frames,
            string outputDirectory)
        {
            var paths = new List<string>();
            foreach (VfxReviewFrame frame in frames)
            {
                paths.Add(Path.Combine(outputDirectory, frame.fileName));
            }
            return paths;
        }

        internal static string ComputeSha256(string path)
        {
            using (SHA256 algorithm = SHA256.Create())
            {
                byte[] hash =
                    algorithm.ComputeHash(File.ReadAllBytes(path));
                return BitConverter.ToString(hash)
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }

        private static void Cleanup(string outputDirectory)
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }
        }

        private static VfxGameplayReviewResult Failure(
            string errorCode,
            string message)
        {
            return new VfxGameplayReviewResult
            {
                ErrorCode = errorCode,
                Message = message
            };
        }

        private sealed class VfxGameplayReviewException : Exception
        {
            public string ErrorCode { get; }

            public VfxGameplayReviewException(
                string errorCode,
                string message)
                : base(message)
            {
                ErrorCode = errorCode;
            }
        }
    }

    internal static class VfxContactSheetBuilder
    {
        private const int MaximumTileWidth = 320;
        private const int Gap = 4;

        public static void Build(
            IReadOnlyList<VfxReviewFrame> frames,
            int rowCount,
            string outputPath)
        {
            if (frames == null
                || frames.Count == 0
                || rowCount <= 0
                || frames.Count % rowCount != 0)
            {
                throw new InvalidOperationException(
                    "Contact Sheet frame grid is invalid.");
            }

            int columnCount = frames.Count / rowCount;
            var textures = new List<Texture2D>();
            try
            {
                foreach (VfxReviewFrame frame in frames)
                {
                    string absolute = ResolveFramePath(
                        outputPath,
                        frame.fileName);
                    var texture =
                        new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!texture.LoadImage(File.ReadAllBytes(absolute), false))
                    {
                        UnityEngine.Object.DestroyImmediate(texture);
                        throw new InvalidOperationException(
                            $"Contact Sheet frame could not be decoded: {frame.fileName}.");
                    }
                    textures.Add(texture);
                }

                Texture2D first = textures[0];
                float scale =
                    Mathf.Min(1f, MaximumTileWidth / (float)first.width);
                int tileWidth =
                    Mathf.Max(1, Mathf.RoundToInt(first.width * scale));
                int tileHeight =
                    Mathf.Max(1, Mathf.RoundToInt(first.height * scale));
                int width =
                    columnCount * tileWidth
                    + Mathf.Max(0, columnCount - 1) * Gap;
                int height =
                    rowCount * tileHeight
                    + Mathf.Max(0, rowCount - 1) * Gap;
                var sheet =
                    new Texture2D(width, height, TextureFormat.RGBA32, false);
                try
                {
                    var background = new Color32[width * height];
                    var backgroundColor =
                        new Color32(18, 18, 18, 255);
                    for (int index = 0;
                        index < background.Length;
                        index++)
                    {
                        background[index] = backgroundColor;
                    }
                    sheet.SetPixels32(background);

                    for (int index = 0; index < textures.Count; index++)
                    {
                        int row = index / columnCount;
                        int column = index % columnCount;
                        Color32[] pixels = ResizeNearest(
                            textures[index],
                            tileWidth,
                            tileHeight);
                        sheet.SetPixels32(
                            column * (tileWidth + Gap),
                            height - tileHeight
                                - row * (tileHeight + Gap),
                            tileWidth,
                            tileHeight,
                            pixels);
                    }
                    sheet.Apply(false, false);
                    File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(sheet);
                }
            }
            finally
            {
                foreach (Texture2D texture in textures)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
        }

        private static string ResolveFramePath(
            string contactSheetPath,
            string relativeFramePath)
        {
            string reviewDirectory =
                Path.GetDirectoryName(contactSheetPath);
            return Path.GetFullPath(
                Path.Combine(reviewDirectory, relativeFramePath));
        }

        private static Color32[] ResizeNearest(
            Texture2D source,
            int width,
            int height)
        {
            Color32[] sourcePixels = source.GetPixels32();
            var output = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                int sourceY =
                    Mathf.Min(
                        source.height - 1,
                        y * source.height / height);
                for (int x = 0; x < width; x++)
                {
                    int sourceX =
                        Mathf.Min(
                            source.width - 1,
                            x * source.width / width);
                    output[y * width + x] =
                        sourcePixels[
                            sourceY * source.width + sourceX];
                }
            }
            return output;
        }
    }
}
