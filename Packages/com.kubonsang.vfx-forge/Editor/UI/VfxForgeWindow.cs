using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Kubonsang.VfxForge.Editor
{
    public sealed class VfxForgeWindow : EditorWindow
    {
        private TextAsset recipeAsset;
        private VfxTemplateCatalog templateCatalog;
        private string artifactDirectory = "Artifacts/VFXForge/manual-run";
        private Vector2 scroll;
        private readonly List<VfxValidationResult> results = new List<VfxValidationResult>();
        private GameObject generatedPrefab;
        private string previewPlayEventName = "OnPlay";
        private VfxPreviewSession previewSession;

        [MenuItem("Tools/VFX Forge/Open Window")]
        public static void Open()
        {
            GetWindow<VfxForgeWindow>("VFX Forge");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("VFX Forge", EditorStyles.boldLabel);
            recipeAsset = (TextAsset)EditorGUILayout.ObjectField("Recipe JSON", recipeAsset, typeof(TextAsset), false);
            templateCatalog = (VfxTemplateCatalog)EditorGUILayout.ObjectField("Template Catalog", templateCatalog, typeof(VfxTemplateCatalog), false);
            artifactDirectory = EditorGUILayout.TextField("Artifact Directory", artifactDirectory);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate Recipe"))
                {
                    ValidateRecipe();
                }

                if (GUILayout.Button("Compile"))
                {
                    CompileRecipe();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            generatedPrefab = (GameObject)EditorGUILayout.ObjectField(
                "Generated Prefab",
                generatedPrefab,
                typeof(GameObject),
                false);
            previewPlayEventName =
                EditorGUILayout.TextField("Play Event", previewPlayEventName);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Preview"))
                {
                    OpenPreview();
                }

                using (new EditorGUI.DisabledScope(previewSession == null))
                {
                    if (GUILayout.Button("Restart"))
                    {
                        previewSession.Restart();
                    }

                    if (GUILayout.Button("Close Preview"))
                    {
                        ClosePreview();
                    }
                }
            }

            EditorGUILayout.Space();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (VfxValidationResult result in results)
            {
                MessageType type = MessageType.Info;
                if (result.severity == VfxValidationSeverity.Error)
                {
                    type = MessageType.Error;
                }
                else if (result.severity == VfxValidationSeverity.Warning)
                {
                    type = MessageType.Warning;
                }
                EditorGUILayout.HelpBox($"[{result.ruleId}] {result.message}", type);
            }
            EditorGUILayout.EndScrollView();
        }

        private void OnDisable()
        {
            ClosePreview();
        }

        private void ValidateRecipe()
        {
            results.Clear();
            VfxRecipeParseResult parsed = ParseSelected();
            if (!parsed.Success)
            {
                results.Add(VfxValidationResult.Error(ParseErrorCode(parsed), parsed.Error));
                return;
            }

            results.AddRange(VfxRecipeValidator.Validate(parsed.Recipe));
        }

        private void CompileRecipe()
        {
            results.Clear();
            VfxRecipeParseResult parsed = ParseSelected();
            if (!parsed.Success)
            {
                results.Add(VfxValidationResult.Error(ParseErrorCode(parsed), parsed.Error));
                return;
            }

            results.AddRange(VfxRecipeValidator.Validate(parsed.Recipe));
            if (VfxRecipeValidator.HasErrors(results))
            {
                return;
            }

            if (templateCatalog == null)
            {
                results.Add(VfxValidationResult.Error("UI-CATALOG", "Template Catalog is required."));
                return;
            }

            string recipePath = AssetDatabase.GetAssetPath(recipeAsset);
            results.Clear();
            VfxCompileResult compile = VfxRecipeCompiler.Compile(parsed.Recipe, recipePath, templateCatalog);
            results.AddRange(compile.Results);
            if (compile.Success
                && templateCatalog.TryGet(parsed.Recipe.template, out VfxTemplateEntry template))
            {
                generatedPrefab = compile.Prefab;
                previewPlayEventName = template.playEventName;
                results.AddRange(VfxValidationPipeline.Run(new VfxValidationContext
                {
                    Recipe = parsed.Recipe,
                    Prefab = compile.Prefab,
                    Template = template,
                    AssetPath = compile.PrefabPath
                }));
            }

            string reportPath = VfxReportWriter.Write(artifactDirectory, parsed.Recipe, compile.PrefabPath, results);
            Debug.Log($"[VFXForge] Report written: {Path.GetFullPath(reportPath)}");
        }

        private void OpenPreview()
        {
            ClosePreview();
            VfxPreviewOpenResult open =
                VfxPreviewSession.Open(generatedPrefab, previewPlayEventName);
            if (!open.Success)
            {
                results.Add(VfxValidationResult.Error(open.ErrorCode, open.Message));
                return;
            }

            previewSession = open.Session;
            results.Add(VfxValidationResult.Pass("PREVIEW-PLAYBACK", open.Message));
        }

        private void ClosePreview()
        {
            previewSession?.Dispose();
            previewSession = null;
        }

        private VfxRecipeParseResult ParseSelected()
        {
            return recipeAsset == null
                ? new VfxRecipeParseResult
                {
                    Success = false,
                    ErrorCode = "UI-RECIPE-NOT-SELECTED",
                    Error = "Recipe JSON is not selected."
                }
                : VfxRecipeParser.ParseJson(recipeAsset.text);
        }

        private static string ParseErrorCode(VfxRecipeParseResult result)
        {
            return string.IsNullOrWhiteSpace(result.ErrorCode)
                ? "UI-PARSE"
                : result.ErrorCode;
        }
    }
}
