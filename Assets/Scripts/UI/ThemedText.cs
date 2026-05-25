using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class ThemedText : MonoBehaviour
{
public enum TypeRole
{
    // Main status headings: "Tap Check-in", "Getting Location", grievance card headers.
    DisplaySemiboldLg,

    // Work type descriptions and secondary card headings.
    DisplaySemiboldMd,

    // Display-level medium text style (16/24) for medium-emphasis headings.
    DisplayMediumLg,

    // Short instructional body copy: "Please wait..." style text.
    BodyRegularMd,

    // Supporting informational body copy.
    BodyRegularLg,

    // Dropdown selected values.
    BodyMediumMd,

    // Contractor names, stretch descriptions, grievance statements.
    BodyMediumLg,

    // Chips, project numbers, contractor/stretch/issue labels.
    LabelMediumSm,

    // Project count text and other small neutral labels.
    LabelRegularSm,

    // Dropdown instruction/helper text.
    LabelMediumMd,

    // All CTA button text.
    ButtonLabel
}

[Header("Type Role")]
[SerializeField] private TypeRole typeRole = TypeRole.BodyRegularMd;

[Header("Apply Options")]
[Tooltip("When disabled, ThemedText applies font and size but leaves the current TMP color unchanged.")]
[SerializeField] private bool applyColor = true;

    private TMP_Text textComponent;
    private Coroutine applyThemeRoutine;
    private bool hasLoggedMissingTheme;
    private bool hasLoggedMissingStyle;

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

    private void ApplyTheme()
    {
        if (textComponent == null)
        {
            textComponent = GetComponent<TMP_Text>();
        }

        UITheme theme = ThemeManager.Theme;
        if (theme == null)
        {
            if (!hasLoggedMissingTheme)
            {
                Debug.LogWarning("[ThemedText] ThemeManager.Theme is null. Keeping existing TMP values.", this);
                hasLoggedMissingTheme = true;
            }
            return;
        }

        UITheme.TypeStyle style = GetStyle(theme, typeRole);
        if (style == null || style.font == null)
        {
            if (!hasLoggedMissingStyle)
            {
                Debug.LogWarning("[ThemedText] Missing TypeStyle or font for role " + typeRole + ". Keeping existing TMP values.", this);
                hasLoggedMissingStyle = true;
            }
            return;
        }

        textComponent.font = style.font;
        textComponent.fontSize = style.size;
        if (applyColor)
        {
            textComponent.color = style.color;
        }
        // lineHeight remains reference metadata in Phase 1 and is not applied here.
    }

    private IEnumerator ApplyThemeNextFrame()
    {
        yield return null;
        ApplyTheme();
        applyThemeRoutine = null;
    }

    private UITheme.TypeStyle GetStyle(UITheme theme, TypeRole role)
    {
        switch (role)
        {
        case TypeRole.DisplaySemiboldLg:
            return theme.DisplaySemiboldLg;
        case TypeRole.DisplaySemiboldMd:
            return theme.DisplaySemiboldMd;
        case TypeRole.DisplayMediumLg:
            return theme.DisplayMediumLg;
        case TypeRole.BodyRegularMd:
            return theme.BodyRegularMd;
        case TypeRole.BodyRegularLg:
            return theme.BodyRegularLg;
        case TypeRole.BodyMediumMd:
            return theme.BodyMediumMd;
        case TypeRole.BodyMediumLg:
            return theme.BodyMediumLg;
        case TypeRole.LabelMediumSm:
            return theme.LabelMediumSm;
        case TypeRole.LabelRegularSm:
            return theme.LabelRegularSm;
        case TypeRole.LabelMediumMd:
            return theme.LabelMediumMd;
        case TypeRole.ButtonLabel:
            return theme.ButtonLabel;
        default:
            return theme.BodyRegularMd;
        }
    }
}