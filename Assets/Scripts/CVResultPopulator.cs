using UnityEngine;
using TMPro;

//This script mirrors the PanelPopulator but for CVAnalysis result 
//Everything is placeholder for now until we get the actual CV result data structure from SB, but the idea is that once we have that we can just swap out the fake CVAnalysisResult with the real one and populate the panel with the real data.
//PanelManager calls this after receiving the CVAnalaysisResult from the ReportSubmissionManager, and this script is responsible for taking that result and populating the CV result panel with the relevant info (defect type, urgency, confidence, message)
//Attach this script ot the reviewResultsPanel GameObject 
public class CVResultPopulator : MonoBehaviour
{
    #region UI Refrences
    [Tooltip("Displays the type of defect detected, e.g. pothole, crack, etc.")]
    public TextMeshProUGUI defectTypeText;

    [Tooltip("Displays the urgency level of the detected defect, e.g. high, medium, low.")]
    public TextMeshProUGUI urgencyText;

    [Tooltip("Displays the model confidence as a percentage, e.g. 92%")]
    public TextMeshProUGUI confidenceText;
    [Tooltip("Displays a user-friendly message from the CV model, e.g. 'Large pothole detected. Repair fast.'")]
    public TextMeshProUGUI messageText;
    #endregion

    public void Populate(CVAnalysisResult data)
    {
        if (data == null) return;

        defectTypeText.text = data.defect_type ?? "";
        urgencyText.text = data.urgency ?? "";
        confidenceText.text = $"{Mathf.RoundToInt(data.confidence * 100)}%";
        messageText.text = data.message?? "";
    }
}
