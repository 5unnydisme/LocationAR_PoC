---
name: learning-mode-explain
description: Use whenever the user asks for code in `Assets/Scripts/**/*.cs` or `Assets/Resources/*.json`. Operationalises the "show, don't edit; explain block-by-block" interaction style.
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

### 3. "Why this structure"
1–3 sentences explaining the file's overall shape and why it matches project conventions (MonoBehaviour, Inspector-driven, coroutine + callback, etc.).

### 4. Full file in one block
Always show the **entire file**, even for small changes. Use ` ```csharp ` or ` ```json `. The user copies the whole thing.

### 5. Block-by-block explanation
Group related lines into logical blocks:
- `#region Inspector Configuration` → why each field is serialized.
- Lifecycle methods (`Awake`, `Start`, `OnDestroy`) → why work happens here vs elsewhere.
- Coroutines → request shape, error branches, callback contract.
- Each block gets the **why**, not the what. Surface trade-offs.

### 6. Cite docs
Every Unity API → link to `https://docs.unity3d.com/2023.2/Documentation/ScriptReference/<API>.html`.
Every Android best practice → link to `https://developer.android.com/...`.
Every Google Maps API → link to `https://developers.google.com/maps/...`.
Quote one line from the doc when a behaviour is non-obvious.

### 7. Inspector checklist
End with:
```
What to type vs. what to wire in the Inspector
- Type:   the script above into Assets/Scripts/Foo.cs
- Wire:   - Foo.locationManager  →  LocationServiceManager (in scene)
          - Foo.statusLabel      →  TMP_Text on Canvas/StatusBar
          - SubmitButton.OnClick → Foo.OnSubmitClicked
```

## Override
The user can say **"edit directly"** or **"apply it"** to skip steps 3–7 for a single response and just have the agent edit the file.
