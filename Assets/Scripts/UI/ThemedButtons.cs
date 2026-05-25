using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ThemedButton : MonoBehaviour
{
    public enum ButtonVariant
    {
        Primary,
        Secondary,
        Destructive,
        Text
    }

    #region Inspector Configuration
    [Header("Style")]
    [SerializeField] private ButtonVariant variant = ButtonVariant.Primary;

    [Header("Optional References")]
    [Tooltip("Optional explicit label reference. If empty, first TMP_Text child is used.")]
    [SerializeField] private TMP_Text label;

    [Tooltip("Optional border outline used by Secondary/Text variants.")]
    [SerializeField] private Outline borderOutline;
    #endregion

    private Button button;
    private Coroutine applyThemeRoutine;
    private bool hasLoggedMissingTheme;
    private bool hasLoggedMissingLabel;
    private bool lastInteractableState;

    private void Awake()
    {
        button = GetComponent<Button>();
        lastInteractableState = button.interactable;
    }

    private void OnEnable()
    {
        if (ThemeManager.Theme != null)
        {
            ApplyTheme();
            return;
        }

        if (applyThemeRoutine != null)
        {
            StopCoroutine(applyThemeRoutine);
        }

        applyThemeRoutine = StartCoroutine(ApplyThemeNextFrame());
    }

    private void OnDisable()
    {
        if (applyThemeRoutine != null)
        {
            StopCoroutine(applyThemeRoutine);
            applyThemeRoutine = null;
        }
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        ApplyTheme();
    }

    private void LateUpdate()
    {
        if (button == null)
        {
            return;
        }

        if (button.interactable != lastInteractableState)
        {
            lastInteractableState = button.interactable;
            ApplyTheme();
        }
    }

    private IEnumerator ApplyThemeNextFrame()
    {
        yield return null;
        ApplyTheme();
        applyThemeRoutine = null;
    }

    private void ApplyTheme()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (label == null)
        {
            label = GetComponentInChildren<TMP_Text>(true);
        }

        UITheme theme = ThemeManager.Theme;
        if (theme == null)
        {
            if (Application.isPlaying && !hasLoggedMissingTheme)
            {
                Debug.LogWarning("[ThemedButton] ThemeManager.Theme is null. Keeping existing Button values.", this);
                hasLoggedMissingTheme = true;
            }
            return;
        }

        Color normalBackground;
        Color highlightedBackground;
        Color pressedBackground;
        Color disabledBackground;

        Color normalLabel;
        Color disabledLabel;

        Color normalBorder;
        Color disabledBorder;

        switch (variant)
        {
            case ButtonVariant.Primary:
                normalBackground = theme.Primary300;
                highlightedBackground = theme.Primary400;
                pressedBackground = theme.Primary500;
                disabledBackground = theme.Primary200;

                normalLabel = theme.White;
                disabledLabel = theme.White;

                normalBorder = Color.clear;
                disabledBorder = Color.clear;
                break;

            case ButtonVariant.Secondary:
                normalBackground = theme.White;
                highlightedBackground = theme.Primary100;
                pressedBackground = theme.Primary100;
                disabledBackground = theme.Grey50;

                normalLabel = theme.Primary300;
                disabledLabel = theme.Grey300;

                normalBorder = theme.Primary300;
                disabledBorder = theme.Grey200;
                break;

            case ButtonVariant.Destructive:
                normalBackground = theme.Red300;
                highlightedBackground = theme.Red400;
                pressedBackground = theme.Red500;
                disabledBackground = theme.Grey100;

                normalLabel = theme.White;
                disabledLabel = theme.Grey300;

                normalBorder = Color.clear;
                disabledBorder = Color.clear;
                break;

            case ButtonVariant.Text:
            default:
                normalBackground = new Color(0f, 0f, 0f, 0f);
                highlightedBackground = theme.Primary50;
                pressedBackground = theme.Primary100;
                disabledBackground = new Color(0f, 0f, 0f, 0f);

                normalLabel = theme.Primary300;
                disabledLabel = theme.Grey300;

                normalBorder = Color.clear;
                disabledBorder = Color.clear;
                break;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = normalBackground;
        colors.highlightedColor = highlightedBackground;
        colors.pressedColor = pressedBackground;
        colors.selectedColor = highlightedBackground;
        colors.disabledColor = disabledBackground;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        button.colors = colors;

        bool isInteractable = button.interactable;
        lastInteractableState = isInteractable;

        if (label != null)
        {
            label.color = isInteractable ? normalLabel : disabledLabel;
        }
        else if (Application.isPlaying && !hasLoggedMissingLabel)
        {
            Debug.LogWarning("[ThemedButton] No TMP_Text child found for label color styling.", this);
            hasLoggedMissingLabel = true;
        }

        if (borderOutline != null)
        {
            borderOutline.effectColor = isInteractable ? normalBorder : disabledBorder;
        }
    }
}