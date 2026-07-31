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
        private VfxForgePipelineRunResult lastRun;
        private VfxForgePipelineStage pipelineStage = VfxForgePipelineStage.Idle;
        private float pipelineProgress;
        private string pipelineMessage = "Ready.";
        private bool pipelineRunning;
        private string visualReviewer = string.Empty;
        private string visualRejectionReason = string.Empty;
        private VfxVisualReviewCriteria visualCriteria =
            new VfxVisualReviewCriteria();
        private VfxVisualReviewRecord expectedVisualReview;

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

            using (new EditorGUI.DisabledScope(pipelineRunning))
            {
                if (GUILayout.Button("Run All", GUILayout.Height(28f)))
                {
                    RunAll();
                }

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
            }

            Rect progressRect = GUILayoutUtility.GetRect(
                1f,
                18f,
                GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(
                progressRect,
                pipelineProgress,
                $"{pipelineStage}: {pipelineMessage}");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            generatedPrefab = (GameObject)EditorGUILayout.ObjectField(
                "Generated Prefab",
                generatedPrefab,
                typeof(GameObject),
                false);
            previewPlayEventName =
                EditorGUILayout.TextField("Play Event", previewPlayEventName);

            using (new EditorGUI.DisabledScope(pipelineRunning))
            {
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

                        if (GUILayout.Button("Capture Frames"))
                        {
                            CaptureFrames();
                        }

                        if (GUILayout.Button("Close Preview"))
                        {
                            ClosePreview();
                        }
                    }
                }
            }

            DrawResultNavigation();
            DrawVisualReview();

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
            EditorUtility.ClearProgressBar();
        }

        private void RunAll()
        {
            ClosePreview();
            results.Clear();
            pipelineRunning = true;
            pipelineProgress = 0f;
            pipelineMessage = "Starting.";

            try
            {
                var request = new VfxForgePipelineRequest
                {
                    RecipeJson = recipeAsset != null ? recipeAsset.text : string.Empty,
                    RecipeAssetPath =
                        recipeAsset != null ? AssetDatabase.GetAssetPath(recipeAsset) : string.Empty,
                    TemplateCatalog = templateCatalog,
                    ArtifactDirectory = artifactDirectory
                };
                var runner = new VfxForgePipelineRunner();
                lastRun = runner.Run(request, UpdatePipelineProgress);

                results.AddRange(lastRun.Results);
                generatedPrefab = lastRun.Prefab;
                if (lastRun.Recipe != null
                    && templateCatalog != null
                    && templateCatalog.TryGet(
                        lastRun.Recipe.template,
                        out VfxTemplateEntry template))
                {
                    previewPlayEventName = template.playEventName;
                }
                PrepareVisualReviewControls();
            }
            finally
            {
                pipelineRunning = false;
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }

        private void UpdatePipelineProgress(VfxForgePipelineProgress progress)
        {
            pipelineStage = progress.Stage;
            pipelineProgress = progress.NormalizedProgress;
            pipelineMessage = progress.Message;
            EditorUtility.DisplayProgressBar(
                "VFX Forge — Run All",
                progress.Message,
                progress.NormalizedProgress);
            Repaint();
        }

        private void DrawResultNavigation()
        {
            if (lastRun == null)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Last Run", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(lastRun.Prefab == null))
                {
                    if (GUILayout.Button("Select Prefab"))
                    {
                        Selection.activeObject = lastRun.Prefab;
                        EditorGUIUtility.PingObject(lastRun.Prefab);
                    }
                }

                using (new EditorGUI.DisabledScope(
                    string.IsNullOrWhiteSpace(lastRun.ReportPath)))
                {
                    if (GUILayout.Button("Reveal Report"))
                    {
                        RevealResult(lastRun.ReportPath, "UI-REPORT-PATH");
                    }
                }

                using (new EditorGUI.DisabledScope(
                    string.IsNullOrWhiteSpace(lastRun.CaptureManifestPath)))
                {
                    if (GUILayout.Button("Reveal Capture"))
                    {
                        RevealResult(
                            lastRun.CaptureManifestPath,
                            "UI-CAPTURE-PATH");
                    }
                }

                using (new EditorGUI.DisabledScope(
                    string.IsNullOrWhiteSpace(lastRun.ContactSheetPath)))
                {
                    if (GUILayout.Button("Open Contact Sheet"))
                    {
                        OpenResult(
                            lastRun.ContactSheetPath,
                            "UI-CONTACT-SHEET-PATH");
                    }
                }
            }
        }

        private void DrawVisualReview()
        {
            if (lastRun?.Recipe?.quality?.requireHumanReview != true)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Human Visual Review",
                EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Status",
                string.IsNullOrWhiteSpace(lastRun.ProductStatus)
                    ? VfxVisualReviewStatus.ReviewRequired
                    : lastRun.ProductStatus);
            visualReviewer =
                EditorGUILayout.TextField(
                    "Reviewer",
                    visualReviewer);
            visualCriteria.meaningClear =
                EditorGUILayout.Toggle(
                    "Meaning delivery",
                    visualCriteria.meaningClear);
            visualCriteria.silhouetteClear =
                EditorGUILayout.Toggle(
                    "Silhouette",
                    visualCriteria.silhouetteClear);
            visualCriteria.shaderPatternFinish =
                EditorGUILayout.Toggle(
                    "Shader / pattern finish",
                    visualCriteria.shaderPatternFinish);
            visualCriteria.timingPolish =
                EditorGUILayout.Toggle(
                    "Timing",
                    visualCriteria.timingPolish);
            visualCriteria.gameplayReadability =
                EditorGUILayout.Toggle(
                    "Gameplay readability",
                    visualCriteria.gameplayReadability);
            visualRejectionReason =
                EditorGUILayout.TextField(
                    "Rejection reason",
                    visualRejectionReason);

            using (new EditorGUI.DisabledScope(
                expectedVisualReview == null))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Accept"))
                    {
                        SubmitVisualReview(true);
                    }
                    if (GUILayout.Button("Reject"))
                    {
                        SubmitVisualReview(false);
                    }
                }
            }
        }

        private void RevealResult(string path, string errorCode)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                results.Add(VfxValidationResult.Error(
                    errorCode,
                    $"Result file does not exist: {path}"));
                return;
            }

            EditorUtility.RevealInFinder(Path.GetFullPath(path));
        }

        private void OpenResult(string path, string errorCode)
        {
            if (string.IsNullOrWhiteSpace(path)
                || !File.Exists(path))
            {
                results.Add(VfxValidationResult.Error(
                    errorCode,
                    $"Result file does not exist: {path}"));
                return;
            }

            EditorUtility.OpenWithDefaultApp(
                Path.GetFullPath(path));
        }

        private void PrepareVisualReviewControls()
        {
            expectedVisualReview = null;
            visualCriteria =
                new VfxVisualReviewCriteria();
            visualRejectionReason = string.Empty;
            if (lastRun?.Recipe?.quality?.requireHumanReview != true
                || string.IsNullOrWhiteSpace(
                    lastRun.ContactSheetPath))
            {
                return;
            }

            try
            {
                expectedVisualReview =
                    VfxVisualReviewStore.CreateExpected(
                        lastRun.PrefabPath,
                        lastRun.CaptureManifestPath,
                        lastRun.ContactSheetPath);
            }
            catch (System.Exception exception)
            {
                results.Add(VfxValidationResult.Error(
                    "UI-VISUAL-REVIEW",
                    exception.Message));
            }
        }

        private void SubmitVisualReview(bool accept)
        {
            VfxVisualReviewWriteResult written =
                VfxVisualReviewStore.Submit(
                    lastRun.VisualReviewPath,
                    expectedVisualReview,
                    visualReviewer,
                    accept,
                    visualCriteria,
                    visualRejectionReason);
            if (!written.Success)
            {
                results.Add(VfxValidationResult.Error(
                    written.ErrorCode,
                    written.Message));
                return;
            }

            lastRun.ProductStatus =
                written.Record.status;
            results.Add(VfxValidationResult.Pass(
                "VISUAL-REVIEW-WRITE",
                written.Message));
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

        private void CaptureFrames()
        {
            VfxRecipeParseResult parsed = ParseSelected();
            if (!parsed.Success)
            {
                results.Add(VfxValidationResult.Error(ParseErrorCode(parsed), parsed.Error));
                return;
            }

            string captureDirectory = Path.Combine(artifactDirectory, "capture");
            VfxFrameCaptureResult capture =
                VfxFrameCapture.Capture(previewSession, parsed.Recipe, captureDirectory);
            if (!capture.Success)
            {
                results.Add(VfxValidationResult.Error(capture.ErrorCode, capture.Message));
                return;
            }

            results.Add(VfxValidationResult.Pass(
                "CAPTURE-WRITE",
                $"Capture manifest written: {Path.GetFullPath(capture.ManifestPath)}"));
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
