using UnityEngine;

namespace Kubonsang.VfxForge
{
    public enum VfxRecipeBindingValueType
    {
        Float,
        Int,
        Bool,
        Vector2,
        Vector3,
        Vector4,
        Color,
        String
    }

    public readonly struct VfxRecipeBindingValue
    {
        public readonly VfxRecipeBindingValueType Type;
        public readonly object Value;

        public VfxRecipeBindingValue(VfxRecipeBindingValueType type, object value)
        {
            Type = type;
            Value = value;
        }

        public bool TryGetFloat(out float value)
        {
            if (Type == VfxRecipeBindingValueType.Float && Value is float typed)
            {
                value = typed;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryGetVector3(out Vector3 value)
        {
            if (Type == VfxRecipeBindingValueType.Vector3 && Value is Vector3 typed)
            {
                value = typed;
                return true;
            }

            value = default;
            return false;
        }
    }

    public interface IVfxRecipeBindingAdapter
    {
        string BindingAdapterId { get; }

        bool SupportsBinding(
            string propertyName,
            VfxRecipeBindingValueType valueType);

        bool TryApplyBinding(
            string propertyName,
            VfxRecipeBindingValue value);
    }
}
