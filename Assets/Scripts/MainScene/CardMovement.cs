using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

public class CardMovement : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private float moveDuration = 0.5f;

    public Transform defaultParent, tempParent;

    private int startIndex;
    private bool isDraggable;
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
        var controller = GetComponent<CardController>(); //mb change later
        offset = transform.position - mainCamera.ScreenToWorldPoint(eventData.position);
        defaultParent = tempParent = transform.parent;

        isDraggable = GameManager.Instance.IsPlayerTurn &&
                      (
                        (defaultParent.GetComponent<DropPlace>().type == FieldType.PlayerHand &&
                        GameManager.Instance.currentGame.player.mana >= controller.self.manaCost)
                        ||
                        (defaultParent.GetComponent<DropPlace>().type == FieldType.PlayerField &&
                        controller.self.canAttack)
                      );

        if (!isDraggable) return;

        startIndex = transform.GetSiblingIndex();

        if (controller.self.isSpell || controller.self.canAttack)
            GameManager.Instance.HighlightTargets(controller, true);

        tempCard.transform.SetParent(defaultParent);
        tempCard.transform.SetSiblingIndex(transform.GetSiblingIndex());
        transform.SetParent(defaultParent.parent);
        GetComponent<CanvasGroup>().blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggable) return;

        Vector3 newPos = mainCamera.ScreenToWorldPoint(eventData.position);
        transform.position = newPos + offset;

        var controller = GetComponent<CardController>();

        if (!controller.self.isSpell)
        {
            if (tempCard.transform.parent != tempParent) tempCard.transform.SetParent(tempParent);
            if (defaultParent.GetComponent<DropPlace>().type != FieldType.PlayerField) CheckPosition();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDraggable) return;

        var controller = GetComponent<CardController>();
        GameManager.Instance.HighlightTargets(controller, false);

        transform.SetParent(defaultParent);
        GetComponent<CanvasGroup>().blocksRaycasts = true;
        transform.SetSiblingIndex(tempCard.transform.GetSiblingIndex());
        tempCard.transform.SetParent(GameObject.Find("Canvas").transform);
        tempCard.transform.localPosition = new Vector3(2340, 0, 0);
    }

    private void CheckPosition()
    {
        int newIndex = tempParent.childCount;
        for (int i = 0; i < tempParent.childCount; i++)
        {
            if (transform.position.x < tempParent.GetChild(i).position.x)
            {
                newIndex = i;
                if (tempCard.transform.GetSiblingIndex() < newIndex) newIndex--;
                break;
            }
        }

        if (tempCard.transform.parent == defaultParent) newIndex = startIndex;
        tempCard.transform.SetSiblingIndex(newIndex);
    }

    public void MoveToField(Transform field)
    {
        transform.SetParent(GameObject.Find("Canvas").transform);
        transform.DOMove(field.position, moveDuration);
    }

    public void MoveToTarget(Transform target)
    {
        StartCoroutine(MoveToTargetCor(target));
    }

    private IEnumerator MoveToTargetCor(Transform target)
    {
        Vector3 pos = transform.position;
        Transform parent = transform.parent;
        int index = transform.GetSiblingIndex();

        if (transform.parent.GetComponent<HorizontalLayoutGroup>())
            transform.parent.GetComponent<HorizontalLayoutGroup>().enabled = false;

        transform.SetParent(GameObject.Find("Canvas").transform);
        transform.DOMove(target.position, moveDuration / 2);
        yield return new WaitForSeconds(moveDuration / 2);
        transform.DOMove(pos, moveDuration / 2);
        yield return new WaitForSeconds(moveDuration / 2);

        transform.SetParent(parent);
        transform.SetSiblingIndex(index);

        if (transform.parent.GetComponent<HorizontalLayoutGroup>())
            transform.parent.GetComponent<HorizontalLayoutGroup>().enabled = true;
    }
}