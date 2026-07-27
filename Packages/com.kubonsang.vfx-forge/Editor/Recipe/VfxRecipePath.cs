using System;

namespace Kubonsang.VfxForge.Editor
{
    public static class VfxRecipePath
    {
        public static bool TryNormalizePrefabAssetPath(string path, out string normalizedPath)
        {
            normalizedPath = path?.Trim().Replace('\\', '/') ?? string.Empty;
            if (!normalizedPath.StartsWith("Assets/", StringComparison.Ordinal)
                || !normalizedPath.EndsWith(".prefab", StringComparison.Ordinal))
            {
                return false;
            }

            string[] segments = normalizedPath.Split('/');
            if (segments.Length < 2 || !string.Equals(segments[0], "Assets", StringComparison.Ordinal))
            {
                return false;
            }

            for (int index = 0; index < segments.Length; index++)
            {
                string segment = segments[index];
                if (string.IsNullOrEmpty(segment)
                    || string.Equals(segment, ".", StringComparison.Ordinal)
                    || string.Equals(segment, "..", StringComparison.Ordinal)
                    || segment.IndexOf(':') >= 0
                    || ContainsControlCharacter(segment))
                {
                    return false;
                }
            }

            string fileName = segments[segments.Length - 1];
            return fileName.Length > ".prefab".Length;
        }

        private static bool ContainsControlCharacter(string value)
        {
            foreach (char character in value)
            {
                if (char.IsControl(character))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
