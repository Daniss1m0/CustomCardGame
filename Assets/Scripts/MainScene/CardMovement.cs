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
    private Vector2 pointerOffsetCanvas; // offset in canvas local coordinates
    private Camera mainCamera;
    private GameObject tempCard;

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
            if (go != null) rootCanvas = go.GetComponent<Canvas>();
        }
        if (rootCanvas != null)
            canvasRect = rootCanvas.GetComponent<RectTransform>();

        if (tempCard == null)
            tempCard = GameObject.Find("TempCard");
    }

    private bool CanReparentNetworkObject()
    {
        if (TryGetComponent<NetworkObject>(out var no))
            return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        return true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        var controller = GetComponent<CardController>();
        if (controller == null) return;

        if (defaultParent == null)
            defaultParent = transform.parent;
        if (tempParent == null)
            tempParent = defaultParent;

        var dropPlace = defaultParent != null ? defaultParent.GetComponent<DropPlace>() : null;
        isDraggable = GameManager.Instance != null && GameManager.Instance.IsPlayerTurn &&
                      dropPlace != null &&
                      (
                        (dropPlace.type == FieldType.PlayerHand && GameManager.Instance.currentGame.player.mana >= controller.self.manaCost)
                        ||
                        (dropPlace.type == FieldType.PlayerField && controller.self.canAttack)
                      );

        if (!isDraggable) return;

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

        EnsureTempCardExists();

        if (tempCard != null && defaultParent != null)
        {
            tempCard.transform.SetParent(defaultParent, false);
            tempCard.transform.SetSiblingIndex(transform.GetSiblingIndex());
            var tr = tempCard.GetComponent<RectTransform>();
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
        if (cg != null) cg.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggable) return;

        if (canvasRect != null)
        {
            Vector2 canvasLocal;
            Camera canvasCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, canvasCam, out canvasLocal))
            {
                rt.anchoredPosition = canvasLocal + pointerOffsetCanvas;
            }
        }
        else
        {
            transform.position += (Vector3)eventData.delta;
        }

        var controller = GetComponent<CardController>();
        if (controller == null) return;

        if (!controller.self.isSpell)
        {
            if (tempCard != null && tempCard.transform.parent != tempParent && tempParent != null)
                tempCard.transform.SetParent(tempParent, false);

            var dp = defaultParent != null ? defaultParent.GetComponent<DropPlace>() : null;
            if (dp != null && dp.type != FieldType.PlayerField)
                CheckPosition();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDraggable) return;

        var controller = GetComponent<CardController>();
        if (controller != null)
            GameManager.Instance.HighlightTargets(controller, false);

        if (defaultParent != null)
            transform.SetParent(defaultParent, false);

        var cg = GetComponent<CanvasGroup>();
        if (cg != null) cg.blocksRaycasts = true;

        if (tempCard != null)
        {
            int sibling = 0;
            try { sibling = Mathf.Clamp(tempCard.transform.GetSiblingIndex(), 0, defaultParent != null ? defaultParent.childCount : 0); } catch { sibling = 0; }

            transform.SetSiblingIndex(sibling);

            if (rootCanvas != null)
            {
                tempCard.transform.SetParent(rootCanvas.transform, false);
                tempCard.transform.localPosition = new Vector3(2340, 0, 0);
            }
            else
            {
                tempCard.transform.SetParent(transform.root, false);
                tempCard.transform.localPosition = new Vector3(2340, 0, 0);
            }
        }
        else
        {
            transform.SetAsLastSibling();
        }

        isDraggable = false;
    }

    private void CheckPosition()
    {
        if (tempParent == null || tempCard == null || defaultParent == null) return;

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
        tempCard.transform.SetSiblingIndex(Mathf.Max(0, newIndex));
    }

    private void EnsureTempCardExists()
    {
        if (tempCard != null) return;

        tempCard = GameObject.Find("TempCard");
        if (tempCard != null) return;

        tempCard = new GameObject("TempCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var img = tempCard.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = false;

        if (rootCanvas != null)
            tempCard.transform.SetParent(rootCanvas.transform, false);
        else
            tempCard.transform.SetParent(transform.root, false);

        var tr = tempCard.GetComponent<RectTransform>();
        if (tr != null && rt != null)
            tr.sizeDelta = rt.sizeDelta;
    }

    public void MoveToField(Transform field)
    {
        if (field == null) return;

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
        if (target == null || transform == null) yield break;

        Vector3 pos = transform.position;
        Transform parent = transform.parent;
        int index = transform.GetSiblingIndex();

        var parentHL = parent?.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        if (parentHL != null) parentHL.enabled = false;

        var canvasGO = GameObject.Find("Canvas");
        if (canvasGO != null) transform.SetParent(canvasGO.transform, false);

        float halfDur = moveDuration / 2f;
        if (halfDur <= 0f)
        {
            transform.position = target.position;
        }
        else
        {
            if (DOTween.IsTweening(transform)) DOTween.Kill(transform, false);

            transform.DOMove(target.position, halfDur).SetLink(gameObject);
            yield return new WaitForSeconds(halfDur);

            if (transform == null) yield break;

            transform.DOMove(pos, halfDur).SetLink(gameObject);
            yield return new WaitForSeconds(halfDur);
        }

        if (transform == null) yield break;

        if (parent != null) transform.SetParent(parent, false);

        transform.SetSiblingIndex(Mathf.Clamp(index, 0, parent?.childCount ?? 0));

        if (parentHL != null) parentHL.enabled = true;
    }
}
