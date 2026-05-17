using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

// Responsible for: building the submission payload, encoding the image, sending the POST request
// to the CV API, and returning the result via callbacks.
// PanelManager calls SubmitReport() — this script handles everything else.

public class ReportSubmissionManager : MonoBehaviour
{
    private const string CV_API_URL = "https://product.airawat.org/cctv-analytics/predict";

    [Tooltip("Supported models verbatim: animals, garbage, potholes, fallentrees, hoardings, roaddamage")]
    [SerializeField] private string modelName = "potholes";

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
        //TODO: Delete the commented out metadata, will be sending these with submit button.
        // form.AddField("road_name", roadName ?? "UNKNOWN");
        // form.AddField("latitude", snapshot.latitude.ToString("F6"));
        // form.AddField("longitude", snapshot.longitude.ToString("F6"));
        // form.AddField("horizontal_accuracy", snapshot.horizontalAccuracy.ToString("F2"));
        // form.AddField("timestamp", snapshot.timestamp.ToString("o")); // ISO 8601 format
        form.AddField("model_name", modelName); //tells the CV Server which model to use for analysis

        // Binary field — the image
        // "file"       = field name the CV server expects
        // "report.jpg" = filename the server will see
        // "image/jpeg" = MIME type
        form.AddBinaryData("file", jpgBytes, "report.jpg", "image/jpeg");

        // Step 3: Send the POST request
        using (UnityWebRequest request = UnityWebRequest.Post(CV_API_URL, form))
        {
            request.timeout = 30; // CV analysis can take time and after 30 it will loop back
            yield return request.SendWebRequest();

            debugOverlayController?.ShowCVAPIResult(
                $"Response | Code: {request.responseCode} | Result: {request.result}");

            // Step 4: Handle failure
            // Three failure shapes we care about:
            //   (a) Transport failure  - no HTTP response at all (no internet, DNS, timeout, TLS).
            //   (b) HTTP non-200       - server replied, but with an error code. 422 is the most
            //                            useful one for us: FastAPI returns the validation error
            //                            body explaining which field it rejected (e.g. wrong
            //                            model_name). We log that body verbatim so the engineer
            //                            can read it on-device without rebuilding.
            //   (c) Anything else      - fall through to onFailure with whatever we have.

            string responseBody = request.downloadHandler?.text ?? "";
            //We are creating different buckets of the responses we will get to get a 
            //better sense of why it the request has failed when it does. 
            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.DataProcessingError)
            {
                //(a) Transport failure - request.error is meaningful here, it contains the error message (e.g. "No internet connection", "TLS handshake failed", "Request timed out")
                debugOverlayController?.ShowCVAPIResult($"Network error: {request.error}");
                onFailure?.Invoke($"Network error: {request.error}");
                yield break;
            }

            if (request.responseCode != 200)
            {
                //HTTP non-200 - log the response body so the 422 validation details are visible
                debugOverlayController?.ShowCVAPIResult(
                    $"HTTP {request.responseCode} | Body: {responseBody}");
                onFailure?.Invoke($"HTTP {request.responseCode}: {responseBody}");
                yield break;
            }

            // Step 5: Handle success — SMOKE TEST ONLY
            // We have a 200 from the real CV endpoint. We are NOT parsing the response yet:
            // the PredictionResponse schema (detected, detections[], bbox, etc.) needs to be
            // confirmed with the CV engineer before we rewrite CVAnalysisResult and the
            // populator. For now we log the raw body for inspection and hand back null.
            // PanelManager's onSuccess lambda treats null as "use the mock CVAnalysisResult"
            // so the rest of the demo flow keeps working.
            // TODO: parse responseBody into CVAnalysisResult once the PredictionResponse
            // field names are confirmed. Drop the null fallback in PanelManager at the same time.
            Debug.Log($"[ReportSubmissionManager] CV 200 OK — raw body: {responseBody}");
            debugOverlayController?.ShowCVAPIResult($"200 OK | bytes={responseBody.Length}");
            onSuccess?.Invoke(null);
        }
    }
}
