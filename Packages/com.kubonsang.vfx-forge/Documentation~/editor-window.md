# Editor Window

Open `Tools > VFX Forge > Open Window`.

## Run All

Select a Recipe JSON, Template Catalog, and Artifact Directory, then select `Run All`.
The Editor pipeline executes these stages in order:

1. parse the Recipe;
2. validate the Recipe, Template Catalog, and artifact path;
3. compile the generated Prefab;
4. validate the generated Prefab;
5. open an isolated Preview;
6. capture the requested frames;
7. write the validation report.

The progress bar and Unity progress dialog show the active stage. The final state is
`Completed` or `Failed`, and a failed result records the stage that stopped the run.

## Failure Gates

- Parse or input validation failures stop before Prefab compilation.
- Compile failures stop before generated-Prefab validation, Preview, and Capture.
- Generated-Prefab validation failures stop before Preview and Capture.
- Preview failures stop before Capture.
- Capture failures are recorded in the validation report.
- Preview sessions are disposed after Capture or an exception.

A failure report is written when a parsed Recipe and artifact path are available. Report
writing does not continue the failed generation pipeline.

## Result Navigation

After a run, the `Last Run` controls provide:

- `Select Prefab`: selects and pings the generated Prefab;
- `Reveal Report`: reveals `validation.json`;
- `Reveal Capture`: reveals `capture-manifest.json`.

Missing result files are reported as UI validation errors instead of opening an invalid
path.

The individual Validate, Compile, Preview, and Capture controls remain available for
focused iteration.
