---
name: swap-mock-cv-for-real
description: Use when replacing the mock CV response in PanelManager / ReportSubmissionManager with a real CV API endpoint. Preserves the offline mock fallback for demos.
---

# Swap mock CV for real CV API

## Pre-flight clarifying questions
1. Is the real CV endpoint contract finalised? (request shape, response JSON schema, auth)
2. Should the mock remain available behind an Inspector toggle (for offline demos), or be removed?
3. What does "CV failure" surface to the user — error panel, or skip-to-complaint per the project's failure rule?
4. Any rate limits / cold-start latency we need to mask with the loading spinner?

## Procedure

### 1. Mirror the real response in `CVAnalysisResult`
Update [LocationDataModels.cs](Assets/Scripts/LocationDataModels.cs) so `CVAnalysisResult` field names match the real JSON exactly. JsonUtility is case-sensitive.

### 2. Add an Inspector toggle for mock vs live
On [ReportSubmissionManager.cs](Assets/Scripts/ReportSubmissionManager.cs):
```csharp
[Header("CV Mode")]
[Tooltip("If true, returns a fake response after fakeDelaySeconds. Used for offline demos.")]
[SerializeField] private bool useMockCV = false;
[SerializeField] private float fakeDelaySeconds = 3f;
```
Default `false` once the real endpoint is reliable; flip to `true` for offline demos.

### 3. Branch in the submit coroutine
```csharp
if (useMockCV)
{
    yield return new WaitForSeconds(fakeDelaySeconds);
    onDone?.Invoke(BuildFakeResult());
    yield break;
}
// real UnityWebRequest path
```
Keep `BuildFakeResult()` as a private method so demo behaviour is reproducible.

### 4. Real call
Use the canonical pattern from the `add-network-call` skill. Multipart POST already in place — preserve `WWWForm` + `AddBinaryData` shape.

### 5. Failure handling
Per `copilot-instructions.md`: **CV service fails → allow complaint submission anyway.**
- On failure, surface a non-blocking notice ("CV unavailable — submit anyway?") and transition `PanelManager` to a state that still permits submission.
- Do **not** block the user.

### 6. Secrets
Move `cvApiUrl` out of source. Load from `Resources/secrets.json` (gitignored) or `PlayerPrefs` override. See `mobile-best-practices` skill.

### 7. Verify
- Live mode: real endpoint round-trip, results render in `CVResultPopulator`.
- Mock mode: 3 s delay, fake result renders.
- Live mode + airplane mode: failure copy + "submit anyway" flow works.

## Out of scope for the swap
- Adding new defect types to the UI (potholes, garbage, animals) — those are separate UI work, do not bundle.
- Changing the multipart wire format unless the ML engineer requires it.
