using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Kubonsang.VfxForge.Editor
{
    [CustomEditor(typeof(VfxTemplateCatalog))]
    public sealed class VfxTemplateCatalogEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var catalog = (VfxTemplateCatalog)target;
            List<VfxValidationResult> results = VfxTemplateCatalogValidator.Validate(catalog);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Catalog Validation", EditorStyles.boldLabel);
            if (results.Count == 0)
            {
                EditorGUILayout.HelpBox("Catalog contract is valid.", MessageType.Info);
            }
            else
            {
                foreach (VfxValidationResult result in results)
                {
                    MessageType messageType = result.severity == VfxValidationSeverity.Error
                        ? MessageType.Error
                        : MessageType.Warning;
                    EditorGUILayout.HelpBox(
                        $"{result.ruleId}: {result.message}",
                        messageType);
                }
            }

            DrawBindingInspection(catalog);
        }

        private static void DrawBindingInspection(VfxTemplateCatalog catalog)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Binding Inspection", EditorStyles.boldLabel);
            if (catalog.templates == null || catalog.templates.Count == 0)
            {
                EditorGUILayout.LabelField("No registered templates.");
                return;
            }

            for (int templateIndex = 0; templateIndex < catalog.templates.Count; templateIndex++)
            {
                VfxTemplateEntry entry = catalog.templates[templateIndex];
                string templateId = entry?.id ?? "<null>";
                EditorGUILayout.LabelField($"[{templateIndex}] {templateId}", EditorStyles.boldLabel);
                if (entry?.bindings == null || entry.bindings.Count == 0)
                {
                    EditorGUILayout.LabelField("  No Property Bindings.");
                    continue;
                }

                foreach (VfxPropertyBinding binding in entry.bindings)
                {
                    if (binding == null)
                    {
                        EditorGUILayout.LabelField("  <null binding>");
                        continue;
                    }

                    string component = binding.targetKind
                        == VfxBindingTargetKind.VisualEffectProperty
                            ? binding.componentIndex < 0
                                ? "all components"
                                : $"component {binding.componentIndex}"
                            : string.IsNullOrEmpty(binding.targetPath)
                                ? "root"
                                : binding.targetPath;
                    EditorGUILayout.LabelField(
                        $"  {binding.recipePath} → {binding.exposedPropertyName}",
                        $"{binding.targetKind}, {binding.propertyType}, {component}, "
                        + $"{(binding.required ? "required" : "optional")}");
                }
            }
        }
    }
}
