using UnityEngine;
using TMPro;

public class ProjectCardController : MonoBehaviour
{
    #region Inspector Configuration
    [Header("Project Card Text Fields")]
    [Tooltip("Displays the project work type as the card title.")]
    [SerializeField] private TMP_Text workTypeTitleText;

    [Tooltip("Displays the budget chip value.")]
    [SerializeField] private TMP_Text budgetChipText;

    [Tooltip("Displays the combined date range chip in StartDate - EndDate format.")]
    [SerializeField] private TMP_Text dateRangeChipText;

    [Tooltip("Displays contractor name.")]
    [SerializeField] private TMP_Text contractorText;

    [Tooltip("Displays stretch description.")]
    [SerializeField] private TMP_Text stretchText;

    [Header("Stretch Expansion")]
    [Tooltip("Optional container to show/hide when stretch details are expanded.")]
    [SerializeField] private GameObject stretchExpandedContainer;

    [Tooltip("If true, stretch details start expanded when the card wakes up.")]
    [SerializeField] private bool startExpanded = false;
    #endregion

    #region Private Constants
    private const string NotAvailableText = "N/A";
    private const string DateSeparator = " - ";
    private const string ContractorPrefix = "Contractor: ";
    private const string StretchPrefix = "Stretch: ";
    #endregion

    #region Private State
    private bool isStretchExpanded;
    #endregion

    #region Unity Lifecycle Methods
    private void Awake()
    {
        SetStretchExpanded(startExpanded);
    }
    #endregion

    #region Public API
    public void Populate(ProjectRecord projectData)
    {
        if (projectData == null)
        {
            Debug.LogWarning("[ProjectCardController] Populate called with null ProjectRecord. Showing fallback values.");
            SetAllFallbackValues();
            return;
        }

        SetText(workTypeTitleText, GetValueOrFallback(projectData.workType));
        SetText(budgetChipText, GetValueOrFallback(projectData.budget));
        SetText(dateRangeChipText, BuildDateRange(projectData.startDate, projectData.endDate));
        SetText(contractorText, BuildLabeledValue(ContractorPrefix, projectData.contractor));
        SetText(stretchText, BuildLabeledValue(StretchPrefix, projectData.stretchDescription));
    }

    // Keep this method now so Inspector OnClick wiring can be added later without refactor.
    public void ToggleStretchExpanded()
    {
        SetStretchExpanded(!isStretchExpanded);
    }

    public void SetStretchExpanded(bool expanded)
    {
        isStretchExpanded = expanded;

        if (stretchExpandedContainer != null)
        {
            stretchExpandedContainer.SetActive(isStretchExpanded);
        }
    }
    #endregion

    #region Private Helpers
    private void SetAllFallbackValues()
    {
        SetText(workTypeTitleText, NotAvailableText);
        SetText(budgetChipText, NotAvailableText);
        SetText(dateRangeChipText, BuildDateRange(null, null));
        SetText(contractorText, BuildLabeledValue(ContractorPrefix, null));
        SetText(stretchText, BuildLabeledValue(StretchPrefix, null));
    }

    private static string GetValueOrFallback(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? NotAvailableText : value.Trim();
    }

    private static string BuildDateRange(string startDate, string endDate)
    {
        string safeStart = GetValueOrFallback(startDate);
        string safeEnd = GetValueOrFallback(endDate);
        return safeStart + DateSeparator + safeEnd;
    }

    private static string BuildLabeledValue(string labelPrefix, string rawValue)
    {
        return labelPrefix + GetValueOrFallback(rawValue);
    }

    private static void SetText(TMP_Text targetText, string value)
    {
        if (targetText == null)
        {
            return;
        }

        targetText.text = value;
    }
    #endregion
}