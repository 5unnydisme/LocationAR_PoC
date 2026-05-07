---
name: gis-data-normalization
description: Use when matching device-reported road names to the infrastructure dataset, ingesting annotated GIS data, or handling multi-match / no-match cases.
---

# GIS / road-name normalisation

## Why this matters here
Google Maps may return `"MG Road"`, the dataset may say `"Mahatma Gandhi Marg"`, the user may be on a campus service road with no name at all. We need a deterministic, dependency-free matching ladder the user can read line-for-line. Exact GIS-to-project mapping is **not available** for this PoC — landmarks are secondary signals only.

## Normalisation pipeline (apply in order)
1. Trim and lowercase.
2. Collapse internal whitespace to a single space.
3. Strip punctuation: `,` `.` `'` `"` (keep `-` and `/` — they appear in Indian road names).
4. Expand common abbreviations from a single in-code map:
   - `rd` → `road`
   - `st` → `street`
   - `ave` → `avenue`
   - `extn` → `extension`
   - `marg` → `marg` (no-op, included for clarity)
   - `nh` → `national highway`
5. Token-sort only as last-resort tier (see ladder).

Keep the abbreviation map in **one** `static readonly Dictionary<string, string>` in the loader. Do not scatter.

## Matching ladder (stop at first hit)
1. **Exact** match on raw name (catches the happy path).
2. **Exact** match on normalised name.
3. **Substring** match (normalised, both directions).
4. **Token-set** match — split on whitespace, intersect; require ≥ N overlapping tokens. Make N a `[SerializeField]` with default 2.
5. **No match** → return the verbatim failure-state copy from `copilot-instructions.md`:
   `"This road is not yet included in the government database."`

**Do not** introduce Levenshtein, Jaro-Winkler, or any fuzzy-matching library. If a tier feels inadequate in practice, raise it in chat first.

## Multi-match
Return the **full list** of candidate `LocationData` entries. The UI surfaces all candidates per the project's failure-state rule:
> Multiple project matches: show a list (do not auto-pick).

## Schema validation when ingesting annotated GIS data
Checklist before merging a new dataset:
- [ ] All required fields present on every record.
- [ ] Field name spelling matches the C# POCO **exactly** (`JsonUtility` is case-sensitive).
- [ ] `latitude ∈ [-90, 90]`, `longitude ∈ [-180, 180]`. Reject records that fail.
- [ ] No empty `name` strings (the lookup key).
- [ ] Top-level wrapper object intact (`{ "locations": [...] }`).
- [ ] File placed in `Assets/Resources/` and loaded via `Resources.Load<TextAsset>("<name>")`.

If the annotation team delivers CSV, convert offline (Python / spreadsheet export) to JSON. **Do not** parse CSV at runtime.

## Citations
- [Unity `JsonUtility` (2023.2)](https://docs.unity3d.com/2023.2/Documentation/ScriptReference/JsonUtility.html)
- [Unity `Resources.Load` (2023.2)](https://docs.unity3d.com/2023.2/Documentation/ScriptReference/Resources.Load.html)
- [Google Geocoding response shape](https://developers.google.com/maps/documentation/geocoding/requests-geocoding)
- [Google Roads — Nearest Roads](https://developers.google.com/maps/documentation/roads/nearest)
