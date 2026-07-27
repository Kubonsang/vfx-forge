using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

namespace Kubonsang.VfxForge.Editor
{
    public sealed class MissingAssetRule : IVfxValidationRule
    {
        public string RuleId => "VAL-001";

        public VfxValidationResult Evaluate(VfxValidationContext context)
        {
            if (context?.Prefab == null)
            {
                return VfxValidationResult.Error(RuleId, "Prefab is null.");
            }

            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(context.Prefab) > 0)
            {
                return VfxValidationResult.Error(RuleId, "Missing MonoBehaviour script detected.");
            }

            VisualEffect[] effects = context.Prefab.GetComponentsInChildren<VisualEffect>(true);
            if (effects.Length == 0)
            {
                return VfxValidationResult.Error(RuleId, "No VisualEffect component exists.");
            }

            foreach (VisualEffect effect in effects)
            {
                if (effect.visualEffectAsset == null)
                {
                    return VfxValidationResult.Error(RuleId, $"VisualEffect asset is missing on {effect.name}.");
                }
            }

            foreach (Renderer renderer in context.Prefab.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        return VfxValidationResult.Error(RuleId, $"Missing material detected on {renderer.name}.");
                    }
                }
            }

            return VfxValidationResult.Pass(RuleId, "No required assets are missing.");
        }
    }
}
