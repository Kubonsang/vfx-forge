using Kubonsang.VfxForge;
using UnityEngine;
using UnityEngine.VFX;

namespace VfxForge.Dogfood
{
    [DisallowMultipleComponent]
    public sealed class GiantShieldDeployment
        : MonoBehaviour, IVfxPreviewTimeEvaluable
    {
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
        private static readonly int PanelIndexId =
            Shader.PropertyToID("_PanelIndex");
        private static readonly int SeedId =
            Shader.PropertyToID("_Seed");

        [SerializeField] private VisualEffect settingsSource;
        [SerializeField] private Transform shieldRoot;
        [SerializeField] private Transform[] panelRoots =
            System.Array.Empty<Transform>();
        [SerializeField] private Renderer[] shieldRenderers =
            System.Array.Empty<Renderer>();
        [SerializeField] private ParticleSystem anchorBurst;
        [SerializeField] private ParticleSystem edgeMotes;
        [SerializeField] private ParticleSystem dissolveShards;

        private MaterialPropertyBlock propertyBlock;
        private float elapsed;
        private float duration = 1.8f;
        private float impactTime = 0.32f;
        private float sustainTime = 1.08f;
        private float decayTime = 0.40f;
        private float radius = 3.2f;
        private float spreadAngle = 120f;
        private float directionality = 1f;
        private float emission = 6.5f;
        private float sharpness = 0.78f;
        private int randomSeed = 130730;
        private Color primaryColor =
            new Color(0.086f, 0.85f, 1f, 1f);
        private Color secondaryColor =
            new Color(0.957f, 1f, 1f, 1f);
        private bool dissolvePlayed;

        public float Duration => duration;
        public float Radius => radius;
        public bool IsDeployed => elapsed >= impactTime
            && elapsed < duration - decayTime;

        public void Configure(
            VisualEffect source,
            Transform visualRoot,
            Transform[] panels,
            Renderer[] renderers,
            ParticleSystem anchors,
            ParticleSystem motes,
            ParticleSystem shards)
        {
            settingsSource = source;
            shieldRoot = visualRoot;
            panelRoots = panels ?? System.Array.Empty<Transform>();
            shieldRenderers = renderers ?? System.Array.Empty<Renderer>();
            anchorBurst = anchors;
            edgeMotes = motes;
            dissolveShards = shards;
        }

        private void OnEnable()
        {
            ReadRecipeOverrides();
            elapsed = 0f;
            dissolvePlayed = false;
            EvaluateVisuals(0f);

            if (!Application.isPlaying)
            {
                return;
            }

            VfxPlayer player = GetComponent<VfxPlayer>();
            if (player != null)
            {
                player.CacheEffects();
                player.Play();
            }
            PlayIfPresent(anchorBurst);
            PlayIfPresent(edgeMotes);
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            elapsed += Time.deltaTime;
            EvaluateVisuals(elapsed);
            if (!dissolvePlayed
                && elapsed >= Mathf.Max(0f, duration - decayTime))
            {
                dissolvePlayed = true;
                PlayIfPresent(dissolveShards);
            }

            if (elapsed >= duration)
            {
                Destroy(gameObject);
            }
        }

        public void EvaluatePreviewTime(float timeSeconds)
        {
            ReadRecipeOverrides();
            elapsed = Mathf.Clamp(timeSeconds, 0f, duration);
            EvaluateVisuals(elapsed);
            SimulateParticle(anchorBurst, elapsed);
            SimulateParticle(edgeMotes, elapsed);

            float dissolveStart = Mathf.Max(0f, duration - decayTime);
            if (elapsed >= dissolveStart)
            {
                SimulateParticle(dissolveShards, elapsed - dissolveStart);
            }
        }

        private void ReadRecipeOverrides()
        {
            if (settingsSource == null)
            {
                return;
            }

            duration = ReadFloat("Duration", duration, 0.2f, 6f);
            impactTime = ReadFloat(
                "ImpactTime",
                impactTime,
                0.04f,
                duration);
            sustainTime = ReadFloat(
                "SustainTime",
                sustainTime,
                0f,
                duration);
            decayTime = ReadFloat(
                "DecayTime",
                decayTime,
                0.05f,
                duration);
            radius = ReadFloat("Radius", radius, 1f, 6f);
            spreadAngle = ReadFloat(
                "SpreadAngle",
                spreadAngle,
                50f,
                170f);
            directionality = ReadFloat(
                "Directionality",
                directionality,
                0f,
                1f);
            emission = ReadFloat(
                "EmissionIntensity",
                emission,
                0f,
                16f);
            sharpness = ReadFloat("Sharpness", sharpness, 0f, 1f);
            primaryColor = ReadColor("PrimaryColor", primaryColor);
            secondaryColor = ReadColor(
                "SecondaryColor",
                secondaryColor);
            if (settingsSource.HasInt("RandomSeed"))
            {
                randomSeed = settingsSource.GetInt("RandomSeed");
            }
        }

        private float ReadFloat(
            string property,
            float fallback,
            float minimum,
            float maximum)
        {
            return settingsSource.HasFloat(property)
                ? Mathf.Clamp(
                    settingsSource.GetFloat(property),
                    minimum,
                    maximum)
                : fallback;
        }

        private Color ReadColor(string property, Color fallback)
        {
            if (!settingsSource.HasVector4(property))
            {
                return fallback;
            }

            Vector4 value = settingsSource.GetVector4(property);
            return new Color(value.x, value.y, value.z, value.w);
        }

        private void EvaluateVisuals(float timeSeconds)
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            float age01 = Mathf.Clamp01(
                timeSeconds / Mathf.Max(0.01f, duration));
            float decayStart = Mathf.Clamp01(
                1f - decayTime / Mathf.Max(0.01f, duration));
            float radiusScale = radius / 3.2f;

            if (shieldRoot != null)
            {
                shieldRoot.localScale = Vector3.one * radiusScale;
                shieldRoot.localPosition = new Vector3(
                    0f,
                    0f,
                    Mathf.Lerp(0.15f, 0.5f, directionality));
            }

            int panelCount = panelRoots.Length;
            float middle = Mathf.Max(1f, (panelCount - 1) * 0.5f);
            for (int index = 0; index < panelCount; index++)
            {
                Transform panel = panelRoots[index];
                if (panel == null)
                {
                    continue;
                }

                float signedIndex = index - middle;
                float distance01 = Mathf.Abs(signedIndex) / middle;
                float delay = distance01 * impactTime * 0.38f;
                float availableImpact = Mathf.Max(
                    0.04f,
                    impactTime - delay);
                float deployProgress = Mathf.Clamp01(
                    (timeSeconds - delay) / availableImpact);
                float deploy = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Sqrt(deployProgress));
                float panelDecay = Mathf.Clamp01(
                    Mathf.InverseLerp(
                        decayStart - distance01 * 0.055f,
                        1f,
                        age01));
                float visibility = deploy
                    * (1f - Mathf.SmoothStep(0f, 1f, panelDecay));

                float angle = signedIndex
                    * (spreadAngle / Mathf.Max(1f, panelCount));
                panel.localRotation = Quaternion.Euler(0f, angle, 0f);
                panel.localScale = new Vector3(
                    Mathf.Lerp(0.24f, 1f, deploy),
                    Mathf.Lerp(0.015f, 1f, deploy),
                    1f);

                ApplyPanelProperties(index, panelCount, age01, visibility);
            }
        }

        private void ApplyPanelProperties(
            int panelIndex,
            int panelCount,
            float age01,
            float visibility)
        {
            float normalizedPanel = panelCount <= 1
                ? 0.5f
                : panelIndex / (float)(panelCount - 1);
            int renderersPerPanel = panelCount == 0
                ? 0
                : shieldRenderers.Length / panelCount;
            int start = panelIndex * renderersPerPanel;
            int end = Mathf.Min(
                shieldRenderers.Length,
                start + renderersPerPanel);

            for (int index = start; index < end; index++)
            {
                Renderer renderer = shieldRenderers[index];
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(PrimaryColorId, primaryColor);
                propertyBlock.SetColor(
                    SecondaryColorId,
                    secondaryColor);
                propertyBlock.SetFloat(EmissionId, emission);
                propertyBlock.SetFloat(SharpnessId, sharpness);
                propertyBlock.SetFloat(AgeId, age01);
                propertyBlock.SetFloat(
                    LayerAlphaId,
                    visibility);
                propertyBlock.SetFloat(
                    PanelIndexId,
                    normalizedPanel);
                propertyBlock.SetFloat(
                    SeedId,
                    randomSeed * 0.0001f);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private static void PlayIfPresent(ParticleSystem system)
        {
            if (system != null)
            {
                system.Play(true);
            }
        }

        private static void SimulateParticle(
            ParticleSystem system,
            float timeSeconds)
        {
            if (system != null)
            {
                system.Simulate(
                    Mathf.Max(0f, timeSeconds),
                    true,
                    true,
                    true);
            }
        }
    }
}
