using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;

namespace Kubonsang.VfxForge.Editor.Tests
{
    public sealed class VfxValidationPipelineTests
    {
        [Test]
        public void CreateDefaultRules_ReturnsStableOrderedRuleIds()
        {
            IReadOnlyList<IVfxValidationRule> rules =
                VfxValidationPipeline.CreateDefaultRules();

            Assert.That(rules.Count, Is.EqualTo(7));
            Assert.That(
                GetRuleIds(rules),
                Is.EqualTo(new[]
                {
                    "VAL-001",
                    "VAL-002",
                    "VAL-003",
                    "VAL-004",
                    "VAL-005",
                    "VAL-006",
                    "VAL-008"
                }));
        }

        [Test]
        public void Run_DefaultRules_ContinuesAfterFailure()
        {
            GameObject prefab = CreatePrefabObject();
            try
            {
                List<VfxValidationResult> results =
                    VfxValidationPipeline.Run(CreateContext(prefab));

                Assert.That(results, Has.Count.EqualTo(7));
                Assert.That(HasFailure(results, "VAL-001", VfxValidationSeverity.Error), Is.True);
                Assert.That(HasPass(results, "VAL-002"), Is.True);
                Assert.That(HasPass(results, "VAL-003"), Is.True);
                Assert.That(HasPass(results, "VAL-004"), Is.True);
                Assert.That(HasPass(results, "VAL-005"), Is.True);
                Assert.That(HasPass(results, "VAL-006"), Is.True);
                Assert.That(HasPass(results, "VAL-008"), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void PropertyBindingRule_MissingRequiredProperty_ReturnsRuleIdAndProperty()
        {
            GameObject prefab = CreatePrefabObject();
            try
            {
                VfxValidationContext context = CreateContext(prefab);
                context.Template.bindings.Add(CreateBinding(true));

                VfxValidationResult result = new PropertyBindingRule().Evaluate(context);

                Assert.That(result.ruleId, Is.EqualTo("VAL-002"));
                Assert.That(result.severity, Is.EqualTo(VfxValidationSeverity.Error));
                Assert.That(result.passed, Is.False);
                Assert.That(result.propertyName, Is.EqualTo("MissingDuration"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void PropertyBindingRule_MissingOptionalProperty_ReturnsWarning()
        {
            GameObject prefab = CreatePrefabObject();
            try
            {
                VfxValidationContext context = CreateContext(prefab);
                context.Template.bindings.Add(CreateBinding(false));

                VfxValidationResult result = new PropertyBindingRule().Evaluate(context);

                Assert.That(result.ruleId, Is.EqualTo("VAL-002"));
                Assert.That(result.severity, Is.EqualTo(VfxValidationSeverity.Warning));
                Assert.That(result.passed, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void DurationBudgetRule_StyleProfileTightensBudget()
        {
            GameObject prefab = CreatePrefabObject();
            VfxStyleProfile profile = ScriptableObject.CreateInstance<VfxStyleProfile>();
            try
            {
                VfxValidationContext context = CreateContext(prefab);
                context.Recipe.timing.duration = 0.75f;
                context.Recipe.budget.maxDuration = 1f;
                profile.maxDuration = 0.5f;
                context.StyleProfile = profile;

                VfxValidationResult result = new DurationBudgetRule().Evaluate(context);

                Assert.That(result.ruleId, Is.EqualTo("VAL-003"));
                Assert.That(result.severity, Is.EqualTo(VfxValidationSeverity.Error));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void LayerSupportRule_UnsupportedLayer_ReturnsStableRuleId()
        {
            GameObject prefab = CreatePrefabObject();
            try
            {
                VfxValidationContext context = CreateContext(prefab);
                context.Recipe.layers = new[] { "shockwave" };
                context.Template.supportedLayers = new[] { "core" };

                VfxValidationResult result = new LayerSupportRule().Evaluate(context);

                Assert.That(result.ruleId, Is.EqualTo("VAL-006"));
                Assert.That(result.severity, Is.EqualTo(VfxValidationSeverity.Error));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void LightPolicyRule_DisallowedLight_ReturnsStableRuleId()
        {
            GameObject prefab = CreatePrefabObject();
            prefab.AddComponent<Light>();
            try
            {
                VfxValidationContext context = CreateContext(prefab);
                context.Recipe.budget.allowLight = false;

                VfxValidationResult result = new LightPolicyRule().Evaluate(context);

                Assert.That(result.ruleId, Is.EqualTo("VAL-008"));
                Assert.That(result.severity, Is.EqualTo(VfxValidationSeverity.Error));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void FiniteBoundsRule_OversizedMesh_ReturnsStableRuleId()
        {
            GameObject prefab = CreatePrefabObject();
            var mesh = new Mesh
            {
                vertices = new[]
                {
                    Vector3.zero,
                    Vector3.right * 20f,
                    Vector3.up
                },
                triangles = new[] { 0, 1, 2 }
            };
            mesh.RecalculateBounds();
            prefab.AddComponent<MeshFilter>().sharedMesh = mesh;
            try
            {
                VfxValidationResult result =
                    new FiniteBoundsRule().Evaluate(CreateContext(prefab));

                Assert.That(result.ruleId, Is.EqualTo("VAL-004"));
                Assert.That(
                    result.severity,
                    Is.EqualTo(VfxValidationSeverity.Error));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void ParticleBudgetRule_ParticleSystemCapacityOverBudget_ReturnsStableRuleId()
        {
            GameObject prefab = CreatePrefabObject();
            ParticleSystem system = prefab.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.maxParticles = 101;
            try
            {
                VfxValidationContext context = CreateContext(prefab);
                context.Recipe.schemaVersion = "1.1";
                VfxValidationResult result =
                    new ParticleBudgetRule().Evaluate(context);

                Assert.That(result.ruleId, Is.EqualTo("VAL-005"));
                Assert.That(
                    result.severity,
                    Is.EqualTo(VfxValidationSeverity.Error));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void MissingAssetRule_MissingVisualEffectAsset_ReturnsStableRuleId()
        {
            GameObject prefab = CreatePrefabObject();
            try
            {
                VfxValidationResult result =
                    new MissingAssetRule().Evaluate(CreateContext(prefab));

                Assert.That(result.ruleId, Is.EqualTo("VAL-001"));
                Assert.That(result.severity, Is.EqualTo(VfxValidationSeverity.Error));
                StringAssert.Contains("VisualEffect asset", result.message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void Runner_ThrowingRule_ReturnsFailureWithRuleId()
        {
            List<VfxValidationResult> results = VfxValidationRunner.Run(
                null,
                new IVfxValidationRule[] { new ThrowingRule() });

            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(HasFailure(results, "TEST-THROW", VfxValidationSeverity.Error), Is.True);
        }

        [Test]
        public void Runner_NullResult_ReturnsFailureWithRuleId()
        {
            List<VfxValidationResult> results = VfxValidationRunner.Run(
                null,
                new IVfxValidationRule[] { new NullResultRule() });

            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(HasFailure(results, "TEST-NULL", VfxValidationSeverity.Error), Is.True);
        }

        [Test]
        public void Runner_EmptyResultRuleId_UsesDeclaredRuleId()
        {
            List<VfxValidationResult> results = VfxValidationRunner.Run(
                null,
                new IVfxValidationRule[] { new EmptyResultIdRule() });

            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].ruleId, Is.EqualTo("TEST-EMPTY"));
        }

        [Test]
        public void Runner_DuplicateRuleIds_ReturnsPipelineFailureId()
        {
            List<VfxValidationResult> results = VfxValidationRunner.Run(
                null,
                new IVfxValidationRule[]
                {
                    new PassingRule("TEST-DUPLICATE"),
                    new PassingRule("TEST-DUPLICATE")
                });

            Assert.That(
                HasFailure(
                    results,
                    "PIPELINE-RULE-ID-DUPLICATE",
                    VfxValidationSeverity.Error),
                Is.True);
        }

        [Test]
        public void Runner_NullRuleCollection_ReturnsPipelineFailureId()
        {
            List<VfxValidationResult> results =
                VfxValidationRunner.Run(null, null);

            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(HasFailure(results, "PIPELINE-RULES", VfxValidationSeverity.Error), Is.True);
        }

        [TestCase(VfxValidationSeverity.Info, true, "passed")]
        [TestCase(VfxValidationSeverity.Warning, false, "warning")]
        [TestCase(VfxValidationSeverity.Error, false, "failed")]
        public void ResolveStatus_FollowsHighestFailedSeverity(
            VfxValidationSeverity severity,
            bool passed,
            string expectedStatus)
        {
            var result = new VfxValidationResult
            {
                ruleId = "TEST-STATUS",
                severity = severity,
                passed = passed,
                message = "Status fixture."
            };

            Assert.That(
                VfxReportWriter.ResolveStatus(new[] { result }),
                Is.EqualTo(expectedStatus));
        }

        [Test]
        public void Write_FailedReport_PreservesStatusAndRuleId()
        {
            string artifactDirectory = Path.Combine(
                Path.GetTempPath(),
                $"vfx-forge-report-{Guid.NewGuid():N}");
            try
            {
                var results = new List<VfxValidationResult>
                {
                    VfxValidationResult.Error("VAL-002", "Required Binding is missing.")
                };

                string path = VfxReportWriter.Write(
                    artifactDirectory,
                    CreateRecipe(),
                    "Assets/VFXForge/Generated/Impact.prefab",
                    results);
                VfxValidationReport report =
                    JsonUtility.FromJson<VfxValidationReport>(File.ReadAllText(path));

                Assert.That(report.status, Is.EqualTo("failed"));
                Assert.That(report.results, Has.Count.EqualTo(1));
                Assert.That(report.results[0].ruleId, Is.EqualTo("VAL-002"));
                Assert.That(report.results[0].severity, Is.EqualTo("Error"));
            }
            finally
            {
                if (Directory.Exists(artifactDirectory))
                {
                    Directory.Delete(artifactDirectory, true);
                }
            }
        }

        private static VfxValidationContext CreateContext(GameObject prefab)
        {
            return new VfxValidationContext
            {
                Recipe = CreateRecipe(),
                Prefab = prefab,
                Template = new VfxTemplateEntry
                {
                    id = "impact_core",
                    prefab = prefab,
                    supportedLayers = new[] { "core" }
                },
                AssetPath = "Assets/VFXForge/Generated/Impact.prefab"
            };
        }

        private static VfxRecipe CreateRecipe()
        {
            return new VfxRecipe
            {
                id = "impact_recipe",
                template = "impact_core",
                outputPath = "Assets/VFXForge/Generated/Impact.prefab",
                layers = Array.Empty<string>(),
                timing = new VfxTiming { duration = 0.5f },
                budget = new VfxBudget
                {
                    maxParticles = 100,
                    maxDuration = 1f,
                    maxBoundsRadius = 5f
                }
            };
        }

        private static GameObject CreatePrefabObject()
        {
            var prefab = new GameObject("ValidationFixture");
            prefab.AddComponent<VisualEffect>();
            return prefab;
        }

        private static VfxPropertyBinding CreateBinding(bool required)
        {
            return new VfxPropertyBinding
            {
                recipePath = "timing.duration",
                exposedPropertyName = "MissingDuration",
                propertyType = VfxPropertyType.Float,
                required = required,
                componentIndex = 0
            };
        }

        private static string[] GetRuleIds(IReadOnlyList<IVfxValidationRule> rules)
        {
            var ids = new string[rules.Count];
            for (int index = 0; index < rules.Count; index++)
            {
                ids[index] = rules[index].RuleId;
            }

            return ids;
        }

        private static bool HasFailure(
            IEnumerable<VfxValidationResult> results,
            string ruleId,
            VfxValidationSeverity severity)
        {
            foreach (VfxValidationResult result in results)
            {
                if (result != null
                    && result.ruleId == ruleId
                    && result.severity == severity
                    && !result.passed)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasPass(
            IEnumerable<VfxValidationResult> results,
            string ruleId)
        {
            foreach (VfxValidationResult result in results)
            {
                if (result != null && result.ruleId == ruleId && result.passed)
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class ThrowingRule : IVfxValidationRule
        {
            public string RuleId => "TEST-THROW";

            public VfxValidationResult Evaluate(VfxValidationContext context)
            {
                throw new InvalidOperationException("Expected test exception.");
            }
        }

        private sealed class NullResultRule : IVfxValidationRule
        {
            public string RuleId => "TEST-NULL";

            public VfxValidationResult Evaluate(VfxValidationContext context)
            {
                return null;
            }
        }

        private sealed class EmptyResultIdRule : IVfxValidationRule
        {
            public string RuleId => "TEST-EMPTY";

            public VfxValidationResult Evaluate(VfxValidationContext context)
            {
                return new VfxValidationResult
                {
                    ruleId = string.Empty,
                    severity = VfxValidationSeverity.Error,
                    passed = false,
                    message = "Missing result id fixture."
                };
            }
        }

        private sealed class PassingRule : IVfxValidationRule
        {
            private readonly string ruleId;

            public PassingRule(string ruleId)
            {
                this.ruleId = ruleId;
            }

            public string RuleId => ruleId;

            public VfxValidationResult Evaluate(VfxValidationContext context)
            {
                return VfxValidationResult.Pass(ruleId, "Passed.");
            }
        }
    }
}
