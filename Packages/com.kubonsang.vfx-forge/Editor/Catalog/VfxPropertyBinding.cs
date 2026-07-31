using System;

namespace Kubonsang.VfxForge.Editor
{
    public enum VfxBindingTargetKind
    {
        VisualEffectProperty,
        TransformProperty,
        MaterialProperty,
        MeshVariant,
        AdapterProperty
    }

    public enum VfxPropertyType
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

    [Serializable]
    public sealed class VfxPropertyBinding
    {
        public string recipePath = string.Empty;
        public string exposedPropertyName = string.Empty;
        public VfxPropertyType propertyType = VfxPropertyType.Float;
        public bool required = true;
        public int componentIndex = -1;
        public VfxBindingTargetKind targetKind =
            VfxBindingTargetKind.VisualEffectProperty;
        public string targetPath = string.Empty;
        public int materialIndex;
        public string adapterId = string.Empty;
    }
}
