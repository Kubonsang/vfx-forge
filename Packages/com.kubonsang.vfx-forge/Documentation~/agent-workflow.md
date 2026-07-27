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
10. Preview in an isolated Scene.
11. Capture the requested frames and manifest.

## Review Rule

A visual change requires before/after captures and validation evidence. A structural
capture fixture alone does not prove visual quality.
