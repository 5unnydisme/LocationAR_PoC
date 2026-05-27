using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform targetRect;
    private Rect lastObservedSafeArea;
    private Rect lastAppliedSafeArea;
    private int lastObservedScreenWidth;
    private int lastObservedScreenHeight;
    private ScreenOrientation lastObservedOrientation;
    private bool hasAppliedSafeArea;

    private void Awake()
    {
        targetRect = GetComponent<RectTransform>();
        ApplySafeArea(force: true);
        CacheObservedScreenState();
    }

    private void Update()
    {
        if (!HasScreenStateChanged())
        {
            return;
        }

        ApplySafeArea(force: false);
        CacheObservedScreenState();
    }

    private bool HasScreenStateChanged()
    {
        return Screen.safeArea != lastObservedSafeArea
            || Screen.width != lastObservedScreenWidth
            ||Screen.height != lastObservedScreenHeight
            || Screen.orientation != lastObservedOrientation;
    }

    private void CacheObservedScreenState()
    {
        lastObservedSafeArea = Screen.safeArea;
        lastObservedScreenWidth = Screen.width;
        lastObservedScreenHeight = Screen.height;
        lastObservedOrientation = Screen.orientation;
    }

    private void ApplySafeArea (bool force)
    {
        Rect safeArea = Screen.safeArea;

        if (!force && hasAppliedSafeArea && safeArea == lastAppliedSafeArea)
        {
            return;
        }

        float width = Screen.width;
        float height = Screen.height;

        if (width <= 0f || height <= 0f)
        {
            return;
        }

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= width;
        anchorMin.y /= height;
        anchorMax.x /= width;
        anchorMax.y /= height;

        targetRect.anchorMin = anchorMin;
        targetRect.anchorMax = anchorMax;

        lastAppliedSafeArea = safeArea;
        hasAppliedSafeArea = true;
    }
}