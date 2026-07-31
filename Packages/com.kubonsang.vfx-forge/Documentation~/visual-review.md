# Human Visual Review

Technical validation and product visual approval are separate outputs:

- `validation.json` records compiler, asset, budget, bounds, and capture gates;
- `visual-review.json` records the human product-quality decision.

VFX Forge never writes an accepted production review automatically. Automated
tests use synthetic fixture records only.

## visual-review-1.0

When `quality.requireHumanReview` is true, the Recipe must request at least one
Catalog Review Context. After technical validation and capture succeed, the
Pipeline writes `<Artifact Directory>/visual-review.json` with:

- generated Prefab dependency hash;
- Capture Manifest SHA-256;
- Contact Sheet SHA-256;
- reviewer and UTC review time;
- accepted, rejected, review-required, or stale status;
- meaning delivery, silhouette, Shader/pattern finish, timing, and gameplay
  readability decisions;
- a required rejection reason when rejected.

All five criteria, reviewer, review time, and current hashes are required for
`accepted`. A changed Prefab, Capture Manifest, or Contact Sheet makes an older
decision `review_stale`.

## Editor Workflow

Run the full Pipeline, open the Contact Sheet, and inspect the isolated and
gameplay sequence. Enter the reviewer, evaluate all five criteria, then:

- `Accept` writes `accepted` only when every criterion passes;
- `Reject` writes `rejected` only when a reason is present.

The user remains the reviewer. Codex and automated validation do not select
Accept for a production VFX.

## BatchMode

Pass a human-authored review file with:

```text
-visualReview "/path/to/visual-review.json"
```

The submitted hashes are compared to the outputs produced by that run:

| Exit | Product status | Meaning |
| ---: | --- | --- |
| 0 | `accepted` | Matching human approval and all five criteria pass. |
| 80 | `review_required` | No usable approval is present. |
| 81 | `rejected` | Matching human review rejected the result. |
| 82 | `review_stale` | At least one reviewed input hash changed. |

Technical report-writing failures also retain the pre-existing exit code 80 and
use the failed stage `WriteReport`; human review states use `VisualReview`.
