---
applyTo:
  - "Assets/Scripts/GoogleMapsServices.cs"
  - "Assets/Scripts/ReportSubmissionManager.cs"
---

# Networking rules

## UnityWebRequest pattern (canonical)
```csharp
private IEnumerator SendRequest(string url, System.Action<Result> onDone)
{
    using (var req = UnityWebRequest.Get(url))
    {
        req.timeout = requestTimeoutSeconds;       // [SerializeField], default 10
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[Foo] {req.error} ({req.responseCode})");
            onDone?.Invoke(Result.Failure(req.error));
            yield break;
        }

        onDone?.Invoke(Result.Success(req.downloadHandler.text));
    }
}
```

Key requirements:
- `using` block — see [docs.unity3d.com/2023.2 UnityWebRequest](https://docs.unity3d.com/2023.2/Documentation/ScriptReference/Networking.UnityWebRequest.html).
- `timeout` is mandatory; default to 10 s, surface as `[SerializeField]`.
- Always check `req.result == UnityWebRequest.Result.Success`. Do **not** use the obsolete `isHttpError`/`isNetworkError` booleans.
- Both success and failure paths invoke the callback exactly once.

## Retry policy
- Retry only on `ConnectionError` or `ProtocolError` with 5xx/timeout. Never on 4xx.
- Max 2 retries with linear backoff (1 s, 2 s). No exponential — keeps demo behaviour predictable.
- Retries must be opt-in per call site, not automatic.

## Offline detection
- Before any request, check `Application.internetReachability != NetworkReachability.NotReachable`. See [docs.unity3d.com/2023.2 Application.internetReachability](https://docs.unity3d.com/2023.2/Documentation/ScriptReference/Application-internetReachability.html). Use the project's failure-state copy if offline.

## Secrets
- Never inline an `apiKey` literal. Load from `Resources/secrets.json` (gitignored) via `JsonUtility`.
- Allow a `PlayerPrefs` override for on-device testing.
- Maps keys must be restricted by Android package name + SHA-1 in Google Cloud Console — see `mobile-best-practices` skill.

## Multipart upload
- Use `WWWForm.AddBinaryData(...)` (already in use). Do not switch to `MultipartFormDataSection` arrays without justification.
- Always include `Content-Type` correctly via `WWWForm` — do not set headers manually.

## Failure-state copy (verbatim)
- Maps no road: `"Unable to detect road. Please try again."`
- CV down: allow complaint submission anyway and surface a non-blocking notice.
