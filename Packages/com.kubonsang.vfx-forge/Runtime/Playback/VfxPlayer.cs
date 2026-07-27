using UnityEngine;
using UnityEngine.VFX;

namespace Kubonsang.VfxForge
{
    [DisallowMultipleComponent]
    public sealed class VfxPlayer : MonoBehaviour
    {
        [SerializeField] private VisualEffect[] effects = System.Array.Empty<VisualEffect>();
        [SerializeField] private string playEventName = "OnPlay";

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
            EnsureEffects();
            foreach (VisualEffect effect in effects)
            {
                if (effect == null)
                {
                    continue;
                }

                effect.SendEvent(playEventName);
            }
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
