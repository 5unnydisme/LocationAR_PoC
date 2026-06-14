using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ShareCamera Script
//Manages a single shared rear-camera WebCamTexture session
//Responsible for: permissions, camera selection, streaming feed, frame capture
//Attach to CameraManager gameObject in the scene.

public class SharedCameraManager : MonoBehaviour
{
    //Camera status is public so PanelManager can read it via cameraManager.Status and show/hide respective panels 
    //SharedCameraManager sets the state and PanelManager reacts to it. 

    public enum CameraStatus
    {
        Idle, //Camera has not been started yet
        Initialising, //Permission request, hardware warm-up, not ready to capture yet
        Ready, //Valid frames are arriving, can capture images
        Failed //Permission denied, no REAR camera found, or some other error
    }

    #region Inspector References
    [Header("Required")]
    [Tooltip("A fullscreen RawImage placed behind all other UI")]
    [SerializeField] private RawImage previewRawImage;

    [Header("Optional")]
    [Tooltip("Keeps the preview proportional — attach to the same RawImage GameObject")]
    [SerializeField] private AspectRatioFitter aspectFitter;

    [Tooltip("On-screen log text for debugging without a PC connected")]
    [SerializeField] private TextMeshProUGUI debugText;

    #endregion

    #region Camera Settings

    [Header("Camera Settings")]
    [Tooltip("Requested capture width - the device may choose a different actual size")]
    [SerializeField] private int requestedWidth = 1280;

    [Tooltip("Requested capture height- the device may choose a different actual size")]
    [SerializeField] private int requestedHeight = 720;

    [Tooltip ("When true, selects the rear-facing camera, if no rear camera is found, fail state selected")]
    [SerializeField] private bool preferRearCamera = true;
    #endregion

    #region Runtime State

    //The streaming texture Unity feeds frame into from the camera
    private WebCamTexture webCamTexture;

    // Guard flag — prevents StartCamera() running twice
    private bool isCameraStarted = false;

    //Source of camera status so we can make sure the camera is running before we capture frames
    public CameraStatus Status {get; private set;} = CameraStatus.Idle; 

    //Error message for failed state — can be read by PanelManager to show user-friendly error messages
    public string FailureReason {get; private set;} = ""; 

    private Texture2D currentCapturedPreview; //Stores the most recent captured preview image for submission, this is what we will be sending over to the backend when the user hits submit. We keep a reference to it here so we can manage its memory and destroy it when we need to capture a new image or when the object is destroyed.
    //Cache for rotation and mirroring values to avoid unnecessary updates to the RawImage transform each frame — only update when values change
    private float cachedRotation = 0f;
    private bool cachedVerticallyMirrored = false;
    private int cachedTexWidth = 0;   
    private int cachedTexHeight = 0;
    #endregion

    #region Unity Lifecycle

    // Start() is called once by Unity when the scene loads.

    void Start()
    {
        StartCamera();
    }

    // OnDisable fires when the GameObject is deactivated (e.g. panel hidden)
    private void OnDisable()
    {
        StopCamera();
    }

    // OnDestroy fires when the GameObject is removed from the scene
    private void OnDestroy()
    {
        ClearPreviewImage(); //Free preview texture memory before the object is destroyed
        StopCamera();
    }

    #endregion

    #region Public Methods - Camera Controls 
    /// <summary>
    /// Begins the permission flow and starts the WebCamTexture feed. 
    /// Safe to call multiple times — subsequent calls will be ignored until the camera is stopped.
    /// </summary>

    public void StartCamera()
    {
        if (isCameraStarted)
        {
            Log("Camera already running — skipping duplicate start.");
            return;
        }

        // StartCoroutine runs an IEnumerator method in the background,
        // pausing at each 'yield return' without freezing the app.
        StartCoroutine(InitCamera());
    }

    /// <summary>
    /// Stops the camera feed, released the texture, and resets the state
    /// Status returns to Idle after this called
    /// </summary>
    public void StopCamera()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            webCamTexture.Stop();
            Log("Camera stopped.");
        }

        // Remove the texture reference from the UI element
        if (previewRawImage != null)
            previewRawImage.texture = null;

        // Free memory - Destroy() will tell Unity to release the native camera resource
        if (webCamTexture != null)
        {
            Destroy(webCamTexture);
            webCamTexture = null;
        }

        isCameraStarted = false;
        Status = CameraStatus.Idle;
    }

    /// <summary>
    /// Extra failsafe concerning the readiness of the camera feed. 
    /// Call this before capturing frames to ensure the camera is delivering valid frames and is not in a limbo state (e.g. waiting for permission, warming up, or failed).
    /// </summary>
    public bool IsReady()
    {
        return Status == CameraStatus.Ready;
    }

    #endregion

    #region Public Methods - Frame Capture

    /// <summary>
    /// Copies the current camera frame into a new Texture2D 
    /// Texture2D is a unity texture type which can store an image, it will not update each frame WebCamTexture
    /// Retutrns null is the camera is not ready 
    /// Caller is responsible for Destroy() on the returned Texture to free memory when done with it.
    /// </summary>
    public Texture2D GetCurrentFrame()
    {
        if(Status != CameraStatus.Ready || webCamTexture == null)
        {
            Log("GetCurrentFrame called before camera is ready-returning null.");
            return null;
        }

        //Create a blank Texture2D with the same dimensions as the camera feed
        Texture2D snapshot = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGB24, false); 
        //false here means no mipmaps (LODS for depth rendering) — we don't need them for a single snapshot, and it saves memory and processing time

        //GetPixels32 reads all pixel calls from the WebCamTexture in one call, it kinda converting the colour info into the image that will be stored
        snapshot.SetPixels32(webCamTexture.GetPixels32());
        snapshot.Apply();//Uploads the raw pixel data to the GPU, making it available for use as a texture in the scene or for encoding/saving

        return snapshot;
    }

    /// <summary>
    /// Captures the current frame and saves it as a JPG file. 
    /// Files are saved to Application.persistentDataPath, which is the directory folder Unity provides 
    /// On Android this maps to: /storage/emulated/0/Android/data/[your app's package name]/files — you can access these files with a file manager app or by connecting the device to a PC.
    /// returns the file path of the saved image, or null if the camera is not ready or if saving fails.
    /// </summary>
    
    ///<summary
    /// Captures the current live frame and stores it as a preview image
    /// Call this when the user taps on the Capture butotn
    /// Panel Manager will be calling the GetPreviewImage() to display it as a RawImage with the capture button disabled
    /// This method is also the "caller" and will destroy an existing preview image if the user takes multiple pictures
    /// </summary>
    
    public Texture2D CapturePreviewFrame()
    {
        Texture2D frame = GetCurrentFrame();

        if (frame == null)
        {
            Log("CapurePreviewFrame: camera not ready-returning null.");
            return null;
        }

        //Destroy the existing preview image before replacing it
        //Without this, the old Texture2D file stays on the GPU until the app closes, vv bad for phone memory
        if (currentCapturedPreview != null)
        {
            Destroy(currentCapturedPreview); //Free the GPU memory used by the previous preview image
            currentCapturedPreview = null; //clear the reference after destroying to avoid confusion
        }

        currentCapturedPreview = frame; 
        Log("Preview frame captured.");
        return currentCapturedPreview;
    }

    /// <summary>
    /// Returns the currently stored preview image captured by CapturePreviewFrame(). 
    /// Use this on the submit button to retreive the image the user just took and senf it over 
    /// </summary>
    
    public Texture2D GetPreviewImage()
    {
        return currentCapturedPreview; 
    }

    /// <summary>
    /// Recapture button logic. This destroys the stored preview image and clears the reference
    /// We call this to discard the image and take another one
    /// </summary>

    public void ClearPreviewImage()
    {
        if (currentCapturedPreview != null)
        {
            Destroy (currentCapturedPreview); //Delete the previously captured preview image
            currentCapturedPreview = null; //Gotsta clear the reference after destroying image
            Log("Preview image cleared."); //we gotta log what we did 
        }
    }
    #endregion

    #region Private — Camera Initialisation
    // IEnumerator is a special method type Unity can pause mid-execution.
    // Each 'yield return null' means: "pause here, resume on the next frame".
    // This keeps the UI responsive during permission requests and hardware warmup.
    private IEnumerator InitCamera()
    {
        Status = CameraStatus.Initialising; 
        //Right as we enter this method, we set the status to Initialising, 
        // which is the state where we are waiting for permissions and camera warmup.
        // PanelManager can use this status to show a loading spinner or some other UI feedback to 
        // the user while they wait.

        // Android permission handling
        // On Android, Application.RequestUserAuthorization does NOT reliably show
        // the system permission dialog. We must use the Android Permission API instead.
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);

            // Poll every frame until the user responds (grant/deny) or we time out.
            // There is no callback API on Android — polling is the standard Unity approach.
            float timeout = 15f;
            float elapsed = 0f;
            while (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                elapsed += Time.deltaTime;
                if (elapsed >= timeout)
                {
                    FailureReason = "Rear camera required. Please grant camera permission to use this app.";
                    Status = CameraStatus.Failed;
                    Log($"ERROR: {FailureReason}");
                    yield break;
                }
                yield return null;
            }
        }

        // Give the OS one extra frame to fully register the granted permission
        // before we try to enumerate camera devices
        yield return null;
#else
        // In the Editor or on non-Android platforms, use the standard Unity API
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            FailureReason = "Rear camera required. Please grant camera permission to use this app.";
            Status = CameraStatus.Failed;
            Log($"ERROR: {FailureReason}");
            yield break;
        }
#endif

        //Device Check- makes a list of the available cameras and selects the Rear Camera. This check is if the device has no cameras and so the array length returned is 0. 
        if (WebCamTexture.devices.Length == 0)
        {
            FailureReason = "Rear camera required. No camera devices were found on this device.";
            Status = CameraStatus.Failed;
            Log($"ERROR: {FailureReason}");
            yield break;
        }

        // ── Rear camera selection ─────────────────────────────────────────────────
        string selectedCameraName = null;

        if (preferRearCamera)
        {
            foreach (WebCamDevice device in WebCamTexture.devices)
            {
                if (!device.isFrontFacing) // isFrontFacing == false → rear camera
                {
                    selectedCameraName = device.name;
                    break;
                }
            }
        }

        // No rear camera found — hard failure, no front-camera fallback.
        // A selfie camera is not suitable for this use case.
        if (selectedCameraName == null)
        {
            FailureReason = "Rear camera required. No rear camera was found on this device.";
            Status = CameraStatus.Failed;
            Log($"ERROR: {FailureReason}");
            yield break;
        }

        Log($"Using rear camera: {selectedCameraName}");

        // ── Start the WebCamTexture ───────────────────────────────────────────────
        // WebCamTexture is a special Unity texture the OS writes camera frames into each frame.
        // The width/height values are hints — the device picks the closest supported resolution.
        webCamTexture = new WebCamTexture(selectedCameraName, requestedWidth, requestedHeight);

        // Assign to the RawImage BEFORE Play() so the first valid frame is never missed
        if (previewRawImage != null)
            previewRawImage.texture = webCamTexture;

        webCamTexture.Play();
        isCameraStarted = true;

        // ── Wait for valid frames ────────────────────────────────────────────────
        // While the camera hardware warms up, Unity reports width == 16.
        // We poll each frame until the real resolution is reported (> 100 is a safe threshold).
        while (webCamTexture != null && webCamTexture.width <= 100)
        {
            yield return null;
        }

        // Camera was stopped externally while we were waiting
        if (webCamTexture == null) yield break;

        // Camera is now delivering real frames — single Status field is the source of truth
        Status = CameraStatus.Ready;
        Log($"Camera ready — {webCamTexture.width} x {webCamTexture.height}");

        ApplyRotationAndAspect();
    }

    // Corrects rotation and mirroring after the camera reports its real resolution.
    // Android cameras internally capture in landscape — Unity exposes a rotation angle to correct this.
    private void ApplyRotationAndAspect()
    {
        if (previewRawImage == null || webCamTexture == null) return;

        // videoRotationAngle is the clockwise degrees the frame needs rotating to appear upright
        float rotation = webCamTexture.videoRotationAngle;
        previewRawImage.rectTransform.localEulerAngles = new Vector3(0, 0, -rotation);

        // Some cameras deliver a mirrored image — flip Y axis if needed
        float scaleY = webCamTexture.videoVerticallyMirrored ? -1f : 1f;
        previewRawImage.rectTransform.localScale = new Vector3(1f, scaleY, 1f);

        // Disable AspectRatioFitter — it conflicts with manual rotation-based sizing
        if (aspectFitter != null)
            aspectFitter.enabled = false;

        bool isPortraitRotation = (rotation == 90f || rotation == 270f);

        // Use parent canvas-space size, not Screen pixels, to avoid zoom/crop when CanvasScaler is active.
        RectTransform parentRect = previewRawImage.rectTransform.parent as RectTransform;
        float canvasWidth = parentRect != null ? parentRect.rect.width : Screen.width;
        float canvasHeight = parentRect != null ? parentRect.rect.height : Screen.height;

        float texWidth = webCamTexture.width;
        float texHeight = webCamTexture.height;
        if (texWidth <= 0f || texHeight <= 0f)
        {
            return;
        }

        // Match preview fit logic: preserve camera aspect and scale to cover the container.
        float scale = Mathf.Max(
            isPortraitRotation ? canvasWidth / texHeight : canvasWidth / texWidth,
            isPortraitRotation ? canvasHeight / texWidth : canvasHeight / texHeight
        );

        // RectTransform controls 2D layout of a UI element.
        // sizeDelta sets width/height only when anchors are center/center (not stretch).
        // If your RawImage uses stretch anchors, switch them to center/center in the Inspector.
        previewRawImage.rectTransform.anchorMin        = new Vector2(0.5f, 0.5f);
        previewRawImage.rectTransform.anchorMax        = new Vector2(0.5f, 0.5f);
        previewRawImage.rectTransform.anchoredPosition = Vector2.zero;
        previewRawImage.rectTransform.sizeDelta        = new Vector2(texWidth * scale, texHeight * scale);

        cachedRotation = webCamTexture.videoRotationAngle;
        cachedVerticallyMirrored = webCamTexture.videoVerticallyMirrored;
        cachedTexWidth = webCamTexture.width;   
        cachedTexHeight = webCamTexture.height; 
        Log($"Rotation: {rotation} | Live preview scaled with preserved aspect.");
    }

    public void ApplyCaptureRotationTo(RawImage target)
    {
    if (target == null) return;

    target.rectTransform.localEulerAngles = new Vector3(0, 0, -cachedRotation);
    float scaleY = cachedVerticallyMirrored ? -1f : 1f;
    target.rectTransform.localScale = new Vector3(1f, scaleY, 1f);

    // Fit the rect to the parent container, respecting the rotation.
    // After a 90/270° rotation, visual width = sizeDelta.y and visual height = sizeDelta.x,
    // so the fit constraints are transposed.
    RectTransform parent = target.rectTransform.parent as RectTransform;
    if (parent == null || cachedTexWidth == 0) return;

    float containerW = parent.rect.width;
    float containerH = parent.rect.height;
    float texW = cachedTexWidth;
    float texH = cachedTexHeight;

    bool isPortraitRotation = (cachedRotation == 90f || cachedRotation == 270f);
    float s = Mathf.Max(
        isPortraitRotation ? containerW / texH : containerW / texW,
        isPortraitRotation ? containerH / texW : containerH / texH
        );

    target.rectTransform.anchorMin        = new Vector2(0.5f, 0.5f);
    target.rectTransform.anchorMax        = new Vector2(0.5f, 0.5f);
    target.rectTransform.anchoredPosition = Vector2.zero;
    target.rectTransform.sizeDelta        = new Vector2(texW * s, texH * s);
    }

    public void SuspendFeed()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            webCamTexture.Stop(); // Stops streaming but keeps the texture object alive
            Log("Camera feed suspended.");
        }
    }

    public void ResumeFeed()
    {
        if (webCamTexture != null && !webCamTexture.isPlaying && Status == CameraStatus.Ready)
        {
        webCamTexture.Play();
        Log("Camera feed resumed.");
        }
    }

    // Writes to the on-screen debug label AND the Unity console
    private void Log(string message)
    {
        if (debugText != null)
            debugText.text = message;

        Debug.Log($"[SharedCameraManager] {message}");
    }

    #endregion

}