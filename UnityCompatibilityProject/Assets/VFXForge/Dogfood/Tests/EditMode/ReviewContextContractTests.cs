using Kubonsang.VfxForge;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace VfxForge.Dogfood.Tests
{
    public sealed class ReviewContextContractTests
    {
        private const string ContextPath =
            "Assets/VFXForge/Dogfood/ReviewContexts/"
            + "TopDownThreeGrounds.prefab";

        [Test]
        public void DefaultContext_HasTopDownCameraAndExplicitReferences()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    ContextPath);
            Assert.That(prefab, Is.Not.Null);
            VfxReviewContext context =
                prefab.GetComponent<VfxReviewContext>();
            Assert.That(context, Is.Not.Null);
            Assert.That(context.reviewCamera, Is.Not.Null);
            Assert.That(context.reviewCamera.orthographic, Is.True);
            Assert.That(context.effectAnchor, Is.Not.Null);
            Assert.That(context.caster, Is.Not.Null);
            Assert.That(context.target, Is.Not.Null);
            Assert.That(
                prefab.transform.Find("Light Ground"),
                Is.Not.Null);
            Assert.That(
                prefab.transform.Find("Medium Ground"),
                Is.Not.Null);
            Assert.That(
                prefab.transform.Find("Dark Ground"),
                Is.Not.Null);
        }
    }
}
