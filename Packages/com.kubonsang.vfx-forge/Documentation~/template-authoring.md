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
