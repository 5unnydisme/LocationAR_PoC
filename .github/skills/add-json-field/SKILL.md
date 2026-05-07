---
name: add-json-field
description: Use when adding, renaming, or removing a field on the location/infrastructure data models. Ensures POCO ↔ JSON ↔ populator ↔ UI all stay in sync given JsonUtility's strict matching.
---

# Add a JSON field (POCO + data + populator + UI)

## Pre-flight clarifying questions
1. Active dataset (`test.json` + `LocationDataModels.cs`) or legacy (`InfrastructureAssets.json` + `InfrastructureAsset.cs`)?
2. Which sub-object does the field belong to (top-level `LocationData`, nested `InfrastructureDetails`, or a `MaintenanceRecord` entry)?
3. Should the field appear in the existing location panel, the report flow, or both?
4. Is the field optional (some entries may omit it) or required for every record?

## Procedure

### 1. POCO
Add the field to the `[Serializable]` class in [LocationDataModels.cs](Assets/Scripts/LocationDataModels.cs):
```csharp
public string contractorPhone;
```
- Use the **exact** spelling that will appear in JSON. `JsonUtility` is case-sensitive.
- Field must be `public` (or `[SerializeField] private` — public is the project convention here).

### 2. JSON
Update **every** record in [Assets/Resources/test.json](Assets/Resources/test.json) — if you skip records, `JsonUtility` will silently default them to `""` / `0` and you won't notice until the UI shows blanks.

### 3. Populator
Add a TMP reference + assignment in [PanelPopulator.cs](Assets/Scripts/PanelPopulator.cs):
```csharp
[SerializeField] private TMP_Text contractorPhoneLabel;
// ...
contractorPhoneLabel.text = data.contractorPhone ?? "";
```
The `?? ""` guard matches the existing null-coalescing pattern.

### 4. Inspector wiring
- Drag the new TMP_Text into the `contractorPhoneLabel` slot on the `PanelPopulator` component.
- If it's a new label, also create the GameObject under the existing panel and parent it correctly.

### 5. Verify
- Enter Play Mode in the active scene (`RevisedLocationResolver.unity`).
- Trigger the Check-In flow with a location whose record has the new field set.
- Confirm the label populates. If blank, re-check spelling parity (POCO ↔ JSON).

## Cautionary tale
[InfrastructureAsset.cs](Assets/Scripts/InfrastructureAsset.cs) declares `assetID` while the JSON uses `assetId`. `JsonUtility` silently leaves the field empty. **This is exactly the bug this skill exists to prevent.**

## Citations
- [Unity `JsonUtility.FromJson` (2023.2)](https://docs.unity3d.com/2023.2/Documentation/ScriptReference/JsonUtility.FromJson.html)
- [Unity scripting `Serializable` rules (2023.2)](https://docs.unity3d.com/2023.2/Documentation/ScriptReference/SerializeField.html)
