# Integration Fixture

The EditMode integration fixture exercises the complete Batch command pipeline with five
versioned Recipe JSON files:

| Recipe | Expected result |
| --- | --- |
| `valid_front.json` | Exit `0`; front PNG, manifest, report, and generated Prefab. |
| `valid_side.json` | Exit `0`; side PNG, manifest, report, and generated Prefab. |
| `valid_top.json` | Exit `0`; top PNG, manifest, report, and generated Prefab. |
| `invalid_contract.json` | Exit `20` at `ParseRecipe`; no generated asset. |
| `invalid_template.json` | Exit `30` at `ValidateInputs`; failure report only. |

The files live under `Tests/Fixtures/Recipes`. They use 64×64 single-frame captures to
keep the integration suite focused and deterministic.

## Template Fixture

`VfxForgeIntegrationFixtureTests` creates a temporary Template Prefab, material, and
Template Catalog through Unity Editor APIs. The Template references the read-only
Minimal System VFX Graph supplied by the installed Visual Effect Graph package. No VFX
Graph serialization file is edited.

Each Recipe executes:

1. Batch argument and path resolution;
2. Recipe parse and validation;
3. safe independent Prefab compilation;
4. generated-Prefab validation;
5. isolated Preview creation;
6. deterministic frame capture;
7. validation report generation.

After every run, the test compares the Template file SHA-256, Template dependency hash,
read-only VFX asset dependency hash, and Catalog JSON with the pre-run snapshot. It also
checks that the Template has no generated `VfxMetadata`.

## Cleanup

The fixture deletes its generated Prefabs, Template fixture, Catalog, capture files, and
temporary Recipe copies in teardown. Preview roots must not remain after either a
successful or failed run.

## Run

Run from the repository root without `-nographics`, because the three valid cases render
frames:

```bash
/Applications/Unity/Hub/Editor/6000.3.8f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath "$PWD/UnityCompatibilityProject" \
  -runTests \
  -testPlatform EditMode \
  -testFilter Kubonsang.VfxForge.Editor.Tests.VfxForgeIntegrationFixtureTests \
  -testResults "$PWD/UnityCompatibilityProject/Artifacts/vf-011-integration-results.xml" \
  -logFile "$PWD/UnityCompatibilityProject/Artifacts/vf-011-integration.log"
```

The captured fixture establishes structural rendering and pipeline behavior. It is not
evidence of VFX visual quality.
