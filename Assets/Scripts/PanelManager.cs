using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//This is the new panel manager script that is handling the UI panels and buttons in the app. It is 
// It is using a state-based approach show/hide panels based on GPS data, and the API call status.

public class PanelManager : MonoBehaviour
{
   //States that we have- 
   //Idle - the app just started and is waiting for GPS data
   //Loading_Location - GOS acquiring a location, all buttons are locked
   // Loading_API- GPS done, API request sent and all buttons remain locked 
   //Success_WithData - road found AND there is JSON data available -> LearnMore button becomes shown
   //CaptureImage - this is the state where the user can take a picture of the road defect, accessed by tapping the Report Issue button in the info panel
   //Success_NoData - road found BUT no JSON data available -> LearnMore button remains hidden, but AddInfo button shown
   //Error - GPS or network failure - Retry button or go to main page(?)
    // To prevent multiple API calls the Check-In button is disabled immediately after tapping
    //Check-In button reenabled when back to main page which is returning to Idle/ 

    public enum UIState
    {
        Idle,
        Loading_Location,
        Loading_LocationSampling,
        Loading_API, 
        Success_WithData,
        CaptureImage,
        Success_NoData,
        Error_GPSAccuracy,
        Error 
    }

    private enum ReportFlowState
    {
        LiveCapture, //Live camera feed, user can see capture button to take photo
        Preview, //Frozen frame shown to user for review
        AwaitingAnalysis, //Dummy loading state for now- CV netween goes here later
        ReviewResult, // This is the phase where the AI result is received - this is where we can have the review panel
        SubmittingGrievance, //POST to grievance database is happening, submit button is hidden, spinner showing
        Success //This is where submission is confirmed, we can success panel here
    }

    public enum TestMode
    {
        Off, //All API calls are real (Maps + CV + Grievance)
        FullMock, //All API calls are fake (Maps + CV + Grievance)
        CVLive //Maps is mocked,CV and Grievance are real
    }

    #region Inspector Configuration
    [Header("UI Panels")]
    [Tooltip("The logo screen shown when the app starts")]
    public GameObject logoScreen; //This is the logo

    [Tooltip("Panel shown when no location data available or an error")]
    public GameObject noLocationPanel; 

    [Tooltip("BG panel for the instructions on the main page")]
    public GameObject infoPanelGameObject;

    //Reusable info panel GameObject (the panel that has the PanelPopulator script attached)
    [Tooltip("Reusable info-PanelManager has PanelPopulator script attached")]
    public GameObject panelPopulatorGameObject;

    [Header("State UI Elements")]
    [Tooltip("Loading spinner shown when API call is in progress")]
    [SerializeField] private GameObject loadingSpinner;

    [Tooltip("TMP text that shows the helper text to the user based on state")]
    [SerializeField] private TextMeshProUGUI statusMessageText;

    [Header("State-Based Buttons (assigned GameObjects, not Button components)")]
    [Tooltip("Show in IDle- starts the full GPS to making API request flow")]
    [SerializeField] private GameObject checkInButton; 

    [Tooltip("Learn More button button - only shown after a successful API result")]
    [SerializeField] private GameObject learnMoreButton;

    [Tooltip("Report Issue button - shown when the PanelPopulator is revealed in Success with data state")]
    [SerializeField] private GameObject reportIssueButton; //this button is what activates the camera picture taking feature 

    [Tooltip("Add Info button - only show if no JSON data available for a found road, future-proofing")]
    [SerializeField] private GameObject addInfoButton; 

    [Tooltip("Retry button - only shown in error state, takes users back to the Main page and restarts the check-in flow")]
    [SerializeField] private GameObject retryButton; 

    [Header("Learn More - Project List UI")]
    [Tooltip("Header panel shown at the top of Learn More with road title and project count.")]
    [SerializeField] private GameObject roadHeaderPanel;

    [Tooltip("Scrollable panel that contains the dynamic project card list.")]
    [SerializeField] private GameObject projectCardScrollPanel;

    [Tooltip("Content container under the scroll view where project cards are instantiated.")]
    [SerializeField] private Transform projectCardContentContainer;

    [Tooltip("Prefab to instantiate for each project linked to the selected road.")]
    [SerializeField] private GameObject projectCardPrefab;

    [Tooltip("Header text field that displays the selected road name.")]
    [SerializeField] private TextMeshProUGUI roadHeaderText;

    [Tooltip("Header text field that displays the number of projects for the selected road.")]
    [SerializeField] private TextMeshProUGUI projectCountText;
 
    [Header("Test Mode")]
    [Tooltip("Off = real flow. FullMock = all fake. CVLive = mock Maps + real CV + grievance")]
    [SerializeField] private TestMode testMode = TestMode.Off;
     private string testRoadName = "Canal Road"; //Default road name from the JSON used for simulating the success call

    [Header("ImageCapture UI Elements - Live Capture")]

    [Tooltip("TMP text that shows the helper text to the user based on state within the substate of Report Issue flow")]
    [SerializeField] private TextMeshProUGUI imageCaptureInstructionText;

    [Tooltip("All the graphic elements for the image capture")]
    [SerializeField] private GameObject imageCapturePanel; //Parent panel that is housing everything, and will be controlling the visibility of the other compnenents
    [SerializeField] private GameObject instructionpanel; //this is the panel with instructions on how to frame the image
    [SerializeField] private GameObject captureSquare; //this is the boundary within which the defect to be reported must be placed 
    [SerializeField] private GameObject captureButton; //capture image button
    [SerializeField] private GameObject backButton; //button to go back to the info panel, it controls the UI state by resetting to previous state or Success_wData

    [Header("ImageCapture UI Elements - Snapshot Review")]
    [SerializeField] private GameObject capturedFramePanel; //Shown AFTER capture, contains the RawImage below
    [SerializeField] private RawImage previewRawImage; // displays the frozen Texture2D snapshot 
    [Tooltip("Panel containing the issue-type dropdown shown during Preview.")]
    [SerializeField] private GameObject modelDropdownPanel;
    [SerializeField] private GameObject analyseButton; //confirm and submit image for analysis
    [SerializeField] private GameObject retakeButton; //discards the captures image and goes back to live capture state

    [Header("ImageCapture UI Elements - Awaiting Analysis")]
    [SerializeField] private GameObject awaitingPanel; //shown during dummy loading 
    //how to add the logic to reuse the loading circle here, will have to copy over the state logic 

    [Header("ImageCapture UI Elements - Future Elements")] //these are the things added after SB's CV integration is done 
    [SerializeField] private GameObject reviewResultsPanel; //Placeholder. Leave unassigned for now, later will write from received json analysed data
    [SerializeField] private GameObject successSubmissionPanel; //Placeholder. Leave unassigned for now, later will be the Complaint Lodged success panel
    [SerializeField] private GameObject submitGrievanceButton; // show in reviewresult, tapped to file the grievance

    [Header("Script References")]
    public  DebugOverlayController debugOverlayController; 
    public LocationServiceManager locationServiceManager; 
    public GoogleMapsServices googleMapsServices; 
    public LocationJSONLoader locationJSONLoader;
    public PanelPopulator panelPopulator; 
    public SharedCameraManager sharedCameraManager; //Reference to the shared camera manager script to trigger the camera which will capture the image
    public LoadingCircleAnimation loadingCircleAnimation; //Reference to the loading circle script to trigger animation. 
    public ReportSubmissionManager submissionManager; //Reference to the report submission manager to trigger the report submission flow when SB's CV section is ready
    public ModelDropdownController modelDropdownController; //Reference to model dropdown controller to read/reset selected issue type during Preview flow
    public CVResultPopulator cvResultPopulator; //Reference to the script that will populate the CV result panel with the data received from SB's CV analysis, this is separate from the PanelPopulator which is specifically for the location data from the JSON
    public GrievanceSubmissionManager grievanceSubmissionManager; //handles the POST to the grievance database
    #endregion

    #region Private State Fields
    private UIState currentState = UIState.Idle;
    private ReportFlowState currentReportFlowState = ReportFlowState.LiveCapture;
    private string currentRoadName = null; //Set when Success condition hit, otherwise remains clear
    private CVAnalysisResult currentCVResult = null; //Stores the CV result between ReviewResult and the submission
    #endregion

    #region Unity Lifecycle Methods

    void Start ()
    {
        SetupInitialUI();
    }
    #endregion

    #region Initial Setup

    public bool isTestModeActive => testMode != TestMode.Off;
    public bool isMapsLocked => testMode != TestMode.Off;
    public bool isFullMock => testMode == TestMode.FullMock;
    public bool isCVLive => testMode == TestMode.CVLive; 
    public void ToggleTestMode()
    {
        testMode = (TestMode)(((int)testMode +1) % 3); //Cycle through - Off -> FullMock -> CVLive -> Off
        debugOverlayController?.ShowTestMode(testMode.ToString());
        Debug.Log($"[PanelManager] Test Mode: {testMode}");
    }

    private void SetupInitialUI()
    {
        HideAllPanels(); //Needed for hiding the logo panel 
        if (loadingSpinner != null) loadingSpinner.SetActive(false); //Ensures that the spinner is hidden at the beginning
        logoScreen.SetActive(true); 
        SetState(UIState.Idle); //Set initial state to Idle
        debugOverlayController?.ShowTestMode(testMode.ToString()); //Update the debug overlay if it exists
    }
    #endregion

    #region UI State Machine 
    public void SetState(UIState newState, string roadName = null)
    {
        currentState = newState; 

        //Keep roadname for the two success states, and clear it for the others 
        if ((newState == UIState.Success_WithData || newState == UIState.Success_NoData) && !string.IsNullOrEmpty(roadName))
        {
            currentRoadName = roadName;
        }

        else if (newState != UIState.Success_WithData && newState != UIState.Success_NoData && newState != UIState.CaptureImage)
        {
            currentRoadName = null; 
        }

        //Always start clean- each state only reveals what it needs
        HideAllPanels(); 
        HideAllButtons(); 
        HideReportSubPanels();
        if (loadingSpinner != null) loadingSpinner.SetActive(false); 

        switch (newState)
        {
            case UIState.Idle:
            logoScreen.SetActive(true); //We want the logo visible on MainPage
            infoPanelGameObject.SetActive(true); //We are reusing the info panel for the main page instructions, so we keep it active and just populate it with the welcome message
            ShowButton (checkInButton, interactable:true); //only state that re-enables the Check-in button, failsafe to prevent multiple calls
            SetStatusMessage("Tap check-in to see information about infrastructure near you.");
            SetImageCaptureInstructionText(string.Empty); // HACK: clear lingering "Complaint submitted" text when returning to first page
            break;

            case UIState.Loading_Location:
            logoScreen.SetActive(true); //Keep the logo visible while the GPS location acquiring takes place
            infoPanelGameObject.SetActive(true); 
            if (loadingSpinner != null) loadingSpinner.SetActive(true); 
            if (loadingCircleAnimation != null) loadingCircleAnimation.StartAnimation(); //Trigger the loading circle animation when we start loading location
            SetStatusMessage("Getting your location...");
            break;

            case UIState.Loading_LocationSampling:
            logoScreen.SetActive(true); 
            infoPanelGameObject.SetActive(true);
            if (loadingSpinner != null) loadingSpinner.SetActive(true);
            if(loadingCircleAnimation != null) loadingCircleAnimation.StartAnimation();
            SetStatusMessage("Checking GPS accuracy...");
            break;

            case UIState.Loading_API:
            logoScreen.SetActive(true); //Logo stays on until the infoPanel is populated with the json data
            infoPanelGameObject.SetActive(true); 
            if (loadingSpinner != null) loadingSpinner.SetActive(true); 
            if (loadingCircleAnimation != null) loadingCircleAnimation.StartAnimation(); //Trigger the loading circle animation when we start loading API call
            break;

            case UIState.Success_WithData:
            logoScreen.SetActive(true); //Stays true because we are revealing the name of infrastructure found
            infoPanelGameObject.SetActive(true); 
            ShowButton(learnMoreButton, interactable: true); //Learn More button becomes interactable, takes user to the info panel with more details about the location
            SetStatusMessage($"Information available for {currentRoadName}. Tap Learn More to see details.");
            break;

            case UIState.CaptureImage:
            imageCapturePanel.SetActive(true); //Show the image capture panel with instructions and capture button
            SetReportFlowState(ReportFlowState.LiveCapture); //Set the sub-state for the report flow to live capture, which will show the relevant UI elements for that phase
            break; 
            
            case UIState.Success_NoData: //TODO make this such that in the event there is a match without city data they can complain
            logoScreen.SetActive(true); //Stays on because we are revealing the name of infrastructure found, even if there is no data available for it
            infoPanelGameObject.SetActive(true); 
            //ShowButton(addInfoButton, interactable: true); //Add info button becomes interactable, this is future proofing for next stage of the app 
            SetStatusMessage($"No information available for {currentRoadName}. Tap Add Info to contribute details about this location.");
            break;

            case UIState.Error_GPSAccuracy:
            logoScreen.SetActive(true); //Keep the logo on
            infoPanelGameObject.SetActive(true); 
            ShowButton(retryButton, interactable: true);
            SetStatusMessage("GPS signal too weak. Move to open sky and try again.");
            break;

            case UIState.Error:
            logoScreen.SetActive(true); //We want the logo to stay on 
            infoPanelGameObject.SetActive(true); //Reusing the info panel for error messages
            ShowButton(retryButton, interactable: true);
            //if (loadingCircleAnimation != null) loadingCircleAnimation.gameObject.SetActive(false); //Hide the loading circle on error
            SetStatusMessage("Could not find nearby infrastructure.\nTap Retry to try again.");
            break;
        }

        // Update the debug overlay with the new state and optional road name
        debugOverlayController?.ShowUIState(newState.ToString(), roadName);
        Debug.Log($"[PanelManager] State → {newState} | road: '{roadName ?? "null"}'");
    }

        //Controls which UI elements are shown during the different phases of the Report Issue flow within Capture Image state
        //Called internally and never by button OnClick events
        //Each substate hides eveyrthing first and then shows what's needed. 
        private void SetReportFlowState(ReportFlowState reportState)
    {
        currentReportFlowState = reportState; 
        HideReportSubPanels(); //Cleares the panels specific to report flow substates
        HideAllButtons(); //clears all buttons including capture sub-buttons

        if (reportState != ReportFlowState.AwaitingAnalysis)
        {
            CancelInvoke("OnAnalysisComplete");
        }
        
        switch(reportState)
        {
            case ReportFlowState.LiveCapture:
            sharedCameraManager?.ResumeFeed();
            imageCapturePanel.SetActive(true); //Keeps the main capture panel active during all states
            ShowButton(captureButton, interactable: true); //Show the capture button and allow interaction
            instructionpanel.SetActive(true); //Show the instructions on how to frame the image 
            captureSquare.SetActive(true); //Show the capture square to guide users on how to frame the image
            SetImageCaptureInstructionText("Position defect within dotted square and tap on capture when ready."); //Set instructions for image capture
            break;

            case ReportFlowState.Preview:
            capturedFramePanel.SetActive(true); 
            if (modelDropdownPanel != null) modelDropdownPanel.SetActive(true);
            modelDropdownController?.ResetSelection();
            previewRawImage.gameObject.SetActive(true); //Show the preview of the captured image to the user for review
            ShowButton(analyseButton, interactable:false); //Starts locked until issue type is selected from dropdown
            ShowButton(retakeButton, interactable: true); //Show the retake button go back to live capture and discard the current image
            instructionpanel.SetActive(true); //Keep the instructions visible during to guide users for next steps 
            SetImageCaptureInstructionText("Review the captured image.\nTap Analyse to submit for analysis or Retake to try again.");
            sharedCameraManager?.SuspendFeed(); //Set instructions for the next steps on sending image for analysis
            break; 

            case ReportFlowState.AwaitingAnalysis:
            sharedCameraManager?.SuspendFeed();
            capturedFramePanel.SetActive(true); 
            previewRawImage.gameObject.SetActive(true); //Show the preview of the captured image to the user for review
            instructionpanel.SetActive(true); //Keep instructions visible to let user know we are working on analysing the image
            SetImageCaptureInstructionText("Submitting image for analysis..."); //Set status message to let user know we are working on analysis
            // if (loadingSpinner != null) loadingSpinner.SetActive(true); 
            // if (loadingCircleAnimation != null) loadingCircleAnimation.StartAnimation(); //Trigger the loading circle animation when we start loading API call
            
            //FullMock uses timer-driven fake CV result to keep demo flow going
            if (isFullMock)
                {
                    Invoke("OnAnalysisComplete", 3f); //After 3 seconds, simulates fake CV results
                }
            break;

            case ReportFlowState.ReviewResult:
            sharedCameraManager?.SuspendFeed();
            capturedFramePanel.SetActive(true); 
            previewRawImage.gameObject.SetActive(true); //Show the preview of the captured image to the user for review
            instructionpanel.SetActive(true); //Keep instructions visible to let user know we are working on analysing the image
            //This is where we will show the results from the CV analysis 
            SetImageCaptureInstructionText("Analysis complete. Press submit to lodge complaint"); //Placeholder, will replace with real results from the CV analysis later
            if (loadingSpinner != null) loadingSpinner.SetActive(false); 
            if(reviewResultsPanel != null) reviewResultsPanel.SetActive(true); //Show the panel with the CV results, this panel will be populated with the actual CV result data once we have that from SB
            ShowButton(submitGrievanceButton, interactable: true); //Submit button appears once the CV result comes in
            break;

            case ReportFlowState.SubmittingGrievance:
            sharedCameraManager?.SuspendFeed();
            capturedFramePanel.SetActive(true); 
            previewRawImage.gameObject.SetActive(true); //Show the preview of the captured image to the user for review
            if (reviewResultsPanel != null) reviewResultsPanel.SetActive(true); //Keep the result visible while submitting
            instructionpanel.SetActive(true);
            SetImageCaptureInstructionText("Filing grievance report...");
            // submitGrievanceButton intentionally NOT shown — HideAllButtons() already hid it
            break;

            case ReportFlowState.Success:
            sharedCameraManager?.ResumeFeed();  
            //Placeholder - wire up when submission backend is ready, this is where we will show the complaint lodged successfully
            instructionpanel.SetActive(true);
            SetImageCaptureInstructionText("Complaint submitted");
            if (loadingSpinner != null) loadingSpinner.SetActive(false);
            if (successSubmissionPanel != null) successSubmissionPanel.SetActive(true);
            ShowButton(backButton, interactable: true); //Show the back button to let user return to main page after seeing success message
            break;
        }

        debugOverlayController?.ShowReportFlowState($"Report Issue Flow → {reportState}"); //Log the current sub-state of the report issue flow in the debug overlay for testing purposes

    }
    
     #endregion

     #region Button Event Methods
     //CheckIn button OnClick method 

     public void onCheckInButtonPressed()
    {
        string modeLabel = isFullMock ? "FullMock": (isCVLive ? "CVLive" : "Real");
        Debug.Log($"[PanelManager] checkIn start mode={modeLabel}");

        if (isMapsLocked)
        {
            ShowButton(checkInButton, interactable: false); //Disable the check-in button immediately to prevent multiple taps and API calls while we process the first tap
            SetState (UIState.Loading_Location);
            Invoke ("SimulateAPIResponse", 2f); //Simulate API response after 2 seconds, this will trigger the success state with data and show the Learn More button
            return;
        }

        if (locationServiceManager == null || googleMapsServices == null)
        {
            SetState(UIState.Error);
            return;
        }

        ShowButton(checkInButton, interactable: false);
        StartLocationSamplingAndResolveRoad();
    }

    private void StartLocationSamplingAndResolveRoad()
    {
        SetState(UIState.Loading_LocationSampling);

        locationServiceManager.RequestLocation((result, snapshot) =>
        {
            float spread = locationServiceManager != null ? locationServiceManager.LastSampleSpreadMeters : 0f;
            string avgAccText = result == LocationServiceManager.SampleResult.Ok
                ? snapshot.horizontalAccuracy.ToString("F1")
                : "n/a";

            string gateLine = $"result={result} avgAcc={avgAccText} spread={spread:F1}";
            debugOverlayController?.ShowGPSGate(gateLine); 

            if (result == LocationServiceManager.SampleResult.Ok)
            {
                Debug.Log($"[PanelManager] gpsGate {gateLine} → nextState=Loading_API");
                SetState(UIState.Loading_API);
                googleMapsServices.GetActiveLocationName(snapshot.latitude, snapshot.longitude, OnRoadNameReceived);
                return;
            }

            Debug.Log($"[PanelManager] gpsGate {gateLine} → nextState=Error_GPSAccuracy");
            SetState(UIState.Error_GPSAccuracy);
        });
    
    }

    //LearnMore button OnClick
    //Uses stored roadName to build the dynamic road-level project list view.
    public void OnLearnMoreButtonPressed()
    {
        if (string.IsNullOrEmpty(currentRoadName) || locationJSONLoader == null)
        {
            Debug.LogWarning("[PanelManager] Learn More blocked: missing road name or loader reference.");
            return;
        }

        string matchedRoadName = locationJSONLoader.GetBestMatchingRoadName(currentRoadName);
        string roadNameForLookup = string.IsNullOrEmpty(matchedRoadName) ? currentRoadName : matchedRoadName;

        ProjectRecord[] projects = locationJSONLoader.GetProjectsForRoad(roadNameForLookup);
        if (projects == null || projects.Length == 0)
        {
            Debug.LogWarning($"[PanelManager] Learn More found no projects for road '{roadNameForLookup}'.");
            SetState(UIState.Success_NoData, roadNameForLookup);
            return;
        }

        HideAllPanels(); //Hide all panels before showing Learn More content and project list
        HideAllButtons(); //Hide buttons first, then show only Report Issue for road-level action
        HideReportSubPanels();
        SetStatusMessage(string.Empty); //Clear state status text so the Learn More header remains the focus

        ClearExistingProjectCards();
        InstantiateProjectCards(projects);
        UpdateRoadHeaderTexts(roadNameForLookup, projects.Length);

        if (roadHeaderPanel != null) roadHeaderPanel.SetActive(true);
        if (projectCardScrollPanel != null) projectCardScrollPanel.SetActive(true);
        ShowButton(reportIssueButton, interactable: true);

        debugOverlayController?.ShowUIState("LearnMore_ProjectCards", roadNameForLookup);
    }

    // Method for the ReportIssue button, what we want here is that on click the UI State changes to the Capture Image one
    public void OnReportIssueButtonPressed()
    {
        HideAllPanels(); //Hide all panels before showing the panels for ImageCapture
        HideAllButtons(); //Hide all buttons
        SetState(UIState.CaptureImage); 
    }

    public void OnCaptureButtonPressed()
    {
        if(sharedCameraManager == null || !sharedCameraManager.IsReady()) return; //Safety check to ensure camera manager is ready before trying to capture, prevents errors

        Texture2D img = sharedCameraManager.CapturePreviewFrame();

        if (img == null)
        {
            debugOverlayController.ShowReportFlowState("[PanelManager] Capture failed: No image returned from camera.");
            SetStatusMessage("Capture failed. Please try again.");
            return;
        }

        //Assign the frozen snapshot to the preview RawImage so the user can review before submitting 
        if(previewRawImage != null)
        {
            previewRawImage.texture = img;
            SetReportFlowState(ReportFlowState.Preview); //Move to the preview sub-state where user can choose to submit for analysis or retake
            sharedCameraManager.ApplyCaptureRotationTo(previewRawImage); //Apply the correct rotation and mirroring to the preview image so it matches what the user just captured, this is done by the SharedCameraManager which has the necessary info about the camera feed orientation
            
        }
    }

    public void OnRetakeButtonPressed()
    {
        CancelInvoke("OnAnalysisComplete");

        if (sharedCameraManager != null)
        sharedCameraManager.ClearPreviewImage();

        if (previewRawImage != null)
        previewRawImage.texture = null;
        currentCVResult = null; //Discard any previously stored result so stale data cannot be submitted
        cvResultPopulator?.Clear();

        SetReportFlowState(ReportFlowState.LiveCapture); //Go back to the live capture state to let user try taking the picture again
    }

    public void OnAnalyseButtonPressed()
    {
        string modelName = modelDropdownController != null ? modelDropdownController.SelectedModelName : string.Empty;
        if (string.IsNullOrEmpty(modelName))
        {
            debugOverlayController?.ShowCVAPIResult("[PanelManager] Analyse blocked: no Issue Type selected");
            return;
        }

        Texture2D image = sharedCameraManager?.GetPreviewImage();
        //When maps are locked (FullMock or CVLive), use the hardcoded stand-ins;
        LocationServiceManager.LocationSnapshot snapshot; 
        string roadName;
        if (isMapsLocked)
        {
            snapshot = new LocationServiceManager.LocationSnapshot(26.515430f,80.235020f,5f); //Nursery GPS value
            roadName = string.IsNullOrEmpty(currentRoadName)? testRoadName: currentRoadName; 
        }
        else
        {
            snapshot = locationServiceManager.LastLocationSnapshot;
            roadName = googleMapsServices.ActiveLocationName;
        }

        SetReportFlowState(ReportFlowState.AwaitingAnalysis); 

        //FullMock does not call CV APi. AwaitingAnalysis is already scheduled OnAnalysisComplete.
        if (isFullMock)
        {
            debugOverlayController?.ShowCVAPIResult("[PanelManager] FullMock active. Skipping real CV call");
            return;
        }

        submissionManager.SubmitReport(
            image, snapshot, roadName, modelName,
            onSuccess: (CVAnalysisResult result) => 
            {
                if (result == null)
                {
                    if (isCVLive)
                    {
                        debugOverlayController?.ShowCVAPIResult("[PanelManager] CVLive 200-OK smoke test returned null payload. Using mock result for UI flow");
                        result = BuildMockCVResult(); //Builds a mock CV result to keep the demo flow intact
                    }
                    else
                    {
                        debugOverlayController?.ShowCVAPIResult("[PanelManager] CV returned null reference in Off mode.");
                        SetReportFlowState (ReportFlowState.Preview); //Return to preview to let user try again, since we didn't get a valid result back from the CV API
                        return;
                    }
                }

                currentCVResult = result; //store so OnSubmitGrievanceButtonPressed can pass it to GrievanceSubmissionManager
                //Force CV Card issue type to match drop-down selected model for consistency
                if(!string.IsNullOrEmpty(modelName))
                {
                    result.defect_type = modelName;
                }

                cvResultPopulator.Populate(result, roadName);  
                SetReportFlowState(ReportFlowState.ReviewResult); // fills the panel
            },

            onFailure: (string error) =>
            {
                debugOverlayController?.ShowCVAPIResult($"CV Analysis Failed: {error}");
                SetReportFlowState(ReportFlowState.Preview); // let user retry
            }
        );

    }

    public void OnSubmitGrievanceButtonPressed()
    {
        if (grievanceSubmissionManager == null)//Main null check 
            {
                debugOverlayController?.ShowGrievanceSubmitResult("[PanelManager] GrievanceSubmissionManager is not assigned in the Inspector");
                return;
            }

        // Full Mock: keep[ entire backend flow fake (no grievance API call
        if (isFullMock)
        {
            debugOverlayController?.ShowGrievanceSubmitResult("[PanelManager] FullMock active. Skipping real API call");
            return;
        }

        LocationServiceManager.LocationSnapshot snapshot;
        string roadName; 

        if (isMapsLocked)
        {
            snapshot = new LocationServiceManager.LocationSnapshot(26.515430f,80.235020f,5f); //Nursery GPS value
            roadName = string.IsNullOrEmpty(currentRoadName) ? testRoadName : currentRoadName;  
        }
        else
        {
            snapshot = locationServiceManager.LastLocationSnapshot;
            roadName = googleMapsServices.ActiveLocationName;
        }
        SetReportFlowState(ReportFlowState.SubmittingGrievance);

         grievanceSubmissionManager.SubmitGrievance(
            currentCVResult,
            snapshot,
            roadName,
            onSuccess: () =>
            {
                currentCVResult = null; // clear after successful submission
                SetReportFlowState(ReportFlowState.Success);
            },
            onFailure: (string error) =>
            {
                debugOverlayController?.ShowGrievanceSubmitResult($"[PanelManager] Grievance submission failed: {error}");
                Debug.LogError($"[PanelManager] Grievance submission failed: {error}");
                SetReportFlowState(ReportFlowState.ReviewResult); // return user to result so they can retry
            }
        );
    }

    public void OnBackButtonPressed()
    {
        CancelInvoke("OnAnalysisComplete");

        if(sharedCameraManager != null)
        {
            sharedCameraManager.ClearPreviewImage(); //Clear the preview image in the camera manager to free up memory, since we are going back and discarding the current capture
        }

        if (previewRawImage != null)
        {
            previewRawImage.texture = null; //Clear the preview image shown to the user to free up memory and reset the UI for next capture
        }
        currentCVResult = null; //discard any stored CV result since we are going back and not submitting, this prevents stale data from being submitted if user goes back but then later decides to submit without retaking the picture
        cvResultPopulator?.Clear();
        SetState(UIState.Idle);
    }

    //Dummy method after Awaiting Analysis timer is hit,replace with real CV response handling once SB's API is live
    // Simulates a CVAnalysisResult so the full ReviewResult flow can be tested end-to-end

    private void OnAnalysisComplete()
    {
        if (!isFullMock)
        {
            return;
        }

        CVAnalysisResult fakeResult = BuildMockCVResult();
        currentCVResult = fakeResult; //Store the fake result so it cane be it can be submitted if user taps submit without retaking
       
       string selectedIssueType = modelDropdownController != null ? modelDropdownController.SelectedModelName : string.Empty;
       if (!string.IsNullOrEmpty(selectedIssueType))
       {
           fakeResult.defect_type = selectedIssueType;
       }

       cvResultPopulator.Populate(fakeResult, string.IsNullOrEmpty(currentRoadName) ? testRoadName : currentRoadName);
        SetReportFlowState(ReportFlowState.ReviewResult); 
    }

    private CVAnalysisResult BuildMockCVResult()
    {
        return new CVAnalysisResult
        {
            defect_type = "Pothole",
            confidence = 0.92f,
            message = "Large pothole detected, needs immediate attention for repair."
        };
    }

    //AddInfo button OnClick - place holder for future user data submission feature
    public void OnAddInfoButtonPressed()
    {
        AddData(currentRoadName); //placeholder method for future feature where users can submit data about a location if there is no existing data for it
    }

    //Retry button OnClick, will restart the full flow 
    public void OnRetryButtonPressed()
    {
       switch (currentState)
        {
            case UIState.Error_GPSAccuracy:
                Debug.Log("[PanelManager] retry from=Error_GPSAccuracy → Loading_LocationSampling");
                if (locationServiceManager == null || googleMapsServices == null)
                {
                    SetState(UIState.Error);
                    return;
                }

                StartLocationSamplingAndResolveRoad();
                break;

            default:
            SetState(UIState.Idle);
            break;
        }

    }

    #endregion

    #region GPS + API Trigger
        //Method for simulating the API response when maps are locked by TestMode.
        private void SimulateAPIResponse()
        {
            SetState(UIState.Loading_API); 
            Invoke("SimulateSuccess", 2f); //Simulate a success message after 2s 
        }

        private void SimulateSuccess()
        {
        OnRoadNameReceived(testRoadName);
        }

    #endregion 

    #region API Callback Method
    //Fired by GoogleMapsServices script when we get the road name back from the API, this is where we determine if we go to the Success_WithData or Success_NoData state based on if we have JSON data for that road name or not
    private void OnRoadNameReceived(string roadName)
    {
        if (roadName == "ERROR_NETWORK")
        {
            SetState(UIState.Error); //If we get network error from the API call, we want to show that in the messaging
            Debug.LogWarning("[PanelManager] API call failed due to network error.");
            return;
        }

        if (roadName == "ERROR_NO_DATA" || string.IsNullOrEmpty(roadName))
        {
            SetState(UIState.Error); 
            Debug.LogWarning("[PanelManager] API call failed due to no data.");
            return;
        }

        string matchedRoadName = locationJSONLoader?.GetBestMatchingRoadName(roadName);
        string roadNameForState = string.IsNullOrEmpty(matchedRoadName) ? roadName : matchedRoadName;
        int projectCount = locationJSONLoader != null ? locationJSONLoader.GetProjectCountForRoad(roadNameForState) : 0;

        Debug.Log($"[PanelManager] Road: '{roadName}' → matched: '{roadNameForState}' | projects: {projectCount}");

        if (projectCount > 0)
            SetState(UIState.Success_WithData, roadNameForState);
        else
            SetState(UIState.Success_NoData, roadNameForState);
    }
    #endregion 

    #region Future Feature - User Input Place Holder Methods 
    //Placeholder- will later add user supported infrastructure data
    private void AddData(string roadName)
    {
        Debug.Log($"[PanelManager] AddData called for road:'{roadName}' - feature to be added");
        //Spot for CV powered submission panel to be wired with backend
    }
    #endregion 

    #region UI Helper
    private void SetStatusMessage(string message)
    {
        if (statusMessageText != null)
        {
          statusMessageText.text = message;  
        }   
    }

    private void SetImageCaptureInstructionText(string message)
    {
        if (imageCaptureInstructionText != null)
        {
          imageCaptureInstructionText.text = message;  
        }   
    }

    private void UpdateRoadHeaderTexts(string roadName, int projectCount)
    {
        if (roadHeaderText != null)
        {
            roadHeaderText.text = roadName;
        }

        if (projectCountText != null)
        {
            string suffix = projectCount == 1 ? "project" : "projects";
            projectCountText.text = $"{projectCount} {suffix}";
        }
    }

    private void ClearExistingProjectCards()
    {
        if (projectCardContentContainer == null)
        {
            return;
        }

        for (int i = projectCardContentContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(projectCardContentContainer.GetChild(i).gameObject);
        }
    }

    private void InstantiateProjectCards(ProjectRecord[] projects)
    {
        if (projectCardContentContainer == null || projectCardPrefab == null || projects == null)
        {
            return;
        }

        foreach (ProjectRecord project in projects)
        {
            GameObject cardObject = Instantiate(projectCardPrefab, projectCardContentContainer);
            ProjectCardController cardController = cardObject.GetComponent<ProjectCardController>();

            if (cardController != null)
            {
                cardController.Populate(project);
            }
            else
            {
                Debug.LogWarning("[PanelManager] Project card prefab is missing ProjectCardController.");
            }
        }

        // Optional fallback if content height appears stale on specific devices:
        // LayoutRebuilder.ForceRebuildLayoutImmediate(projectCardContentContainer as RectTransform);
    }

    //Hide all four Statebuttons before each state shows the correct on
    private void HideAllButtons()
    {
        if (checkInButton != null) checkInButton.SetActive(false);
        if (learnMoreButton != null) learnMoreButton.SetActive(false); 
        if (addInfoButton != null) addInfoButton.SetActive(false); 
        if (retryButton != null) retryButton.SetActive(false);
        if (reportIssueButton != null) reportIssueButton.SetActive(false);
        if (captureButton != null) captureButton.SetActive(false);
        if(backButton != null) backButton.SetActive(false); 
        if (analyseButton != null) analyseButton.SetActive(false); 
        if (retakeButton != null) retakeButton.SetActive(false);
        if(submitGrievanceButton != null) submitGrievanceButton.SetActive(false);
    }

    //Show a button and set its interactability
    private void ShowButton(GameObject buttonObject, bool interactable)
    {
        if (buttonObject == null) return;
        buttonObject.SetActive(true); 
        var btn = buttonObject.GetComponent<Button>();
        if(btn != null) btn.interactable = interactable;
    }

    public void HideAllPanels()
    {
        if (logoScreen != null) logoScreen.SetActive(false); 
        if (noLocationPanel != null) noLocationPanel.SetActive(false);
        if (infoPanelGameObject != null) infoPanelGameObject.SetActive(false);
        if (panelPopulatorGameObject != null) panelPopulatorGameObject.SetActive(false);
        if (roadHeaderPanel != null) roadHeaderPanel.SetActive(false);
        if (projectCardScrollPanel != null) projectCardScrollPanel.SetActive(false);
    }

    private void HideReportSubPanels()
{
    if (instructionpanel != null) instructionpanel.SetActive(false);
    if (captureSquare != null) captureSquare.SetActive(false);
    if(capturedFramePanel != null) capturedFramePanel.SetActive(false); 
        if (modelDropdownPanel != null) modelDropdownPanel.SetActive(false);
    if (awaitingPanel != null) awaitingPanel.SetActive(false);
    if(reviewResultsPanel != null) reviewResultsPanel.SetActive(false);
    if(successSubmissionPanel != null) successSubmissionPanel.SetActive(false); // HACK: force-hide success panel when leaving report flow
}
    #endregion
}