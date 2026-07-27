using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Kubonsang.VfxForge.Editor
{
    [Serializable]
    public sealed class VfxCapturedFrame
    {
        public int frameIndex;
        public float timeSeconds;
        public string view = string.Empty;
        public string fileName = string.Empty;
        public int width;
        public int height;
    }

    [Serializable]
    public sealed class VfxCaptureManifest
    {
        public string schemaVersion = "1.0";
        public string recipeId = string.Empty;
        public string status = "failed";
        public float durationSeconds;
        public int width;
        public int height;
        public List<VfxCapturedFrame> frames = new List<VfxCapturedFrame>();
    }

    public sealed class VfxFrameCaptureResult
    {
        public bool Success;
        public string ErrorCode = string.Empty;
        public string Message = string.Empty;
        public string ManifestPath = string.Empty;
        public List<string> FramePaths = new List<string>();
    }

    public static class VfxFrameCapture
    {
        public const string ManifestFileName = "capture-manifest.json";
        public const int MaximumDimension = 8192;
        public const int MaximumFrameCount = 4096;

        private static readonly string[] CanonicalViews = { "front", "side", "top" };

        public static VfxFrameCaptureResult Capture(
            VfxPreviewSession session,
            VfxRecipe recipe,
            string artifactDirectory)
        {
            if (session == null || session.IsDisposed || session.PreviewCamera == null)
            {
                return Failure("CAPTURE-SESSION", "An active Preview session is required.");
            }

            if (!TryBuildPlan(recipe, out List<VfxCapturedFrame> frames, out string error))
            {
                return Failure("CAPTURE-SETTINGS", error);
            }

            if (string.IsNullOrWhiteSpace(artifactDirectory))
            {
                return Failure("CAPTURE-OUTPUT", "Artifact directory is required.");
            }

            string outputDirectory;
            try
            {
                outputDirectory = Path.GetFullPath(artifactDirectory);
            }
            catch (Exception exception)
            {
                return Failure("CAPTURE-OUTPUT", exception.Message);
            }

            string manifestPath = Path.Combine(outputDirectory, ManifestFileName);
            foreach (VfxCapturedFrame frame in frames)
            {
                string framePath = Path.Combine(outputDirectory, frame.fileName);
                if (File.Exists(framePath) || Directory.Exists(framePath))
                {
                    return Failure(
                        "CAPTURE-OUTPUT-EXISTS",
                        $"Capture output already exists: {frame.fileName}");
                }
            }

            if (File.Exists(manifestPath) || Directory.Exists(manifestPath))
            {
                return Failure(
                    "CAPTURE-OUTPUT-EXISTS",
                    $"Capture output already exists: {ManifestFileName}");
            }

            bool directoryCreated = false;
            var createdPaths = new List<string>();
            bool resumePlayback = session.IsPlaying;

            try
            {
                if (File.Exists(outputDirectory))
                {
                    return Failure(
                        "CAPTURE-OUTPUT",
                        "Artifact directory points to an existing file.");
                }

                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                    directoryCreated = true;
                }

                for (int index = 0; index < frames.Count; index++)
                {
                    VfxCapturedFrame frame = frames[index];
                    session.SimulateTo(frame.timeSeconds);
                    session.SetCameraView(ParseView(frame.view));

                    byte[] png = RenderPng(
                        session.PreviewCamera,
                        frame.width,
                        frame.height);
                    string framePath = Path.Combine(outputDirectory, frame.fileName);
                    createdPaths.Add(framePath);
                    File.WriteAllBytes(framePath, png);
                }

                RestorePlayback(session, resumePlayback);

                var manifest = new VfxCaptureManifest
                {
                    recipeId = recipe.id,
                    status = "passed",
                    durationSeconds = recipe.capture.duration,
                    width = recipe.capture.width,
                    height = recipe.capture.height,
                    frames = frames
                };
                createdPaths.Add(manifestPath);
                File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true));

                return new VfxFrameCaptureResult
                {
                    Success = true,
                    Message = $"Captured {frames.Count} frame(s).",
                    ManifestPath = manifestPath,
                    FramePaths = BuildFramePaths(outputDirectory, frames)
                };
            }
            catch (Exception exception)
            {
                string restoreError = TryRestorePlayback(session, resumePlayback);
                string cleanupError =
                    CleanupCreatedFiles(createdPaths, outputDirectory, directoryCreated);
                var failures = new List<string> { exception.Message };
                if (!string.IsNullOrWhiteSpace(restoreError))
                {
                    failures.Add($"Playback restore also failed: {restoreError}");
                }
                if (!string.IsNullOrWhiteSpace(cleanupError))
                {
                    failures.Add($"Artifact cleanup also failed: {cleanupError}");
                }
                string message = string.Join(" ", failures);
                return Failure("CAPTURE-FAILED", message);
            }
        }

        public static string BuildFileName(
            string recipeId,
            int frameIndex,
            float timeSeconds,
            string view)
        {
            long microseconds = (long)Math.Round(
                timeSeconds * 1000000d,
                MidpointRounding.AwayFromZero);
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}_f{1:D3}_{2}_t{3:D8}.png",
                recipeId,
                frameIndex,
                view,
                microseconds);
        }

        private static bool TryBuildPlan(
            VfxRecipe recipe,
            out List<VfxCapturedFrame> frames,
            out string error)
        {
            frames = new List<VfxCapturedFrame>();
            error = string.Empty;

            if (recipe == null || recipe.capture == null)
            {
                error = "Recipe and capture settings are required.";
                return false;
            }

            VfxCaptureSettings settings = recipe.capture;
            if (!IsSafeFileStem(recipe.id))
            {
                error = "Recipe ID is not safe for capture file names.";
                return false;
            }

            if (settings.width < 64
                || settings.height < 64
                || settings.width > MaximumDimension
                || settings.height > MaximumDimension)
            {
                error = $"Capture dimensions must be between 64 and {MaximumDimension}.";
                return false;
            }

            if (!IsFinite(settings.duration) || settings.duration <= 0f)
            {
                error = "Capture duration must be finite and greater than zero.";
                return false;
            }

            if (settings.frameTimes == null || settings.frameTimes.Length == 0)
            {
                error = "At least one frame time is required.";
                return false;
            }

            if (settings.views == null || settings.views.Length == 0)
            {
                error = "At least one capture view is required.";
                return false;
            }

            var requestedViews = new HashSet<string>(settings.views, StringComparer.Ordinal);
            if (requestedViews.Count != settings.views.Length)
            {
                error = "Capture views must be unique.";
                return false;
            }

            foreach (string view in requestedViews)
            {
                if (Array.IndexOf(CanonicalViews, view) < 0)
                {
                    error = $"Unsupported capture view: {view}";
                    return false;
                }
            }

            float[] sortedTimes = (float[])settings.frameTimes.Clone();
            Array.Sort(sortedTimes);
            foreach (float time in sortedTimes)
            {
                if (!IsFinite(time) || time < 0f || time > settings.duration)
                {
                    error = $"Frame time {time} is outside capture duration.";
                    return false;
                }
            }

            long plannedCount = (long)sortedTimes.Length * requestedViews.Count;
            if (plannedCount > MaximumFrameCount)
            {
                error = $"Capture plan exceeds {MaximumFrameCount} frames.";
                return false;
            }

            int frameIndex = 0;
            foreach (float time in sortedTimes)
            {
                foreach (string view in CanonicalViews)
                {
                    if (!requestedViews.Contains(view))
                    {
                        continue;
                    }

                    frames.Add(new VfxCapturedFrame
                    {
                        frameIndex = frameIndex,
                        timeSeconds = time,
                        view = view,
                        fileName = BuildFileName(recipe.id, frameIndex, time, view),
                        width = settings.width,
                        height = settings.height
                    });
                    frameIndex++;
                }
            }

            return true;
        }

        private static byte[] RenderPng(Camera camera, int width, int height)
        {
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture renderTexture = null;
            Texture2D texture = null;

            try
            {
                renderTexture = RenderTexture.GetTemporary(
                    width,
                    height,
                    24,
                    RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.sRGB);
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                texture.Apply(false, false);

                byte[] png = texture.EncodeToPNG();
                if (png == null || png.Length < 8)
                {
                    throw new InvalidOperationException("Camera render did not produce PNG data.");
                }

                return png;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                if (renderTexture != null)
                {
                    RenderTexture.ReleaseTemporary(renderTexture);
                }
                if (texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
        }

        private static VfxPreviewView ParseView(string view)
        {
            switch (view)
            {
                case "front":
                    return VfxPreviewView.Front;
                case "side":
                    return VfxPreviewView.Side;
                case "top":
                    return VfxPreviewView.Top;
                default:
                    throw new ArgumentOutOfRangeException(nameof(view), view, "Unsupported view.");
            }
        }

        private static void RestorePlayback(VfxPreviewSession session, bool resumePlayback)
        {
            if (resumePlayback)
            {
                session.Restart();
            }
            else
            {
                session.Stop();
            }
        }

        private static string TryRestorePlayback(
            VfxPreviewSession session,
            bool resumePlayback)
        {
            try
            {
                if (session != null && !session.IsDisposed)
                {
                    RestorePlayback(session, resumePlayback);
                }
                return string.Empty;
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
        }

        private static List<string> BuildFramePaths(
            string outputDirectory,
            IEnumerable<VfxCapturedFrame> frames)
        {
            var paths = new List<string>();
            foreach (VfxCapturedFrame frame in frames)
            {
                paths.Add(Path.Combine(outputDirectory, frame.fileName));
            }
            return paths;
        }

        private static string CleanupCreatedFiles(
            IEnumerable<string> paths,
            string outputDirectory,
            bool directoryCreated)
        {
            var failures = new List<string>();
            foreach (string path in paths)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch (Exception exception)
                {
                    failures.Add($"{Path.GetFileName(path)}: {exception.Message}");
                }
            }

            try
            {
                if (directoryCreated
                    && Directory.Exists(outputDirectory)
                    && Directory.GetFileSystemEntries(outputDirectory).Length == 0)
                {
                    Directory.Delete(outputDirectory);
                }
            }
            catch (Exception exception)
            {
                failures.Add($"output directory: {exception.Message}");
            }

            return string.Join("; ", failures);
        }

        private static bool IsSafeFileStem(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            foreach (char character in value)
            {
                if (!char.IsLetterOrDigit(character)
                    && character != '_'
                    && character != '-')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static VfxFrameCaptureResult Failure(string errorCode, string message)
        {
            return new VfxFrameCaptureResult
            {
                ErrorCode = errorCode,
                Message = message
            };
        }
    }
}
