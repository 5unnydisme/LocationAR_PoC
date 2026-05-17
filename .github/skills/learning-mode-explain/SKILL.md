---
name: learning-mode-explain
description: Use whenever the user asks for code in `Assets/Scripts/**/*.cs` or `Assets/Resources/*.json`. Operationalises the "show, don't edit; explain the why and the targeted snippet" interaction style.
---

# Learning-mode explanation ritual

This skill mirrors [.github/instructions/learning-mode.instructions.md](../../instructions/learning-mode.instructions.md) — read that first.

## Step-by-step

### 1. Clarify
Ask **at least one** clarifying question. Examples that count:
- "Should this retry on any failure or only on transient HTTP/timeout?"
- "Do you want this field shown on the location panel as well, or just in the report flow?"
- "Should the new button live on the existing `MainPanel` or a new sub-panel?"

Wait for the answer before any code.

### 2. Plan (if non-trivial)
Use the `planning-task-breakdown` skill template if the change spans more than one method or one file.

### 3. "Why this change"
1–3 sentences: what problem the edit solves, what alternative was rejected, any Unity/Android/Maps gotcha worth flagging. No code in this paragraph.

### 4. Targeted snippet
Show **only the changed or added code** in one fenced ` ```csharp ` / ` ```json ` block. Include the enclosing method signature only when that's the cheapest way to locate the edit. No BEFORE/AFTER pairs. No diff markers. No full file unless the user typed **"show the whole file"** / **"full file"**.

### 5. Where it goes
In prose, name the file + method or region the snippet belongs in (e.g. *"replace the body of `FetchRoadName()` in [Assets/Scripts/GoogleMapsService.cs](Assets/Scripts/GoogleMapsService.cs)"*).

### 6. Cite docs
Every Unity API → link to `https://docs.unity3d.com/2023.2/Documentation/ScriptReference/<API>.html`.
Every Android best practice → link to `https://developer.android.com/...`.
Every Google Maps API → link to `https://developers.google.com/maps/...`.
Quote one line from the doc when a behaviour is non-obvious.

### 7. Inspector checklist
When Inspector fields or OnClick targets changed, end with:
```
What to type vs. what to wire in the Inspector
- Type:   the snippet above into Assets/Scripts/Foo.cs
- Wire:   - Foo.locationManager  →  LocationServiceManager (in scene)
          - Foo.statusLabel      →  TMP_Text on Canvas/StatusBar
          - SubmitButton.OnClick → Foo.OnSubmitClicked
```

## Override
- **"show the whole file"** / **"full file"** → show the entire file in one block this response.
- **"just the code"** → skip step 3; show only the snippet and the "where it goes" line.
- **"edit directly"** / **"apply it"** → skip steps 3–7; agent edits the file.
