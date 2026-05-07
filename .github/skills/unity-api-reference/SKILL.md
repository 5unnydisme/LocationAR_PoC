---
name: unity-api-reference
description: Use whenever a Unity API is mentioned in a code suggestion or explanation. Ensures every Unity API is cited with a version-correct docs link and a one-line behaviour summary, and that obsolete usages are flagged.
---

# Unity API reference discipline

Project Unity version: **2023.2.22f1** (see `ProjectSettings/ProjectVersion.txt`).

## When to invoke
Any time a Unity type, method, attribute, or coroutine helper appears in a code block or explanation.

## Required for each Unity API mentioned
1. **Exact API name**, e.g. `UnityWebRequest.SendWebRequest`, `Input.location.Start`, `WebCamTexture.GetPixels32`.
2. **Doc link** under the project's version:
   `https://docs.unity3d.com/2023.2/Documentation/ScriptReference/<Path>.html`
3. **One-line behaviour summary** in your own words (do not copy long doc paragraphs).
4. **Deprecation flag**: if the doc page is marked `[Obsolete]` or the 2023.2 docs say "Use X instead", call it out and use the replacement.

## Common pitfalls to flag
- `WWW` — obsolete, use `UnityWebRequest`.
- `UnityWebRequest.isHttpError` / `isNetworkError` — obsolete, use `result == UnityWebRequest.Result.Success`.
- `Application.LoadLevel` — obsolete, use `SceneManager.LoadScene`.
- `OnLevelWasLoaded` — obsolete, use `SceneManager.sceneLoaded` event (only if event is justified, otherwise just initialise in `Start`).
- `Find`/`FindObjectOfType` in performance-sensitive code — works but should be avoided per project conventions.

## Format example
> `UnityWebRequest.timeout` ([docs.unity3d.com/2023.2](https://docs.unity3d.com/2023.2/Documentation/ScriptReference/Networking.UnityWebRequest-timeout.html)) — sets the maximum number of seconds the request will wait before aborting. Defaults to 0 (no timeout). Mandatory for mobile networks.

## When the user is reading along
The user wants to read the source. Prefer linking the doc page over paraphrasing more than one sentence.
