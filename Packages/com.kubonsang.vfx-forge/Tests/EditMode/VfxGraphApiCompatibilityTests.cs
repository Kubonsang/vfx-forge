using NUnit.Framework;
using UnityEngine.VFX;

namespace Kubonsang.VfxForge.Editor.Tests
{
    public sealed class VfxGraphApiCompatibilityTests
    {
        [TestCase(nameof(VisualEffect.HasFloat))]
        [TestCase(nameof(VisualEffect.SetFloat))]
        [TestCase(nameof(VisualEffect.HasInt))]
        [TestCase(nameof(VisualEffect.SetInt))]
        [TestCase(nameof(VisualEffect.HasBool))]
        [TestCase(nameof(VisualEffect.SetBool))]
        [TestCase(nameof(VisualEffect.HasVector2))]
        [TestCase(nameof(VisualEffect.SetVector2))]
        [TestCase(nameof(VisualEffect.HasVector3))]
        [TestCase(nameof(VisualEffect.SetVector3))]
        [TestCase(nameof(VisualEffect.HasVector4))]
        [TestCase(nameof(VisualEffect.SetVector4))]
        [TestCase(nameof(VisualEffect.SendEvent))]
        [TestCase(nameof(VisualEffect.Stop))]
        [TestCase(nameof(VisualEffect.Reinit))]
        [TestCase(nameof(VisualEffect.Simulate))]
        [TestCase(nameof(VisualEffect.pause))]
        [TestCase(nameof(VisualEffect.visualEffectAsset))]
        public void RequiredVisualEffectMember_Exists(string memberName)
        {
            Assert.That(typeof(VisualEffect).GetMember(memberName), Is.Not.Empty, memberName);
        }
    }
}
