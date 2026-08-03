using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace VfxForge.Dogfood.Tests
{
    public sealed class CavalierWallFacingV3PlayModeTests
    {
        [UnityTest]
        public IEnumerator DemoV3_WallKeepsEnemyFacingContract()
        {
            SceneManager.LoadScene(
                "CavalierWallShapeV3Demo",
                LoadSceneMode.Single);
            yield return null;

            CavalierWallFacing wall =
                Object.FindFirstObjectByType<CavalierWallFacing>();
            CavalierWallShapeDemoController demo =
                Object.FindFirstObjectByType<
                    CavalierWallShapeDemoController>();
            Assert.That(wall, Is.Not.Null);
            Assert.That(demo, Is.Not.Null);
            demo.enabled = false;

            Assert.That(wall.TrySetAimDirection(Vector3.left), Is.True);
            Assert.That(
                Vector3.Dot(wall.CurrentForward, Vector3.left),
                Is.GreaterThan(0.999f));
            Assert.That(wall.TrySetAimDirection(Vector3.up), Is.False);
        }
    }
}
