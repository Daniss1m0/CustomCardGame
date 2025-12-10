using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using Unity.Netcode;

public class CardMovement : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private float moveDuration = 0.5f;

    public Transform defaultParent, tempParent;

    private int startIndex;
    private bool isDraggable;
    private Vector2 pointerOffsetCanvas;
    private Camera mainCamera;
    private GameObject cardTemp, attackPlaceholder;
    private RectTransform rt, canvasRect;
    private Canvas rootCanvas;

    void Awake()
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

        cardTemp = GameObject.Find("CardTemp");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        var controller = GetComponent<CardController>();
        if (controller == null)
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

        startIndex = transform.GetSiblingIndex();

        if (controller.self.isSpell || controller.self.canAttack)
            GameManager.Instance.HighlightTargets(controller, true);

        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null)
            {
                var go = GameObject.Find("Canvas");
                if (go != null) rootCanvas = go.GetComponent<Canvas>();
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
        {
            transform.SetParent(rootCanvas.transform, true);
        }
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

        var cg = GetComponent<CanvasGroup>();
        if (cg != null)
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

        var controller = GetComponent<CardController>();
        if (controller == null)
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

        var controller = GetComponent<CardController>();
        if (controller != null)
            GameManager.Instance.HighlightTargets(controller, false);

        if (defaultParent != null)
            transform.SetParent(defaultParent, false);

        var cg = GetComponent<CanvasGroup>();
        if (cg != null)
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
            newIndex = startIndex;

        cardTemp.transform.SetSiblingIndex(Mathf.Max(0, newIndex));
    }

    private void EnsureCardTempExists()
    {
        if (cardTemp != null)
            return;

        cardTemp = GameObject.Find("CardTemp");
        if (cardTemp != null)
            return;

        cardTemp = new GameObject("CardTemp", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
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
        if (field == null)
            return;

        var canvasGO = GameObject.Find("Canvas");
        if (canvasGO != null)
            transform.SetParent(canvasGO.transform, false);

        if (DOTween.IsTweening(transform))
            DOTween.Kill(transform, false);

        if (moveDuration <= 0f)
        {
            transform.position = field.position;
            return;
        }

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

        var parentHL = parent?.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        if (parentHL != null) parentHL.enabled = false;

        var canvasGO = GameObject.Find("Canvas");
        if (canvasGO != null) transform.SetParent(canvasGO.transform, false);

        float halfDur = moveDuration / 2f;
        if (halfDur <= 0f)
            transform.position = target.position;
        else
        {
            if (DOTween.IsTweening(transform))
                DOTween.Kill(transform, false);

            transform.DOMove(target.position, halfDur).SetLink(gameObject);
            yield return new WaitForSeconds(halfDur);

            if (transform == null)
                yield break;

            transform.DOMove(pos, halfDur).SetLink(gameObject);
            yield return new WaitForSeconds(halfDur);
        }

        if (transform == null)
            yield break;

        if (parent != null)
            transform.SetParent(parent, false);

        transform.SetSiblingIndex(Mathf.Clamp(index, 0, parent?.childCount ?? 0));

        if (parentHL != null)
            parentHL.enabled = true;
    }

    public void AnimateAttack(Transform target, System.Action onImpactCallback)
    {
        if (this == null || transform == null || gameObject == null)
            return;

        var controller = GetComponent<CardController>();

        if (controller != null) 
            controller.IsAnimating = true;

        if (attackPlaceholder != null)
            Destroy(attackPlaceholder);

        if (target == null)
        {
            onImpactCallback?.Invoke();
            return;
        }

        DOTween.Kill(transform);

        Transform originalParent = transform.parent;
        int originalIndex = transform.GetSiblingIndex();

        attackPlaceholder = new GameObject("AttackPlaceholder", typeof(RectTransform));
        attackPlaceholder.transform.SetParent(originalParent, false);
        attackPlaceholder.transform.SetSiblingIndex(originalIndex);

        RectTransform myRect = GetComponent<RectTransform>();
        RectTransform phRect = attackPlaceholder.GetComponent<RectTransform>();

        if (myRect != null)
        {
            phRect.sizeDelta = myRect.sizeDelta;
            phRect.anchorMin = myRect.anchorMin;
            phRect.anchorMax = myRect.anchorMax;
            phRect.pivot = myRect.pivot;
            phRect.localScale = myRect.localScale;
        }

        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null && rootCanvas.rootCanvas != null)
            rootCanvas = rootCanvas.rootCanvas;

        Transform topLevel = rootCanvas != null ? rootCanvas.transform : transform.root;

        transform.SetParent(topLevel, true);

        Vector3 impactPos = target.position;

        Sequence seq = DOTween.Sequence();
        seq.SetLink(gameObject);

        seq.Append(transform.DOMove(impactPos, 0.4f).SetEase(Ease.InQuad));
        seq.Join(transform.DORotate(new Vector3(0, 0, 5f), 0.2f));

        seq.AppendCallback(() =>
        {
            if (this == null || gameObject == null)
                return;

            onImpactCallback?.Invoke();
            if (target != null)
                target.DOShakePosition(0.3f, 15, 20);
        });

        seq.AppendCallback(() =>
        {
            if (attackPlaceholder != null && transform != null)
            {
                Vector3 returnTarget = attackPlaceholder.transform.position;

                transform.DOMove(returnTarget, 0.4f).SetEase(Ease.OutQuad);
                transform.DORotate(Vector3.zero, 0.4f).SetEase(Ease.OutQuad);
                transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutQuad);
            }
        });

        seq.AppendInterval(0.4f);

        seq.OnComplete(() =>
        {
            if (attackPlaceholder != null)
                Destroy(attackPlaceholder);

            attackPlaceholder = null;

            if (this == null || transform == null)
                return;

            transform.SetParent(originalParent, true);
            transform.SetSiblingIndex(originalIndex);

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            LayoutRebuilder.ForceRebuildLayoutImmediate(originalParent as RectTransform);

            if (controller != null) 
                controller.IsAnimating = false;
        });
    }

    public void ForceCleanupAnimation()
    {
        DOTween.Kill(transform);

        if (attackPlaceholder != null)
        {
            Destroy(attackPlaceholder);
            attackPlaceholder = null;
        }
    }
}