using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

/// <summary>
/// Manages location services within the application.
/// This script is responsible for handling starting/stopping location services, requesting permissions,
/// and providing the location date to other scripts and now we are controlling it using a button and also storing that coarse GPS value

public class LocationServiceManager : MonoBehaviour
{
  #region Inspector Configuration
  [Header("Location Service Settings")]
  [Tooltip("Desired accuracy in meters. Lower values = more precise but will use more battery. ")]
  [SerializeField] private float desiredAccuracyInMeters = 100f;
  [Tooltip("Minimum distance (meters) device must move before location updates")]
  [SerializeField] private float updateDistanceInMeters = 1f;
  [Tooltip ("Maximum time to wait for location service initialization")]
  [SerializeField] private float initalizationTimeoutSeconds = 20f;
  [Header("GPS Sampling + Accuracy Gate")]
  [Tooltip("Number of unique GPS readings to consider in one request.")]
  [SerializeField] private int sampleCount =4;

  [Tooltip("Maximum seconds to spend collecting samples.")]
  [SerializeField] private float sampleWindowSeconds =10f;

  [Tooltip("Reject an individual sample above this horizontal accuracy (meters).")]
  [SerializeField] private float perSampleMaxAccuracyMeters = 50f;

  [Tooltip("Average horizontal accuracy must be <= this threshold to pass gate.")]
  [SerializeField] private float gateThresholdMeters = 50f;

  [Tooltip ("Minimum accepted samples required. 0 uses majority rule: sampleCount/2+1.")]
  [SerializeField] private int minAcceptedSamples =2;

  [Tooltip("If false, uses single-read legacy-compatible behavior for the callback overload.")]
  [SerializeField] private bool enableSampleAveraging = true;

    [Tooltip("If true, repeated timestamps can count as stationary observations when quality checks pass.")]
    [SerializeField] private bool allowStationaryRepeatedTimestamps = true;

    [Tooltip("Repeated stationary observations are allowed only after this many seconds in one sampling attempt.")]
    [SerializeField] private float stationaryRepeatUnlockSeconds = 2f;

    [Tooltip("Max horizontal accuracy (m) for counting repeated stationary observations.")]
    [SerializeField] private float stationaryRepeatMaxAccuracyMeters = 35f;

    [Tooltip("Max spread (m) allowed when counting repeated stationary observations.")]
    [SerializeField] private float stationaryMaxSpreadMeters = 5f;

    [Tooltip("Max reading age (seconds) allowed for repeated stationary observations.")]
    [SerializeField] private float stationaryMaxReadingAgeSeconds = 10f;

    [Header("Debug References")]
    [Tooltip("Optional debug overlay used to mirror GPS sampling diagnostics on-device.")]
    [SerializeField] private DebugOverlayController debugOverlayController;
    #endregion

  #region Location Service State
  //Represents the current state of the location service 
  public enum LocationServiceState
    {
        Disabled, //Location services are turned off
        Initializing,//Location services are starting up
        Running, //Successfully getting location updates
        Failed //Failed to start or permission denied
    }

    #region SampleResult
    public enum SampleResult
    {
        Ok, 
        InsufficientSamples, 
        AllRejected,
        Timeout
    }

    #endregion

    #region GPS Accuracy States
    public enum GPSAccuracyStatus
    {
        Good, 
        Fair, 
        Poor
    }

    #endregion

    //Current state of our location service
    private LocationServiceState _currentState = LocationServiceState.Disabled;

    //Store the latest location data; we are adding a private field to store the coarse GPS value as snapshot
    private LocationSnapshot _lastValidSnapshot;

    //Flag to track if we have received one valid GPS reading
    private bool _hasValidLocation = false;
    #endregion

#region Location Snapshot Structure
//STRUCT: A simple containter to group related data together
//We use this to store the snapshot of GPS data when we get a valid reading
[System.Serializable]
public struct LocationSnapshot
    {
        public float latitude;
        public float longitude;
        public float horizontalAccuracy;
        public System.DateTime timestamp; //When was this reading taken

        //Constructor: A special method that creates a new LocationSnapshot
        public LocationSnapshot(float lat, float lon, float accuracy)
        {
            latitude = lat; 
            longitude = lon;
            horizontalAccuracy = accuracy;
            timestamp = System.DateTime.Now; //Set the timestamp to the current time when we create the snapshot
        }

    #endregion
    }

    // //Store that latest location data
    // private Location
    // #endregion

    #region Public Properties
    // //Current latitude and longitude in decimal degrees. Only valid when state is Running
    // public float Latitude => _latitude;
    // public float Longitude => _longitude;
    // //Horizontal accuracy in meters. Lower values mean more precise location
    // //Values above 10 meters are less accurate for AR applications
    // public float HorizontalAccuracy => _horizontalAccuracy;

    public float LastSampleSpreadMeters => _lastSampleSpreadMeters;
    public float LastSampleAverageAccuracyMeters => _lastSampleAvgAccuracyMeters;
    public int LastAcceptedSampleCount => _lastAcceptedSampleCount;
    public int LastTotalSampleCount => _lastTotalSampleCount;
    public SampleResult LastSampleResult => _lastSampleResult;

    //Returns true if we have a valid GPS snapshot (read-only)
    public bool HasValidLocation => _hasValidLocation;

    //Returns the last captured GPS snapshot
    public LocationSnapshot LastLocationSnapshot => _lastValidSnapshot;

    //Returns Current state of the location service
    public LocationServiceState State => _currentState;
    #endregion

    void Start()
    {
                // GPS does NOT auto-start anymore
        // We wait for the user to press a button that calls RequestLocation()
        Debug.Log("LocationServiceManager ready. Call RequestLocation() to start GPS.");
    } 

    #region Public Methods (Called by UI Buttons)
    //Adding public method for the button to call
    public void RequestLocation()    
    {
        //Only start GPS is it is not already running
        if (_currentState == LocationServiceState.Disabled || _currentState == LocationServiceState.Failed)
        {
            StartCoroutine(InitializeLocationService());
        }
    }

    public void RequestLocation(System.Action<SampleResult, LocationSnapshot> onComplete)
    {
        StartCoroutine(RequestLocationWithSampling(onComplete));
    }
    #endregion

#region Private Methods (Internal Logic)
    private float _lastSampleSpreadMeters = 0f;
    private float _lastSampleAvgAccuracyMeters = 0f;
    private int _lastAcceptedSampleCount = 0;
    private int _lastTotalSampleCount = 0;
    private SampleResult _lastSampleResult = SampleResult.Timeout;
    private const float samplePollIntervalSeconds = 0.5f;
    private IEnumerator InitializeLocationService()
    {
        _currentState = LocationServiceState.Initializing;

        //Step 1: Request location permission on Android if not already granted
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
            // Wait for the user to respond to the permission dialog
            float permissionWait = 0f;
            while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation) && permissionWait < 10f)
            {
                yield return new WaitForSeconds(0.25f);
                permissionWait += 0.25f;
            }
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                Debug.LogWarning("Location permission denied by user.");
                _currentState = LocationServiceState.Failed;
                yield break;
            }

        }

        // Always wait a few frames on Android before checking GPS state.
        // This is needed even when permission was already granted — some OEMs (e.g. Xiaomi MIUI)
        // need extra time for the OS to register the permission before Input.location responds correctly.
        yield return null;
        yield return null;
        yield return null;
#endif

        // Step 2: Check if location services are enabled at the OS level.
        // NOTE: On Xiaomi MIUI, Input.location.isEnabledByUser is unreliable — it can return false
        // even when system GPS is on. We log a warning but attempt to start anyway on Android.
        // The definitive result is determined by the LocationServiceStatus check in Step 5.
        if (!Input.location.isEnabledByUser)
        {
#if UNITY_ANDROID
            // On Android, do not treat this as fatal — MIUI and other OEM skins report this incorrectly.
            // Proceed and let Input.location.Start() + the status check below determine the real outcome.
            Debug.LogWarning("isEnabledByUser returned false — may be an OEM reporting issue (e.g. MIUI). Attempting to start anyway.");
#else
            Debug.LogWarning("Location services are disabled by the user.");
            _currentState = LocationServiceState.Disabled;
            yield break;
#endif
        }

        //Step 3: Starting the location service with the desired settings
        //How Unity GPS is working
        //Unity wraps the native GPS APIs, LocationManager on Android
        //~desiredAccuracyInMeters: tells the GPS how precise we want the location, lower values is more battery intensive
        //~updateDistanceInMeters: increments the distance after which the value is updated, battery saving
        Input.location.Start (desiredAccuracyInMeters, updateDistanceInMeters);

        //Step 4: Wait for the service to initilialize
        float timeElapsed = 0f;
        while (Input.location.status == LocationServiceStatus.Initializing && timeElapsed <initalizationTimeoutSeconds)
        {
            //Wait a bit before checking again
            yield return new WaitForSeconds(0.5f);
            timeElapsed += 0.5f;
        }

        //Step 5: Check if initialization was successful
        switch (Input.location.status)
        {
            case LocationServiceStatus.Running:
            _currentState = LocationServiceState.Running;
            Debug.Log("Location service started successfully");

            //Capture first GPS snapshot
            CaptureLocationSnapshot();
            break;

            case LocationServiceStatus.Failed:
            _currentState = LocationServiceState.Failed;
            Debug.LogError("Location service failed to start.");
            break;

            case LocationServiceStatus.Initializing:
            _currentState = LocationServiceState.Failed;
            Debug.LogError("Location service initialization timed out.");
            break;
        }
    }

    // PRIVATE METHOD: Captures a snapshot of current GPS data
    // We store a snapshot instead of reading live GPS because:
    // 1. GPS data can change rapidly and we want consistent readings
    // 2. Other scripts can safely use the data without worrying about it changing mid-calculation
    // 3. We can validate the quality of the snapshot before storing it
    private void CaptureLocationSnapshot() //running when the location service is working properly
    {
        if (Input.location.status == LocationServiceStatus.Running)
        {
            //Get current GPS data from Unity
            LocationInfo locationInfo = Input.location.lastData;

            //Create a new snapshot with current data
            _lastValidSnapshot = new LocationSnapshot (
                locationInfo.latitude,
                locationInfo.longitude,
                locationInfo.horizontalAccuracy
            );

            //Mark that we now have valid location data
            _hasValidLocation = true; 
        }
    }

    private IEnumerator RequestLocationWithSampling(System.Action<SampleResult, LocationSnapshot> onComplete)
    {
        int targetSampleCount = Mathf.Max(1, sampleCount);
        int requiredAccepted = ResolveRequiredAcceptedSamples(targetSampleCount);

        // Ensure service is running before sampling starts.
        if (_currentState == LocationServiceState.Disabled || _currentState == LocationServiceState.Failed)
        {
            yield return StartCoroutine(InitializeLocationService());
        }

        // If another request is currently initializing, wait for it to settle.
        float settleWaitElapsed = 0f;
        while (_currentState == LocationServiceState.Initializing && settleWaitElapsed < initalizationTimeoutSeconds)
        {
            yield return new WaitForSeconds(0.5f);
            settleWaitElapsed += 0.5f;
        }

        float initWaitElapsed = 0f;
        while (Input.location.status == LocationServiceStatus.Initializing && initWaitElapsed < initalizationTimeoutSeconds)
        {
            yield return new WaitForSeconds(0.5f);
            initWaitElapsed += 0.5f;
        } 

        if (_currentState != LocationServiceState.Running || Input.location.status != LocationServiceStatus.Running)
        {
            CompleteSampling(onComplete, SampleResult.Timeout, default(LocationSnapshot), 0f, 0f, 0, 0);
            yield break;
        }

        //Kill-switch: Single read be behavior for quick rollback and compatibility testing
        if (!enableSampleAveraging)
        {
            LocationInfo one = Input.location.lastData;
            long ageMs = ComputeAgeMs(one.timestamp);
            Debug.Log($"[LocationServiceManager] sample i=1 lat={one.latitude:F6} lon={one.longitude:F6} acc={one.horizontalAccuracy:F1} ageMs={ageMs} accepted");

            CaptureLocationSnapshot(); //mirrors today's behavior of storing a single snapshot
            CompleteSampling(onComplete, SampleResult.Ok, _lastValidSnapshot, _lastValidSnapshot.horizontalAccuracy, 0f, 1, 1);
            yield break;
        }

        List<LocationInfo> acceptedSamples = new List<LocationInfo>();
        int uniqueSamplesSeen = 0;
        float elapsed = 0f;
        double lastTimestamp = double.MinValue; 
        WaitForSeconds pollWait = new WaitForSeconds(samplePollIntervalSeconds);
        float maxWindow = Mathf.Max(sampleWindowSeconds, samplePollIntervalSeconds); 

        while (elapsed < maxWindow && acceptedSamples.Count < requiredAccepted && uniqueSamplesSeen < targetSampleCount)
        {
            LocationInfo sample = Input.location.lastData;
            bool hasTimestamp = sample.timestamp > 0d;
            bool repeatedTimestamp = hasTimestamp && System.Math.Abs(sample.timestamp - lastTimestamp) < 0.0001d;

            if (repeatedTimestamp)
            {
                bool canCountAsStationary = false;
                long repeatedAgeMs = ComputeAgeMs(sample.timestamp);
                float projectedSpreadMeters = 0f;

                if (allowStationaryRepeatedTimestamps)
                {
                    float maxAgeMs = stationaryMaxReadingAgeSeconds * 1000f;

                    List<LocationInfo> projectedSamples = new List<LocationInfo>(acceptedSamples);
                    projectedSamples.Add(sample);
                    projectedSpreadMeters = ComputeSpreadMeters(projectedSamples);

                    canCountAsStationary =
                        elapsed >= stationaryRepeatUnlockSeconds &&
                        sample.horizontalAccuracy <= stationaryRepeatMaxAccuracyMeters &&
                        repeatedAgeMs <= maxAgeMs &&
                        projectedSpreadMeters <= stationaryMaxSpreadMeters;
                }

                if (!canCountAsStationary)
                {
                    yield return pollWait;
                    elapsed += samplePollIntervalSeconds;
                    continue;
                }

                Debug.Log($"[LocationServiceManager] stationaryFallback accepted ageMs={repeatedAgeMs} spreadM={projectedSpreadMeters:F1} acc={sample.horizontalAccuracy:F1}");
                debugOverlayController?.ShowGPSSamplingDebug($"stationary fallback accepted ageMs={repeatedAgeMs} spreadM={projectedSpreadMeters:F1} acc={sample.horizontalAccuracy:F1}");
            }
            
            if (hasTimestamp)
            {
                lastTimestamp = sample.timestamp;
            }

            uniqueSamplesSeen++;

            bool accepted = sample.horizontalAccuracy <= perSampleMaxAccuracyMeters;
            long ageMs = ComputeAgeMs(sample.timestamp);

            if (accepted)
            {
                acceptedSamples.Add(sample);
                Debug.Log($"[LocationServiceManager] sample i={uniqueSamplesSeen} lat={sample.latitude:F6} lon={sample.longitude:F6} acc={sample.horizontalAccuracy:F1} ageMs={ageMs} accepted");
            }
            else
            {
                Debug.Log($"[LocationServiceManager] sample i={uniqueSamplesSeen} lat={sample.latitude:F6} lon={sample.longitude:F6} acc={sample.horizontalAccuracy:F1} ageMs={ageMs} rejected reason=accuracy>threshold");
            }

            if (acceptedSamples.Count < requiredAccepted && uniqueSamplesSeen < targetSampleCount)
            {
                yield return pollWait;
                elapsed += samplePollIntervalSeconds;
            }

        }

        if (uniqueSamplesSeen == 0)
        {
            CompleteSampling(onComplete, SampleResult.Timeout, default(LocationSnapshot), 0f, 0f, 0, 0);
            yield break;
        }

        if (acceptedSamples.Count == 0)
        {
            CompleteSampling(onComplete, SampleResult.AllRejected, default(LocationSnapshot), 0f, 0f, 0, uniqueSamplesSeen);
            yield break;
        }

        if (acceptedSamples.Count < requiredAccepted)
        {
            CompleteSampling(onComplete, SampleResult.InsufficientSamples, default(LocationSnapshot), 0f, 0f, acceptedSamples.Count, uniqueSamplesSeen);
            yield break;
        }

        float avgLat = 0f;
        float avgLon = 0f;
        float avgAcc = 0f;

        for (int i = 0; i < acceptedSamples.Count; i++)
        {
            avgLat += acceptedSamples[i].latitude;
            avgLon += acceptedSamples[i].longitude;
            avgAcc += acceptedSamples[i].horizontalAccuracy;
        }

        avgLat /= acceptedSamples.Count;
        avgLon /= acceptedSamples.Count;
        avgAcc /= acceptedSamples.Count;

        float spreadMeters = ComputeSpreadMeters(acceptedSamples);
        SampleResult gateResult = avgAcc <= gateThresholdMeters ? SampleResult.Ok : SampleResult.AllRejected;

        if (gateResult == SampleResult.Ok)
        {
            _lastValidSnapshot = new LocationSnapshot(avgLat, avgLon, avgAcc);
            _hasValidLocation = true;
            CompleteSampling(onComplete, gateResult, _lastValidSnapshot, avgAcc, spreadMeters, acceptedSamples.Count, uniqueSamplesSeen);
        }
        else
        {
            CompleteSampling(onComplete, gateResult, default(LocationSnapshot), avgAcc, spreadMeters, acceptedSamples.Count, uniqueSamplesSeen);
        }
    }

    private void CompleteSampling(
        System.Action<SampleResult, LocationSnapshot> onComplete,
        SampleResult result,
        LocationSnapshot snapshot,
        float avgAccuracy,
        float spreadMeters,
        int acceptedCount,
        int totalCount)
    {
        _lastSampleResult = result;
        _lastSampleSpreadMeters = spreadMeters;
        _lastSampleAvgAccuracyMeters = avgAccuracy;
        _lastAcceptedSampleCount = acceptedCount;
        _lastTotalSampleCount = totalCount;

        Debug.Log($"[LocationServiceManager] gate avgAcc={avgAccuracy:F1} spreadM={spreadMeters:F1} accepted={acceptedCount}/{totalCount} result={result}");

        onComplete?.Invoke(result, snapshot);
    }

    private int ResolveRequiredAcceptedSamples(int targetSampleCount)
    {
        int majorityDefault = (targetSampleCount / 2) + 1;
        int requested = minAcceptedSamples <= 0 ? majorityDefault : minAcceptedSamples;
        return Mathf.Clamp(requested, 1, targetSampleCount);
    }

    private long ComputeAgeMs(double unixTimestampSeconds)
    {
        if (unixTimestampSeconds <= 0d)
        {
            return 0L;
        }

        long readingMs = (long)(unixTimestampSeconds * 1000d);
        long nowMs = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return System.Math.Max(0L, nowMs - readingMs);
    }

    private float ComputeSpreadMeters(List<LocationInfo> samples)
    {
        if (samples == null || samples.Count <= 1)
        {
            return 0f;
        }

        float minLat = float.MaxValue;
        float maxLat = float.MinValue;
        float minLon = float.MaxValue;
        float maxLon = float.MinValue;
        float meanLat = 0f;

        for (int i = 0; i < samples.Count; i++)
        {
            float lat = samples[i].latitude;
            float lon = samples[i].longitude;

            if (lat < minLat) minLat = lat;
            if (lat > maxLat) maxLat = lat;
            if (lon < minLon) minLon = lon;
            if (lon > maxLon) maxLon = lon;

            meanLat += lat;
        }

        meanLat /= samples.Count;

        float latSpreadMeters = (maxLat - minLat) * 111320f;
        float lonMetersPerDegree = 111320f * Mathf.Abs(Mathf.Cos(meanLat * Mathf.Deg2Rad));
        float lonSpreadMeters = (maxLon - minLon) * lonMetersPerDegree;

        return Mathf.Max(latSpreadMeters, lonSpreadMeters);
    }
    #endregion

    //Clean up when game object is destroyed

    void OnDestroy()
    {
        //Stop location service when not in use
        if (Input.location.status == LocationServiceStatus.Running)
        {
            Input.location.Stop();
            Debug.Log("Location service stopped.");
        }   
    }

        //provides an easy to read description of the location service state
        //we are using the GPS Accuracy enum here
        public GPSAccuracyStatus GetAccuracyDescription(float horizontalAccuracy)
    {
        if (horizontalAccuracy <= 30f)
        return GPSAccuracyStatus.Good;
        else if (horizontalAccuracy < 50f)
        return GPSAccuracyStatus.Fair;
        else
        return GPSAccuracyStatus.Poor;  
    }

    //Another line of messaging that explains the accuracy status in words
    public string GetAccuracyStatusMessage (float horizontalAccuracy)
    {
        switch(GetAccuracyDescription(horizontalAccuracy))
        {
        case GPSAccuracyStatus.Good:
        return "GPS estimate is realiable.";

        case GPSAccuracyStatus.Fair:
        return "GPS estimate is usable but may be inaccurate.";

        case GPSAccuracyStatus.Poor:
        return "GPS estimate is unrealiable, try moving to open area.";

        default: 
        return "Unknown GPS accuracy status.";
        }     
    
}

    
}

