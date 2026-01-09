using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardMovement : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private float moveDuration = 0.5f;

    public Transform defaultParent, tempParent;

    private int startIdx;
    private bool isDraggable;
    private Vector2 pointerOffsetCanvas;
    private Camera mainCamera;
    private GameObject cardTemp;
    private RectTransform rt, canvasRect;
    private Canvas rootCanvas;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null)
        {
            var go = GameObject.Find("Canvas");
            if (go != null)
                rootCanvas = go.GetComponent<Canvas>();
        }

        if (rootCanvas != null)
            canvasRect = rootCanvas.GetComponent<RectTransform>();

        cardTemp = GameObject.Find("Card Temp");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!TryGetComponent<CardController>(out var controller))
            return;

        if (GameManager.Instance.IsGameOver)
            return;

        if (defaultParent == null)
            defaultParent = transform.parent;

        if (tempParent == null)
            tempParent = defaultParent;

        var dropPlace = defaultParent != null ? defaultParent.GetComponent<DropPlace>() : null;
        isDraggable = GameManager.Instance != null && GameManager.Instance.IsMyTurn &&
                      dropPlace != null &&
                      (
                        (dropPlace.type == FieldType.PlayerHand && GameManager.Instance.currentGame.player.mana >= controller.self.manaCost)
                        ||
                        (dropPlace.type == FieldType.PlayerField && controller.self.canAttack)
                      );

        if (!isDraggable)
            return;

        startIdx = transform.GetSiblingIndex();

        if (controller.self.isSpell || controller.self.canAttack)
            GameManager.Instance.HighlightTargets(controller, true);

        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null)
            {
                var go = GameObject.Find("Canvas");
                if (go != null)
                    rootCanvas = go.GetComponent<Canvas>();
            }
            if (rootCanvas != null)
                canvasRect = rootCanvas.GetComponent<RectTransform>();
        }

        EnsureCardTempExists();

        if (cardTemp != null && defaultParent != null)
        {
            cardTemp.transform.SetParent(defaultParent, false);
            cardTemp.transform.SetSiblingIndex(transform.GetSiblingIndex());
            var tr = cardTemp.GetComponent<RectTransform>();
            if (tr != null && rt != null)
            {
                tr.sizeDelta = rt.sizeDelta;
                tr.pivot = rt.pivot;
                tr.anchorMin = rt.anchorMin;
                tr.anchorMax = rt.anchorMax;
            }
        }

        if (rootCanvas != null)
            transform.SetParent(rootCanvas.transform, true);
        else if (defaultParent != null)
        {
            var targetParent = defaultParent.parent;
            if (targetParent != null)
                transform.SetParent(targetParent, false);
        }

        if (canvasRect != null)
        {
            Vector2 canvasLocal;
            Camera canvasCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, canvasCam, out canvasLocal);

            pointerOffsetCanvas = rt.anchoredPosition - canvasLocal;
        }
        else
        {
            pointerOffsetCanvas = Vector2.zero;
        }

        if (TryGetComponent<CanvasGroup>(out var cg))
            cg.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggable)
            return;

        if (canvasRect != null)
        {
            Vector2 canvasLocal;
            Camera canvasCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, canvasCam, out canvasLocal))
                rt.anchoredPosition = canvasLocal + pointerOffsetCanvas;
        }
        else
            transform.position += (Vector3)eventData.delta;

        if (!TryGetComponent<CardController>(out var controller))
            return;

        if (!controller.self.isSpell)
        {
            if (cardTemp != null && tempParent != null && cardTemp.transform.parent != tempParent)
            {
                cardTemp.transform.SetParent(tempParent, false);
                cardTemp.transform.localScale = Vector3.one;
            }

            if (tempParent != null)
            {
                var dropPlace = tempParent.GetComponent<DropPlace>();

                if (dropPlace != null && dropPlace.type != FieldType.EnemyField && dropPlace.type != FieldType.EnemyHand)
                    CheckPosition();
                else if (dropPlace != null && dropPlace.type == FieldType.PlayerHand)
                    CheckPosition();
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDraggable)
            return;

        if (TryGetComponent<CardController>(out var controller))
            GameManager.Instance.HighlightTargets(controller, false);

        if (defaultParent != null)
            transform.SetParent(defaultParent, false);

        if (TryGetComponent<CanvasGroup>(out var cg))
            cg.blocksRaycasts = true;

        if (cardTemp != null)
        {
            int sibling = 0;
            try
            {
                sibling = Mathf.Clamp(cardTemp.transform.GetSiblingIndex(), 0, defaultParent != null ? defaultParent.childCount : 0);
            }
            catch
            {
                sibling = 0;
            }

            transform.SetSiblingIndex(sibling);

            if (rootCanvas != null)
            {
                cardTemp.transform.SetParent(rootCanvas.transform, false);
                cardTemp.transform.localPosition = new Vector3(2340, 0, 0);
            }
            else
            {
                cardTemp.transform.SetParent(transform.root, false);
                cardTemp.transform.localPosition = new Vector3(2340, 0, 0);
            }
        }
        else
            transform.SetAsLastSibling();

        isDraggable = false;
    }

    private void CheckPosition()
    {
        if (tempParent == null || cardTemp == null || defaultParent == null)
            return;

        int newIndex = tempParent.childCount;
        for (int i = 0; i < tempParent.childCount; i++)
        {
            if (transform.position.x < tempParent.GetChild(i).position.x)
            {
                newIndex = i;
                if (cardTemp.transform.GetSiblingIndex() < newIndex)
                    newIndex--;

                break;
            }
        }

        if (cardTemp.transform.parent == defaultParent)
            newIndex = startIdx;

        cardTemp.transform.SetSiblingIndex(Mathf.Max(0, newIndex));
    }

    private void EnsureCardTempExists()
    {
        if (cardTemp != null)
            return;

        cardTemp = GameObject.Find("Card Temp");
        if (cardTemp != null)
            return;

        cardTemp = new GameObject("Card Temp", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var img = cardTemp.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = false;

        if (rootCanvas != null)
            cardTemp.transform.SetParent(rootCanvas.transform, false);
        else
            cardTemp.transform.SetParent(transform.root, false);

        var tr = cardTemp.GetComponent<RectTransform>();
        if (tr != null && rt != null)
            tr.sizeDelta = rt.sizeDelta;
    }

    public void MoveToField(Transform field)
    {
        if (AnimationManager.Instance != null)
            AnimationManager.Instance.MoveToField(transform, field, moveDuration);
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
        if (AnimationManager.Instance != null)
        {
            AnimationManager.Instance.MoveToField(transform, target, moveDuration / 2f);
            yield return new WaitForSeconds(moveDuration / 2f);

        }

        yield break;
    }

    public void AnimateAttack(Transform target, System.Action onImpactCallback)
    {
        if (AnimationManager.Instance != null)
            AnimationManager.Instance.PlayAttack(transform, target, onImpactCallback);
    }
}