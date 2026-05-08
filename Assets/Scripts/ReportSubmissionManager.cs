using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

// Responsible for: building the submission payload, encoding the image, sending the POST request
// to the CV API, and returning the result via callbacks.
// PanelManager calls SubmitReport() — this script handles everything else.

public class ReportSubmissionManager : MonoBehaviour
{
    [Tooltip("The full URL of the CV analysis endpoint")]
    private string cvApiUrl = "https://webhook.site/474a07a2-86d1-4bc6-8452-9bb1f56bea10"; // Replace with actual CV API endpoint

    [Tooltip("Reference to DebugOverlayController to see submission steps on device")]
    [SerializeField] private DebugOverlayController debugOverlayController;

    // Public entry point — called by PanelManager when the Analyse button is pressed
    public void SubmitReport(
        Texture2D image,
        LocationServiceManager.LocationSnapshot snapshot,
        string roadName,
        System.Action<CVAnalysisResult> onSuccess,
        System.Action<string> onFailure)
    {
        if (image == null)
        {
            onFailure?.Invoke("No image to submit");
            return;
        }

        StartCoroutine(SendReportToAPI(image, snapshot, roadName, onSuccess, onFailure));
    }

    private IEnumerator SendReportToAPI(
        Texture2D image,
        LocationServiceManager.LocationSnapshot snapshot,
        string roadName,
        System.Action<CVAnalysisResult> onSuccess,
        System.Action<string> onFailure)
    {
        // Step 1: Encode the Texture2D into a JPG byte array
        // EncodeToJPG() compresses the raw pixel data into a standard JPG file in memory
        // 85 = quality (0-100), a good balance between quality and file size
        debugOverlayController?.ShowCVAPIResult("Encoding image...");
        byte[] jpgBytes = image.EncodeToJPG(85);
        debugOverlayController?.ShowCVAPIResult("Image encoded, building payload...");

        // Step 2: Build the multipart form body
        // WWWForm is Unity's helper for multipart/form-data — each AddField() is a text input,
        // AddBinaryData() is the file attachment
        WWWForm form = new WWWForm();

        // Text fields — metadata
        form.AddField("road_name", roadName ?? "UNKNOWN");
        form.AddField("latitude", snapshot.latitude.ToString("F6"));
        form.AddField("longitude", snapshot.longitude.ToString("F6"));
        form.AddField("horizontal_accuracy", snapshot.horizontalAccuracy.ToString("F2"));
        form.AddField("timestamp", snapshot.timestamp.ToString("o")); // ISO 8601 format

        // Binary field — the image
        // "image"      = field name the CV server expects
        // "report.jpg" = filename the server will see
        // "image/jpeg" = MIME type
        form.AddBinaryData("image", jpgBytes, "report.jpg", "image/jpeg");

        // Step 3: Send the POST request
        using (UnityWebRequest request = UnityWebRequest.Post(cvApiUrl, form))
        {
            request.timeout = 30; // CV analysis can take time and after 30 it will loop back
            yield return request.SendWebRequest();

            debugOverlayController?.ShowCVAPIResult(
                $"Response | Code: {request.responseCode} | Result: {request.result}");

            // Step 4: Handle failure
            if (request.result != UnityWebRequest.Result.Success)
            {
                debugOverlayController?.ShowCVAPIResult($"Request failed: {request.error}");
                onFailure?.Invoke(request.error);
                yield break;
            }

            // Step 5: Handle success — parse the JSON response
            string responseJSON = request.downloadHandler.text;
            debugOverlayController?.ShowCVAPIResult($"Raw JSON: {responseJSON}");

            if (string.IsNullOrEmpty(responseJSON))
            {
                onFailure?.Invoke("Empty response from server");
                yield break;
            }

            CVAnalysisResult result = JsonUtility.FromJson<CVAnalysisResult>(responseJSON);

            if (result == null)
            {
                debugOverlayController?.ShowCVAPIResult("Failed to parse CV response");
                onFailure?.Invoke("Invalid JSON response");
                yield break;
            }

            debugOverlayController?.ShowCVAPIResult($"Analysis complete: {result.defect_type}");
            onSuccess?.Invoke(result);
        }
    }
}
  