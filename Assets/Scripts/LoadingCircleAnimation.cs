using UnityEngine;

public class LoadingCircleAnimation : MonoBehaviour
{
    private RectTransform rectComponent;
    private float rotateSpeed = 200f;

    public void StartAnimation()
    {
        rectComponent = GetComponent<RectTransform>();
    }

    public void Update()
    {
        rectComponent.Rotate(0f, 0f, -rotateSpeed * Time.deltaTime); //negative rotateSpeed to rotate clockwise
    }
}