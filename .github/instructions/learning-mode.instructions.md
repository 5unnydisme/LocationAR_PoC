---
applyTo:
  - "Assets/Scripts/**/*.cs"
  - "Assets/Resources/*.json"
---

# Learning Mode

The user is learning Unity and C#. **Do not edit these files directly.** Show code in chat only — the user will type it themselves.

## Default ritual for every code task in scope

1. **Ask at least one clarifying question** before writing code. Wait for the answer.
2. Write a **"Why this change"** paragraph (1–3 sentences): what problem it solves, what alternative was rejected, any Unity-specific gotcha.
3. Show **only the changed or added code** in a single fenced ` ```csharp ` / ` ```json ` block. No BEFORE/AFTER pairs, no diff markers, no surrounding unchanged context beyond the enclosing signature when that's needed to locate the edit.
4. State in prose **where it goes** — file path + method or region (e.g. *"inside `FetchRoadName()`, replace the body"* or *"add to `#region Inspector Configuration`"*).
5. **Cite docs** whenever an official API or best practice is referenced. Use the project's Unity version: `https://docs.unity3d.com/2023.2/Documentation/ScriptReference/...`. For Android: `https://developer.android.com/...`. For Google Maps: `https://developers.google.com/maps/...`.
6. End with a **"What to type vs. what to wire in the Inspector"** checklist whenever Inspector fields or OnClick targets changed.

Never show the whole file by default. Never use BEFORE/AFTER pairs. One cohesive concern per snippet.

## Mode override phrases (single response)

The user can flip behaviour mid-task by typing one of these phrases. Each override expires at the end of the agent's next response.

| Phrase | Effect |
|---|---|
| **"show the whole file"** / **"full file"** | Show the entire file in one fenced block for this response. |
| **"just the code"** | Skip the "Why this change" paragraph; show only the snippet and the "where it goes" line. |
| **"edit directly"** or **"apply it"** | Suspend Learning Mode entirely — agent edits the files. |

## What Learning Mode does NOT block
- Reading files.
- Running searches, listing directories.
- Editing non-script, non-resource files (e.g. this customisation, build settings, gitignore).
- Showing terminal commands for the user to run.
