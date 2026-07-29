using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;

namespace Kubonsang.VfxForge.Editor
{
    public enum VfxPreviewView
    {
        Front,
        Side,
        Top
    }

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
            SetEffectsPaused(false);
            player.StopAndReinitialize();
            int playedEffectCount = player.PlayAll();
            EvaluateCustomPreviewTime(0f);
            IsPlaying = playedEffectCount > 0;
            return playedEffectCount;
        }

        public void Stop()
        {
            ThrowIfDisposed();
            SetEffectsPaused(false);
            player.StopAndReinitialize();
            IsPlaying = false;
        }

        public void SimulateTo(float timeSeconds)
        {
            ThrowIfDisposed();
            if (float.IsNaN(timeSeconds)
                || float.IsInfinity(timeSeconds)
                || timeSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeSeconds),
                    "Preview time must be finite and non-negative.");
            }

            Restart();
            foreach (VisualEffect effect in GetEffects())
            {
                if (effect == null)
                {
                    continue;
                }

                effect.pause = false;
                if (timeSeconds > 0f)
                {
                    effect.Simulate(timeSeconds, 1);
                }
                effect.pause = true;
            }

            EvaluateCustomPreviewTime(timeSeconds);

            IsPlaying = false;
        }

        public void SetCameraView(VfxPreviewView view)
        {
            ThrowIfDisposed();
            Vector3 target = previewInstance.transform.position + Vector3.up;
            Vector3 position;
            Vector3 up = Vector3.up;

            switch (view)
            {
                case VfxPreviewView.Front:
                    position = target + new Vector3(0f, 0.5f, -5f);
                    break;
                case VfxPreviewView.Side:
                    position = target + new Vector3(5f, 0.5f, 0f);
                    break;
                case VfxPreviewView.Top:
                    position = target + new Vector3(0f, 5f, 0f);
                    up = Vector3.forward;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(view), view, "Unsupported view.");
            }

            previewCamera.transform.position = position;
            previewCamera.transform.rotation =
                Quaternion.LookRotation(target - position, up);
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
            previewCamera.cameraType = CameraType.Preview;
            previewCamera.renderingPath = RenderingPath.Forward;
            previewCamera.useOcclusionCulling = false;
            previewCamera.scene = previewScene;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f);
            previewCamera.fieldOfView = 45f;
            previewCamera.nearClipPlane = 0.01f;
            previewCamera.farClipPlane = 1000f;
            SetCameraView(VfxPreviewView.Front);
        }

        private VisualEffect[] GetEffects()
        {
            return previewInstance.GetComponentsInChildren<VisualEffect>(true);
        }

        private void EvaluateCustomPreviewTime(float timeSeconds)
        {
            const BindingFlags Flags =
                BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.NonPublic;

            foreach (MonoBehaviour behaviour in
                previewInstance.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null)
                {
                    continue;
                }

                MethodInfo method = behaviour.GetType().GetMethod(
                    "EvaluatePreviewTime",
                    Flags,
                    null,
                    new[] { typeof(float) },
                    null);
                method?.Invoke(behaviour, new object[] { timeSeconds });
            }
        }

        private void SetEffectsPaused(bool paused)
        {
            foreach (VisualEffect effect in GetEffects())
            {
                if (effect != null)
                {
                    effect.pause = paused;
                }
            }
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
