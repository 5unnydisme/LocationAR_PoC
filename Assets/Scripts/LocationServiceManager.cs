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
  [SerializeField] private float updateDistanceInMeters = 5f;
  [Tooltip ("Maximum time to wait for location service initialization")]
  [SerializeField] private float initalizationTimeoutSeconds = 20f;
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

    #endregion
#region Private Methods (Internal Logic)
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
        if (horizontalAccuracy <= 150f)
        return GPSAccuracyStatus.Good;
        else if (horizontalAccuracy < 200f)
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

