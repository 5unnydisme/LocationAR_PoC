---
name: planning-task-breakdown
description: Use before any non-trivial change, integration, or feature. Produces a milestone-aware plan with blockers, owners, and explicit out-of-scope. Skip only for changes < 15 minutes and unambiguous.
---

# Planning / task breakdown

## Why
The project has hard demos (Mon + May 15), parallel teammates (annotation team, ML engineer, complaint-API teammate), and staged integrations. A short plan up front prevents scope creep and surfaces blockers early.

## Output template
```
Goal: <one sentence>
Milestone fit: [Mon demo | May 15 | post-deadline]

Blockers (with owner):
  - <blocker> — <annotation team | ML engineer | complaint-API teammate | self>

Dependencies (in order):
  1. ...
  2. ...

Do now (no blockers):
  - ...

Wait-for (blocked):
  - ...

Out of scope for this task:
  - ...

Risk / fallback:
  - <what breaks the demo if this fails, and the fallback>
```

## Rules
- Always separate **Do now** from **Wait-for-teammate**.
- If anything drifts from the demo narrative, label it **scope creep** and put it under **Out of scope**.
- Prefer the smallest reversible step that proves the next thing.
- For tasks < 15 minutes and unambiguous, write `No plan needed — <one-line reason>` and proceed (still in Learning Mode).
- Plans live in chat. Do not create planning markdown files unless the user asks.
- Missing inputs listed as blockers, not assumptions. Surface assumptions as blockers with a question mark if the answer is unknown.

## Worked example
```
Goal: Make Maps lookup retry once on timeout before showing failure copy.
Milestone fit: Mon demo

Blockers: none.

Dependencies:
  1. Confirm timeout value with user (current default 10 s).

Do now:
  - Add retryCount [SerializeField], default 1.
  - Wrap GetActiveLocationName coroutine in retry loop on timeout / 5xx only.
  - Keep existing callback contract.

Wait-for: none.

Out of scope:
  - Exponential backoff.
  - Retry on Place Details step (separate ticket).

Risk / fallback:
  - If retry hangs the UI, fall back to current single-shot behaviour by setting retryCount = 0 in the Inspector.
```
