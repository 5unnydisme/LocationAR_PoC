using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Populates the duplicated CVAnalysisCard UI using CV analysis output.
// Keep this script passive: no state changes, no network calls.
public class CVResultPopulator : MonoBehaviour
{
    #region Inspector Configuration
    [Header("CV Result Card Fields")]
    [Tooltip("Card title: road name (for example, Curzon Road).")]
    [SerializeField] private TMP_Text roadNameTitleText;

    [Tooltip("Chip line: issue type (for example, Issue Type: Pothole).")]
    [SerializeField] private TMP_Text issueTypeChipText;

    [Tooltip("Line under title/chips: confidence text (for example, Detection Confidence: 95%).")]
    [SerializeField] private TMP_Text confidenceLineText;

    [Tooltip("Main body line: grievance/summary message.")]
    [SerializeField] private TMP_Text grievanceLineText;

    [Tooltip("Icon shown on the left of the card.")]
    [SerializeField] private Image iconImage;

    [Header("Fallback Display Values")]
    [Tooltip("Shown when road name is missing.")]
    [SerializeField] private string unknownRoadFallback = "Unknown Road";

    [Tooltip("Shown when defect type is missing.")]
    [SerializeField] private string unknownIssueFallback = "Unknown";

    [Tooltip("Shown when summary message is missing.")]
    [SerializeField] private string emptyMessageFallback = "";

    [Tooltip("Prefix for issue type line.")]
    [SerializeField] private string issueTypePrefix = "Issue Type: ";

    [Tooltip("Prefix for confidence line.")]
    [SerializeField] private string confidencePrefix = "Detection Confidence: ";

    [Tooltip("Prefix for summary line.")]
    [SerializeField] private string grievancePrefix = "Grievance: ";

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

        string roadValue = string.IsNullOrEmpty(cachedRoadName) ? unknownRoadFallback : cachedRoadName;
        string issueValue = string.IsNullOrEmpty(data.defect_type) ? unknownIssueFallback : data.defect_type.Trim();
        float confidencePercent = NormalizeConfidenceToPercent(data.confidence);
        string messageValue = string.IsNullOrEmpty(data.message) ? emptyMessageFallback : data.message.Trim();

        SetText(roadNameTitleText, roadValue);
        SetText(issueTypeChipText, issueTypePrefix + issueValue);
        SetText(confidenceLineText, confidencePrefix + Mathf.RoundToInt(confidencePercent) + "%");
        SetText(grievanceLineText, grievancePrefix + messageValue);
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
        SetText(roadNameTitleText, unknownRoadFallback);
        SetText(issueTypeChipText, issueTypePrefix + unknownIssueFallback);
        SetText(confidenceLineText, confidencePrefix + "0%");
        SetText(grievanceLineText, grievancePrefix + emptyMessageFallback);

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

    // Supports either 0..1 or 0..100 confidence payloads.
    private static float NormalizeConfidenceToPercent(float rawConfidence)
    {
        if (rawConfidence <= 1f)
        {
            return Mathf.Clamp01(rawConfidence) * 100f;
        }

        return Mathf.Clamp(rawConfidence, 0f, 100f);
    }
    #endregion
}