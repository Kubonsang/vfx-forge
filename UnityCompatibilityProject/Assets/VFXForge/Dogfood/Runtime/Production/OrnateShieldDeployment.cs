using Kubonsang.VfxForge;
using UnityEngine;
using UnityEngine.VFX;

namespace VfxForge.Dogfood
{
    [DisallowMultipleComponent]
    public sealed class OrnateShieldDeployment : MonoBehaviour
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
        private static readonly int OrnamentPhaseId =
            Shader.PropertyToID("_OrnamentPhase");
        private static readonly int SeedId =
            Shader.PropertyToID("_Seed");

        [SerializeField] private VisualEffect settingsSource;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform[] panelRoots =
            System.Array.Empty<Transform>();
        [SerializeField] private Renderer[] panelRenderers =
            System.Array.Empty<Renderer>();
        [SerializeField] private Transform[] ornamentRoots =
            System.Array.Empty<Transform>();
        [SerializeField] private Renderer[] ornamentRenderers =
            System.Array.Empty<Renderer>();

        private MaterialPropertyBlock propertyBlock;
        private Vector3[] ornamentBasePositions =
            System.Array.Empty<Vector3>();
        private float elapsed;
        private float duration = 2f;
        private float impactTime = 0.38f;
        private float sustainTime = 1.15f;
        private float decayTime = 0.47f;
        private float radius = 3.4f;
        private float spreadAngle = 135f;
        private float directionality = 1f;
        private float emission = 5.2f;
        private float sharpness = 0.84f;
        private int randomSeed = 140730;
        private Color primaryColor =
            new Color(0.094f, 0.875f, 1f, 1f);
        private Color secondaryColor =
            new Color(0.97f, 0.99f, 1f, 1f);

        public float Duration => duration;
        public float Radius => radius;
        public int OrnamentCount => ornamentRoots.Length;
        public bool IsDeployed => elapsed >= impactTime
            && elapsed < GetDecayStartSeconds();

        public void Configure(
            VisualEffect source,
            Transform root,
            Transform[] panels,
            Renderer[] panelsToRender,
            Transform[] ornaments,
            Renderer[] ornamentsToRender)
        {
            settingsSource = source;
            visualRoot = root;
            panelRoots = panels ?? System.Array.Empty<Transform>();
            panelRenderers =
                panelsToRender ?? System.Array.Empty<Renderer>();
            ornamentRoots =
                ornaments ?? System.Array.Empty<Transform>();
            ornamentRenderers =
                ornamentsToRender ?? System.Array.Empty<Renderer>();
            CacheOrnamentPositions();
        }

        private void OnEnable()
        {
            ReadRecipeOverrides();
            CacheOrnamentPositions();
            elapsed = 0f;
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
            ReadRecipeOverrides();
            CacheOrnamentPositions();
            elapsed = Mathf.Clamp(timeSeconds, 0f, duration);
            EvaluateVisuals(elapsed);
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
                60f,
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
                12f);
            sharpness = ReadFloat("Sharpness", sharpness, 0f, 1f);
            primaryColor = ReadColor("PrimaryColor", primaryColor);
            secondaryColor =
                ReadColor("SecondaryColor", secondaryColor);
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
            float decayStart01 = GetDecayStartSeconds()
                / Mathf.Max(0.01f, duration);
            float decay = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(decayStart01, 1f, age01));
            float radiusScale = radius / 3.4f;

            if (visualRoot != null)
            {
                visualRoot.localScale =
                    Vector3.one * (0.62f * radiusScale);
                visualRoot.localPosition = new Vector3(
                    0f,
                    0f,
                    Mathf.Lerp(0.08f, 0.42f, directionality));
            }

            EvaluatePanels(timeSeconds, age01, decay);
            EvaluateOrnaments(timeSeconds, age01, decay);
        }

        private void EvaluatePanels(
            float timeSeconds,
            float age01,
            float decay)
        {
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
                float delay = distance01 * impactTime * 0.34f;
                float deploy = SmoothDeploy(
                    timeSeconds,
                    delay,
                    impactTime);
                float edgeDecay = Mathf.Clamp01(
                    decay + distance01 * decay * 0.24f);
                float visibility = deploy
                    * (1f - edgeDecay);

                float angle = signedIndex
                    * (spreadAngle / Mathf.Max(1f, panelCount));
                panel.localRotation =
                    Quaternion.Euler(0f, angle, 0f);
                panel.localScale = new Vector3(
                    Mathf.Lerp(0.16f, 1f, deploy),
                    Mathf.Lerp(0.012f, 1f, deploy),
                    1f);
                ApplyProperties(
                    panelRenderers,
                    index * 2,
                    2,
                    age01,
                    visibility,
                    index / Mathf.Max(1f, panelCount - 1f));
            }
        }

        private void EvaluateOrnaments(
            float timeSeconds,
            float age01,
            float decay)
        {
            for (int index = 0; index < ornamentRoots.Length; index++)
            {
                Transform ornament = ornamentRoots[index];
                if (ornament == null)
                {
                    continue;
                }

                float delay = GetOrnamentDelay(index);
                float deploy = SmoothDeploy(
                    timeSeconds,
                    delay,
                    impactTime + 0.12f);
                float visibility = deploy
                    * (1f - Mathf.SmoothStep(0f, 1f, decay));
                float overshoot = 1f
                    + Mathf.Sin(deploy * Mathf.PI) * 0.08f;
                ornament.localScale =
                    Vector3.one * Mathf.Lerp(
                        0.015f,
                        overshoot,
                        deploy);

                if (index < ornamentBasePositions.Length)
                {
                    Vector3 basePosition =
                        ornamentBasePositions[index];
                    ornament.localPosition = basePosition
                        + Vector3.up
                        * Mathf.Sin(
                            timeSeconds * 4.2f + index * 0.9f)
                        * 0.025f
                        * deploy;
                }

                ApplyProperties(
                    ornamentRenderers,
                    index,
                    1,
                    age01,
                    visibility,
                    index / Mathf.Max(
                        1f,
                        ornamentRoots.Length - 1f));
            }
        }

        private void ApplyProperties(
            Renderer[] renderers,
            int start,
            int count,
            float age01,
            float visibility,
            float phase)
        {
            int end = Mathf.Min(renderers.Length, start + count);
            for (int index = start; index < end; index++)
            {
                Renderer renderer = renderers[index];
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(
                    PrimaryColorId,
                    primaryColor);
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
                    OrnamentPhaseId,
                    phase);
                propertyBlock.SetFloat(
                    SeedId,
                    randomSeed * 0.0001f);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private float GetDecayStartSeconds()
        {
            float budgetStart =
                Mathf.Max(0f, duration - decayTime);
            return Mathf.Clamp(
                Mathf.Max(impactTime + sustainTime, budgetStart),
                0f,
                duration);
        }

        private float GetOrnamentDelay(int index)
        {
            switch (index)
            {
                case 0:
                    return impactTime * 0.05f;
                case 1:
                case 2:
                    return impactTime * 0.22f;
                case 3:
                    return impactTime * 0.38f;
                default:
                    return impactTime * 0.54f;
            }
        }

        private static float SmoothDeploy(
            float timeSeconds,
            float delay,
            float totalImpact)
        {
            float available = Mathf.Max(0.04f, totalImpact - delay);
            float progress = Mathf.Clamp01(
                (timeSeconds - delay) / available);
            return Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Sqrt(progress));
        }

        private void CacheOrnamentPositions()
        {
            if (ornamentBasePositions.Length == ornamentRoots.Length)
            {
                return;
            }

            ornamentBasePositions =
                new Vector3[ornamentRoots.Length];
            for (int index = 0; index < ornamentRoots.Length; index++)
            {
                if (ornamentRoots[index] != null)
                {
                    ornamentBasePositions[index] =
                        ornamentRoots[index].localPosition;
                }
            }
        }
    }
}
