using System;

namespace Kubonsang.VfxForge.Editor
{
    public enum VfxPropertyType
    {
        Float,
        Int,
        Bool,
        Vector2,
        Vector3,
        Vector4,
        Color
    }

    [Serializable]
    public sealed class VfxPropertyBinding
    {
        public string recipePath = string.Empty;
        public string exposedPropertyName = string.Empty;
        public VfxPropertyType propertyType = VfxPropertyType.Float;
        public bool required = true;
        public int componentIndex = -1;
    }
}
