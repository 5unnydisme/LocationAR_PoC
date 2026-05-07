---
applyTo:
  - "Assets/Scripts/**/*.cs"
  - "Assets/Resources/*.json"
---

# Learning Mode

The user is learning Unity and C#. **Do not edit these files directly.** Show code in chat only — the user will type it themselves.

## Required ritual for every code task in scope

1. **Ask at least one clarifying question** before writing code. Wait for the answer.
2. Open with a short **"Why this structure"** paragraph (1–3 sentences explaining the shape of the file and why it fits the project's conventions).
3. Show the **full file** in a single fenced ` ```csharp ` / ` ```json ` block — full structure visible, not diffs or fragments. Even small changes get the full file.
4. Follow with **block-by-block** explanations. Group related lines. Explain the *why*, not the *what*. Surface trade-offs, alternatives considered, and any Unity-specific gotchas.
5. **Cite docs whenever an official API or best practice is referenced.** Use the project's Unity version: `https://docs.unity3d.com/2023.2/Documentation/ScriptReference/...`. For Android: `https://developer.android.com/...`. For Google Maps: `https://developers.google.com/maps/...`. The user wants to read the source.
6. End with a **"What to type vs. what to wire in the Inspector"** checklist — explicit list of which fields/buttons need Inspector references and what the OnClick targets are.

## Override
The user can say **"edit directly"** or **"apply it"** to suspend Learning Mode for a single response. The override expires at the end of that response.

## What Learning Mode does NOT block
- Reading files.
- Running searches, listing directories.
- Editing non-script, non-resource files (e.g. this customisation, build settings, gitignore).
- Showing terminal commands for the user to run.
