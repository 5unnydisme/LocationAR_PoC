---
name: mobile-best-practices
description: Reference for Android mobile concerns — runtime permissions, GPS battery/accuracy, network resilience, secret handling, PII for GPS+photos. Cite when these topics come up.
---

# Mobile best practices (Android, Unity 2023.2)

## 1. Runtime permissions
Android 6.0+ requires runtime permission requests for `ACCESS_FINE_LOCATION` and `CAMERA`. Unity's helper: `UnityEngine.Android.Permission`.

- Request **just-in-time**, not at startup. Ask for location when the user taps "Check-In", camera when they tap "Report Issue".
- If `Permission.HasUserAuthorizedPermission(Permission.FineLocation)` is false, call `Permission.RequestUserPermission(Permission.FineLocation)`.
- Handle "Denied forever" — if the second request is also denied without a system dialog appearing, surface a UI prompt directing the user to system settings. There is no Unity API to open the app settings page; use `AndroidJavaObject` to launch `Settings.ACTION_APPLICATION_DETAILS_SETTINGS` (one-line snippet, document why).
- Show **rationale UI** before re-requesting a previously denied permission.

Citations:
- [Unity `Permission` API (2023.2)](https://docs.unity3d.com/2023.2/Documentation/ScriptReference/Android.Permission.html)
- [Android: Request runtime permissions](https://developer.android.com/training/permissions/requesting)
- [Android: Permission rationale](https://developer.android.com/training/permissions/requesting#explain)

## 2. GPS — battery vs accuracy
`Input.location.Start(desiredAccuracyInMeters, updateDistanceInMeters)`:
- For this PoC, `desiredAccuracyInMeters = 10f`, `updateDistanceInMeters = 5f` is a reasonable balance. Tighter values (e.g. 1 m) trigger fused-location high-accuracy mode and drain battery quickly.
- **Always** call `Input.location.Stop()` once a fix is obtained or when leaving the report flow. Active GPS can drain ~10–20% per hour.
- Treat `Input.location.lastData.horizontalAccuracy > 50 m` as "Poor" — surface in UI, and offer a re-try rather than blindly using the value for road lookup.

Citations:
- [Unity `LocationService` (2023.2)](https://docs.unity3d.com/2023.2/Documentation/ScriptReference/LocationService.html)
- [Android: Optimise location for battery](https://developer.android.com/develop/sensors-and-location/location/battery)

## 3. Network resilience
- Always set `UnityWebRequest.timeout` (default 10 s for this project).
- Check `Application.internetReachability` before request; surface failure copy if `NotReachable`.
- Retry only on `ConnectionError` or 5xx. Never on 4xx (the request itself is bad). Linear backoff, max 2 retries.
- Don't retry non-idempotent POSTs (e.g. complaint submission) without a server-side dedup key.

Citations:
- [Unity `UnityWebRequest` (2023.2)](https://docs.unity3d.com/2023.2/Documentation/ScriptReference/Networking.UnityWebRequest.html)
- [Unity `Application.internetReachability` (2023.2)](https://docs.unity3d.com/2023.2/Documentation/ScriptReference/Application-internetReachability.html)
- [Android: Optimise network for battery](https://developer.android.com/topic/performance/power/network)

## 4. Secrets
- **Do not** ship API keys in source. Both Maps and the CV endpoint must be loaded at runtime.
- Recommended pattern:
  1. Add `Assets/Resources/secrets.json` to `.gitignore`.
  2. Define a `[Serializable]` POCO with `googleMapsApiKey`, `cvApiUrl`, `complaintApiUrl`.
  3. Load via `JsonUtility.FromJson<Secrets>(Resources.Load<TextAsset>("secrets").text)` in a `SecretsProvider` MonoBehaviour referenced from the scene.
  4. Allow `PlayerPrefs.GetString("cvApiUrl")` to override at runtime for on-device testing.
- **Restrict** the Google Maps key in Google Cloud Console: Application restriction = Android apps, package = your Android app id, SHA-1 = your release key fingerprint. Otherwise a leaked key is a billing risk.

Citations:
- [Google Maps Platform: API key best practices](https://developers.google.com/maps/api-security-best-practices)
- [Google Maps Platform: Restrict API keys](https://developers.google.com/maps/documentation/javascript/get-api-key#restrict_key)
- [Unity `Resources.Load` (2023.2)](https://docs.unity3d.com/2023.2/Documentation/ScriptReference/Resources.Load.html)

## 5. PII — GPS + photos
- GPS coordinates and citizen-captured photos of public infrastructure are **personal data** under most privacy regimes once associated with a user.
- Strip EXIF before upload (or capture as `Texture2D.EncodeToJPG`, which is already metadata-free — confirm in code review).
- Show a **first-run privacy notice** explaining what data is collected, why, and where it goes. Persist consent in `PlayerPrefs`.
- Document retention with the complaint-API teammate before May 15.

Citations:
- [Google Play: Data safety form](https://support.google.com/googleplay/android-developer/answer/10787469)
- [Android: User data privacy](https://developer.android.com/privacy-and-security)
