using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModelDropdownController : MonoBehaviour
{
    #region Inspector Configuration
    [Header("Dropdown References")]
    [Tooltip("TMP dropdown used to choose the issue model.")]
    [SerializeField] private TMP_Dropdown modelDropdown;

    [Tooltip("Analyse button that should only be enabled after a valid model is selected.")]
    [SerializeField] private Button analyseButton;

    [Tooltip("Arrow RectTransform on the dropdown bar; rotated 180° when the list is open.")]
    [SerializeField] private RectTransform dropdownArrow;

    [Tooltip("Optional debug overlay for mirroring selection logs on device.")]
    [SerializeField] private DebugOverlayController debugOverlayController;
    #endregion

    #region Private Constants
    private const string PlaceholderOption = "Select Issue Type";
    private bool isOpen = false;

    private static readonly List<string> ModelOptions = new List<string>
    {
        PlaceholderOption,
        "Animals",
        "Garbage",
        "Potholes",
        "Fallen Trees",
        "Hoardings",
        "Road Damage"
    };
    #endregion

    #region Public State
    public string SelectedModelName { get; private set; } = string.Empty;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (modelDropdown == null)
        {
            LogError("Missing required reference: modelDropdown.");
        }

        if (analyseButton == null)
        {
            LogError("Missing required reference: analyseButton.");
        }

        if (modelDropdown == null || analyseButton == null)
        {
            SelectedModelName = string.Empty;
            return;
        }

        modelDropdown.ClearOptions();
        modelDropdown.AddOptions(ModelOptions);

        modelDropdown.SetValueWithoutNotify(0);
        SelectedModelName = string.Empty;
        analyseButton.interactable = false;

        if (dropdownArrow != null)
        {
            dropdownArrow.localEulerAngles = Vector3.zero;
        }

        Log("Dropdown initialized with 1 placeholder and 6 issue models.");
    }
    #endregion

    #region Inspector Event Handlers
    public void OnDropdownValueChanged(int index)
    {
        if (modelDropdown == null || analyseButton == null)
        {
            LogError("OnDropdownValueChanged called before required references were assigned.");
            return;
        }

        if (index < 0 || index >= modelDropdown.options.Count)
        {
            SelectedModelName = string.Empty;
            analyseButton.interactable = false;
            Log("No valid model selected. Analyse disabled.");
            return;
        }

        string selectedText = modelDropdown.options[index].text;
        bool isPlaceholder = selectedText == PlaceholderOption;

        SelectedModelName = isPlaceholder ? string.Empty : selectedText;
        analyseButton.interactable = !isPlaceholder;
        Log(isPlaceholder
            ? "Placeholder selected. Analyse disabled."
            : $"Model selected: {SelectedModelName}");
    }

    private void Update()
    {
        if (modelDropdown == null || dropdownArrow == null)
        {
            return;
        }

        // TMP_Dropdown spawns a child named "Dropdown List" while open.
        bool openNow = modelDropdown.transform.Find("Dropdown List") != null;
        if (openNow != isOpen)
        {
            isOpen = openNow;
            dropdownArrow.localEulerAngles = new Vector3(0f, 0f, isOpen ? 180f : 0f);
        }
    }

    public void ResetSelection()
    {
        if (modelDropdown == null || analyseButton == null)
        {
            LogError("ResetSelection called before required references were assigned.");
            return;
        }

        modelDropdown.SetValueWithoutNotify(0);
        SelectedModelName = string.Empty;
        analyseButton.interactable = false;

        Log("Dropdown reset to placeholder.");
    }
    #endregion

    #region Private Helpers
    private void Log(string message)
    {
        string taggedMessage = $"[ModelDropdownController] {message}";
        Debug.Log(taggedMessage);
        debugOverlayController?.ShowCVAPIResult(taggedMessage);
    }

    private void LogError(string message)
    {
        string taggedMessage = $"[ModelDropdownController] {message}";
        Debug.LogError(taggedMessage);
        debugOverlayController?.ShowCVAPIResult(taggedMessage);
    }
    #endregion
}