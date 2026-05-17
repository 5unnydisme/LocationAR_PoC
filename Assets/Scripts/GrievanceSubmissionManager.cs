using System;
using System.Collections;
using System.Text;
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
     private const string grievanceApiUrl = "http://35.154.241.163:8002/v1/ingestion/run"; 
         
    // HARDCODED for 200 SMOKE TEST — move to secrets.json post-demo
    private const string apiKey = "1Diq7jeowJGTosFK4p4Q4j-bF92vtLTf8DW2gDEk9YI";

    [Tooltip("Prefix used to build each record's External ID sent with every grievance to identify the reports coming from the mobile app. A Unix timestamp is appended at submission time to keep IDs unique.")]
    [SerializeField] private string externalId ="MA";

    [Tooltip("Request timeout in seconds.")]
    [SerializeField] private int requestTimeoutSeconds = 30;

    [Header("References")]
    [Tooltip("Reference to DebugOverlayController to see grievance submission steps on the device")]
    [SerializeField] private DebugOverlayController debugOverlayController;
    #endregion

    //DTO - Data Transfer Objects for the ingestion payload expected by the API
    [System.Serializable]
    public class IngestionRequest
    {
        public IngestionComplaintRecord[] records;
    }

    [System.Serializable]
    public class IngestionComplaintRecord
    {
        public string external_id;
        public string created_at;
        public string updated_at;
        public string category;
        public string status;
        public string zone;
        public string ward;
    }

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
            onFailure?.Invoke("No network connection");
            yield break;
        }

            debugOverlayController?.ShowGrievanceSubmitResult("[GrievanceSubmissionManager] Building grievance payload...");

        //Step 2: Build record payload and wrap in top-level ingestion request shape
        string createdAt = snapshot.timestamp.ToString("o");

        IngestionComplaintRecord record = new IngestionComplaintRecord
        {
            external_id = $"{externalId}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
            created_at = createdAt,
            updated_at = createdAt,
            category = "Roads", // HARDCODED: no category source wired from app data yet
            status = "Pending", // HARDCODED: no lifecycle/status source wired from app data yet
            zone = "Zone 6", // HARDCODED: no zone mapping available from current app context
            ward = "Vishnupuri-40" // HARDCODED: no ward mapping available from current app context
        };

        IngestionRequest requestPayload = new IngestionRequest
        {
            records = new[] { record }
        };

        string json = JsonUtility.ToJson(requestPayload);
        debugOverlayController?.ShowGrievanceSubmitResult($"[GrievanceSubmissionManager] Payload: {json}");

        byte[] bodyBytes = Encoding.UTF8.GetBytes(json); //Check if this is a helper method
        debugOverlayController?.ShowGrievanceSubmitResult($"[GrievanceSubmissionManager] Sending grievance POST...");

        //Step 3: Send raw JSON POST request to API
        //Use bare UnityWebRequest + UploadHandlerRaw so the API receives application/json.
        using (UnityWebRequest request = new UnityWebRequest(grievanceApiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("X-API-Key", apiKey);

            request.timeout = requestTimeoutSeconds;
            yield return request.SendWebRequest();

            debugOverlayController?.ShowGrievanceSubmitResult(
            $"[GrievanceSubmissionManager] Response | Code:{request.responseCode} | Result: {request.result}");
           
            //Step 4: Handle failure
            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorMsg = $"Submission failed: {request.error} (code{request.responseCode})";
                debugOverlayController?.ShowGrievanceSubmitResult($"[GrievanceSubmissionManager] {errorMsg}");
               debugOverlayController?.ShowGrievanceSubmitResult($"[GrievanceSubmissionManager] Response body: {request.downloadHandler?.text}");
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

