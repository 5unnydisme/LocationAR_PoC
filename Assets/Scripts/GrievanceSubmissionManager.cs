using System;
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
    private const string grievanceApiUrl = "https://product.airawat.org/kmc-pgr/v1/ingestion/complaint";

    [Tooltip("X-API-Key header value required by the grievance API.")]
    [SerializeField] private string grievanceApiKey = "1Diq7jeowJGTosFK4p4Q4j-bF92vtLTf8DW2gDEk9YI";

    [Tooltip("Prefix used to build each complaint's external ID. A Unix timestamp is appended per submission to keep IDs unique.")]
    [SerializeField] private string externalIdPrefix = "MA";

    [Tooltip("Default complaint category when CV output is missing or empty.")]
    [SerializeField] private string defaultCategory = "";

    // [Tooltip("Optional department value expected by the backend.")]
    // [SerializeField] private string defaultDepartment = "";

    [Tooltip("Complaint status to send with every submission.")]
    [SerializeField] private string defaultStatus = "Pending";

    // [Tooltip("Optional ward value for submissions.")]
    // [SerializeField] private string defaultWard = "";

    // [Tooltip("Optional zone value for submissions.")]
    // [SerializeField] private string defaultZone = "";

    [Tooltip("JPEG quality used when encoding the complaint image (0-100).")]
    [SerializeField] private int jpegQuality = 85;

    [Tooltip("Request timeout in seconds.")]
    [SerializeField] private int requestTimeoutSeconds = 30;

    [Header("References")]
    [Tooltip("Reference to DebugOverlayController to see grievance submission steps on the device")]
    [SerializeField] private DebugOverlayController debugOverlayController;
    #endregion

    //Public entry point- called by PanelManager when the Submit button is pressed
    public void SubmitGrievance(
        Texture2D image,
        CVAnalysisResult cvResult,
        LocationServiceManager.LocationSnapshot snapshot,
        string roadName,
        System.Action<string> onSuccess,
        System.Action<string> onFailure)

    {
        if (image == null)
        {
            onFailure?.Invoke("No image to submit");
            return;
        }

        StartCoroutine(SendGrievanceToAPI(image, cvResult, snapshot, roadName, onSuccess, onFailure));
    }

    private IEnumerator SendGrievanceToAPI(
        Texture2D image,
        CVAnalysisResult cvResult,
        LocationServiceManager.LocationSnapshot snapshot,
        string roadName, 
        System.Action<string> onSuccess, 
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

        if (string.IsNullOrEmpty(grievanceApiKey))
        {
            const string message = "Missing grievance API key in Inspector.";
            debugOverlayController?.ShowGrievanceSubmitResult($"[GrievanceSubmissionManager] {message}");
            onFailure?.Invoke(message);
            yield break;
        }

        debugOverlayController?.ShowGrievanceSubmitResult("[GrievanceSubmissionManager] Building grievance multipart payload...");

        // Step 2: Build multipart form expected by /v1/ingestion/complaint
        byte[] jpgBytes = image.EncodeToJPG(jpegQuality);
        string externalId = $"{externalIdPrefix}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        string startedAt = snapshot.timestamp.ToUniversalTime().ToString("o");
        string category = !string.IsNullOrEmpty(cvResult?.defect_type) ? cvResult.defect_type : defaultCategory; //TODO change this to the new string agreed with SB

        WWWForm form = new WWWForm();
        form.AddField("external_id", externalId);
        form.AddField("category", category);
        form.AddField("started_at", startedAt);
        form.AddField("description", BuildDescription(cvResult));
        form.AddField("status", defaultStatus);
        form.AddField("latitude", snapshot.latitude.ToString("F6"));
        form.AddField("longitude", snapshot.longitude.ToString("F6"));
        form.AddField("accuracy", snapshot.horizontalAccuracy.ToString("F2"));
        form.AddField("road_name", string.IsNullOrEmpty(roadName) ? "UNKNOWN" : roadName);

        // if (!string.IsNullOrEmpty(cvResult?.defect_type))
        // {
        //     form.AddField("subcategory", cvResult.defect_type);
        // }

        // if (!string.IsNullOrEmpty(defaultDepartment))
        // {
        //     form.AddField("department", defaultDepartment);
        // }

        // if (!string.IsNullOrEmpty(defaultWard))
        // {
        //     form.AddField("ward", defaultWard);
        // }

        // if (!string.IsNullOrEmpty(defaultZone))
        // {
        //     form.AddField("zone", defaultZone);
        // }

        form.AddBinaryData("image", jpgBytes, "complaint.jpg", "image/jpeg");

        debugOverlayController?.ShowGrievanceSubmitResult($"[GrievanceSubmissionManager] Sending grievance POST...");

        //Step 3: Send multipart POST request
        using (UnityWebRequest request = UnityWebRequest.Post(grievanceApiUrl, form))
        {
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("X-API-Key", grievanceApiKey);

            request.timeout = requestTimeoutSeconds;
            yield return request.SendWebRequest();

            debugOverlayController?.ShowGrievanceSubmitResult(
            $"[GrievanceSubmissionManager] Response | Code:{request.responseCode} | Result: {request.result}");

            string responseBody = request.downloadHandler?.text ?? "";
           
            //Step 4: Handle failure
            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorMsg = $"Submission failed: {request.error} (code{request.responseCode})";
                debugOverlayController?.ShowGrievanceSubmitResult($"[GrievanceSubmissionManager] {errorMsg}");
                debugOverlayController?.ShowGrievanceSubmitResult($"[GrievanceSubmissionManager] Response body: {responseBody}");
                onFailure?.Invoke(errorMsg);
                yield break;
            }

            if (request.responseCode < 200 || request.responseCode >= 300)
            {
                string errorMsg = $"Submission failed: HTTP {request.responseCode}";
                debugOverlayController?.ShowGrievanceSubmitResult($"[GrievanceSubmissionManager] {errorMsg}");
                debugOverlayController?.ShowGrievanceSubmitResult($"[GrievanceSubmissionManager] Response body: {responseBody}");
                onFailure?.Invoke($"{errorMsg} | {responseBody}");
                yield break;
            }

            //Step 5: Success
            //No POCO parsing yet- treat any 200 as success until API contact is confirmed
                debugOverlayController?.ShowGrievanceSubmitResult("[GrievanceSubmissionManager] Grievance submitted successfully.");
            onSuccess?.Invoke(request.responseCode.ToString());
        }
    }

    private string BuildDescription(CVAnalysisResult cvResult)
    {
        if (cvResult == null)
        {
            return "Complaint submitted from mobile app.";
        }

        string message = string.IsNullOrEmpty(cvResult.message) ? "Complaint submitted from mobile app." : cvResult.message;
        string urgency = string.IsNullOrEmpty(cvResult.urgency) ? "unknown" : cvResult.urgency;
        return $"{message} | urgency:{urgency} | confidence:{cvResult.confidence:F2}";
    }

}

