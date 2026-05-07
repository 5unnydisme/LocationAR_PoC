---
name: prep-demo-build
description: Use the day before each demo (Mon, May 15). Pre-flight checklist covering secrets, scene, permissions, logging, fallback strings, and a dry-run.
---

# Pre-demo build checklist

Run through this **the day before**, not the morning of. Each item is a hard gate.

## Secrets
- [ ] `Assets/Resources/secrets.json` exists locally and is in `.gitignore`.
- [ ] `googleMapsApiKey` is the production key, restricted in Google Cloud Console to the Android package + release SHA-1.
- [ ] `cvApiUrl` and `complaintApiUrl` point at the agreed staging/prod endpoints (confirm with teammates).
- [ ] No API keys appear in `git diff` of the last 10 commits — `git log -p -S "AIza" | head` returns empty.

## Scene + build settings
- [ ] Active scene in Build Settings = `Assets/Scenes/RevisedLocationResolver.unity`. Legacy `LocationResolver.unity` is **unchecked**.
- [ ] Player Settings → Other Settings → Package Name matches the one whitelisted on the Maps API key.
- [ ] Player Settings → Other Settings → Minimum API Level = Android 7.0 (API 24) or as agreed.
- [ ] Player Settings → Publishing Settings → uses release keystore (not debug).

## Permissions
- [ ] AndroidManifest declares `ACCESS_FINE_LOCATION` and `CAMERA`.
- [ ] Runtime permission requests fire on Check-In and Report Issue respectively (test on a freshly installed APK).
- [ ] "Deny forever" path surfaces a UI prompt (not a silent failure).

## Logging
- [ ] Tag filter ready in Android Logcat: `Unity` plus the `[GoogleMapsService]`, `[ReportSubmissionManager]`, `[PanelManager]`, `[LocationServiceManager]` tags.
- [ ] On-device `DebugOverlayController` toggles **ON** for demo runs (or off if the demo audience shouldn't see it — confirm with user).

## Fallback strings (verify each appears verbatim)
- [ ] No road from Maps: `"Unable to detect road. Please try again."`
- [ ] Road not in dataset: `"This road is not yet included in the government database."`
- [ ] Multi-match: list rendered, no auto-pick.
- [ ] CV failure: complaint submission still allowed.

## Dry runs
- [ ] **Online dry run** in the actual demo location (or as close as possible).
- [ ] **Airplane-mode dry run** — every screen should reach a graceful failure state, no soft-lock.
- [ ] **Permission-denied dry run** — deny location, deny camera, observe UI.
- [ ] **CV-mock dry run** — flip `useMockCV = true` and confirm the demo works fully offline as a backup plan.

## Device
- [ ] Battery > 50% on the demo device.
- [ ] Spare device (or APK on a USB stick) ready as backup.
- [ ] Charging cable + power bank packed.

## Day-of-demo
- [ ] Open the `RevisedLocationResolver` scene in Editor as a backup demo path.
- [ ] Hot-fix branch is clean — no uncommitted experimental code on the demo branch.
