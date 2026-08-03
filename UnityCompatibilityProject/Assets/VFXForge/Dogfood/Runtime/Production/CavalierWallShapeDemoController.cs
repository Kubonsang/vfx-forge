using UnityEngine;

namespace VfxForge.Dogfood
{
    [DisallowMultipleComponent]
    public sealed class CavalierWallShapeDemoController
        : MonoBehaviour
    {
        [SerializeField] private Transform caster;
        [SerializeField] private Transform target;
        [SerializeField] private CavalierWallFacing wall;
        [SerializeField] private float targetDistance = 7f;
        [SerializeField] private float sweepDegrees = 34f;
        [SerializeField] private float sweepSpeed = 0.55f;

        public void Configure(
            Transform casterTransform,
            Transform targetTransform,
            CavalierWallFacing facing)
        {
            caster = casterTransform;
            target = targetTransform;
            wall = facing;
        }

        private void Start()
        {
            if (wall != null)
            {
                wall.Configure(caster, wall.FacingPivot);
            }
        }

        private void Update()
        {
            if (caster == null || target == null || wall == null)
            {
                return;
            }

            float yaw = Mathf.Sin(Time.time * sweepSpeed)
                * sweepDegrees;
            Vector3 direction = Quaternion.Euler(0f, yaw, 0f)
                * Vector3.forward;
            caster.rotation = Quaternion.LookRotation(
                direction,
                Vector3.up);
            target.position = caster.position
                + direction * targetDistance;
            wall.EvaluateFacing();
        }
    }
}
