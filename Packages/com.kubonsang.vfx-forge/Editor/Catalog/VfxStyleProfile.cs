using UnityEngine;

namespace Kubonsang.VfxForge.Editor
{
    [CreateAssetMenu(menuName = "VFX Forge/Style Profile", fileName = "VfxStyleProfile")]
    public sealed class VfxStyleProfile : ScriptableObject
    {
        public string id = "default";
        public float maxDuration = 1f;
        public int maxParticles = 500;
        public float maxRadius = 5f;
        public int maxLayerCount = 6;
        public bool allowLight;
        public bool allowDistortion;
    }
}
