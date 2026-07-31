using UnityEngine;

namespace Kubonsang.VfxForge
{
    [DisallowMultipleComponent]
    public sealed class VfxReviewContext : MonoBehaviour
    {
        public Camera reviewCamera;
        public Transform effectAnchor;
        public Transform caster;
        public Transform target;
    }
}
