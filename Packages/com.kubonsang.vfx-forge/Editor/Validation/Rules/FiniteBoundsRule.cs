using UnityEngine;

namespace Kubonsang.VfxForge.Editor
{
    public sealed class FiniteBoundsRule : IVfxValidationRule
    {
        public string RuleId => "VAL-004";

        public VfxValidationResult Evaluate(VfxValidationContext context)
        {
            if (context?.Prefab == null || context.Recipe?.budget == null)
            {
                return VfxValidationResult.Error(
                    RuleId,
                    "Generated Prefab and bounds budget are required.");
            }

            float maximumRadius = context.Recipe.budget.maxBoundsRadius;
            int checkedBounds = 0;
            foreach (Renderer renderer in
                context.Prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                {
                    continue;
                }

                checkedBounds++;
                if (!IsValid(renderer.bounds, Vector3.one, maximumRadius))
                {
                    return VfxValidationResult.Error(
                        RuleId,
                        $"Renderer bounds are non-finite or exceed radius budget: {renderer.name}.");
                }
            }

            foreach (MeshFilter filter in
                context.Prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter == null || filter.sharedMesh == null)
                {
                    continue;
                }

                checkedBounds++;
                if (!IsValid(
                    filter.sharedMesh.bounds,
                    filter.transform.lossyScale,
                    maximumRadius))
                {
                    return VfxValidationResult.Error(
                        RuleId,
                        $"Mesh bounds are non-finite or exceed radius budget: {filter.name}.");
                }
            }

            foreach (SkinnedMeshRenderer renderer in
                context.Prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer == null || renderer.sharedMesh == null)
                {
                    continue;
                }

                checkedBounds++;
                if (!IsValid(
                    renderer.localBounds,
                    renderer.transform.lossyScale,
                    maximumRadius))
                {
                    return VfxValidationResult.Error(
                        RuleId,
                        $"Skinned Mesh bounds are non-finite or exceed radius budget: {renderer.name}.");
                }
            }

            return VfxValidationResult.Pass(
                RuleId,
                $"Validated {checkedBounds} finite Renderer and Mesh bound(s).");
        }

        private static bool IsValid(
            Bounds bounds,
            Vector3 scale,
            float maximumRadius)
        {
            if (!IsFinite(bounds.center)
                || !IsFinite(bounds.extents)
                || !IsFinite(scale))
            {
                return false;
            }

            Vector3 scaled = Vector3.Scale(
                bounds.extents,
                new Vector3(
                    Mathf.Abs(scale.x),
                    Mathf.Abs(scale.y),
                    Mathf.Abs(scale.z)));
            return IsFinite(scaled)
                && scaled.magnitude <= maximumRadius;
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x)
                && IsFinite(value.y)
                && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
