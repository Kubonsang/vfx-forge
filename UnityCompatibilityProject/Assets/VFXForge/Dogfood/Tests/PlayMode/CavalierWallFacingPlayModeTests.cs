using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace VfxForge.Dogfood.Tests
{
    public sealed class CavalierWallFacingPlayModeTests
    {
        [UnityTest]
        public IEnumerator Demo_WallFollowsPlayerFacingAndAimOverride()
        {
            SceneManager.LoadScene(
                "CavalierWallShapeDemo",
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

            Transform source = wall.FacingSource;
            Assert.That(source, Is.Not.Null);
            source.rotation = Quaternion.LookRotation(
                Vector3.left,
                Vector3.up);
            wall.ClearAimOverride();
            wall.EvaluateFacing();
            Assert.That(
                Vector3.Dot(wall.CurrentForward, Vector3.left),
                Is.GreaterThan(0.999f));

            Assert.That(
                wall.TrySetAimDirection(Vector3.right),
                Is.True);
            Assert.That(
                Vector3.Dot(wall.CurrentForward, Vector3.right),
                Is.GreaterThan(0.999f));
            Assert.That(wall.HasAimOverride, Is.True);

            Assert.That(
                wall.TrySetAimDirection(Vector3.up),
                Is.False);
            Assert.That(
                Vector3.Dot(wall.CurrentForward, Vector3.right),
                Is.GreaterThan(0.999f));
        }
    }
}
