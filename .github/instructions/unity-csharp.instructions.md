---
applyTo:
  - "Assets/Scripts/**/*.cs"
---

# Unity C# conventions for this project

## Class shape
- `public class Foo : MonoBehaviour` — no namespaces, no abstract base classes, no interfaces unless an existing one is being implemented.
- One `MonoBehaviour` per file. Colocate small enums/structs/POCOs that are only used by that behaviour.
- File name matches class name exactly (Unity requirement).

## Inspector configuration
- All Inspector-tunable fields are `[SerializeField] private`, never `public`.
- Group them inside a `#region Inspector Configuration` at the top of the class.
- Always add `[Header("...")]` for the section and `[Tooltip("...")]` per field.
- Provide sensible defaults so a freshly added component works without manual tuning.

```csharp
#region Inspector Configuration
[Header("Network")]
[Tooltip("Request timeout in seconds. Mobile networks vary widely.")]
[SerializeField] private int requestTimeoutSeconds = 10;
#endregion
```

## Async style
- **Coroutines + `System.Action<T>` callbacks only.** No `async`/`await`, no `Task`, no UniTask.
- Coroutine signature: `private IEnumerator DoThing(string input, System.Action<Result> onDone)`.
- Always provide a callback path for both success and failure (either two callbacks, or a result struct with a status field).

## Logging
- Tagged `Debug.Log($"[Foo] message")` so logs are filterable in Android Logcat.
- Errors → `Debug.LogError`. Warnings → `Debug.LogWarning`.
- Mirror user-relevant messages to the on-device debug overlay via `DebugOverlayController` if one is referenced.

## Disposing UnityWebRequest
- Wrap with `using (var req = UnityWebRequest.Get(url)) { ... }` so the native handle is freed even on early-out paths.
- See [docs.unity3d.com/2023.2 UnityWebRequest](https://docs.unity3d.com/2023.2/Documentation/ScriptReference/Networking.UnityWebRequest.html).

## Things to avoid
- `FindObjectOfType` / `GameObject.Find` in hot paths — assign references in the Inspector.
- LINQ in `Update()` or per-frame code — fine for one-shot loaders.
- `static` mutable state — prefer a serialized reference to a single scene instance.
- New `*Manager` classes when an existing one fits.
