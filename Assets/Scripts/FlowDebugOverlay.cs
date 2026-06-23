using UnityEngine;
using TMPro;

public class FlowDebugOverlay : MonoBehaviour
{
    private const string UnknownFailureSignature = "Code: unknown | Result: unknown | Error: unknown";

    #region Inspector Configuration
    [Header("Overlay Root")]
    [SerializeField] private GameObject overlayRoot;

    [Header("Debug Text")]
    [SerializeField] private TextMeshProUGUI uiStateText;
    [SerializeField] private TextMeshProUGUI reportFlowStateText;
    [SerializeField] private TextMeshProUGUI analyseApiStatusText;
    [SerializeField] private TextMeshProUGUI submitApiStatusText;
    #endregion

    public void ToggleOverlayRoot()
    {
        if (overlayRoot == null) return;
        overlayRoot.SetActive(!overlayRoot.activeSelf);
    }

    public bool IsOverlayVisible()
    {
        return overlayRoot != null && overlayRoot.activeSelf;
    }

    public void SetOverlayVisible(bool isVisible)
    {
        if (overlayRoot == null) return;
        overlayRoot.SetActive(isVisible);
    }

    public void ShowUIState(string state)
    {
        if (uiStateText == null) return;
        uiStateText.text = "UI State: " + state;
    }

    public void ShowReportFlowState(string state)
    {
        if (reportFlowStateText == null) return;
        reportFlowStateText.text = "Report Flow State: " + state;
    }

    public void MarkAnalyseStarted()
    {
        if (analyseApiStatusText == null) return;
        analyseApiStatusText.text = "Analyse API: STARTED";
    }

    public void MarkAnalyseFailed()
    {
        MarkAnalyseFailed(null);
    }

    public void MarkAnalyseFailed(string failureSignature)
    {
        if (analyseApiStatusText == null) return;
        analyseApiStatusText.text = "Analyse API: FAILED (" + BuildFailureSignature(failureSignature) + ")";
    }

    public void MarkAnalyseSuccess(string responseCode)
    {
        if (analyseApiStatusText == null) return;
        string code = string.IsNullOrEmpty(responseCode) ? "unknown" : responseCode;
        analyseApiStatusText.text = "Analyse API: SUCCESS (" + code + ")";
    }

    public void MarkSubmitStarted()
    {
        if (submitApiStatusText == null) return;
        submitApiStatusText.text = "Submit API: STARTED";
    }

    public void MarkSubmitSuccess(string responseCode)
    {
        if (submitApiStatusText == null) return;
        string code = string.IsNullOrEmpty(responseCode) ? "unknown" : responseCode;
        submitApiStatusText.text = "Submit API: SUCCESS (" + code + ")";
    }

    public void MarkSubmitFailed()
    {
        MarkSubmitFailed(null);
    }

    public void MarkSubmitFailed(string failureSignature)
    {
        if (submitApiStatusText == null) return;
        submitApiStatusText.text = "Submit API: FAILED (" + BuildFailureSignature(failureSignature) + ")";
    }

    private string BuildFailureSignature(string rawFailure)
    {
        if (string.IsNullOrEmpty(rawFailure))
        {
            return UnknownFailureSignature;
        }

        int bodyIndex = rawFailure.IndexOf(" | Body:");
        if (bodyIndex >= 0)
        {
            rawFailure = rawFailure.Substring(0, bodyIndex);
        }

        return rawFailure;
    }
}
