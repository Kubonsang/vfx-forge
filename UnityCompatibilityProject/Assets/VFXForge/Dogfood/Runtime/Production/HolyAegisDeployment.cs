using Kubonsang.VfxForge;
using UnityEngine;

namespace VfxForge.Dogfood
{
    [DisallowMultipleComponent]
    public sealed class HolyAegisDeployment
        : MonoBehaviour,
            IVfxPreviewTimeEvaluable,
            IVfxRecipeBindingAdapter
    {
        public const string AdapterId = "holy-aegis-v3";

        private static readonly int PrimaryColorId =
            Shader.PropertyToID("_PrimaryColor");
        private static readonly int SecondaryColorId =
            Shader.PropertyToID("_SecondaryColor");
        private static readonly int EmissionId =
            Shader.PropertyToID("_Emission");
        private static readonly int SharpnessId =
            Shader.PropertyToID("_Sharpness");
        private static readonly int AgeId =
            Shader.PropertyToID("_Age01");
        private static readonly int LayerAlphaId =
            Shader.PropertyToID("_LayerAlpha");
        private static readonly int SeedId =
            Shader.PropertyToID("_Seed");

        [SerializeField] private Transform assembly;
        [SerializeField] private Transform plate;
        [SerializeField] private Transform rim;
        [SerializeField] private Transform crest;
        [SerializeField] private Transform ornaments;
        [SerializeField] private Renderer[] plateRenderers =
            System.Array.Empty<Renderer>();
        [SerializeField] private Renderer[] rimRenderers =
            System.Array.Empty<Renderer>();
        [SerializeField] private Renderer[] crestRenderers =
            System.Array.Empty<Renderer>();
        [SerializeField] private Renderer[] ornamentRenderers =
            System.Array.Empty<Renderer>();

        [SerializeField] private int randomSeed = 190731;
        [SerializeField] private float duration = 1.8f;
        [SerializeField] private float impactTime = 0.28f;
        [SerializeField] private float sustainTime = 1.10f;
        [SerializeField] private float decayTime = 0.42f;
        [SerializeField] private float radius = 2.6f;
        [SerializeField] private float spreadAngle = 140f;
        [SerializeField] private float directionality = 1f;
        [SerializeField] private float emission = 3.2f;
        [SerializeField] private float sharpness = 0.82f;
        [SerializeField] private float pulseRate = 0.8f;
        [SerializeField] private Vector3 localDirection =
            Vector3.forward;
        [SerializeField] private Color primaryColor =
            new Color(0.02f, 0.84f, 0.46f, 1f);
        [SerializeField] private Color secondaryColor =
            new Color(1f, 0.72f, 0.18f, 1f);

        private MaterialPropertyBlock propertyBlock;
        private float elapsed;

        public string BindingAdapterId => AdapterId;
        public float Duration => duration;
        public float Radius => radius;
        public bool IsDeployed =>
            elapsed >= impactTime
            && elapsed < impactTime + sustainTime;

        public void Configure(
            Transform shieldAssembly,
            Transform plateRoot,
            Transform rimRoot,
            Transform crestRoot,
            Transform ornamentRoot,
            Renderer[] plateLayers,
            Renderer[] rimLayers,
            Renderer[] crestLayers,
            Renderer[] ornamentLayers)
        {
            assembly = shieldAssembly;
            plate = plateRoot;
            rim = rimRoot;
            crest = crestRoot;
            ornaments = ornamentRoot;
            plateRenderers =
                plateLayers ?? System.Array.Empty<Renderer>();
            rimRenderers =
                rimLayers ?? System.Array.Empty<Renderer>();
            crestRenderers =
                crestLayers ?? System.Array.Empty<Renderer>();
            ornamentRenderers =
                ornamentLayers ?? System.Array.Empty<Renderer>();
        }

        private void OnEnable()
        {
            elapsed = 0f;
            EvaluateVisuals(0f);
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            elapsed += Time.deltaTime;
            EvaluateVisuals(elapsed);
            if (elapsed >= duration)
            {
                Destroy(gameObject);
            }
        }

        public void EvaluatePreviewTime(float timeSeconds)
        {
            elapsed = Mathf.Clamp(timeSeconds, 0f, duration);
            EvaluateVisuals(elapsed);
        }

        public bool SupportsBinding(
            string propertyName,
            VfxRecipeBindingValueType valueType)
        {
            switch (propertyName)
            {
                case "RandomSeed":
                    return valueType == VfxRecipeBindingValueType.Int;
                case "LocalDirection":
                    return valueType
                        == VfxRecipeBindingValueType.Vector3;
                case "PrimaryColor":
                case "SecondaryColor":
                    return valueType
                        == VfxRecipeBindingValueType.Color;
                case "Duration":
                case "ImpactTime":
                case "SustainTime":
                case "DecayTime":
                case "Radius":
                case "SpreadAngle":
                case "Directionality":
                case "EmissionIntensity":
                case "Sharpness":
                case "PulseRate":
                    return valueType
                        == VfxRecipeBindingValueType.Float;
                default:
                    return false;
            }
        }

        public bool TryApplyBinding(
            string propertyName,
            VfxRecipeBindingValue value)
        {
            if (!SupportsBinding(propertyName, value.Type))
            {
                return false;
            }

            if (propertyName == "RandomSeed"
                && value.Value is int seed)
            {
                randomSeed = seed;
                return true;
            }
            if (propertyName == "LocalDirection"
                && value.Value is Vector3 direction)
            {
                localDirection = direction;
                return true;
            }
            if (propertyName == "PrimaryColor"
                && value.Value is Color primary)
            {
                primaryColor = primary;
                return true;
            }
            if (propertyName == "SecondaryColor"
                && value.Value is Color secondary)
            {
                secondaryColor = secondary;
                return true;
            }
            if (!(value.Value is float number))
            {
                return false;
            }

            switch (propertyName)
            {
                case "Duration":
                    duration = number;
                    break;
                case "ImpactTime":
                    impactTime = number;
                    break;
                case "SustainTime":
                    sustainTime = number;
                    break;
                case "DecayTime":
                    decayTime = number;
                    break;
                case "Radius":
                    radius = number;
                    break;
                case "SpreadAngle":
                    spreadAngle = number;
                    break;
                case "Directionality":
                    directionality = number;
                    break;
                case "EmissionIntensity":
                    emission = number;
                    break;
                case "Sharpness":
                    sharpness = number;
                    break;
                case "PulseRate":
                    pulseRate = number;
                    break;
                default:
                    return false;
            }
            return true;
        }

        private void EvaluateVisuals(float timeSeconds)
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            float safeImpact = Mathf.Max(0.01f, impactTime);
            float decayStart =
                Mathf.Min(duration, impactTime + sustainTime);
            float safeDecay = Mathf.Max(
                0.01f,
                Mathf.Min(decayTime, duration - decayStart));
            float decay01 = Mathf.Clamp01(
                (timeSeconds - decayStart) / safeDecay);
            float age01 = Mathf.Clamp01(
                timeSeconds / Mathf.Max(0.01f, duration));
            float radiusScale = radius / 2.6f;
            float pulse = timeSeconds >= impactTime
                && timeSeconds < decayStart
                    ? 1f + Mathf.Sin(
                        (timeSeconds - impactTime)
                        * Mathf.PI
                        * 2f
                        * Mathf.Max(0f, pulseRate)) * 0.012f
                    : 1f;

            float crestReveal = SmoothRange(
                timeSeconds,
                0f,
                safeImpact * 0.46f);
            float plateReveal = SmoothRange(
                timeSeconds,
                safeImpact * 0.10f,
                safeImpact * 0.92f);
            float rimReveal = SmoothRange(
                timeSeconds,
                safeImpact * 0.20f,
                safeImpact);
            float ornamentReveal = SmoothRange(
                timeSeconds,
                safeImpact * 0.40f,
                safeImpact);

            float ornamentAlpha =
                1f - SmoothRange(decay01, 0f, 0.36f);
            float rimAlpha =
                1f - SmoothRange(decay01, 0.16f, 0.62f);
            float plateAlpha =
                1f - SmoothRange(decay01, 0.38f, 0.88f);
            float crestAlpha =
                1f - SmoothRange(decay01, 0.62f, 1f);

            if (assembly != null)
            {
                Vector3 direction = localDirection.sqrMagnitude > 0f
                    ? localDirection.normalized
                    : Vector3.forward;
                assembly.localPosition =
                    new Vector3(0f, 2.28f, 0f)
                    + direction
                    * Mathf.Lerp(
                        0f,
                        0.08f,
                        Mathf.Clamp01(directionality));
            }

            SetScale(
                plate,
                radiusScale * pulse * plateReveal);
            SetScale(
                rim,
                radiusScale * pulse * rimReveal);
            SetScale(
                crest,
                radiusScale * crestReveal);
            SetScale(
                ornaments,
                radiusScale * pulse * ornamentReveal);

            ApplyGroup(
                plateRenderers,
                age01,
                plateReveal * plateAlpha,
                false);
            ApplyGroup(
                rimRenderers,
                age01,
                rimReveal * rimAlpha,
                true);
            ApplyGroup(
                crestRenderers,
                age01,
                crestReveal * crestAlpha,
                true);
            ApplyGroup(
                ornamentRenderers,
                age01,
                ornamentReveal * ornamentAlpha,
                true);
        }

        private void ApplyGroup(
            Renderer[] renderers,
            float age01,
            float alpha,
            bool goldForward)
        {
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                if (renderer == null)
                {
                    continue;
                }

                Color layerPrimary = goldForward
                    ? Color.Lerp(
                        secondaryColor,
                        primaryColor,
                        index % 2 == 0 ? 0.04f : 0.20f)
                    : primaryColor;
                Color layerSecondary = goldForward
                    ? Color.Lerp(
                        secondaryColor,
                        new Color(1f, 0.80f, 0.26f, 1f),
                        0.22f)
                    : Color.Lerp(
                        primaryColor,
                        new Color(0.18f, 0.94f, 0.54f, 1f),
                        0.30f);

                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(
                    PrimaryColorId,
                    layerPrimary);
                propertyBlock.SetColor(
                    SecondaryColorId,
                    layerSecondary);
                propertyBlock.SetFloat(
                    EmissionId,
                    emission
                    * (goldForward ? 0.94f : 1f));
                propertyBlock.SetFloat(
                    SharpnessId,
                    sharpness);
                propertyBlock.SetFloat(AgeId, age01);
                propertyBlock.SetFloat(
                    LayerAlphaId,
                    Mathf.Clamp01(alpha));
                propertyBlock.SetFloat(
                    SeedId,
                    randomSeed * 0.0001f);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private static float SmoothRange(
            float value,
            float start,
            float end)
        {
            if (end <= start)
            {
                return value >= end ? 1f : 0f;
            }
            return Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(start, end, value));
        }

        private static void SetScale(
            Transform target,
            float scale)
        {
            if (target != null)
            {
                target.localScale =
                    Vector3.one * Mathf.Max(0.001f, scale);
            }
        }
    }
}
