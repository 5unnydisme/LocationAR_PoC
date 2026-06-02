using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebugOverlayController : MonoBehaviour
{
    #region Inspector Configuration
    [Header("Debug Panel Visibility")]
    [Tooltip("The parent game object which contains all the panel info")]
    [SerializeField] private GameObject debugPanel;

    [Header("Required References")]
    [Tooltip("Drag the game object with the LocationServiceManager script here")]
    [SerializeField] private LocationServiceManager locationServiceManager;
    
    [Header("UI TextMeshPro Elements")]
    [Tooltip("TextMeshPro element to display the current GPS state (Running, Failed, etc.)")]
    [SerializeField] private TextMeshProUGUI GPSStateText;

    [Tooltip("TextMeshPro element to display current latitude")]
    [SerializeField] private TextMeshProUGUI latitudeText;

    [Tooltip("TextMeshPro element to display current longitude")]
    [SerializeField] private TextMeshProUGUI longitudeText;

    [Tooltip("TextMeshPro element to display GPS accuracy in meters")]
    [SerializeField] private TextMeshProUGUI GPSAccuracyText; 

    [Tooltip("TextMeshPro element to display accuracy status description (Good, Poor etc)")]
    [SerializeField] private TextMeshProUGUI GPSAccuracyStatusMessageText; 

    [Header("Google Maps API Debug")]
    [Tooltip("Shows Nearest Roads API status - Step 1 (placeId result or error)")]
    [SerializeField] private TextMeshProUGUI roadsAPIDebugText;

    [Tooltip("Shows Place Details API status - Step 2 (road name result or error)")]
    [SerializeField] private TextMeshProUGUI placesAPIDebugText;

    [Header("UI State Debug")]
    [Tooltip("Shows the current PanelManager UIState — updated on every state transition")]
    [SerializeField] private TextMeshProUGUI uiStateDebugText;

    [Header("Report Flow State Debug")]
    [Tooltip("Shows the current PanelManager ReportFlowState — updated on every state transition")]
    [SerializeField] private TextMeshProUGUI reportFlowStateDebugText;

    [Header("CV API Result Debug")]
    [Tooltip("Shows the current CV API result ")]
    [SerializeField] private TextMeshProUGUI cvAPIResultDebugText;

    [Header("Grievance Submit Debug")]
    [Tooltip("Shows submit-button/grievance POST debug messages only")]
    [SerializeField] private TextMeshProUGUI grievanceSubmitDebugText;

    //[SerializeField] private PanelManager panelManager; //Reference to PanelManager to get state info for debugging
    [SerializeField] private TextMeshProUGUI testModeStatusText; //Shows whether we're in UI test mode or not, NO REAL API calls

    [Header ("GPS Gate Debug")]
    [Tooltip("Shows one-line gate summary sent by PanelManager after each check-in attempt")]
    [SerializeField] private TextMeshProUGUI gpsGateResultText; 
    
    [Tooltip("Shows latest sample spread from LocationServiceManager")]
    [SerializeField] private TextMeshProUGUI gpsSpreadText;

    [Tooltip("Shows accepted/total samples from latest GPS gate attempt")]
    [SerializeField] private TextMeshProUGUI gpsAcceptedSamplesText;

    [Tooltip("Shows average horizontal accuracy used by latest gate attempt")]
    [SerializeField] private TextMeshProUGUI gpsAverageAccuracyText;

    [Tooltip("Shows stationary fallback sampling diagnostics from LocationServiceManager")]
    [SerializeField] private TextMeshProUGUI gpsSamplingDebugText;
   #endregion
    
    #region Private Fields
    private bool isUpdating = false; //prevents multiple updates in coroutines running
    private float updateInterval = 0.5f; //How often to refresh the UI

    private bool isDebugPanelVisible = false; //Sets the Debug Panel to be inactive
    private string _roadsAPIDebugLine = "";  // set by GoogleMapsServices, Step 1 status
    private string _placesAPIDebugLine = ""; // set by GoogleMapsServices, Step 2 status
    private string _gpsGateLine = ""; // set by PanelManager after each check-in attempt, e.g. "result=Ok avgAcc=12.3 spread=3.1"
    private string _gpsSamplingDebugLine = ""; // set by LocationServiceManager when stationary fallback accepts a repeated timestamp

    #endregion

    void Start()
    {
        //Set panel to start hidden
         //SetDebugPanelVisibility(isDebugPanelVisible); // This will set it to false (hidden)

        //Start updating the UI elements
        StartCoroutine(UpdateUI());
    }

    public void ShowTestMode(string mode)
    {
        if (testModeStatusText == null) return;

        switch (mode)
        {
            case "FullMock":
            testModeStatusText.text = "TEST MODE: FULL MOCK (no real API calls)";
            break;

            case "CVLive":
            testModeStatusText.text = "TEST MODE: CV LIVE (Maps mocked, CV Real)";
            break;

            default:
            testModeStatusText.text = "TEST MODE: OFF (real API calls)";
            break;
        }
    }
    private IEnumerator UpdateUI()
    {
        isUpdating = true; 
        while (isUpdating)
        {
            if (locationServiceManager != null)
            {
                UpdateLocationDisplay();
            }
            else
            {
                //Show error if location service manager is not assigned
                DisplayError("LocationServiceManager not assigned!");
            }
            //Wait before updatting
            yield return new WaitForSeconds(updateInterval);
        }
    }

    private void UpdateLocationDisplay()
    {
    //Get current state from LocationServiceManager
    LocationServiceManager.LocationServiceState currentState = locationServiceManager.State;

    //Update state display
    if (GPSStateText != null)
        {
        //String formatting
        GPSStateText.text = string.Format("State: {0}", currentState);
        }

    //Update coordinates based on current state
    switch (currentState)
        {
        case LocationServiceManager.LocationServiceState.Running:
        DisplayRunningState();
        break;

        case LocationServiceManager.LocationServiceState.Initializing:
        DisplayInitializingState();
        break;

        case LocationServiceManager.LocationServiceState.Failed:
        DisplayFailedState();
        break;      

        case LocationServiceManager.LocationServiceState.Disabled:
        DisplayDisabledState();
        break;    
        }

        // Show Roads API step status
        if (roadsAPIDebugText != null)
        {
            roadsAPIDebugText.text = !string.IsNullOrEmpty(_roadsAPIDebugLine)
                ? _roadsAPIDebugLine
                : "Roads API: not called yet";
        }

        // Show Places API step status
        if (placesAPIDebugText != null)
        {
            placesAPIDebugText.text = !string.IsNullOrEmpty(_placesAPIDebugLine)
                ? _placesAPIDebugLine
                : "Places API: not called yet";
        }

        //Show the gpsGate Result 
        if (gpsGateResultText !=null)
        {
            gpsGateResultText.text = !string.IsNullOrEmpty(_gpsGateLine)
                ?$"GPS Gate: {_gpsGateLine}"
                : $"GPS Gate: result={locationServiceManager.LastSampleResult}";
        }

        if (gpsSpreadText != null)
        {
            gpsSpreadText.text = string.Format("spread: {0:F1} m", locationServiceManager.LastSampleSpreadMeters);
        }

        if (gpsAcceptedSamplesText != null)
        {
            gpsAcceptedSamplesText.text = string.Format(
                "accepted: {0}/{1}",
                locationServiceManager.LastAcceptedSampleCount,
                locationServiceManager.LastTotalSampleCount
            );
        }

        if (gpsAverageAccuracyText != null)
        {
            gpsAverageAccuracyText.text = string.Format(
                "avgAcc: {0:F1} m",
                locationServiceManager.LastSampleAverageAccuracyMeters
            );
        }

        if (gpsSamplingDebugText != null)
        {
            gpsSamplingDebugText.text = !string.IsNullOrEmpty(_gpsSamplingDebugLine)
                ? $"GPS Sampling: {_gpsSamplingDebugLine}"
                : "GPS Sampling: no stationary fallback accepted yet";
        }
    }

    public void ShowGPSGate(string line)
    {
        _gpsGateLine = line;
        if(gpsGateResultText == null) return;

        gpsGateResultText.text = string.IsNullOrEmpty(line)
        ? $"GPS Gate: result={locationServiceManager.LastSampleResult}"
        : $"GPS Gate: {line}";
    }

    public void ShowGPSSamplingDebug(string line)
    {
        _gpsSamplingDebugLine = line;
        if (gpsSamplingDebugText == null) return;

        gpsSamplingDebugText.text = string.IsNullOrEmpty(line)
            ? "GPS Sampling: no stationary fallback accepted yet"
            : $"GPS Sampling: {line}";
    }
//Display UI when GPS is working
    private void DisplayRunningState()
    {
        // Check if we have a valid GPS snapshot before accessing data
        if (!locationServiceManager.HasValidLocation)
        {
            SetCoordinateTexts("GPS Running...", "GPS Running...", "Waiting for location...");
            return;
        }

        // Get the GPS snapshot data
        LocationServiceManager.LocationSnapshot snapshot = locationServiceManager.LastLocationSnapshot;

        //Display latitude with 6 decimal places for precision; {0:F6} means to format with 6 places after decimal
        if (latitudeText != null)
        {
            latitudeText.text = string.Format("Lat: {0:F6}", snapshot.latitude);
        }

        //Display longitude 
        if (longitudeText != null)
        {
            longitudeText.text = string.Format("Lon: {0:F6}", snapshot.longitude);
        }

        //Display Horizontal accuracy with one decimal place
        if (GPSAccuracyText != null)
        {
            GPSAccuracyText.text = string.Format("Accuracy: ±{0:F1}m", snapshot.horizontalAccuracy);
        }

        //Display status message description
        if (GPSAccuracyStatusMessageText != null)
        {
            GPSAccuracyStatusMessageText.text = string.Format("Accuracy Description: {0}", locationServiceManager.GetAccuracyDescription(snapshot.horizontalAccuracy));

        }

    }



        //Display UI when GPS is starting up
        private void DisplayInitializingState()
    {
        //Clear coordinate displays while initializing
        SetCoordinateTexts("Intializing...", "Initializing...", "Waiting for GPS...");

            if (GPSStateText != null)
            {
            GPSStateText.text = "Status: Initializing GPS...";
            }
    }
        private void DisplayFailedState()
    {
        SetCoordinateTexts("GPS Failed", "GPS Failed", "No GPS signal");
        
        if (GPSStateText != null)
        {
            GPSStateText.text = "Status: Check permissions & GPS settings";
        }
    }

    /// <summary>
    /// Display UI when GPS is disabled by user
    /// </summary>
    private void DisplayDisabledState()
    {
        SetCoordinateTexts("GPS Disabled", "GPS Disabled", "Enable location services");
        
        if (GPSStateText != null)
        {
            GPSStateText.text = "Status: Location services disabled";
        }
    }
    private void SetCoordinateTexts(string latText, string lonText, string accText)
    {
        if (latitudeText != null) latitudeText.text = latText;
        if (longitudeText != null) longitudeText.text = lonText;
        if (GPSAccuracyText != null) GPSAccuracyText.text = accText;
    }

    /// <summary>
    /// Display error message when LocationServiceManager is missing
    /// </summary>
    private void DisplayError(string errorMessage)
    {
        // Display error in all text fields so user notices the problem
        if (GPSStateText != null) GPSStateText.text = "ERROR";
        if (GPSAccuracyStatusMessageText != null) GPSAccuracyStatusMessageText.text = errorMessage;
        
        SetCoordinateTexts("ERROR", "ERROR", "Check Inspector");
        
        // Also log to console for developers
        Debug.LogError(errorMessage);
    }

    private void SetDebugPanelVisibility (bool isVisible)
    {
        if (debugPanel != null)
        {
            debugPanel.SetActive (isVisible); 
            isDebugPanelVisible = isVisible; //keeps the bool in sync           
        }

    } 

    // Called by GoogleMapsServices to report each API step on-device
    // step: "roads" or "places", message: status or result string
    public void ShowAPIDebug(string step, string message)
    {
        if (step == "roads")
            _roadsAPIDebugLine = $"Roads API: {message}";
        else if (step == "places")
            _placesAPIDebugLine = $"Places API: {message}";
    }

    // Called by PanelManager.SetState on every state transition
    // state: the UIState enum name as a string (e.g. "Loading_API", "Error")
    // detail: optional road name or extra context shown alongside the state
    public void ShowUIState(string state, string detail = null)
    {
        if (uiStateDebugText == null) return;

        uiStateDebugText.text = string.IsNullOrEmpty(detail)
            ? $"UI State: {state}"
            : $"UI State: {state} | {detail}";
    } 
    
    public void ShowReportFlowState(string reportFlowState)
    {
        if (reportFlowStateDebugText == null) return;

        reportFlowStateDebugText.text = $"Report Flow State: {reportFlowState}";
    } 

    public void ShowCVAPIResult(string cvAPIResult)
    {
        if (cvAPIResultDebugText == null) return;

        cvAPIResultDebugText.text = $"CV API Result: {cvAPIResult}";
    } 

    public void ShowGrievanceSubmitResult(string grievanceSubmitResult)
    {
        if (grievanceSubmitDebugText == null) return;

        grievanceSubmitDebugText.text = $"Grievance Submit: {grievanceSubmitResult}";
    }

    public void ToggleDebugPanelVisibility()
    {
        //Flips the bool on button press 
        isDebugPanelVisible = !isDebugPanelVisible;
        SetDebugPanelVisibility(isDebugPanelVisible);
    }
    
    
      /// <summary>
    /// Stop updating when this GameObject is destroyed
    /// </summary>
    void OnDestroy()
    {
        isUpdating = false; // This will stop the coroutine loop
    }

    
    }