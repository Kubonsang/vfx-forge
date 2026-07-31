using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

namespace Kubonsang.VfxForge.Editor
{
    public sealed class ParticleBudgetRule : IVfxValidationRule
    {
        public string RuleId => "VAL-005";

        public VfxValidationResult Evaluate(VfxValidationContext context)
        {
            if (context?.Prefab == null || context.Recipe?.budget == null)
            {
                return VfxValidationResult.Error(
                    RuleId,
                    "Generated Prefab and particle budget are required.");
            }

            if (context.Recipe.schemaVersion == "1.0")
            {
                return VfxValidationResult.Pass(
                    RuleId,
                    "Recipe 1.0 preserves legacy declared-budget validation.");
            }

            int actualCapacity = 0;
            foreach (ParticleSystem system in
                context.Prefab.GetComponentsInChildren<ParticleSystem>(true))
            {
                actualCapacity += system.main.maxParticles;
            }

            foreach (VisualEffect effect in
                context.Prefab.GetComponentsInChildren<VisualEffect>(true))
            {
                if (effect == null || effect.visualEffectAsset == null)
                {
                    continue;
                }

                if (!VfxGraphCapacityReader.TryRead(
                    effect.visualEffectAsset,
                    out int graphCapacity,
                    out string error))
                {
                    return VfxValidationResult.Error(RuleId, error);
                }

                actualCapacity += graphCapacity;
            }

            int maximum = context.Recipe.budget.maxParticles;
            if (context.StyleProfile != null)
            {
                maximum = Mathf.Min(maximum, context.StyleProfile.maxParticles);
            }

            return actualCapacity <= maximum
                ? VfxValidationResult.Pass(
                    RuleId,
                    $"Actual particle capacity {actualCapacity} is within budget {maximum}.")
                : VfxValidationResult.Error(
                    RuleId,
                    $"Actual particle capacity {actualCapacity} exceeds budget {maximum}.");
        }
    }

    internal static class VfxGraphCapacityReader
    {
        public static bool TryRead(
            VisualEffectAsset asset,
            out int totalCapacity,
            out string error)
        {
            totalCapacity = 0;
            error = string.Empty;
            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrWhiteSpace(path))
            {
                error = "VisualEffect Graph Asset path is unavailable.";
                return false;
            }

            try
            {
                Type resourceType =
                    FindLoadedType("UnityEditor.VFX.VisualEffectResource");
                MethodInfo getResource = resourceType?
                    .GetMethods(
                        BindingFlags.Static
                            | BindingFlags.Public
                            | BindingFlags.NonPublic)
                    .FirstOrDefault(
                        method =>
                        {
                            ParameterInfo[] parameters =
                                method.GetParameters();
                            return method.Name
                                    == "GetResourceAtPath"
                                && parameters.Length == 1
                                && parameters[0].ParameterType
                                    == typeof(string);
                        });
                object resource = getResource?.Invoke(null, new object[] { path });
                if (resource == null)
                {
                    error =
                        $"VFX Graph resource could not be inspected: {path}.";
                    return false;
                }
                Type extensionType = FindLoadedType(
                    "UnityEditor.VFX.VisualEffectResourceExtensions");
                MethodInfo getGraph = extensionType?
                    .GetMethods(
                        BindingFlags.Static
                            | BindingFlags.Public
                            | BindingFlags.NonPublic)
                    .FirstOrDefault(
                        method =>
                            method.Name == "GetOrCreateGraph"
                            && method.GetParameters().Length == 1
                            && method.GetParameters()[0]
                                .ParameterType
                                .IsAssignableFrom(resource.GetType()));
                object graph = getGraph?.Invoke(null, new[] { resource });
                if (graph == null)
                {
                    error = $"VFX Graph could not be inspected: {path}.";
                    return false;
                }

                var pending = new Stack<object>();
                var visited = new HashSet<int>();
                pending.Push(graph);
                while (pending.Count > 0)
                {
                    object model = pending.Pop();
                    if (!(model is UnityEngine.Object graphObject)
                        || graphObject == null
                        || !visited.Add(graphObject.GetInstanceID()))
                    {
                        continue;
                    }

                    var serialized = new SerializedObject(graphObject);
                    SerializedProperty capacity =
                        serialized.FindProperty("capacity");
                    if (capacity != null
                        && capacity.propertyType
                            == SerializedPropertyType.Integer
                        && capacity.intValue > 0)
                    {
                        totalCapacity += capacity.intValue;
                    }

                    SerializedProperty data =
                        serialized.FindProperty("m_Data");
                    if (data != null
                        && data.propertyType
                            == SerializedPropertyType.ObjectReference
                        && data.objectReferenceValue != null)
                    {
                        pending.Push(data.objectReferenceValue);
                    }

                    PropertyInfo children = model.GetType()
                        .GetProperties(
                            BindingFlags.Instance
                                | BindingFlags.Public)
                        .FirstOrDefault(
                            property =>
                                property.Name == "children"
                                && property
                                    .GetIndexParameters()
                                    .Length == 0);
                    if (children?.GetValue(model) is IEnumerable childModels)
                    {
                        foreach (object child in childModels)
                        {
                            if (child != null)
                            {
                                pending.Push(child);
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                error = $"VFX Graph capacity inspection failed for {path}: {exception.Message}";
                return false;
            }
        }

        private static Type FindLoadedType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
