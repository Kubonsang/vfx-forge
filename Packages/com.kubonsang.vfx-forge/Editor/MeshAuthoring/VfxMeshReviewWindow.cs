using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Kubonsang.VfxForge.Editor
{
    public sealed class VfxMeshReviewWindow : EditorWindow
    {
        private string manifestPath = string.Empty;
        private string reviewOutputPath = string.Empty;
        private string captureDirectory =
            "Dogfooding/Evidence/VFX-Mesh-Review";
        private VfxMeshReferenceManifest manifest;
        private Texture2D modelSheet;
        private int selectedView;
        private float referenceOpacity = 0.65f;
        private bool showReference = true;
        private string reviewer = string.Empty;
        private string rejectionReason = string.Empty;
        private readonly VfxMeshReviewCriteria criteria =
            new VfxMeshReviewCriteria();
        private Vector2 scroll;

        [MenuItem("Tools/VFX Forge/Vfx Mesh Review")]
        public static void Open()
        {
            GetWindow<VfxMeshReviewWindow>("Vfx Mesh Review");
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField(
                "Concept-to-Mesh Review",
                EditorStyles.boldLabel);
            manifestPath = EditorGUILayout.TextField(
                "Reference manifest",
                manifestPath);
            if (GUILayout.Button("Load Reference Manifest"))
            {
                LoadManifest();
            }

            if (manifest != null)
            {
                DrawReferenceControls();
                DrawReferenceImage();
                DrawCaptureControls();
                DrawReviewControls();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawReferenceControls()
        {
            string[] viewNames = Array.ConvertAll(
                manifest.views,
                view => view.id);
            selectedView = EditorGUILayout.Popup(
                "Reference view",
                Mathf.Clamp(selectedView, 0, viewNames.Length - 1),
                viewNames);
            showReference = EditorGUILayout.Toggle(
                "Show reference",
                showReference);
            referenceOpacity = EditorGUILayout.Slider(
                "Reference opacity",
                referenceOpacity,
                0f,
                1f);
            if (GUILayout.Button("Lock SceneView Camera"))
            {
                LockSceneViewCamera(manifest.views[selectedView]);
            }
        }

        private void DrawReferenceImage()
        {
            if (!showReference || modelSheet == null)
            {
                return;
            }
            VfxMeshReferenceView view = manifest.views[selectedView];
            float aspect = view.normalizedImageRect.width
                * modelSheet.width
                / (view.normalizedImageRect.height * modelSheet.height);
            Rect destination = GUILayoutUtility.GetAspectRect(
                Mathf.Max(0.1f, aspect));
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, referenceOpacity);
            GUI.DrawTextureWithTexCoords(
                destination,
                modelSheet,
                view.normalizedImageRect.ToRect(),
                true);
            GUI.color = previous;
        }

        private void DrawCaptureControls()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Review captures", EditorStyles.boldLabel);
            captureDirectory = EditorGUILayout.TextField(
                "Output directory",
                captureDirectory);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clay"))
            {
                Capture(VfxMeshCaptureMode.Clay);
            }
            if (GUILayout.Button("Wireframe"))
            {
                Capture(VfxMeshCaptureMode.Wireframe);
            }
            if (GUILayout.Button("Normals"))
            {
                Capture(VfxMeshCaptureMode.Normals);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawReviewControls()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Human review", EditorStyles.boldLabel);
            reviewer = EditorGUILayout.TextField("Reviewer", reviewer);
            criteria.visibleShapeFidelity = EditorGUILayout.Toggle(
                "Visible shape fidelity",
                criteria.visibleShapeFidelity);
            criteria.structuralFrameReadability = EditorGUILayout.Toggle(
                "Structural frame readability",
                criteria.structuralFrameReadability);
            criteria.connectedAnchors = EditorGUILayout.Toggle(
                "Connected anchors",
                criteria.connectedAnchors);
            criteria.depthConsistency = EditorGUILayout.Toggle(
                "Depth consistency",
                criteria.depthConsistency);
            criteria.gameplayReadability = EditorGUILayout.Toggle(
                "Gameplay readability",
                criteria.gameplayReadability);
            rejectionReason = EditorGUILayout.TextField(
                "Rejection reason",
                rejectionReason);
            reviewOutputPath = EditorGUILayout.TextField(
                "Review output",
                reviewOutputPath);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = criteria.AllPassed()
                && !string.IsNullOrWhiteSpace(reviewer);
            if (GUILayout.Button("Accept"))
            {
                SaveReview(true);
            }
            GUI.enabled = !string.IsNullOrWhiteSpace(reviewer)
                && !string.IsNullOrWhiteSpace(rejectionReason);
            if (GUILayout.Button("Reject"))
            {
                SaveReview(false);
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void LoadManifest()
        {
            string absoluteManifest = ResolveRepositoryPath(manifestPath);
            if (!File.Exists(absoluteManifest))
            {
                throw new FileNotFoundException(
                    "Mesh reference manifest was not found.",
                    absoluteManifest);
            }
            VfxMeshReferenceManifest loaded = JsonUtility.FromJson<
                VfxMeshReferenceManifest>(File.ReadAllText(absoluteManifest));
            VfxMeshContractValidation validation =
                VfxMeshContractValidator.Validate(loaded);
            if (!validation.Valid)
            {
                throw new InvalidDataException(
                    "Mesh reference manifest failed validation.");
            }

            string absoluteSheet = ResolveRepositoryPath(loaded.modelSheetPath);
            byte[] bytes = File.ReadAllBytes(absoluteSheet);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes))
            {
                DestroyImmediate(texture);
                throw new InvalidDataException("Model sheet image is invalid.");
            }
            texture.name = Path.GetFileNameWithoutExtension(absoluteSheet);
            if (modelSheet != null)
            {
                DestroyImmediate(modelSheet);
            }
            modelSheet = texture;
            manifest = loaded;
            selectedView = 0;
            Repaint();
        }

        private static void LockSceneViewCamera(VfxMeshReferenceView view)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                throw new InvalidOperationException("No active SceneView exists.");
            }
            bool orthographic = view.projection == "orthographic";
            float focusDistance = Vector3.Distance(view.position, view.target);
            sceneView.LookAtDirect(
                view.target,
                Quaternion.Euler(view.rotationEuler),
                orthographic ? view.orthographicSize : focusDistance);
            sceneView.orthographic = orthographic;
            if (!orthographic)
            {
                sceneView.camera.fieldOfView = view.fieldOfView;
            }
            sceneView.Repaint();
        }

        private void Capture(VfxMeshCaptureMode mode)
        {
            VfxMeshReferenceView view = manifest.views[selectedView];
            LockSceneViewCamera(view);
            string directory = ResolveRepositoryPath(captureDirectory);
            Directory.CreateDirectory(directory);
            string path = Path.Combine(
                directory,
                view.id + "-" + mode.ToString().ToLowerInvariant() + ".png");
            VfxMeshReviewCapture.CaptureSceneView(path, mode, 1280, 720);
            AssetDatabase.Refresh();
        }

        private void SaveReview(bool accept)
        {
            string path = ResolveRepositoryPath(reviewOutputPath);
            if (File.Exists(path))
            {
                throw new IOException($"Refusing to overwrite review: {path}");
            }
            string inputHash = VfxMeshReviewStore.ComputeCombinedSha256(
                manifest.candidateBoardSha256,
                manifest.modelSheetSha256);
            var record = new VfxMeshReviewRecord
            {
                taskId = manifest.taskId,
                stage = VfxMeshReviewStage.ModelSheet,
                status = accept
                    ? VfxMeshReviewStatus.Accepted
                    : VfxMeshReviewStatus.Rejected,
                inputSha256 = inputHash,
                reviewer = reviewer,
                reviewTimeUtc = DateTime.UtcNow.ToString("O"),
                accepted = accept,
                criteria = criteria,
                rejectionReason = accept ? string.Empty : rejectionReason
            };
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(record, true) + "\n");
            AssetDatabase.Refresh();
        }

        private static string ResolveRepositoryPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path))
            {
                throw new ArgumentException("A repository-relative path is required.");
            }
            string repository = Directory.GetParent(Application.dataPath)
                .Parent.FullName;
            string resolved = Path.GetFullPath(Path.Combine(repository, path));
            string root = Path.GetFullPath(repository)
                + Path.DirectorySeparatorChar;
            if (!resolved.StartsWith(root, StringComparison.Ordinal))
            {
                throw new ArgumentException("Path escapes the repository.");
            }
            return resolved;
        }
    }

    public enum VfxMeshCaptureMode
    {
        Clay,
        Wireframe,
        Normals
    }

    public static class VfxMeshReviewCapture
    {
        public static void CaptureSceneView(
            string path,
            VfxMeshCaptureMode mode,
            int width,
            int height)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                throw new InvalidOperationException("No active SceneView exists.");
            }
            if (File.Exists(path))
            {
                throw new IOException($"Refusing to overwrite capture: {path}");
            }

            var previousMaterials = new Dictionary<Renderer, Material[]>();
            Material replacement = null;
            bool previousWireframe = GL.wireframe;
            RenderTexture previousTarget = sceneView.camera.targetTexture;
            RenderTexture target = null;
            try
            {
                if (mode != VfxMeshCaptureMode.Wireframe)
                {
                    string shaderName = mode == VfxMeshCaptureMode.Normals
                        ? "Hidden/VFXForge/MeshReviewNormals"
                        : "Hidden/VFXForge/MeshReviewClay";
                    Shader shader = Shader.Find(shaderName);
                    if (shader == null)
                    {
                        throw new InvalidOperationException(
                            $"Review Shader is missing: {shaderName}");
                    }
                    replacement = new Material(shader);
                    foreach (Renderer renderer in UnityEngine.Object
                        .FindObjectsByType<Renderer>(
                            FindObjectsInactive.Include,
                            FindObjectsSortMode.None))
                    {
                        if (sceneView.camera.scene.IsValid()
                            && renderer.gameObject.scene
                                != sceneView.camera.scene)
                        {
                            continue;
                        }
                        previousMaterials[renderer] = renderer.sharedMaterials;
                        var materials = new Material[
                            Mathf.Max(1, renderer.sharedMaterials.Length)];
                        for (int index = 0; index < materials.Length; index++)
                        {
                            materials[index] = replacement;
                        }
                        renderer.sharedMaterials = materials;
                    }
                }
                GL.wireframe = mode == VfxMeshCaptureMode.Wireframe;
                target = new RenderTexture(width, height, 24);
                sceneView.camera.targetTexture = target;
                sceneView.camera.Render();
                RenderTexture previousActive = RenderTexture.active;
                RenderTexture.active = target;
                var texture = new Texture2D(
                    width,
                    height,
                    TextureFormat.RGBA32,
                    false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                RenderTexture.active = previousActive;
            }
            finally
            {
                GL.wireframe = previousWireframe;
                sceneView.camera.targetTexture = previousTarget;
                foreach (KeyValuePair<Renderer, Material[]> pair in previousMaterials)
                {
                    if (pair.Key != null)
                    {
                        pair.Key.sharedMaterials = pair.Value;
                    }
                }
                if (replacement != null)
                {
                    UnityEngine.Object.DestroyImmediate(replacement);
                }
                if (target != null)
                {
                    UnityEngine.Object.DestroyImmediate(target);
                }
            }
        }
    }
}
