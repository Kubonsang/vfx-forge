#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace VfxForge.Dogfood.Tests
{
    public sealed class SymmetricShieldBlockoutPlayModeTests
    {
        private const string RuntimePrefabPath =
            "Assets/VFXForge/Dogfood/HolyAegisV4/Runtime/"
            + "SymmetricShieldBlockoutV1.prefab";

        [UnityTest]
        public IEnumerator RuntimeShieldRendersAndFollowsAimDirection()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                RuntimePrefabPath);
            Assert.That(prefab, Is.Not.Null);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                yield return null;
                CavalierWallFacing facing =
                    instance.GetComponent<CavalierWallFacing>();
                Assert.That(facing, Is.Not.Null);
                Assert.That(
                    facing.TrySetAimDirection(new Vector3(1f, 0f, 1f)),
                    Is.True);
                Vector3 expected = new Vector3(1f, 0f, 1f).normalized;
                Assert.That(
                    Vector3.Dot(facing.CurrentForward, expected),
                    Is.GreaterThan(0.999f));

                Renderer[] renderers =
                    instance.GetComponentsInChildren<Renderer>(true);
                Assert.That(renderers, Has.Length.EqualTo(6));
                Bounds bounds = renderers[0].bounds;
                for (int index = 1; index < renderers.Length; index++)
                {
                    bounds.Encapsulate(renderers[index].bounds);
                }
                Assert.That(bounds.size.x, Is.GreaterThan(5f));
                Assert.That(bounds.size.y, Is.GreaterThan(4f));
                Assert.That(
                    instance.GetComponent<SymmetricShieldBlockoutMarker>(),
                    Is.Not.Null);
            }
            finally
            {
                Object.Destroy(instance);
            }
            yield return null;
        }
    }
}
#endif
