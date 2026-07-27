# Unity 6 Compatibility

## Verified Matrix

| Component | Version |
| --- | --- |
| Unity Editor | 6000.3.8f1 |
| Universal Render Pipeline | 17.3.0 |
| Visual Effect Graph | 17.3.0 |
| Unity Test Framework | 1.6.0 |

The repository-level `UnityCompatibilityProject` is the isolated host used for this
matrix. It references the package through a local UPM dependency and declares the
package in `testables`.

## Assembly References

- `VFXForge.Runtime` references `Unity.VisualEffectGraph.Runtime`.
- `VFXForge.Editor` references `VFXForge.Runtime` and
  `Unity.VisualEffectGraph.Runtime`.
- `VFXForge.Editor.Tests` references both VFX Forge assemblies and
  `Unity.VisualEffectGraph.Runtime`.

No assembly-name or `Kubonsang.VfxForge` namespace collision was found in the package.

## Verified VisualEffect API

The EditMode compatibility test verifies the runtime members used by VFX Forge:

- `HasFloat`, `SetFloat`
- `HasInt`, `SetInt`
- `HasBool`, `SetBool`
- `HasVector2`, `SetVector2`
- `HasVector3`, `SetVector3`
- `HasVector4`, `SetVector4`
- `SendEvent`, `Stop`, `Reinit`
- `visualEffectAsset`

No Unity 6000.3 or Visual Effect Graph 17.3 API deviation required a production-code
change.

## Verification

Run from the repository root:

```bash
/Applications/Unity/Hub/Editor/6000.3.8f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath "$PWD/UnityCompatibilityProject" \
  -runTests -testPlatform EditMode \
  -testResults "$PWD/UnityCompatibilityProject/Artifacts/vf-002-editmode-results.xml" \
  -logFile "$PWD/UnityCompatibilityProject/Logs/vf-002-editmode-tests.log"
```

Verified result: 20 tests passed, 0 failed, 0 skipped.

The host-only `VfxForgeCompatibilityProbe.CaptureConsoleCounts` method writes
`UnityCompatibilityProject/Artifacts/vf-002-console.json`. The verified result contains
0 Console errors.

## Known Limitations

- The starter repository does not include the PRD or SRS source documents.
- This matrix verifies compilation, EditMode behavior, and the VFX runtime API surface.
  It does not claim visual quality or capture correctness.
- The compatibility host reports warnings for `.meta` files paired with Unity-hidden
  package folders whose names end in `~`. These warnings do not enter the Console as
  errors.
