using UnityEngine;

namespace VfxForge.Dogfood
{
    [DisallowMultipleComponent]
    public sealed class CavalierWallFacing : MonoBehaviour
    {
        [SerializeField] private Transform facingSource;
        [SerializeField] private Transform facingPivot;
        [SerializeField] private bool followSourceEveryFrame = true;
        [SerializeField] private Vector3 fallbackDirection =
            Vector3.forward;

        private bool hasAimOverride;
        private Vector3 aimDirection = Vector3.forward;

        public Transform FacingSource => facingSource;
        public Transform FacingPivot => facingPivot;
        public bool HasAimOverride => hasAimOverride;
        public Vector3 CurrentForward => facingPivot != null
            ? facingPivot.forward
            : Vector3.forward;

        public void Configure(
            Transform source,
            Transform pivot,
            bool followEveryFrame = true)
        {
            facingSource = source;
            facingPivot = pivot;
            followSourceEveryFrame = followEveryFrame;
            EvaluateFacing();
        }

        public bool TrySetAimDirection(Vector3 worldDirection)
        {
            Vector3 horizontal = Flatten(worldDirection);
            if (horizontal.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            aimDirection = horizontal.normalized;
            hasAimOverride = true;
            EvaluateFacing();
            return true;
        }

        public void ClearAimOverride()
        {
            hasAimOverride = false;
            EvaluateFacing();
        }

        public void EvaluateFacing()
        {
            if (facingPivot == null)
            {
                return;
            }

            Vector3 direction = ResolveDirection();
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector3.forward;
            }

            facingPivot.rotation = Quaternion.LookRotation(
                direction.normalized,
                Vector3.up);
        }

        private void OnEnable()
        {
            EvaluateFacing();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying
                || (!followSourceEveryFrame && !hasAimOverride))
            {
                return;
            }

            EvaluateFacing();
        }

        private Vector3 ResolveDirection()
        {
            if (hasAimOverride)
            {
                return Flatten(aimDirection);
            }

            if (facingSource != null)
            {
                return Flatten(facingSource.forward);
            }

            return Flatten(fallbackDirection);
        }

        private static Vector3 Flatten(Vector3 direction)
        {
            direction.y = 0f;
            return direction;
        }
    }
}
