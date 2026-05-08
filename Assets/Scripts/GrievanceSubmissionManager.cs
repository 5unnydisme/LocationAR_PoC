using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

//Responsible for building the grievance payload. POSTing it to the grievance database
//and returning success/failure via callbacks.
//PanelManager calls SubmitGrievance() - this is script handles everything else.
//Pattern mirrors the ReportSubmissionManager, but with a different payload and endpoint.

public class GrievanceSubmissionManager : MonoBehaviour
{
    #region Inspector Configuration
    [Header("The full URL of the Grievance submission endpoint")]
    [Tooltip("The full URL of the grievance database endpoint.")]
     private string grievanceApiUrl = "https://webhook.site/b6bdfc05-add8-4a7c-a074-fca4b5ead106"; //TODO: Replace with actual API link 

    [Tooltip("External ID sent with every grievance to identify the reports coming from the mobile app.")]
    [SerializeField] private string externalId ="MA001";

    [Tooltip("Request timeout in seconds.")]
    [SerializeField] private int requestTimeoutSeconds = 30;

    [Header("References")]
    [Tooltip("Reference to DebugOverlayController to see grievance submission steps on the device")]
    [SerializeField] private DebugOverlayController debugOverlayController;
    #endregion

    //Public entry point- called by PanelManager when the Submit button is pressed
    public void SubmitGrievance(
        CVAnalysisResult cvResult,
        LocationServiceManager.LocationSnapshot snapshot,
        string roadName,
        System.Action onSuccess,
        System.Action<string> onFailure)

    {
        if (cvResult == null)
        {
            onFailure?.Invoke ("No CV analysis result to submit");
            return;
        }

        StartCoroutine(SendGrievanceToAPI(cvResult, snapshot, roadName, onSuccess, onFailure));  
    }

    private IEnumerator SendGrievanceToAPI(
        CVAnalysisResult cvResult,
        LocationServiceManager.LocationSnapshot snapshot,
        string roadName, 
        System.Action onSuccess, 
        System.Action<string> onFailure)
    {
        //Step 1: Offline check
        //Application,internetReachability tells us if a network interface is up.
        //It does not guarantee the server is reachable, but it's a fast local check
        //that avoids a pointless timeout wait when the device has no connectivity
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
                debugOverlayController?.ShowGrievanceSubmitResult("[GrievanceSubmissionManager] Offline - skipping grievance POST");
            onFailure?.Invoke("No netwrok connection");
            yield break;
        }

            debugOverlayController?.ShowGrievanceSubmitResult("[GrievanceSubmissionManager] Building grievance payload...");

        //Step 2: Build the form payload
        //Text-only POST- no image being sent, that is sent to CVAPI for analysis 
        //WWWForm encodes each AddField() call as a key-value pair in the multipart body
        WWWForm form = new WWWForm();

        //CV analysis fields- the result produced by ReportSubmissionManager
        form.AddField("defect_type",    cvResult.defect_type ??"");
        form.AddField("urgency",        cvResult.urgency ??"");
        form.AddField("confidence",     cvResult.confidence.ToString("F4"));
        form.AddField("message",        cvResult.message ??"");

        //Location fields
        form.AddField("road_name",           roadName ??"UKNOWN");
        form.AddField("latitude",            snapshot.latitude.ToString("F6"));
        form.AddField("longitude",           snapshot.longitude.ToString("F6"));
        form.AddField("horizontal_accuracy", snapshot.horizontalAccuracy.ToString("F2"));
        form.AddField("timestamp",           snapshot.timestamp.ToString("o")); // ISO 8601

        //App-source identifier
        //The grievance database uses this field to know the report came from the mobile app
        //The value is Inspector-configurable and can be edited without code change
        form.AddField("external_id", externalId);

            debugOverlayController?.ShowGrievanceSubmitResult("[GrievanceSubmissionManager] Sending grievance POST...");

        //Step 3: Send the POST request
        //UnityWebRequest.Post() with a WWWForm sets the correct Content-Type header automatically
        //The using block disposes the native handle when done-prevents memory leaks
        using (UnityWebRequest request = UnityWebRequest.Post(grievanceApiUrl, form))
        {
            request.timeout = requestTimeoutSeconds;
            yield return request.SendWebRequest();

                debugOverlayController?.ShowGrievanceSubmitResult(
                $"[GrievanceSubmissionManager] Response | Code:{request.responseCode} | Result: {request.result}");

            //Step 4: Handle failure
            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorMsg = $"Submission failed: {request.error} (code{request.responseCode})";
                    debugOverlayController?.ShowGrievanceSubmitResult($"[GrievanceSubmissionManager] {errorMsg}");
                onFailure?.Invoke(errorMsg);
                yield break;
            }

            //Step 5: Success
            //No POCO parsing yet- treat any 200 as success until API contact is confirmed
                debugOverlayController?.ShowGrievanceSubmitResult("[GrievanceSubmissionManager] Grievance submitted successfully.");
            onSuccess?.Invoke();
        }
    }
}
