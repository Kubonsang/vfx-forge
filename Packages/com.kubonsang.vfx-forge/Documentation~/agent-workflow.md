# Agent Workflow

## Input

- Recipe JSON
- Template Catalog
- Optional Style Profile

## Safe Loop

1. Parse Recipe.
2. Normalize deterministic fields.
3. Validate semantic constraints.
4. Resolve a registered Template.
5. Duplicate the Template Prefab.
6. Apply only registered Property Bindings.
7. Add generation metadata.
8. Validate the generated Prefab.
9. Save a report.
10. Only later, preview and capture.

## Review Rule

A visual change requires a before/after capture once capture support exists. Until then, report only structural correctness, never visual quality.
