---
applyTo:
  - "Assets/Scripts/LocationDataModels.cs"
  - "Assets/Scripts/InfrastructureAsset.cs"
  - "Assets/Resources/*.json"
---

# Data model rules (JsonUtility-bound)

## JsonUtility constraints to remember
- Field names in the JSON file **must match the C# field names exactly**, including case. `assetId` ≠ `assetID`.
- Only `[System.Serializable]` POCOs are deserialised.
- **No `Dictionary<,>`**, no polymorphism, no nullable value types, no properties — fields only.
- Top-level JSON must be an object, not an array. Wrap arrays in a container class (already done: `LocationCollection { locations[] }`, `InfrastructureAssetCollection { assets[] }`).
- Reference: [docs.unity3d.com/2023.2 JsonUtility](https://docs.unity3d.com/2023.2/Documentation/ScriptReference/JsonUtility.html).

## Adding or renaming a field — checklist
1. Add the field to the POCO with `public` visibility (JsonUtility ignores private fields by default).
2. Update **every** entry in the corresponding JSON file with the same key spelling.
3. Update any populator script (`PanelPopulator`, `CVResultPopulator`) that pulls from the model.
4. If the field is shown in UI, wire the new TMP reference in the Inspector.
5. Use the `add-json-field` skill for the full procedure.

## Known issue
[InfrastructureAsset.cs](Assets/Scripts/InfrastructureAsset.cs) declares `assetID` while [Assets/Resources/InfrastructureAssets.json](Assets/Resources/InfrastructureAssets.json) uses `assetId`. The field will silently deserialise as empty. **Flag in chat; do not fix without user approval** — the legacy data layer may be retired.

## Active vs legacy
- Active: [LocationDataModels.cs](Assets/Scripts/LocationDataModels.cs) ↔ [test.json](Assets/Resources/test.json) via [LocationJSONLoader.cs](Assets/Scripts/LocationJSONLoader.cs).
- Legacy: [InfrastructureAsset.cs](Assets/Scripts/InfrastructureAsset.cs) ↔ [InfrastructureAssets.json](Assets/Resources/InfrastructureAssets.json) via [InfrastructureAssetDatabase.cs](Assets/Scripts/InfrastructureAssetDatabase.cs).
- Default new work to the active pair unless the user says otherwise.
