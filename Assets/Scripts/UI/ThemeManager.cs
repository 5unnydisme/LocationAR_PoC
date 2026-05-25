using UnityEngine;

public class ThemeManager : MonoBehaviour
{
    [Header("Theme Source")]
    [SerializeField] private UITheme theme;

    public static UITheme Theme { get; private set; }

    private void Awake()
    {
        Theme = theme;

        if (Theme == null)
        {
            Debug.LogWarning("[ThemeManager] No UITheme assigned. Themed components will have no data source.");
        }
    }

    private void OnDisable()
    {
        if (Theme == theme)
        {
            Theme = null;
        }
    }
}