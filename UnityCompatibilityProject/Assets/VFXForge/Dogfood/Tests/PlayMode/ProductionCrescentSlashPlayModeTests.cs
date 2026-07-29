using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace VfxForge.Dogfood.Tests
{
    public sealed class ProductionCrescentSlashPlayModeTests
    {
        [UnityTest]
        public IEnumerator Demo_FiresMovesPlaysAndEndsAtConfiguredLifetime()
        {
            SceneManager.LoadScene("ProductionCrescentDemo", LoadSceneMode.Single);
            yield return null;

            ProductionCrescentSlash[] slashes =
                Object.FindObjectsByType<ProductionCrescentSlash>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
            Assert.That(slashes, Has.Length.EqualTo(3));

            ProductionCrescentSlash slash = slashes[0];
            GameObject instance = slash.gameObject;
            float startZ = instance.transform.position.z;
            VisualEffect effect = instance.GetComponentInChildren<VisualEffect>();
            Assert.That(effect, Is.Not.Null);
            Assert.That(effect.enabled, Is.True);
            Assert.That(effect.visualEffectAsset, Is.Not.Null);
            Assert.That(slash.Duration, Is.EqualTo(0.52f).Within(0.001f));
            Assert.That(slash.TravelSpeed, Is.EqualTo(11f).Within(0.001f));

            yield return new WaitForSeconds(0.20f);

            Assert.That(instance, Is.Not.Null);
            Assert.That(
                instance.transform.position.z - startZ,
                Is.GreaterThan(1.8f));

            yield return new WaitForSeconds(0.36f);

            Assert.That(instance == null, Is.True);
        }
    }
}
