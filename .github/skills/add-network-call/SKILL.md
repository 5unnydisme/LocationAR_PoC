---
name: add-network-call
description: Use when adding a new HTTP request to a manager class. Provides the canonical UnityWebRequest coroutine shape, secret retrieval, error mapping, and callback contract used across the project.
---

# Add a UnityWebRequest call

## Pre-flight clarifying questions
1. GET or POST? If POST, JSON body or multipart form?
2. What's the success payload shape? (will need a `[Serializable]` POCO if JSON)
3. Should it retry on transient failures, or fail-fast?
4. Where does the URL/secret come from — config asset, `Resources/secrets.json`, or `PlayerPrefs` override?
5. What's the user-visible failure copy if the call fails? (use verbatim copy from `copilot-instructions.md` when one applies)

## Canonical pattern

```csharp
[Header("Network")]
[Tooltip("Request timeout in seconds.")]
[SerializeField] private int requestTimeoutSeconds = 10;

[Tooltip("Number of retries on transient errors (timeout / 5xx). Set 0 to disable.")]
[SerializeField] private int retryCount = 1;

private IEnumerator FetchSomething(string url, System.Action<FetchResult> onDone)
{
    if (Application.internetReachability == NetworkReachability.NotReachable)
    {
        Debug.LogWarning("[Foo] Offline — skipping request.");
        onDone?.Invoke(FetchResult.Failure("offline"));
        yield break;
    }

    int attempt = 0;
    while (true)
    {
        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = requestTimeoutSeconds;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                onDone?.Invoke(FetchResult.Success(req.downloadHandler.text));
                yield break;
            }

            bool transient =
                req.result == UnityWebRequest.Result.ConnectionError ||
                (req.result == UnityWebRequest.Result.ProtocolError && req.responseCode >= 500);

            if (!transient || attempt >= retryCount)
            {
                Debug.LogError($"[Foo] {req.error} ({req.responseCode}) after {attempt + 1} attempt(s)");
                onDone?.Invoke(FetchResult.Failure(req.error));
                yield break;
            }

            attempt++;
            Debug.LogWarning($"[Foo] Retry {attempt}/{retryCount} after {req.error}");
            yield return new WaitForSeconds(attempt); // 1s, 2s, ...
        }
    }
}
```

## Required elements
- `using` block — disposes the native handle, see [docs.unity3d.com/2023.2 UnityWebRequest](https://docs.unity3d.com/2023.2/Documentation/ScriptReference/Networking.UnityWebRequest.html).
- `timeout` set from a `[SerializeField]`.
- `Application.internetReachability` precheck — see [docs.unity3d.com/2023.2](https://docs.unity3d.com/2023.2/Documentation/ScriptReference/Application-internetReachability.html).
- `result == UnityWebRequest.Result.Success` (not the obsolete `isHttpError`/`isNetworkError`).
- Tagged `[Foo]` log on every error/retry path.
- Callback invoked **exactly once** on every code path.
- Retries only on `ConnectionError` or 5xx. Never on 4xx.
- Linear backoff (no exponential — predictable for demo).

## POST + multipart (image upload)
Already in [ReportSubmissionManager.cs](Assets/Scripts/ReportSubmissionManager.cs). Mirror its `WWWForm` + `AddBinaryData` shape rather than reaching for `MultipartFormDataSection[]`.

## Secrets
- Never inline the URL or key. Pull from a `SecretsProvider` (see `mobile-best-practices` skill) or `PlayerPrefs.GetString("...")` for device overrides.
- Demos use `Resources/secrets.json` (gitignored).

## Result type pattern
```csharp
public struct FetchResult
{
    public bool ok;
    public string body;
    public string error;
    public static FetchResult Success(string b) => new FetchResult { ok = true, body = b };
    public static FetchResult Failure(string e) => new FetchResult { ok = false, error = e };
}
```
Colocate with the `MonoBehaviour` that owns the call (project convention).
