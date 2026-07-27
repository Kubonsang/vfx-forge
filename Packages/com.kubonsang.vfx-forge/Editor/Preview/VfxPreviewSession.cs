using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;

namespace Kubonsang.VfxForge.Editor
{
    public sealed class VfxPreviewOpenResult
    {
        public bool Success;
        public string ErrorCode = string.Empty;
        public string Message = string.Empty;
        public VfxPreviewSession Session;
    }

    public sealed class VfxPreviewSession : IDisposable
    {
        public const string PreviewRootName = "VFX Forge Preview Root";
        public const string CameraRigName = "VFX Forge Camera Rig";
        public const string CameraName = "VFX Forge Preview Camera";

        private Scene previewScene;
        private GameObject previewRoot;
        private GameObject previewInstance;
        private Camera previewCamera;
        private VfxPlayer player;
        private bool disposed;

        private VfxPreviewSession()
        {
        }

        public Scene PreviewScene => previewScene;
        public GameObject PreviewInstance => previewInstance;
        public Camera PreviewCamera => previewCamera;
        public bool IsPlaying { get; private set; }
        public bool IsDisposed => disposed;

        public static VfxPreviewOpenResult Open(
            GameObject generatedPrefab,
            string playEventName = "OnPlay")
        {
            if (generatedPrefab == null)
            {
                return Failure("PREVIEW-PREFAB", "Generated Prefab is required.");
            }

            if (!EditorUtility.IsPersistent(generatedPrefab)
                || PrefabUtility.GetPrefabAssetType(generatedPrefab) == PrefabAssetType.NotAPrefab)
            {
                return Failure("PREVIEW-PREFAB", "Preview input must be a Prefab asset.");
            }

            if (generatedPrefab.GetComponent<VfxMetadata>() == null)
            {
                return Failure(
                    "PREVIEW-GENERATED",
                    "Preview input must contain VfxMetadata from the VFX Forge compiler.");
            }

            if (generatedPrefab.GetComponentsInChildren<VisualEffect>(true).Length == 0)
            {
                return Failure(
                    "PREVIEW-EFFECT",
                    "Generated Prefab must contain at least one VisualEffect component.");
            }

            var session = new VfxPreviewSession();
            try
            {
                session.Bootstrap(generatedPrefab, playEventName);
                int playedEffectCount = session.Restart();
                if (playedEffectCount == 0)
                {
                    session.Dispose();
                    return Failure(
                        "PREVIEW-PLAYBACK",
                        "No VisualEffect component accepted the playback event.");
                }

                return new VfxPreviewOpenResult
                {
                    Success = true,
                    Message = $"Preview started for {playedEffectCount} VisualEffect component(s).",
                    Session = session
                };
            }
            catch (Exception exception)
            {
                session.Dispose();
                return Failure("PREVIEW-BOOTSTRAP", exception.Message);
            }
        }

        public int Restart()
        {
            ThrowIfDisposed();
            player.StopAndReinitialize();
            int playedEffectCount = player.PlayAll();
            IsPlaying = playedEffectCount > 0;
            return playedEffectCount;
        }

        public void Stop()
        {
            ThrowIfDisposed();
            player.StopAndReinitialize();
            IsPlaying = false;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            IsPlaying = false;

            if (previewScene.IsValid() && EditorSceneManager.IsPreviewScene(previewScene))
            {
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
            else if (previewRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(previewRoot);
            }

            player = null;
            previewCamera = null;
            previewInstance = null;
            previewRoot = null;
            previewScene = default;
        }

        private void Bootstrap(GameObject generatedPrefab, string playEventName)
        {
            previewScene = EditorSceneManager.NewPreviewScene();

            previewRoot = new GameObject(PreviewRootName);
            SceneManager.MoveGameObjectToScene(previewRoot, previewScene);

            previewInstance =
                PrefabUtility.InstantiatePrefab(generatedPrefab, previewScene) as GameObject;
            if (previewInstance == null)
            {
                throw new InvalidOperationException("Generated Prefab could not be instantiated.");
            }

            previewInstance.name = generatedPrefab.name;
            previewInstance.transform.SetParent(previewRoot.transform, false);

            player = previewInstance.GetComponent<VfxPlayer>();
            if (player == null)
            {
                player = previewInstance.AddComponent<VfxPlayer>();
            }
            player.Configure(playEventName);

            var cameraRig = new GameObject(CameraRigName);
            cameraRig.transform.SetParent(previewRoot.transform, false);
            cameraRig.transform.localPosition = new Vector3(0f, 1f, 0f);

            var cameraObject = new GameObject(CameraName);
            cameraObject.transform.SetParent(cameraRig.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0.5f, -5f);
            cameraObject.transform.localRotation = Quaternion.identity;

            previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.enabled = false;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f);
            previewCamera.fieldOfView = 45f;
            previewCamera.nearClipPlane = 0.01f;
            previewCamera.farClipPlane = 1000f;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(VfxPreviewSession));
            }
        }

        private static VfxPreviewOpenResult Failure(string errorCode, string message)
        {
            return new VfxPreviewOpenResult
            {
                ErrorCode = errorCode,
                Message = message
            };
        }
    }
}
