using UnityEngine;

namespace Kubonsang.VfxForge
{
    [DisallowMultipleComponent]
    public sealed class VfxMotionBindingAdapter : MonoBehaviour, IVfxRecipeBindingAdapter
    {
        public const string AdapterId = "vfx-motion";

        [SerializeField] private float speed;
        [SerializeField] private Vector3 localDirection = Vector3.forward;

        public string BindingAdapterId => AdapterId;
        public float Speed => speed;
        public Vector3 LocalDirection => localDirection;

        public bool SupportsBinding(
            string propertyName,
            VfxRecipeBindingValueType valueType)
        {
            return propertyName == "speed"
                ? valueType == VfxRecipeBindingValueType.Float
                : propertyName == "localDirection"
                    && valueType == VfxRecipeBindingValueType.Vector3;
        }

        public bool TryApplyBinding(
            string propertyName,
            VfxRecipeBindingValue value)
        {
            if (propertyName == "speed" && value.TryGetFloat(out float nextSpeed))
            {
                speed = nextSpeed;
                return true;
            }

            if (propertyName == "localDirection"
                && value.TryGetVector3(out Vector3 nextDirection))
            {
                localDirection = nextDirection;
                return true;
            }

            return false;
        }
    }
}
