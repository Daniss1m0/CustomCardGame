using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

public class CardMovement : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public bool isDraggable;
    public Transform defaultParent, defaultTempCardParent;
    public CardController CC;
    
    private int startID;
    private Vector3 offset;
    private Camera mainCamera;
    private GameObject tempCard;

    void Awake()
    {
        mainCamera = Camera.allCameras[0];
        tempCard = GameObject.Find("TempCard");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        offset = transform.position - mainCamera.ScreenToWorldPoint(eventData.position);

        defaultParent = defaultTempCardParent = transform.parent;

        isDraggable = GameManager.Instance.IsPlayerTurn &&
        (
            (defaultParent.GetComponent<DropPlace>().type == FieldType.SELF_HAND &&
            GameManager.Instance.currentGame.player.mana >= CC.self.manaCost)
            ||
            (defaultParent.GetComponent<DropPlace>().type == FieldType.SELF_FIELD &&
            CC.self.canAttack)
        );

        if (!isDraggable)
            return;

        startID = transform.GetSiblingIndex();

        if (CC.self.isSpell || CC.self.canAttack)
            GameManager.Instance.HighlightTargets(CC, true);

        tempCard.transform.SetParent(defaultParent);
        tempCard.transform.SetSiblingIndex(transform.GetSiblingIndex());

        transform.SetParent(defaultParent.parent);
        GetComponent<CanvasGroup>().blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggable)
            return;

        Vector3 newPos = mainCamera.ScreenToWorldPoint(eventData.position);
        transform.position = newPos + offset;

        if (!CC.self.isSpell)
        {
            if (tempCard.transform.parent != defaultTempCardParent)
                tempCard.transform.SetParent(defaultTempCardParent);

            if (defaultParent.GetComponent<DropPlace>().type != FieldType.SELF_FIELD)
                CheckPosition();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDraggable)
            return;

        GameManager.Instance.HighlightTargets(CC, false);

        transform.SetParent(defaultParent);
        GetComponent<CanvasGroup>().blocksRaycasts = true;

        transform.SetSiblingIndex(tempCard.transform.GetSiblingIndex());
        tempCard.transform.SetParent(GameObject.Find("Canvas").transform);
        tempCard.transform.localPosition = new Vector3(2340, 0, 0);
    }

    private void CheckPosition()
    {
        int newIndex = defaultTempCardParent.childCount;

        for (int i = 0; i < defaultTempCardParent.childCount; i++)
        {
            if (transform.position.x < defaultTempCardParent.GetChild(i).position.x)
            {
                newIndex = i;

                if (tempCard.transform.GetSiblingIndex() < newIndex)
                    newIndex--;

                break;
            }
        }

        if (tempCard.transform.parent == defaultParent)
            newIndex = startID;

        tempCard.transform.SetSiblingIndex(newIndex);
    }

    public void MoveToField(Transform field) 
    {
        transform.SetParent(GameObject.Find("Canvas").transform);
        transform.DOMove(field.position, .5f);
    }

    public void MoveToTarget(Transform target)
    {
        StartCoroutine(MoveToTargetCor(target));
    }

    IEnumerator MoveToTargetCor(Transform target)
    {
        Vector3 pos = transform.position;
        Transform parent = transform.parent;
        int index = transform.GetSiblingIndex();

        if (transform.parent.GetComponent<HorizontalLayoutGroup>())
            transform.parent.GetComponent<HorizontalLayoutGroup>().enabled = false;

        transform.SetParent(GameObject.Find("Canvas").transform);

        transform.DOMove(target.position, .25f);

        yield return new WaitForSeconds(.25f);

        transform.DOMove(pos, .25f);

        yield return new WaitForSeconds(.25f);

        transform.SetParent(parent);
        transform.SetSiblingIndex(index);

        if (transform.parent.GetComponent<HorizontalLayoutGroup>())
            transform.parent.GetComponent<HorizontalLayoutGroup>().enabled = true;
    }
}
