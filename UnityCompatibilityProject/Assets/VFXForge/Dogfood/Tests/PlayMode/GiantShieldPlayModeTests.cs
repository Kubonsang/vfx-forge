using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace VfxForge.Dogfood.Tests
{
    public sealed class GiantShieldPlayModeTests
    {
        [UnityTest]
        public IEnumerator Demo_DeploysHoldsAndEndsAtConfiguredLifetime()
        {
            SceneManager.LoadScene(
                "GiantShieldDemo",
                LoadSceneMode.Single);
            yield return null;

            GiantShieldDeployment shield =
                Object.FindFirstObjectByType<GiantShieldDeployment>();
            Assert.That(shield, Is.Not.Null);
            GameObject instance = shield.gameObject;
            VisualEffect effect =
                instance.GetComponentInChildren<VisualEffect>();
            Assert.That(effect, Is.Not.Null);
            Assert.That(effect.enabled, Is.True);
            Assert.That(effect.visualEffectAsset, Is.Not.Null);
            Assert.That(
                shield.Duration,
                Is.EqualTo(1.8f).Within(0.001f));
            Assert.That(
                shield.Radius,
                Is.EqualTo(3.2f).Within(0.001f));

            yield return new WaitForSeconds(0.42f);

            Assert.That(instance, Is.Not.Null);
            Assert.That(shield.IsDeployed, Is.True);

            yield return new WaitForSeconds(1.43f);

            Assert.That(instance == null, Is.True);
        }
    }
}
