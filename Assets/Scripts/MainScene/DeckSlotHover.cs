using UnityEngine;
using UnityEngine.EventSystems;

public class DeckSlotHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject counterObject;

    private void Start()
    {
        if (counterObject != null)
            counterObject.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (counterObject != null)
            counterObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (counterObject != null)
            counterObject.SetActive(false);
    }
}