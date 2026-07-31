using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

namespace Kubonsang.VfxForge.Editor.Tests
{
    public sealed class VfxFrameCaptureTests
    {
        private string testAssetRoot;
        private string captureRoot;
        private GameObject generatedPrefab;
        private VfxPreviewSession previewSession;

        [SetUp]
        public void SetUp()
        {
            string suffix = Guid.NewGuid().ToString("N");
            string folderName = $"__VfxForgeCaptureTests_{suffix}";
            AssetDatabase.CreateFolder("Assets", folderName);
            testAssetRoot = $"Assets/{folderName}";
            captureRoot = Path.Combine(Path.GetTempPath(), $"vfx-forge-capture-{suffix}");
            generatedPrefab = CreateGeneratedPrefab($"{testAssetRoot}/Generated.prefab");
        }

        [TearDown]
        public void TearDown()
        {
            previewSession?.Dispose();
            previewSession = null;

            if (!string.IsNullOrWhiteSpace(testAssetRoot)
                && AssetDatabase.IsValidFolder(testAssetRoot))
            {
                AssetDatabase.DeleteAsset(testAssetRoot);
            }

            if (!string.IsNullOrWhiteSpace(captureRoot)
                && Directory.Exists(captureRoot))
            {
                Directory.Delete(captureRoot, true);
            }
            else if (!string.IsNullOrWhiteSpace(captureRoot)
                && File.Exists(captureRoot))
            {
                File.Delete(captureRoot);
            }
        }

        [Test]
        public void Capture_ValidPlan_WritesEveryPngAndPassedManifest()
        {
            OpenPreview();
            VfxRecipe recipe = CreateRecipe(
                new[] { 0f, 0.125f },
                new[] { "front", "side", "top" });

            VfxFrameCaptureResult result =
                VfxFrameCapture.Capture(previewSession, recipe, captureRoot);

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(result.FramePaths, Has.Count.EqualTo(6));
            Assert.That(File.Exists(result.ManifestPath), Is.True);

            foreach (string framePath in result.FramePaths)
            {
                AssertPng(framePath, 64, 64);
            }
            AssertContainsForeground(result.FramePaths[0], 0.01f);

            VfxCaptureManifest manifest = JsonUtility.FromJson<VfxCaptureManifest>(
                File.ReadAllText(result.ManifestPath));
            Assert.That(manifest.status, Is.EqualTo("passed"));
            Assert.That(manifest.schemaVersion, Is.EqualTo("1.1"));
            Assert.That(manifest.recipeId, Is.EqualTo(recipe.id));
            Assert.That(manifest.frames, Has.Count.EqualTo(6));
            Assert.That(manifest.width, Is.EqualTo(64));
            Assert.That(manifest.height, Is.EqualTo(64));
            foreach (VfxCapturedFrame frame in manifest.frames)
            {
                Assert.That(
                    frame.foregroundRatio,
                    Is.GreaterThanOrEqualTo(0.01f));
                Assert.That(
                    frame.borderForegroundRatio,
                    Is.LessThanOrEqualTo(0.005f));
                Assert.That(frame.boundsSize.sqrMagnitude, Is.GreaterThan(0f));
            }
            Assert.That(previewSession.IsPlaying, Is.True);
        }

        [Test]
        public void Capture_UnsortedTimesAndViews_UsesDeterministicNamesAndOrder()
        {
            OpenPreview();
            VfxRecipe recipe = CreateRecipe(
                new[] { 0.25f, 0f },
                new[] { "top", "front", "side" });

            VfxFrameCaptureResult result =
                VfxFrameCapture.Capture(previewSession, recipe, captureRoot);

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(
                GetFileNames(result.FramePaths),
                Is.EqualTo(new[]
                {
                    "capture_recipe_f000_front_t00000000.png",
                    "capture_recipe_f001_side_t00000000.png",
                    "capture_recipe_f002_top_t00000000.png",
                    "capture_recipe_f003_front_t00250000.png",
                    "capture_recipe_f004_side_t00250000.png",
                    "capture_recipe_f005_top_t00250000.png"
                }));
        }

        [Test]
        public void Capture_ExistingTarget_ReturnsFailureWithoutOverwrite()
        {
            OpenPreview();
            VfxRecipe recipe = CreateRecipe(
                new[] { 0f },
                new[] { "front" });
            Directory.CreateDirectory(captureRoot);
            string fileName =
                VfxFrameCapture.BuildFileName(recipe.id, 0, 0f, "front");
            string existingPath = Path.Combine(captureRoot, fileName);
            File.WriteAllText(existingPath, "user-owned");

            VfxFrameCaptureResult result =
                VfxFrameCapture.Capture(previewSession, recipe, captureRoot);

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("CAPTURE-OUTPUT-EXISTS"));
            Assert.That(File.ReadAllText(existingPath), Is.EqualTo("user-owned"));
            Assert.That(
                File.Exists(Path.Combine(captureRoot, VfxFrameCapture.ManifestFileName)),
                Is.False);
        }

        [Test]
        public void Capture_InvalidFrameTime_ReturnsFailureWithoutArtifacts()
        {
            OpenPreview();
            VfxRecipe recipe = CreateRecipe(
                new[] { 1.5f },
                new[] { "front" });

            VfxFrameCaptureResult result =
                VfxFrameCapture.Capture(previewSession, recipe, captureRoot);

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("CAPTURE-SETTINGS"));
            Assert.That(Directory.Exists(captureRoot), Is.False);
        }

        [Test]
        public void Capture_DisposedSession_ReturnsFailure()
        {
            OpenPreview();
            previewSession.Dispose();

            VfxFrameCaptureResult result = VfxFrameCapture.Capture(
                previewSession,
                CreateRecipe(new[] { 0f }, new[] { "front" }),
                captureRoot);

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("CAPTURE-SESSION"));
            Assert.That(Directory.Exists(captureRoot), Is.False);
        }

        [Test]
        public void Capture_OutputDirectoryIsFile_ReturnsFailure()
        {
            OpenPreview();
            File.WriteAllText(captureRoot, "occupied");

            VfxFrameCaptureResult result = VfxFrameCapture.Capture(
                previewSession,
                CreateRecipe(new[] { 0f }, new[] { "front" }),
                captureRoot);

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("CAPTURE-OUTPUT"));
            Assert.That(File.ReadAllText(captureRoot), Is.EqualTo("occupied"));
        }

        [Test]
        public void SetCameraView_FrontSideAndTopUseDistinctPositions()
        {
            OpenPreview();

            previewSession.SetCameraView(VfxPreviewView.Front);
            Vector3 front = previewSession.PreviewCamera.transform.position;
            previewSession.SetCameraView(VfxPreviewView.Side);
            Vector3 side = previewSession.PreviewCamera.transform.position;
            previewSession.SetCameraView(VfxPreviewView.Top);
            Vector3 top = previewSession.PreviewCamera.transform.position;

            Assert.That(front, Is.Not.EqualTo(side));
            Assert.That(side, Is.Not.EqualTo(top));
            Assert.That(top.y, Is.GreaterThan(front.y));
        }

        [Test]
        public void PrepareFraming_TopIsOrthographicAndFrontIsPerspective()
        {
            OpenPreview();

            Assert.That(
                previewSession.TryPrepareCaptureFraming(
                    new[] { 0f, 0.1f },
                    1.15f,
                    1f,
                    out string error),
                Is.True,
                error);
            previewSession.SetCameraView(VfxPreviewView.Top);
            Assert.That(previewSession.PreviewCamera.orthographic, Is.True);
            previewSession.SetCameraView(VfxPreviewView.Front);
            Assert.That(previewSession.PreviewCamera.orthographic, Is.False);
            Assert.That(
                previewSession.CaptureBounds.size.sqrMagnitude,
                Is.GreaterThan(0f));
        }

        [Test]
        public void CaptureContentGate_BackgroundOnly_ReturnsVal007()
        {
            var pixels = new Color32[64 * 64];
            for (int index = 0; index < pixels.Length; index++)
            {
                pixels[index] = new Color32(30, 30, 30, 255);
            }

            VfxCaptureContentMetrics metrics =
                VfxCaptureContentGate.Measure(pixels, 64, 64);
            VfxValidationResult result = VfxCaptureContentGate.Evaluate(
                CreateRecipe(new[] { 0f }, new[] { "front" }),
                new VfxCapturedFrame
                {
                    fileName = "blank.png",
                    foregroundRatio = metrics.ForegroundRatio,
                    borderForegroundRatio = metrics.BorderForegroundRatio
                });

            Assert.That(result.ruleId, Is.EqualTo("VAL-007"));
            Assert.That(result.passed, Is.False);
            Assert.That(
                result.severity,
                Is.EqualTo(VfxValidationSeverity.Error));
        }

        [Test]
        public void CaptureContentGate_ForegroundOnBorder_ReturnsVal007()
        {
            var background = new Color32(30, 30, 30, 255);
            var pixels = new Color32[64 * 64];
            for (int index = 0; index < pixels.Length; index++)
            {
                pixels[index] = background;
            }
            for (int x = 0; x < 64; x++)
            {
                pixels[x] = new Color32(255, 255, 255, 255);
            }

            VfxCaptureContentMetrics metrics =
                VfxCaptureContentGate.Measure(pixels, 64, 64, background);
            VfxValidationResult result = VfxCaptureContentGate.Evaluate(
                CreateRecipe(new[] { 0f }, new[] { "front" }),
                new VfxCapturedFrame
                {
                    fileName = "clipped.png",
                    foregroundRatio = metrics.ForegroundRatio,
                    borderForegroundRatio = metrics.BorderForegroundRatio
                });

            Assert.That(metrics.ForegroundRatio, Is.GreaterThan(0.01f));
            Assert.That(metrics.BorderForegroundRatio, Is.GreaterThan(0.005f));
            Assert.That(result.ruleId, Is.EqualTo("VAL-007"));
            Assert.That(result.passed, Is.False);
        }

        [Test]
        public void Capture_StoppedSession_RemainsStopped()
        {
            OpenPreview();
            previewSession.Stop();

            VfxFrameCaptureResult result = VfxFrameCapture.Capture(
                previewSession,
                CreateRecipe(new[] { 0f }, new[] { "front" }),
                captureRoot);

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(previewSession.IsPlaying, Is.False);
        }

        private void OpenPreview()
        {
            VfxPreviewOpenResult open = VfxPreviewSession.Open(generatedPrefab);
            Assert.That(open.Success, Is.True, open.Message);
            previewSession = open.Session;
        }

        private static VfxRecipe CreateRecipe(float[] frameTimes, string[] views)
        {
            return new VfxRecipe
            {
                id = "capture_recipe",
                capture = new VfxCaptureSettings
                {
                    duration = 1f,
                    frameTimes = frameTimes,
                    views = views,
                    width = 64,
                    height = 64
                }
            };
        }

        private GameObject CreateGeneratedPrefab(string path)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Sprites/Default");
            Assert.That(shader, Is.Not.Null);

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.magenta);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.magenta);
            }
            AssetDatabase.CreateAsset(material, $"{testAssetRoot}/CaptureMaterial.mat");

            var source = new GameObject("Generated");
            try
            {
                source.AddComponent<VfxMetadata>();
                source.AddComponent<VisualEffect>();

                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "CaptureTarget";
                cube.transform.SetParent(source.transform, false);
                cube.transform.localPosition = Vector3.up;
                cube.GetComponent<Renderer>().sharedMaterial = material;
                UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
                Assert.That(prefab, Is.Not.Null);
                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static string[] GetFileNames(System.Collections.Generic.IEnumerable<string> paths)
        {
            var names = new System.Collections.Generic.List<string>();
            foreach (string path in paths)
            {
                names.Add(Path.GetFileName(path));
            }
            return names.ToArray();
        }

        private static void AssertPng(
            string path,
            int expectedWidth,
            int expectedHeight)
        {
            Assert.That(File.Exists(path), Is.True, path);
            byte[] bytes = File.ReadAllBytes(path);
            Assert.That(bytes.Length, Is.GreaterThan(8));
            byte[] signature = { 137, 80, 78, 71, 13, 10, 26, 10 };
            for (int index = 0; index < signature.Length; index++)
            {
                Assert.That(bytes[index], Is.EqualTo(signature[index]));
            }

            var texture = new Texture2D(2, 2);
            try
            {
                Assert.That(ImageConversion.LoadImage(texture, bytes), Is.True);
                Assert.That(texture.width, Is.EqualTo(expectedWidth));
                Assert.That(texture.height, Is.EqualTo(expectedHeight));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void AssertContainsForeground(
            string path,
            float minimumRatio)
        {
            byte[] bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2);
            try
            {
                Assert.That(ImageConversion.LoadImage(texture, bytes), Is.True);
                Color32[] pixels = texture.GetPixels32();
                Color32 background = pixels[0];
                int foregroundPixels = 0;
                foreach (Color32 pixel in pixels)
                {
                    int difference =
                        Math.Abs(pixel.r - background.r)
                        + Math.Abs(pixel.g - background.g)
                        + Math.Abs(pixel.b - background.b);
                    if (difference > 12)
                    {
                        foregroundPixels++;
                    }
                }

                float ratio = (float)foregroundPixels / pixels.Length;
                Assert.That(
                    ratio,
                    Is.GreaterThanOrEqualTo(minimumRatio),
                    $"Expected at least {minimumRatio:P0} foreground pixels in {path}, got {ratio:P2}.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
