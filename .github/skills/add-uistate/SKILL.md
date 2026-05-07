---
name: add-uistate
description: Use when adding a new value to `UIState` or `ReportFlowState` in PanelManager. Ensures every switch, panel reference, transition, and back-path is updated so the state machine stays exhaustive.
---

# Add a UIState (or ReportFlowState)

## Pre-flight clarifying questions
1. Is this a top-level `UIState` (whole-screen mode) or a `ReportFlowState` (sub-state inside the report flow)?
2. What triggers entry into the new state? (button click via Inspector, network callback, timer)
3. What are the exit paths — success, error, user-cancel? Each needs a handler.
4. Does the new state need a new panel GameObject, or does it reuse an existing one?

## Procedure

### 1. Add the enum value
In [PanelManager.cs](Assets/Scripts/PanelManager.cs), add the new value to the relevant enum. Place it next to logically adjacent states, not just at the end:
```csharp
public enum UIState
{
    Idle,
    Loading_Location,
    Loading_API,
    Success_WithData,
    Success_NoData,
    AwaitingPermission,   // ← new
    CaptureImage,
    Error
}
```

### 2. Update the SetState switch
The central `switch (state)` in `SetState(...)` must handle the new case. Toggle the relevant panels via the existing helper, mirror to the debug overlay, and log with the `[PanelManager]` tag.

### 3. Wire any new panel
- Add a `[SerializeField] private GameObject awaitingPermissionPanel;` field under `#region Inspector Configuration`.
- Drag the panel GameObject into the slot on the `PanelManager` component in the active scene (`RevisedLocationResolver.unity`).

### 4. Entry trigger
- If user-driven: add a button in the scene, expose `public void OnRequestPermissionClicked()` on `PanelManager`, and wire OnClick **in the Inspector** (not via `AddListener`).
- If callback-driven: call `SetState(UIState.AwaitingPermission)` from the existing callback site. Do not introduce events.

### 5. Exit paths
Every new state needs at least one exit transition. Spell them out before writing code:
- Success → `SetState(...)` to next state.
- Error → `SetState(UIState.Error)` with the verbatim failure copy from `copilot-instructions.md`.
- Back / cancel → `SetState(UIState.Idle)`.

### 6. Debug overlay
Echo the state change to [DebugOverlayController.cs](Assets/Scripts/DebugOverlayController.cs) so on-device demos are diagnosable.

### 7. Verify
- Enter Play Mode in `RevisedLocationResolver.unity`.
- Trigger every entry path.
- Trigger every exit path.
- Watch the on-device debug overlay show the transitions in order.

## Anti-patterns to avoid
- Toggling `panel.SetActive(...)` from outside `PanelManager`.
- Adding the state but forgetting one of the `switch` arms.
- Wiring the new button via `AddListener` instead of the Inspector.
