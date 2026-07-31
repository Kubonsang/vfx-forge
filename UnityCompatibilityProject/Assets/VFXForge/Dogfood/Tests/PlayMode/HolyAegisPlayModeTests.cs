using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace VfxForge.Dogfood.Tests
{
    public sealed class HolyAegisPlayModeTests
    {
        [UnityTest]
        public IEnumerator Demo_DeploysAndEndsAtRecipeLifetime()
        {
            SceneManager.LoadScene(
                "HolyAegisV3Demo",
                LoadSceneMode.Single);
            yield return null;

            HolyAegisDeployment shield =
                Object.FindFirstObjectByType<
                    HolyAegisDeployment>();
            Assert.That(shield, Is.Not.Null);
            GameObject instance = shield.gameObject;
            Assert.That(
                shield.Duration,
                Is.EqualTo(1.8f).Within(0.001f));
            Assert.That(
                shield.Radius,
                Is.EqualTo(2.6f).Within(0.001f));
            Assert.That(
                instance.GetComponentsInChildren<
                    ParticleSystem>(true),
                Is.Empty);
            Assert.That(
                instance.GetComponentsInChildren<Light>(true),
                Is.Empty);

            yield return new WaitForSeconds(0.32f);

            Assert.That(instance, Is.Not.Null);
            Assert.That(shield.IsDeployed, Is.True);

            yield return new WaitForSeconds(1.55f);

            Assert.That(instance == null, Is.True);
        }
    }
}
