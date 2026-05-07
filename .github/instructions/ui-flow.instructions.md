---
applyTo:
  - "Assets/Scripts/PanelManager.cs"
  - "Assets/Scripts/PanelPopulator.cs"
  - "Assets/Scripts/CVResultPopulator.cs"
  - "Assets/Scripts/DebugOverlayController.cs"
---

# UI flow rules

## Inspector OnClick (hard rule)
- Buttons existing in the scene at edit time are wired via the **Inspector OnClick list**.
- Do **not** generate `someButton.onClick.AddListener(...)` in `Awake()`/`Start()` for these buttons.
- Handler methods exposed for designers must be:
  - `public void`
  - parameterless (or use Unity's dynamic-parameter signatures: `int`, `float`, `bool`, `string`, `Object`)
  - on a `MonoBehaviour` that lives on, or is referenced from, the button's GameObject
- Runtime-instantiated buttons (e.g. dynamic list rows for multi-match results) are the **only** allowed exception. In that case:
  - Add the listener immediately after `Instantiate`.
  - Remove it in `OnDestroy` of the row component to avoid leaks.

## State machine
- All UI transitions go through `PanelManager.SetState(UIState)`. Other scripts must not toggle `panel.SetActive(...)` directly.
- Adding a new state → use the `add-uistate` skill and update every `switch` on `UIState`.
- `ReportFlowState` is a sub-state-machine inside the report flow; same rules apply.

## Populators
- `PanelPopulator` and `CVResultPopulator` are passive: they accept a data object via a `public void Populate(...)` call and do nothing else.
- Populators do **not** initiate state transitions, network calls, or scene changes.

## Avoid
- `UnityEvent` chains beyond the `Button.onClick` that ships with uGUI.
- Event buses, pub/sub, `SendMessage`, `BroadcastMessage`.
- "Manager of managers" patterns.
- Hidden subscriptions — every behaviour must be traceable from the Inspector.

## Debug overlay
- All major user-visible state changes should be echoed to `DebugOverlayController` so on-device demos are diagnosable without a laptop.
