using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class VfxForgeCompatibilityProbe
{
    private const string ArtifactPath = "Artifacts/vf-005-console.json";

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

    [Serializable]
    private sealed class ConsoleCounts
    {
        public int errors;
        public int warnings;
        public int logs;
    }
}
