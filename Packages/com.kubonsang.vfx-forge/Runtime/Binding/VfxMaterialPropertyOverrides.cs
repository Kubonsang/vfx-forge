using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kubonsang.VfxForge
{
    [Serializable]
    public sealed class VfxMaterialPropertyOverride
    {
        public int materialIndex;
        public string propertyName = string.Empty;
        public VfxRecipeBindingValueType valueType;
        public float floatValue;
        public int intValue;
        public Vector4 vectorValue;
        public Color colorValue = Color.white;
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Renderer))]
    public sealed class VfxMaterialPropertyOverrides : MonoBehaviour
    {
        [SerializeField] private List<VfxMaterialPropertyOverride> overrides =
            new List<VfxMaterialPropertyOverride>();

        public IReadOnlyList<VfxMaterialPropertyOverride> Overrides => overrides;

        public void Set(
            int materialIndex,
            string propertyName,
            VfxRecipeBindingValue value)
        {
            VfxMaterialPropertyOverride entry = Find(materialIndex, propertyName);
            if (entry == null)
            {
                entry = new VfxMaterialPropertyOverride
                {
                    materialIndex = materialIndex,
                    propertyName = propertyName
                };
                overrides.Add(entry);
            }

            entry.valueType = value.Type;
            switch (value.Type)
            {
                case VfxRecipeBindingValueType.Float:
                    entry.floatValue = (float)value.Value;
                    break;
                case VfxRecipeBindingValueType.Int:
                    entry.intValue = (int)value.Value;
                    break;
                case VfxRecipeBindingValueType.Vector2:
                    Vector2 vector2 = (Vector2)value.Value;
                    entry.vectorValue = new Vector4(vector2.x, vector2.y, 0f, 0f);
                    break;
                case VfxRecipeBindingValueType.Vector3:
                    Vector3 vector3 = (Vector3)value.Value;
                    entry.vectorValue = new Vector4(
                        vector3.x,
                        vector3.y,
                        vector3.z,
                        0f);
                    break;
                case VfxRecipeBindingValueType.Vector4:
                    entry.vectorValue = (Vector4)value.Value;
                    break;
                case VfxRecipeBindingValueType.Color:
                    entry.colorValue = (Color)value.Value;
                    break;
            }

            ApplyNow();
        }

        public void ApplyNow()
        {
            Renderer targetRenderer = GetComponent<Renderer>();
            if (targetRenderer == null)
            {
                return;
            }

            var block = new MaterialPropertyBlock();
            foreach (VfxMaterialPropertyOverride entry in overrides)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.propertyName))
                {
                    continue;
                }

                targetRenderer.GetPropertyBlock(block, entry.materialIndex);
                switch (entry.valueType)
                {
                    case VfxRecipeBindingValueType.Float:
                        block.SetFloat(entry.propertyName, entry.floatValue);
                        break;
                    case VfxRecipeBindingValueType.Int:
                        block.SetInt(entry.propertyName, entry.intValue);
                        break;
                    case VfxRecipeBindingValueType.Vector2:
                    case VfxRecipeBindingValueType.Vector3:
                    case VfxRecipeBindingValueType.Vector4:
                        block.SetVector(entry.propertyName, entry.vectorValue);
                        break;
                    case VfxRecipeBindingValueType.Color:
                        block.SetColor(entry.propertyName, entry.colorValue);
                        break;
                    default:
                        continue;
                }

                targetRenderer.SetPropertyBlock(block, entry.materialIndex);
                block.Clear();
            }
        }

        private void OnEnable()
        {
            ApplyNow();
        }

        private VfxMaterialPropertyOverride Find(
            int materialIndex,
            string propertyName)
        {
            foreach (VfxMaterialPropertyOverride entry in overrides)
            {
                if (entry != null
                    && entry.materialIndex == materialIndex
                    && entry.propertyName == propertyName)
                {
                    return entry;
                }
            }

            return null;
        }
    }
}
