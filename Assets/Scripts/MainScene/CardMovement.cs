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
        if (mainCamera == null) 
            mainCamera = Camera.main;

        if (tempCard == null) 
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

        if (tempCard != null)
        {
            // безопасно вычислим индекс
            int sibling = Mathf.Clamp(tempCard.transform.GetSiblingIndex(), 0, defaultParent.childCount);
            transform.SetSiblingIndex(sibling);
            // убираем tempCard в безопасное место (канвас), чтобы не мешала
            var canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                tempCard.transform.SetParent(canvas.transform);
                tempCard.transform.localPosition = new Vector3(2340, 0, 0);
            }
        }
        else
        {
            transform.SetAsLastSibling();
        }
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
        if (field == null) return;

        // Привилегированная проверка Canvas
        var canvasGO = GameObject.Find("Canvas");
        if (canvasGO != null)
            transform.SetParent(canvasGO.transform);

        // Убиваем предыдущие твины на этом трансформе (без completion)
        if (DOTween.IsTweening(transform))
            DOTween.Kill(transform, false);

        // Если вдруг скорость нулевая — ставим мгновенно
        if (moveDuration <= 0f)
        {
            transform.position = field.position;
            return;
        }

        // Запускаем tween и привязываем его к gameObject — при уничтожении объекта tween будет убит автоматически
        transform.DOMove(field.position, moveDuration).SetLink(gameObject);
    }

    public void MoveToTarget(Transform target)
    {
        StartCoroutine(MoveToTargetCor(target));
    }

    private IEnumerator MoveToTargetCor(Transform target)
    {
        if (target == null || transform == null)
            yield break;

        Vector3 pos = transform.position;
        Transform parent = transform.parent;
        int index = transform.GetSiblingIndex();

        var parentHL = parent?.GetComponent<HorizontalLayoutGroup>();
        if (parentHL != null)
            parentHL.enabled = false;

        var canvasGO = GameObject.Find("Canvas");
        if (canvasGO != null)
            transform.SetParent(canvasGO.transform);

        float halfDur = moveDuration / 2f;
        if (halfDur <= 0f)
        {
            transform.position = target.position;
        }
        else
        {
            // убиваем возможные предыдущие твины
            if (DOTween.IsTweening(transform))
                DOTween.Kill(transform, false);

            // запускаем и привязываем к объекту
            transform.DOMove(target.position, halfDur).SetLink(gameObject);
            yield return new WaitForSeconds(halfDur);

            // если объект уничтожён — выходим
            if (transform == null) yield break;

            transform.DOMove(pos, halfDur).SetLink(gameObject);
            yield return new WaitForSeconds(halfDur);
        }

        // восстановление родителя/индекса
        if (transform == null) yield break;
        transform.SetParent(parent);
        transform.SetSiblingIndex(Mathf.Clamp(index, 0, parent?.childCount ?? 0));

        if (parentHL != null)
            parentHL.enabled = true;
    }
}