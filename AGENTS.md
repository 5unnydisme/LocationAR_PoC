# AGENTS.md

LocationAR_PoC — Unity 2023.2.22f1 mobile PoC for civic road-issue reporting.

**All agents (Copilot, Codex, etc.) must read [.github/copilot-instructions.md](.github/copilot-instructions.md) before acting.** Scoped rules live in [.github/instructions/](.github/instructions/) and reusable how-tos in [.github/skills/](.github/skills/).

## Hard rules summary
- **Learning Mode is ON** for `Assets/Scripts/**/*.cs` and `Assets/Resources/*.json` — show code in chat, do not edit those files. Override: user says "edit directly" or "apply it".
- **Planning first.** Ask at least one clarifying question before code.
- **Inspector-wired OnClick** — no `onClick.AddListener` for scene buttons.
- **No** async/await, **no** Newtonsoft, **no** new packages, **no** event buses.
- Active scene = `RevisedLocationResolver.unity`. Active data = `test.json` via `LocationJSONLoader`.
- Never delete `_BackUp` files. Never commit API keys.

Demos: **Mon** + **May 15**. Stability over features.
