using UnityEngine;

public class LoadingDotsCircleLayout : MonoBehaviour
{
    #region Inspector Configuration
    [Header("Dots")]
    [Tooltip("Assign dot RectTransforms in clockwise order, starting with Dot0 at 12 o'clock.")]
    [SerializeField] private RectTransform[] dots = new RectTransform[8];

    [Header("Layout")]
    [Tooltip("Distance from center to each dot in anchored UI units.")]
    [SerializeField] private float radius = 28f;

    [Tooltip("Arrange when the object is enabled (runtime-safe for prefabs).")]
    [SerializeField] private bool arrangeOnEnable = true;
    #endregion

    private void Awake()
    {
        ArrangeDots();
    }

    private void OnEnable()
    {
        if (arrangeOnEnable)
        {
            ArrangeDots();
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ArrangeDots();
        }
    }

    [ContextMenu("Arrange Dots Now")]
    public void ArrangeDots()
    {
        if (dots == null || dots.Length == 0)
        {
            return;
        }

        float stepDegrees = 360f / dots.Length;

        for (int i = 0; i < dots.Length; i++)
        {
            RectTransform dot = dots[i];
            if (dot == null)
            {
                continue;
            }

            // Dot0 = 12 o'clock, then clockwise by subtracting angle.
            float angleDegrees = 90f - (i * stepDegrees);
            float angleRadians = angleDegrees * Mathf.Deg2Rad;

            dot.anchoredPosition = new Vector2(
                Mathf.Cos(angleRadians) * radius,
                Mathf.Sin(angleRadians) * radius
            );
        }
    }
}