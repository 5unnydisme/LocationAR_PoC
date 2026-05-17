# LocationAR_PoC — Copilot Instructions

Geospatial civic-infrastructure reporting PoC in Unity. Hard demos: **Mon** and **May 15**.
Stability and clarity beat cleverness. When in doubt: ask, then plan, then code.

## Project at a glance
- Unity **2023.2.22f1** (see [ProjectSettings/ProjectVersion.txt](ProjectSettings/ProjectVersion.txt)).
- Mobile (Android primary). Built-in `UnityEngine.LocationService` for GPS, Google Maps Roads + Places APIs for road name resolution, JSON-driven dataset for project lookup, `WebCamTexture` for image capture, multipart `UnityWebRequest` for CV/complaint upload.
- Active scene: [Assets/Scenes/RevisedLocationResolver.unity](Assets/Scenes/RevisedLocationResolver.unity). Older [LocationResolver.unity](Assets/Scenes/LocationResolver.unity) is legacy.
- Active data layer: [LocationJSONLoader.cs](Assets/Scripts/LocationJSONLoader.cs) + [Assets/Resources/test.json](Assets/Resources/test.json).
- Legacy / parallel data layer: [InfrastructureAssetDatabase.cs](Assets/Scripts/InfrastructureAssetDatabase.cs) + [Assets/Resources/InfrastructureAssets.json](Assets/Resources/InfrastructureAssets.json). Do not delete; do not extend without asking.
- `_BackUp`-suffixed files (e.g. [PanelManager_BackUp.cs](Assets/Scripts/PanelManager_BackUp.cs)) are intentional history. **Never delete or "tidy".**

## Engineering philosophy
- Prioritise demo stability over architectural perfection.
- Avoid unnecessary refactors before deadlines.
- Prefer understandable Unity patterns over abstract systems.
- Maintain Inspector visibility wherever possible.
- Keep APIs modular and replaceable.
- Favour incremental iteration over large rewrites.

## Architecture constraints
- No premature abstraction. No interfaces "just in case".
- No new `*Manager` class if an existing one fits. Avoid manager proliferation.
- No DI containers, no MVVM frameworks, no Zenject / VContainer / MessagePipe / R3.
- Modify existing working systems incrementally; do not rewrite.
- Stability and clarity > scalability.

## UI workflow (Inspector-first)
- Wire button `OnClick` in the **Inspector**, not in code.
- Do **not** call `button.onClick.AddListener(...)` for buttons that exist in the scene at edit time. Runtime-instantiated buttons are the only allowed exception, and listeners must be removed in `OnDestroy`.
- Public handler methods exposed for the Inspector must be `public void Foo()` — parameterless, or use Unity's dynamic-parameter signatures — so they appear in the OnClick dropdown.
- Avoid `UnityEvent` chains, event buses, `SendMessage`/`BroadcastMessage`, or messaging frameworks unless a concrete need is argued in chat first. Default = direct method calls.
- Every button's behaviour must be traceable from the Inspector — no hidden subscriptions.

## Collaboration / workflow
- **Planning first.** Explain the approach before generating large code changes. See the `planning-task-breakdown` skill.
- Always ask at least one clarifying question before writing code (see Learning Mode below).
- Prefer incremental modifications over sweeping refactors.
- Break complex systems into smaller, individually explainable steps.
- Clarity and maintainability beat abstraction.

## Learning Mode (default ON for `Assets/Scripts/**/*.cs` and `Assets/Resources/*.json`)
The user is learning. **Do not edit those files directly.** Show code in chat only — they will type it themselves. The full ritual lives in [.github/instructions/learning-mode.instructions.md](.github/instructions/learning-mode.instructions.md). Override phrase: **"edit directly"** or **"apply it"** suspends Learning Mode for a single response.

## Tech stack (do not deviate without asking)
- All scripts are `MonoBehaviour`. No namespaces. No `.asmdef`.
- Coroutines + `System.Action<T>` callbacks. **No** `async/await`, **no** `Task`, **no** UniTask.
- Networking: `UnityEngine.Networking.UnityWebRequest` only. **No** `HttpClient`.
- JSON: `UnityEngine.JsonUtility` only. **No** Newtonsoft. JSON field names are **case-sensitive** and must match POCO fields exactly. (See the existing `assetId` vs `assetID` mismatch in [InfrastructureAsset.cs](Assets/Scripts/InfrastructureAsset.cs) — flagged, not fixed.)
- No new packages without explicit approval.

## Project conventions
- `[SerializeField] private` fields with `[Header]` and `[Tooltip]`, grouped under `#region Inspector Configuration`.
- Tagged `Debug.Log` (e.g. `[GoogleMapsService] ...`) mirrored to the on-device [DebugOverlayController.cs](Assets/Scripts/DebugOverlayController.cs).
- Status enums colocated with their owning `MonoBehaviour` (e.g. `LocationServiceState`, `CameraStatus`, `UIState`).
- Resources loaded by name without extension: `Resources.Load<TextAsset>("test")`.

## Failure-state copy (use verbatim)
- No road from Maps API: `"Unable to detect road. Please try again."`
- Road has no dataset entry: `"This road is not yet included in the government database."`
- Multiple project matches: show a list (do not auto-pick).
- CV service fails: allow complaint submission anyway.

## Secrets
- Never commit Google Maps `apiKey` or real CV/complaint endpoint URLs.
- Recommended: load from gitignored `Assets/Resources/secrets.json` via `JsonUtility`, with `PlayerPrefs` for device overrides. Restrict Maps keys by Android package + SHA-1 in Google Cloud Console. See the `mobile-best-practices` skill for details.

## Integration boundaries (other teammates own these)
- **Annotated GIS dataset** → keep ingestion in a thin loader; see `gis-data-normalization` skill.
- **Real CV API** → adapter in [ReportSubmissionManager.cs](Assets/Scripts/ReportSubmissionManager.cs); see `swap-mock-cv-for-real` skill.
- **Complaint API** → new thin adapter when the contract lands; do not inline into [PanelManager.cs](Assets/Scripts/PanelManager.cs).

## Skills (invoke when the task matches)
- `learning-mode-explain` — code presentation rules (chat-only, prose "why" + targeted snippet, full file only on request).
- `planning-task-breakdown` — milestone-aware planning with blockers + owners.
- `mobile-best-practices` — Android permissions, GPS battery, network resilience, secrets, PII.
- `unity-api-reference` — cite docs.unity3d.com for Unity 2023.2.
- `gis-data-normalization` — road-name matching, GIS ingestion.
- `add-json-field`, `add-uistate`, `add-network-call`, `swap-mock-cv-for-real`, `prep-demo-build` — task templates.
