using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class CardEventProxy : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public CardMovement targetMovement;

    private void EnsureTarget()
    {
        if (targetMovement != null) return;
        targetMovement = GetComponent<CardMovement>() ?? GetComponentInChildren<CardMovement>(true);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        EnsureTarget();
        if (targetMovement != null)
            targetMovement.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        EnsureTarget();
        if (targetMovement != null)
            targetMovement.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EnsureTarget();
        if (targetMovement != null)
            targetMovement.OnEndDrag(eventData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        EnsureTarget();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        EnsureTarget();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        EnsureTarget();
    }
}
