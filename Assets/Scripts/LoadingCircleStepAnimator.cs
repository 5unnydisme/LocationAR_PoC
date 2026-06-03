using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingCircleStepAnimator : MonoBehaviour
{
    #region Inspector Configuration
    [Header("Dots")]
    [Tooltip ("Assign in clockwise order starting from top dor")]
    [SerializeField] private Image[] dots = new Image[8];

    [Header ("Animation")]
    [Tooltip("Seconds between each step. Lower values = faster animation.")]
    [SerializeField] private float stepIntervalSeconds = 0.12f;

    [Header("Colors")]
    [Tooltip("Color of the highlighted dot.")]
    [SerializeField] private Color activeColor = new Color(0.00f, 0.62f, 0.95f, 1f);

    [Tooltip("Color for all non-highlighted dots.")]
    [SerializeField] private Color inactiveColor = new Color(0.50f, 0.50f, 0.50f, 1f);
    #endregion
    private int currentIndex;

    private void OnEnable()
    {
        ApplyColors();
        StartCoroutine(AnimateDots());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator AnimateDots()
    {
        float safeInterval = stepIntervalSeconds > 0f ? stepIntervalSeconds : 0.1f; 
        WaitForSeconds wait = new WaitForSeconds(safeInterval); 

        while (true)
        {
            yield return wait; 
            
            if (dots == null || dots.Length ==0)
            {
                continue;
            }
            currentIndex = (currentIndex + 1) % dots.Length;
            ApplyColors();

        }
    }

    private void ApplyColors()
    {
        if (dots == null || dots.Length ==0)
        {
            return;
        }

        for (int i=0; i<dots.Length; i++)
        {
            if(dots[i] == null)
            {
                continue;
            }
            dots[i].color = (i==currentIndex) ? activeColor : inactiveColor;
        }
    }
}