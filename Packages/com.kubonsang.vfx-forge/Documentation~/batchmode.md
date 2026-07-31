# BatchMode Contract

Invoke VFX Forge through Unity's `-executeMethod` option:

```bash
/Applications/Unity/Hub/Editor/6000.3.8f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath "/path/to/UnityProject" \
  -executeMethod Kubonsang.VfxForge.Editor.VfxForgeBatchEntry.Run \
  -recipe "/path/to/recipe.json" \
  -templateCatalog "Assets/VFXForge/VfxTemplateCatalog.asset" \
  -artifactPath "/path/to/artifacts/run-001" \
  -logFile -
```

Do not add `-quit`; the entry point exits Unity with the contract exit code. The complete
pipeline captures rendered frames, so do not add `-nographics` to a normal run.

## Arguments

All VFX Forge arguments are required and case-sensitive:

- `-recipe`: absolute path or Unity-project-relative path to a Recipe JSON file;
- `-templateCatalog`: project-relative or absolute path to a
  `VfxTemplateCatalog` asset inside the project's `Assets` directory;
- `-artifactPath`: absolute path or Unity-project-relative output directory.

Unity's own arguments are ignored by the VFX Forge argument parser. Duplicate VFX Forge
arguments and arguments without values fail before the Recipe is read.

Relative Recipe and Artifact paths are resolved against the Unity project root. The
resolved absolute Artifact path is returned in the command result. A successful run
writes:

- `validation.json` directly under the Artifact path;
- captured PNG files and `capture-manifest.json` under `capture/`.

## Result Line

The entry point writes exactly one compact JSON result line to standard output before
Unity exits. Select the line whose `tool` field is `VFXForge`.

```json
{"schemaVersion":"1.0","tool":"VFXForge","status":"passed","exitCode":0,"failedStage":"","recipeId":"impact_blue","artifactPath":"/tmp/vfx-forge/impact_blue","reportPath":"/tmp/vfx-forge/impact_blue/validation.json","generatedPrefab":"Assets/Generated/ImpactBlue.prefab","captureManifest":"/tmp/vfx-forge/impact_blue/capture/capture-manifest.json","reviewManifest":"/tmp/vfx-forge/impact_blue/review/review-manifest.json","contactSheet":"/tmp/vfx-forge/impact_blue/review/contact-sheet.png","message":"Run All completed."}
```

Messages containing line breaks are JSON-escaped, so one invocation still produces one
physical result line. `reviewManifest` and `contactSheet` are empty when the Recipe
does not request any gameplay Context IDs.

## Exit Codes

| Code | Stage | Meaning |
| ---: | --- | --- |
| 0 | Success | Pipeline completed; `status` is `passed` or `warning`. |
| 10 | Arguments | Required, duplicate, missing-value, or invalid-path argument. |
| 20 | ParseRecipe | Recipe file read or JSON parsing failure. |
| 30 | ValidateInputs | Recipe, Template Catalog, or Artifact input validation failure. |
| 40 | CompilePrefab | Generated Prefab compilation failure. |
| 50 | ValidatePrefab | Generated Prefab validation failure. |
| 60 | OpenPreview | Isolated Preview bootstrap failure. |
| 70 | CaptureFrames | Frame capture failure. |
| 80 | WriteReport | Validation report write failure. |
| 90 | Unexpected | Unclassified exception or missing pipeline result. |

Nonzero exit codes identify the first failed stage. Later generation stages do not run
after a failure.
