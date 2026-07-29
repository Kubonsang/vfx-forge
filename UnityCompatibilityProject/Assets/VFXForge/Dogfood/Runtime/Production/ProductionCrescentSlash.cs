using Kubonsang.VfxForge;
using UnityEngine;
using UnityEngine.VFX;

namespace VfxForge.Dogfood
{
    [DisallowMultipleComponent]
    public sealed class ProductionCrescentSlash : MonoBehaviour
    {
        private static readonly int PrimaryColorId = Shader.PropertyToID("_PrimaryColor");
        private static readonly int SecondaryColorId = Shader.PropertyToID("_SecondaryColor");
        private static readonly int EmissionId = Shader.PropertyToID("_Emission");
        private static readonly int SharpnessId = Shader.PropertyToID("_Sharpness");
        private static readonly int AgeId = Shader.PropertyToID("_Age01");
        private static readonly int LayerAlphaId = Shader.PropertyToID("_LayerAlpha");
        private static readonly int SeedId = Shader.PropertyToID("_Seed");

        [SerializeField] private VisualEffect settingsSource;
        [SerializeField] private Transform bodyRoot;
        [SerializeField] private Renderer[] bodyRenderers = System.Array.Empty<Renderer>();
        [SerializeField] private ParticleSystem leadingSparks;
        [SerializeField] private ParticleSystem trailingWisps;
        [SerializeField] private ParticleSystem dissipateBurst;
        [SerializeField, Min(0f)] private float travelSpeed = 11f;

        private MaterialPropertyBlock propertyBlock;
        private float elapsed;
        private float duration = 0.52f;
        private float impactTime = 0.08f;
        private float sustainTime = 0.24f;
        private float decayTime = 0.20f;
        private float radius = 1.65f;
        private float spreadAngle = 140f;
        private float directionality = 1f;
        private float emission = 5.5f;
        private float sharpness = 0.82f;
        private int randomSeed = 120729;
        private Color primaryColor = new Color(0.07f, 0.85f, 1f, 1f);
        private Color secondaryColor = new Color(0.91f, 1f, 1f, 1f);
        private bool dissipatePlayed;

        public float Duration => duration;
        public float TravelSpeed => travelSpeed;

        public void Configure(
            VisualEffect source,
            Transform visualRoot,
            Renderer[] renderers,
            ParticleSystem sparks,
            ParticleSystem wisps,
            ParticleSystem dissipate,
            float speed)
        {
            settingsSource = source;
            bodyRoot = visualRoot;
            bodyRenderers = renderers ?? System.Array.Empty<Renderer>();
            leadingSparks = sparks;
            trailingWisps = wisps;
            dissipateBurst = dissipate;
            travelSpeed = Mathf.Max(0f, speed);
        }

        private void OnEnable()
        {
            ReadRecipeOverrides();
            elapsed = 0f;
            dissipatePlayed = false;
            EvaluateVisuals(0f, true);

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
            PlayIfPresent(leadingSparks);
            PlayIfPresent(trailingWisps);
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            elapsed += deltaTime;
            transform.position += transform.forward
                * (travelSpeed * Mathf.Lerp(0.88f, 1.08f, directionality) * deltaTime);
            EvaluateVisuals(elapsed, false);

            if (!dissipatePlayed && elapsed >= Mathf.Max(0f, duration - decayTime))
            {
                dissipatePlayed = true;
                PlayIfPresent(dissipateBurst);
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
            EvaluateVisuals(elapsed, true);
            SimulateParticle(leadingSparks, elapsed);
            SimulateParticle(trailingWisps, elapsed);
            if (elapsed >= Mathf.Max(0f, duration - decayTime))
            {
                SimulateParticle(dissipateBurst, elapsed - (duration - decayTime));
            }
        }

        private void ReadRecipeOverrides()
        {
            if (settingsSource == null)
            {
                return;
            }

            duration = ReadFloat("Duration", duration, 0.05f, 4f);
            impactTime = ReadFloat("ImpactTime", impactTime, 0.01f, duration);
            sustainTime = ReadFloat("SustainTime", sustainTime, 0f, duration);
            decayTime = ReadFloat("DecayTime", decayTime, 0.01f, duration);
            radius = ReadFloat("Radius", radius, 0.25f, 5f);
            spreadAngle = ReadFloat("SpreadAngle", spreadAngle, 45f, 220f);
            directionality = ReadFloat("Directionality", directionality, 0f, 1f);
            emission = ReadFloat("EmissionIntensity", emission, 0f, 16f);
            sharpness = ReadFloat("Sharpness", sharpness, 0f, 1f);
            primaryColor = ReadColor("PrimaryColor", primaryColor);
            secondaryColor = ReadColor("SecondaryColor", secondaryColor);
            if (settingsSource.HasInt("RandomSeed"))
            {
                randomSeed = settingsSource.GetInt("RandomSeed");
            }
        }

        private float ReadFloat(string property, float fallback, float minimum, float maximum)
        {
            return settingsSource.HasFloat(property)
                ? Mathf.Clamp(settingsSource.GetFloat(property), minimum, maximum)
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

        private void EvaluateVisuals(float timeSeconds, bool includePreviewOffset)
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
            float age01 = Mathf.Clamp01(timeSeconds / Mathf.Max(0.01f, duration));
            float revealProgress = Mathf.Clamp01(
                timeSeconds / Mathf.Max(0.01f, impactTime));
            float reveal = Mathf.SmoothStep(0f, 1f, Mathf.Sqrt(revealProgress));
            float decayStart = Mathf.Clamp01(1f - decayTime / Mathf.Max(0.01f, duration));
            float decayProgress = Mathf.InverseLerp(decayStart, 1f, age01);
            float dissolve = 1f - Mathf.SmoothStep(0f, 1f, decayProgress);
            float alpha = reveal * dissolve;
            float expansion = Mathf.Lerp(0.78f, 1.06f, Mathf.SmoothStep(0f, 1f, age01));

            if (bodyRoot != null)
            {
                float radiusScale = radius;
                float spreadScale = Mathf.Lerp(0.72f, 1.22f, Mathf.InverseLerp(80f, 180f, spreadAngle));
                bodyRoot.localScale = new Vector3(
                    radiusScale * spreadScale * expansion,
                    radiusScale,
                    radiusScale * expansion);
                bodyRoot.localPosition = includePreviewOffset
                    ? new Vector3(0f, 0.03f, 0f)
                    : Vector3.zero;
            }

            for (int index = 0; index < bodyRenderers.Length; index++)
            {
                Renderer renderer = bodyRenderers[index];
                if (renderer == null)
                {
                    continue;
                }

                float layerPosition = bodyRenderers.Length <= 1
                    ? 0.5f
                    : index / (float)(bodyRenderers.Length - 1);
                Color layerPrimary = Color.Lerp(
                    primaryColor,
                    secondaryColor,
                    Mathf.Lerp(0.02f, 0.72f, layerPosition));
                Color layerSecondary = Color.Lerp(
                    primaryColor,
                    secondaryColor,
                    Mathf.Lerp(0.28f, 1f, layerPosition));
                float layerAlpha = alpha * Mathf.Lerp(0.68f, 0.82f, layerPosition);
                float layerEmission = emission * Mathf.Lerp(0.62f, 1.18f, layerPosition);

                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(PrimaryColorId, layerPrimary);
                propertyBlock.SetColor(SecondaryColorId, layerSecondary);
                propertyBlock.SetFloat(EmissionId, layerEmission);
                propertyBlock.SetFloat(SharpnessId, sharpness);
                propertyBlock.SetFloat(AgeId, age01);
                propertyBlock.SetFloat(LayerAlphaId, layerAlpha);
                propertyBlock.SetFloat(SeedId, randomSeed * 0.0001f);
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

        private static void SimulateParticle(ParticleSystem system, float timeSeconds)
        {
            if (system == null)
            {
                return;
            }
            system.Simulate(Mathf.Max(0f, timeSeconds), true, true, true);
        }
    }
}
