using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;

namespace Kubonsang.VfxForge.Editor.Tests
{
    public sealed class VfxPreviewSessionTests
    {
        private string testAssetRoot;
        private GameObject generatedPrefab;
        private VfxPreviewSession openSession;

        [SetUp]
        public void SetUp()
        {
            string folderName = $"__VfxForgePreviewTests_{Guid.NewGuid():N}";
            AssetDatabase.CreateFolder("Assets", folderName);
            testAssetRoot = $"Assets/{folderName}";
            generatedPrefab = CreateGeneratedPrefab($"{testAssetRoot}/Generated.prefab");
        }

        [TearDown]
        public void TearDown()
        {
            openSession?.Dispose();
            openSession = null;

            if (!string.IsNullOrWhiteSpace(testAssetRoot)
                && AssetDatabase.IsValidFolder(testAssetRoot))
            {
                AssetDatabase.DeleteAsset(testAssetRoot);
            }
        }

        [Test]
        public void Open_GeneratedPrefab_CreatesIsolatedSceneCameraAndPlayback()
        {
            VfxPreviewOpenResult result =
                VfxPreviewSession.Open(generatedPrefab, "OnPreviewPlay");
            openSession = result.Session;

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(openSession, Is.Not.Null);
            Assert.That(
                EditorSceneManager.IsPreviewScene(openSession.PreviewScene),
                Is.True);
            Assert.That(openSession.PreviewScene.path, Is.Empty);
            Assert.That(openSession.PreviewInstance, Is.Not.Null);
            Assert.That(openSession.PreviewInstance.scene, Is.EqualTo(openSession.PreviewScene));
            Assert.That(openSession.PreviewCamera, Is.Not.Null);
            Assert.That(openSession.PreviewCamera.enabled, Is.False);
            Assert.That(openSession.PreviewCamera.scene, Is.EqualTo(openSession.PreviewScene));
            Assert.That(openSession.PreviewCamera.gameObject.name, Is.EqualTo(VfxPreviewSession.CameraName));
            Assert.That(openSession.IsPlaying, Is.True);
            Assert.That(
                openSession.PreviewInstance.GetComponent<VfxPlayer>().PlayEventName,
                Is.EqualTo("OnPreviewPlay"));
        }

        [Test]
        public void Open_PreviewCameraUsesDeterministicPreviewRenderingSettings()
        {
            VfxPreviewOpenResult result = VfxPreviewSession.Open(generatedPrefab);
            openSession = result.Session;

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(openSession.PreviewCamera.cameraType, Is.EqualTo(CameraType.Preview));
            Assert.That(
                openSession.PreviewCamera.renderingPath,
                Is.EqualTo(RenderingPath.Forward));
            Assert.That(openSession.PreviewCamera.useOcclusionCulling, Is.False);
        }

        [Test]
        public void Open_DoesNotChangeActiveSceneOrDirtyState()
        {
            Scene activeSceneBefore = SceneManager.GetActiveScene();
            bool dirtyBefore = activeSceneBefore.isDirty;

            VfxPreviewOpenResult result = VfxPreviewSession.Open(generatedPrefab);
            openSession = result.Session;

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(activeSceneBefore));
            Assert.That(activeSceneBefore.isDirty, Is.EqualTo(dirtyBefore));

            openSession.Dispose();
            openSession = null;

            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(activeSceneBefore));
            Assert.That(activeSceneBefore.isDirty, Is.EqualTo(dirtyBefore));
        }

        [Test]
        public void Dispose_ClosesPreviewSceneAndRemovesTemporaryInstance()
        {
            VfxPreviewOpenResult result = VfxPreviewSession.Open(generatedPrefab);
            openSession = result.Session;
            Scene previewScene = openSession.PreviewScene;
            GameObject instance = openSession.PreviewInstance;

            openSession.Dispose();
            openSession = null;

            Assert.That(previewScene.IsValid(), Is.False);
            Assert.That(instance == null, Is.True);
        }

        [Test]
        public void Dispose_CanBeCalledMoreThanOnce()
        {
            VfxPreviewOpenResult result = VfxPreviewSession.Open(generatedPrefab);
            openSession = result.Session;

            Assert.DoesNotThrow(() => openSession.Dispose());
            Assert.DoesNotThrow(() => openSession.Dispose());
            Assert.That(openSession.IsDisposed, Is.True);
            openSession = null;
        }

        [Test]
        public void OpenAndDispose_DoesNotModifyGeneratedPrefabAsset()
        {
            string prefabPath = AssetDatabase.GetAssetPath(generatedPrefab);
            Hash128 hashBefore = AssetDatabase.GetAssetDependencyHash(prefabPath);

            VfxPreviewOpenResult result = VfxPreviewSession.Open(generatedPrefab);
            openSession = result.Session;
            Assert.That(result.Success, Is.True, result.Message);

            openSession.Stop();
            openSession.Dispose();
            openSession = null;

            Assert.That(
                AssetDatabase.GetAssetDependencyHash(prefabPath),
                Is.EqualTo(hashBefore));
        }

        [Test]
        public void StopAndRestart_UpdatesPlaybackState()
        {
            VfxPreviewOpenResult result = VfxPreviewSession.Open(generatedPrefab);
            openSession = result.Session;

            openSession.Stop();
            Assert.That(openSession.IsPlaying, Is.False);

            int playedEffectCount = openSession.Restart();
            Assert.That(playedEffectCount, Is.EqualTo(1));
            Assert.That(openSession.IsPlaying, Is.True);
        }

        [Test]
        public void Open_NonGeneratedPrefab_IsRejectedBeforeSceneCreation()
        {
            GameObject source = new GameObject("UserOwned");
            GameObject userPrefab;
            try
            {
                source.AddComponent<VisualEffect>();
                userPrefab = PrefabUtility.SaveAsPrefabAsset(
                    source,
                    $"{testAssetRoot}/UserOwned.prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }

            VfxPreviewOpenResult result = VfxPreviewSession.Open(userPrefab);

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("PREVIEW-GENERATED"));
            Assert.That(result.Session, Is.Null);
        }

        [Test]
        public void Open_GeneratedPrefabWithoutVisualEffect_IsRejected()
        {
            GameObject source = new GameObject("NoEffect");
            GameObject prefab;
            try
            {
                source.AddComponent<VfxMetadata>();
                prefab = PrefabUtility.SaveAsPrefabAsset(
                    source,
                    $"{testAssetRoot}/NoEffect.prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }

            VfxPreviewOpenResult result = VfxPreviewSession.Open(prefab);

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("PREVIEW-EFFECT"));
            Assert.That(result.Session, Is.Null);
        }

        private static GameObject CreateGeneratedPrefab(string path)
        {
            var source = new GameObject("Generated");
            try
            {
                source.AddComponent<VfxMetadata>();
                source.AddComponent<VisualEffect>();
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
                Assert.That(prefab, Is.Not.Null);
                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }
    }
}
