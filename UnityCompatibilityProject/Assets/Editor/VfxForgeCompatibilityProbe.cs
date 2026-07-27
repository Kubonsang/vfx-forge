using System;
using System.IO;
using System.Reflection;
using Kubonsang.VfxForge;
using Kubonsang.VfxForge.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

public static class VfxForgeCompatibilityProbe
{
    private const string ArtifactPath = "Artifacts/vf-011-console.json";
    private const string CaptureArtifactDirectory = "Artifacts/vf-008-capture";
    private const string CaptureAssetRoot = "Assets/__VfxForgeCaptureProbe";

    public static void CaptureConsoleCounts()
    {
        Type logEntriesType = typeof(Editor).Assembly.GetType("UnityEditor.LogEntries", true);
        MethodInfo getCountsMethod = logEntriesType.GetMethod(
            "GetCountsByType",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        if (getCountsMethod == null)
        {
            throw new MissingMethodException(logEntriesType.FullName, "GetCountsByType");
        }

        object[] arguments = { 0, 0, 0 };
        getCountsMethod.Invoke(null, arguments);

        var counts = new ConsoleCounts
        {
            errors = (int)arguments[0],
            warnings = (int)arguments[1],
            logs = (int)arguments[2]
        };

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string outputPath = Path.Combine(projectRoot, ArtifactPath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        File.WriteAllText(outputPath, JsonUtility.ToJson(counts, true));
        Debug.Log($"[VFXForge] Console counts written to {outputPath}");
    }

    public static void CaptureFrameFixture()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string outputDirectory = Path.Combine(projectRoot, CaptureArtifactDirectory);
        if (Directory.Exists(outputDirectory))
        {
            Directory.Delete(outputDirectory, true);
        }

        if (AssetDatabase.IsValidFolder(CaptureAssetRoot))
        {
            AssetDatabase.DeleteAsset(CaptureAssetRoot);
        }
        AssetDatabase.CreateFolder("Assets", "__VfxForgeCaptureProbe");

        VfxPreviewSession session = null;
        GameObject source = null;
        try
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                throw new InvalidOperationException("No unlit fixture Shader is available.");
            }

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.magenta);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.magenta);
            }
            AssetDatabase.CreateAsset(material, $"{CaptureAssetRoot}/CaptureMaterial.mat");

            source = new GameObject("Capture Fixture");
            source.AddComponent<VfxMetadata>();
            source.AddComponent<VisualEffect>();

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(source.transform, false);
            cube.transform.localPosition = Vector3.up;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
                source,
                $"{CaptureAssetRoot}/CaptureFixture.prefab");
            VfxPreviewOpenResult open = VfxPreviewSession.Open(prefab);
            if (!open.Success)
            {
                throw new InvalidOperationException(
                    $"{open.ErrorCode}: {open.Message}");
            }
            session = open.Session;

            var recipe = new VfxRecipe
            {
                id = "vf008_fixture",
                capture = new VfxCaptureSettings
                {
                    duration = 0.1f,
                    frameTimes = new[] { 0f, 0.1f },
                    views = new[] { "front", "side", "top" },
                    width = 128,
                    height = 128
                }
            };
            VfxFrameCaptureResult capture =
                VfxFrameCapture.Capture(session, recipe, outputDirectory);
            if (!capture.Success)
            {
                throw new InvalidOperationException(
                    $"{capture.ErrorCode}: {capture.Message}");
            }
            if (capture.FramePaths.Count != 6 || !File.Exists(capture.ManifestPath))
            {
                throw new InvalidOperationException(
                    "Capture fixture did not produce six frames and a manifest.");
            }

            Debug.Log(
                $"[VFXForge] Capture fixture written: {capture.ManifestPath}");
        }
        finally
        {
            session?.Dispose();
            if (source != null)
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
            if (AssetDatabase.IsValidFolder(CaptureAssetRoot))
            {
                AssetDatabase.DeleteAsset(CaptureAssetRoot);
            }
        }
    }

    [Serializable]
    private sealed class ConsoleCounts
    {
        public int errors;
        public int warnings;
        public int logs;
    }
}
