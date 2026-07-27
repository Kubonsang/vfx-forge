# Preview Scene

VFX Forge previews generated Prefabs in a Unity Preview Scene. The scene has no asset
path, is isolated from the active user Scene, and is closed when the preview session is
disposed.

The bootstrap creates:

- a temporary playback root;
- an instance of a generated Prefab containing `VfxMetadata`;
- a fixed camera rig with a disabled Camera ready for explicit rendering;
- a temporary root-level `VfxPlayer` when the Prefab does not already contain one.

The configured play event is sent immediately. `Restart` stops, reinitializes, and sends
the event again. `Stop` reinitializes the effect without saving the temporary state.

Use the Preview controls in `Tools > VFX Forge > Open Window`, or call
`VfxPreviewSession.Open` from Editor code. API callers own the returned session and must
dispose it.

`Capture Frames` simulates the Recipe frame times and renders `front`, `side`, and `top`
views to deterministic PNG names. The output contract is documented in
[`Documentation~/frame-capture.md`](../Documentation~/frame-capture.md).
