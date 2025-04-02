using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardMovementScr : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Camera MainCamera;
    private Vector3 offset;
    public Transform DefaultParent, DefaultTempCardParent;
    private GameObject TempCardGO;
    public GameManagerScr GameManager;
    public bool IsDraggable;

    void Awake()
    {
        MainCamera = Camera.allCameras[0];
        TempCardGO = GameObject.Find("TempCardGO");
        GameManager = FindObjectOfType<GameManagerScr>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        offset = transform.position - MainCamera.ScreenToWorldPoint(eventData.position);

        DefaultParent = DefaultTempCardParent = transform.parent;

        IsDraggable = GameManager.IsPlayerTurn &&
        (
            (DefaultParent.GetComponent<DropPlaceScr>().Type == FieldType.SELF_HAND &&
            GameManager.PlayerMana >= GetComponent<CardInfoScr>().SelfCard.Manacost)
            ||
            (DefaultParent.GetComponent<DropPlaceScr>().Type == FieldType.SELF_FIELD &&
            GetComponent<CardInfoScr>().SelfCard.CanAttack)
        );

        if (!IsDraggable)
            return;

        if (GetComponent<CardInfoScr>().SelfCard.CanAttack)
            GameManager.HighlightTargets(true);

        TempCardGO.transform.SetParent(DefaultParent);
        TempCardGO.transform.SetSiblingIndex(transform.GetSiblingIndex());

        transform.SetParent(DefaultParent.parent);
        GetComponent<CanvasGroup>().blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsDraggable)
            return;

        Vector3 newPos = MainCamera.ScreenToWorldPoint(eventData.position);
        transform.position = newPos + offset;

        if (TempCardGO.transform.parent != DefaultTempCardParent)
            TempCardGO.transform.SetParent(DefaultTempCardParent);

        if (DefaultParent.GetComponent<DropPlaceScr>().Type != FieldType.SELF_FIELD)
            CheckPosition();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!IsDraggable)
            return;

        GameManager.HighlightTargets(false);

        transform.SetParent(DefaultParent);
        GetComponent<CanvasGroup>().blocksRaycasts = true;

        transform.SetSiblingIndex(TempCardGO.transform.GetSiblingIndex());
        TempCardGO.transform.SetParent(GameObject.Find("Canvas").transform);
        TempCardGO.transform.localPosition = new Vector3(2340, 0, 0);
    }

    private void CheckPosition()
    {
        int newIndex = DefaultTempCardParent.childCount;

        for (int i = 0; i < DefaultTempCardParent.childCount; i++)
        {
            if (transform.position.x < DefaultTempCardParent.GetChild(i).position.x)
            {
                newIndex = i;

                if (TempCardGO.transform.GetSiblingIndex() < newIndex)
                    newIndex--;

                break;
            }
        }

        TempCardGO.transform.SetSiblingIndex(newIndex);
    }
}
