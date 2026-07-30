using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace VfxForge.Dogfood.Tests
{
    public sealed class OrnateShieldPlayModeTests
    {
        [UnityTest]
        public IEnumerator Demo_DeploysFiveShaderedOrnamentsAndEnds()
        {
            SceneManager.LoadScene(
                "OrnateGiantShieldDemo",
                LoadSceneMode.Single);
            yield return null;

            OrnateShieldDeployment shield =
                Object.FindFirstObjectByType<OrnateShieldDeployment>();
            Assert.That(shield, Is.Not.Null);
            GameObject instance = shield.gameObject;
            VisualEffect effect =
                instance.GetComponentInChildren<VisualEffect>();
            Assert.That(effect, Is.Not.Null);
            Assert.That(effect.enabled, Is.True);
            Assert.That(
                shield.Duration,
                Is.EqualTo(2f).Within(0.001f));
            Assert.That(
                shield.Radius,
                Is.EqualTo(3.4f).Within(0.001f));
            Assert.That(shield.OrnamentCount, Is.EqualTo(5));
            Assert.That(
                instance.GetComponentsInChildren<ParticleSystem>(true),
                Is.Empty);

            yield return new WaitForSeconds(0.46f);

            Assert.That(instance, Is.Not.Null);
            Assert.That(shield.IsDeployed, Is.True);

            yield return new WaitForSeconds(1.61f);

            Assert.That(instance == null, Is.True);
        }
    }
}
