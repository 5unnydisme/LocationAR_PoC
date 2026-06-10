using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

// Responsible for: building the submission payload, encoding the image, sending the POST request
// to the CV API, and returning the result via callbacks.
// PanelManager calls SubmitReport() — this script handles everything else.

public class ReportSubmissionManager : MonoBehaviour
{
    private const string CV_API_URL = "https://product.airawat.org/cctv-analytics/predict";

    [Tooltip("Reference to DebugOverlayController to see submission steps on device")]
    [SerializeField] private DebugOverlayController debugOverlayController;

    // Public entry point — called by PanelManager when the Analyse button is pressed
    public void SubmitReport(
        Texture2D image,
        LocationServiceManager.LocationSnapshot snapshot,
        string roadName,
        string modelName,
        System.Action<CVAnalysisResult, string> onSuccess,
        System.Action<string> onFailure)
    {
        if (image == null)
        {
            onFailure?.Invoke("No image to submit");
            return;
        }

        StartCoroutine(SendReportToAPI(image, snapshot, roadName, modelName, onSuccess, onFailure));
    }

    private IEnumerator SendReportToAPI(
        Texture2D image,
        LocationServiceManager.LocationSnapshot snapshot,
        string roadName,
        string modelName,
        System.Action<CVAnalysisResult, string> onSuccess,
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
                string errorMsg = $"Network/Data error | Code: {request.responseCode} | Result: {request.result} | Error: {request.error}";
                debugOverlayController?.ShowCVAPIResult(errorMsg);
                onFailure?.Invoke(errorMsg);
                yield break;
            }

            if (request.responseCode != 200)
            {
                //HTTP non-200 - log the response body so the 422 validation details are visible
                string errorMsg = $"HTTP error | Code: {request.responseCode} | Result: {request.result} | Error: {request.error}";
                debugOverlayController?.ShowCVAPIResult(
                    $"{errorMsg} | Body: {responseBody}");
                onFailure?.Invoke($"{errorMsg} | Body: {responseBody}");
                yield break;
            }

            // Step 5: Handle success - actual response
            if (string.IsNullOrWhiteSpace (responseBody))
            {
                onSuccess?.Invoke(null, request.responseCode.ToString());
                yield break;
            }

            PredictionResponse parsed = null;
            try
            {
                parsed = JsonUtility.FromJson<PredictionResponse>(responseBody);
            }
            catch (System.Exception e)
            {
                string parseError = $"[ReportSubmissionManager] Failed to parse CV response:{e.Message} | body: {responseBody}";
                Debug.LogWarning (parseError);
                debugOverlayController?.ShowCVAPIResult(parseError);
                onSuccess?.Invoke(null, request.responseCode.ToString());
                yield break;
            }

            CVAnalysisResult result = new CVAnalysisResult
            {
                detected = parsed.detected,
                model_name = parsed.model_name
            };

            string successMsg =
            $"[ReportSubmissionManager] Parsed 200 OK - detected ={parsed.detected} model={parsed.model_name}";
            Debug.Log(successMsg);
            debugOverlayController?.ShowCVAPIResult(successMsg);
            onSuccess?.Invoke(result, request.responseCode.ToString());
        }
    }
}
