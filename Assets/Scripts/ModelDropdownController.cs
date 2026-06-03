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

    private bool isOpen;
    private const string PlaceholderOption = "Select issue type";

    #region Private Constants
    private class ModelOption
    {
        public string displayName;
        public string apiValue;       

        public ModelOption(string displayName, string apiValue)
        {
            this.displayName = displayName; 
            this.apiValue = apiValue;
        }
    }

    private static readonly List<ModelOption> ModelOptions = new List<ModelOption>
    {
        new ModelOption(PlaceholderOption, string.Empty),
        new ModelOption("Animals", "animals"),
        new ModelOption("Garbage", "garbage"),
        new ModelOption("Potholes", "potholes"),
        new ModelOption("Fallen Trees", "fallentrees"),
        new ModelOption("Hoardings", "hoardings"), 
        new ModelOption("Road Damage", "roaddamage")
    };
    
    #endregion

    #region Public State
    public string SelectedModelName {get; private set;} = string.Empty; //API value
    public string SelectedModelDisplayName {get;private set;} = string.Empty; //UI label
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

        List<string> displayOptions = new List<string> (ModelOptions.Count);
        for (int i = 0; i < ModelOptions.Count; i++)
        {
            displayOptions.Add(ModelOptions[i].displayName);
        }
        modelDropdown.AddOptions(displayOptions);
        
        modelDropdown.SetValueWithoutNotify(0);
        SelectedModelName = string.Empty;
        SelectedModelDisplayName = string.Empty;
        analyseButton.interactable = false;
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

        if (index < 0 || index >= ModelOptions.Count)
        {
            SelectedModelName = string.Empty;
            SelectedModelDisplayName = string.Empty;
            analyseButton.interactable = false; 
            Log ("No valid model selected. Analyse disabled.");
            return;
        }

        ModelOption selectedOption = ModelOptions[index];
        bool isPlaceholder = string.IsNullOrEmpty(selectedOption.apiValue);

        SelectedModelName = isPlaceholder ? string.Empty : selectedOption.apiValue;
        SelectedModelDisplayName = isPlaceholder ? string.Empty : selectedOption.displayName;
        analyseButton.interactable = !isPlaceholder; 

        Log (isPlaceholder
        ? "Placeholder selected. Analyse disabled."
        : $"Model selected: {SelectedModelDisplayName} (api:{SelectedModelName})");  

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
        SelectedModelDisplayName = string.Empty;
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