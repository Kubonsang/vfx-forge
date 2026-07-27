using UnityEngine;
using UnityEngine.VFX;

namespace Kubonsang.VfxForge
{
    [DisallowMultipleComponent]
    public sealed class VfxPlayer : MonoBehaviour
    {
        [SerializeField] private VisualEffect[] effects = System.Array.Empty<VisualEffect>();
        [SerializeField] private string playEventName = "OnPlay";

        public string PlayEventName => playEventName;

        public void Configure(string eventName)
        {
            playEventName = string.IsNullOrWhiteSpace(eventName) ? "OnPlay" : eventName.Trim();
            CacheEffects();
        }

        public void CacheEffects()
        {
            effects = GetComponentsInChildren<VisualEffect>(true);
        }

        public void StopAndReinitialize()
        {
            EnsureEffects();
            foreach (VisualEffect effect in effects)
            {
                if (effect == null)
                {
                    continue;
                }

                effect.Stop();
                effect.Reinit();
            }
        }

        public void Play()
        {
            PlayAll();
        }

        public int PlayAll()
        {
            EnsureEffects();
            int playedEffectCount = 0;
            foreach (VisualEffect effect in effects)
            {
                if (effect == null)
                {
                    continue;
                }

                effect.SendEvent(playEventName);
                playedEffectCount++;
            }

            return playedEffectCount;
        }

        private void EnsureEffects()
        {
            if (effects == null || effects.Length == 0)
            {
                CacheEffects();
            }
        }
    }
}
