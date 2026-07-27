using System;

namespace Kubonsang.VfxForge
{
    [Serializable]
    public sealed class VfxRecipe
    {
        public string schemaVersion = "1.0";
        public string id = string.Empty;
        public string displayName = string.Empty;
        public string template = string.Empty;
        public string styleProfile = string.Empty;
        public string intent = string.Empty;
        public string outputPath = string.Empty;
        public string anchor = "HitPoint";
        public int seed = 1;
        public VfxTiming timing = new VfxTiming();
        public VfxShape shape = new VfxShape();
        public VfxStyle style = new VfxStyle();
        public string[] layers = Array.Empty<string>();
        public VfxBudget budget = new VfxBudget();
        public VfxCaptureSettings capture = new VfxCaptureSettings();
    }

    [Serializable]
    public sealed class VfxTiming
    {
        public float duration = 0.5f;
        public float anticipation;
        public float impact = 0.1f;
        public float sustain = 0.1f;
        public float decay = 0.3f;
    }

    [Serializable]
    public sealed class VfxShape
    {
        public float radius = 1f;
        public float directionality;
        public float spreadAngle = 45f;
    }

    [Serializable]
    public sealed class VfxStyle
    {
        public string primaryColor = "#FFFFFFFF";
        public string secondaryColor = "#FFFFFFFF";
        public float emissionIntensity = 1f;
        public float sharpness = 0.5f;
        public float distortionStrength;
    }

    [Serializable]
    public sealed class VfxBudget
    {
        public int maxParticles = 500;
        public float maxDuration = 1f;
        public float maxBoundsRadius = 5f;
        public bool allowDistortion;
        public bool allowLight;
    }

    [Serializable]
    public sealed class VfxCaptureSettings
    {
        public float duration = 1f;
        public float[] frameTimes = { 0f, 0.1f, 0.2f, 0.5f, 1f };
        public string[] views = { "front" };
        public int width = 1024;
        public int height = 1024;
    }
}
