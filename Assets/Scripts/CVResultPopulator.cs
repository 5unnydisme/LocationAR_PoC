using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Populates the duplicated CVAnalysisCard UI using CV analysis output.
// Keep this script passive: no state changes, no network calls.
public class CVResultPopulator : MonoBehaviour
{
    #region Inspector Configuration
    [Header("CV Result Card Fields")]
    [Tooltip("Card title: [Road Name] Issue Report.")]
    [SerializeField] private TMP_Text roadNameTitleText;

    [Tooltip("Static section header text object for issue type label.")]
    [SerializeField] private TMP_Text issueTypeHeaderText;

    [Tooltip("Issue type line: selected model from dropdown (for example, Pothole).")]
    [SerializeField] private TMP_Text issueTypeChipText;

    [Tooltip("Static section header text object for grievance label.")]
    [SerializeField] private TMP_Text grievanceHeaderText;

    [Tooltip("Status line: [Issue Type] Detected or [Issue Type] Not Detected.")]
    [SerializeField] private TMP_Text statusLineText;

    [Tooltip("Main body line: [Issue Type] detected/not detected at [Road Name] at [Timestamp].")]
    [SerializeField] private TMP_Text grievanceLineText;

    [Tooltip("Location manager used for existing snapshot timestamp.")]
    [SerializeField] private LocationServiceManager locationServiceManager;

    [Tooltip("Icon shown on the left of the card.")]
    [SerializeField] private Image iconImage;

    [Header("CV Result Card Header Labels")]
    [Tooltip("Displayed text for issue type header label.")]
    [SerializeField] private string issueTypeHeaderLabel = "Issue Type";

    [Tooltip("Displayed text for grievance header label.")]
    [SerializeField] private string grievanceHeaderLabel = "Grievance";

    [Tooltip("Optional icon used when SetIcon receives null.")]
    [SerializeField] private Sprite fallbackIcon;
    #endregion

    #region Private State
    private string cachedRoadName = string.Empty;
    #endregion

    #region Public API
    // Use this if road title is supplied earlier from context.
    public void SetRoadName(string roadName)
    {
        cachedRoadName = Sanitize(roadName);
    }

    // Backward-compatible call site: existing callbacks can keep calling Populate(data).
    public void Populate(CVAnalysisResult data)
    {
        Populate(data, cachedRoadName);
    }

    // Preferred call path for deterministic road title from PanelManager context.
    public void Populate(CVAnalysisResult data, string roadNameFromContext)
    {
        if (data == null)
        {
            Clear();
            return;
        }

        cachedRoadName = Sanitize(roadNameFromContext);

        string roadValue = cachedRoadName;
        string issueValue = Sanitize(data.defect_type);
        bool isDetected = data.detected;
        string statusText = isDetected ? "Detected" : "Not Detected";
        string grievanceVerb = isDetected ? "detected" : "not detected";
        string timestampValue = locationServiceManager.LastLocationSnapshot.timestamp.ToString("yyyy-MM-dd HH:mm");

        SetText(issueTypeHeaderText, issueTypeHeaderLabel);
        SetText(grievanceHeaderText, grievanceHeaderLabel);
        SetText(roadNameTitleText, roadValue + " Issue Report");
        SetText(issueTypeChipText, issueValue);
        SetText(statusLineText, issueValue + " " + statusText);
        SetText(grievanceLineText, issueValue + " " + grievanceVerb + " at " + roadValue + " at " + timestampValue + ".");
    }

    // Optional runtime icon swap if you decide to drive icon per defect type later.
    public void SetIcon(Sprite iconSprite)
    {
        if (iconImage == null)
        {
            return;
        }

        iconImage.sprite = iconSprite != null ? iconSprite : fallbackIcon;
    }

    // Clears stale values before a new analysis cycle.
    public void Clear()
    {
        SetText(issueTypeHeaderText, issueTypeHeaderLabel);
        SetText(grievanceHeaderText, grievanceHeaderLabel);
        SetText(roadNameTitleText, string.Empty);
        SetText(issueTypeChipText, string.Empty);
        SetText(statusLineText, string.Empty);
        SetText(grievanceLineText, string.Empty);

        if (iconImage != null && fallbackIcon != null)
        {
            iconImage.sprite = fallbackIcon;
        }
    }
    #endregion

    #region Private Helpers
    private static void SetText(TMP_Text targetText, string value)
    {
        if (targetText == null)
        {
            return;
        }

        targetText.text = value;
    }

    private static string Sanitize(string input)
    {
        return string.IsNullOrEmpty(input) ? string.Empty : input.Trim();
    }
    #endregion
}