# Template Authoring Contract

Each project-owned Template Prefab should:

- Contain at least one `VisualEffect` component.
- Use a stable event name, default `OnPlay`.
- Expose only parameters intended for agent mutation.
- Keep property names stable across versions.
- Use fixed or controllable randomness for review.
- Declare supported semantic layers in the Catalog.
- Avoid hidden Light or Distortion usage when the project policy forbids them.

Recommended initial exposed properties:

```text
Duration
Radius
SpreadAngle
Directionality
PrimaryColor
SecondaryColor
EmissionIntensity
Sharpness
DistortionStrength
RandomSeed
```

## Catalog registration

Use `VfxTemplateCatalog.TryRegister` when adding entries from tools or automation. The
method validates the candidate before mutating the Catalog and rejects duplicate IDs.
Direct Inspector edits remain visible for correction, but validation errors are displayed
below the serialized Catalog and the compiler refuses to use an invalid Catalog.

A valid entry must:

- use an ID matching `^[a-z0-9][a-z0-9_-]{2,63}$`;
- reference a persistent Prefab asset containing at least one `VisualEffect` component;
- declare a non-empty play event name;
- contain no empty or duplicate supported layers;
- contain only supported, type-compatible Property Bindings;
- use a component index of `-1` for all components, or a valid zero-based index;
- avoid overlapping bindings to the same exposed property and component target.

Required bindings whose exposed property cannot be found are errors. Missing optional
properties are warnings and do not prevent registration.

## Supported Recipe binding paths

| Recipe path | Property type |
| --- | --- |
| `seed` | `Int` |
| `timing.duration`, `timing.anticipation`, `timing.impact`, `timing.sustain`, `timing.decay` | `Float` |
| `shape.radius`, `shape.directionality`, `shape.spreadAngle` | `Float` |
| `style.emissionIntensity`, `style.sharpness`, `style.distortionStrength` | `Float` |
| `style.primaryColor`, `style.secondaryColor` | `Color` |
