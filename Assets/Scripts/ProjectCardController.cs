using UnityEngine;
using TMPro;

public class ProjectCardController : MonoBehaviour
{
    #region Inspector Configuration
    [Header("Project Card Text Fields")]
    [Tooltip("Displays Project 1, Project 2, etc.")]
    [SerializeField] private TMP_Text projectIndexTitle;

    [Tooltip("Displays the project work type as the card title.")]
    [SerializeField] private TMP_Text workTypeTitleText;

    [Tooltip("Displays the budget chip value.")]
    [SerializeField] private TMP_Text budgetChipText;

    [Tooltip("Displays the combined date range chip in StartDate - EndDate format.")]
    [SerializeField] private TMP_Text dateRangeChipText;

    [Tooltip("Contractor header")]
    [SerializeField] private TMP_Text contractorHeaderText;

    [Tooltip("Displays contractor name.")]
    [SerializeField] private TMP_Text contractorText;

    [Tooltip("Stretch header")]
    [SerializeField] private TMP_Text stretchHeaderText;

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
    private const string BudgetPrefix= "Budget: ";
    private const string DateRangePrefix = "Timeline: ";
    private const string DateSeparator = " - ";
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
    public void Populate(ProjectRecord projectData, int cardIndex)
    {
        if (projectData == null)
        {
            Debug.LogWarning("[ProjectCardController] Populate called with null ProjectRecord. Showing fallback values.");
            SetAllFallbackValues();
            return;
        }

        SetText(projectIndexTitle, BuildProjectIndexTitle(cardIndex));
        SetText(workTypeTitleText, GetValueOrFallback(projectData.workType));
        SetText(budgetChipText, BudgetPrefix + GetValueOrFallback(projectData.budget));
        SetText(dateRangeChipText, DateRangePrefix + BuildDateRange(projectData.startDate, projectData.endDate));

        SetText(contractorHeaderText, "Contractor");
        SetText(stretchHeaderText, "Stretch");
        SetText(contractorText, GetValueOrFallback(projectData.contractor));
        SetText(stretchText, GetValueOrFallback(projectData.stretchDescription));
    }

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
        SetText(projectIndexTitle, NotAvailableText);
        SetText(workTypeTitleText, NotAvailableText);
        SetText(budgetChipText, BudgetPrefix + NotAvailableText);
        SetText(dateRangeChipText, DateRangePrefix + BuildDateRange(null, null));
        SetText(contractorText, NotAvailableText);
        SetText(stretchText, NotAvailableText);
    }

    private static string BuildProjectIndexTitle(int cardIndex)
    {
        return "Project " + (cardIndex + 1);
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
