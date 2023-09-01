using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonAnimator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler
{
    [SerializeField] private new RectTransform transform;

    [SerializeField] private float hoverSize;

    private Vector2 startSize;

    private void Start()
    {
        startSize = transform.sizeDelta;
    }

    public void OnPointerDown(PointerEventData eventData)
    {

    }

    public void OnPointerUp(PointerEventData eventData)
    {

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        LeanTween.value(1, 0, 0.3f).setEaseInExpo().setOnUpdate(t => {
            transform.sizeDelta = startSize + new Vector3(hoverSize, 0, 0) * t;
        });
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        LeanTween.value(0, 1, 0.3f).setEaseOutExpo().setOnUpdate(t => {
            transform.sizeDelta = startSize + new Vector3(hoverSize, 0, 0) * t;
        });
    }
}
