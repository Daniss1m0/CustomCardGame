using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class AnimationManager : MonoBehaviour
{
    public static AnimationManager Instance;

    public int GlobalBusyCount { get; private set; } = 0;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void PlayOpponentDraw(CardController card, Transform targetParent, int fallbackIndex)
    {
        if (card == null) 
            return;

        GlobalBusyCount++;
        card.IsAnimating = true;

        Canvas rootCanvas = card.GetComponentInParent<Canvas>();
        if (rootCanvas != null && rootCanvas.rootCanvas != null)
            rootCanvas = rootCanvas.rootCanvas;

        Transform showcaseParent = rootCanvas != null ? rootCanvas.transform : card.transform.root;
        card.transform.SetParent(showcaseParent, true);

        card.Info?.ShowCard(card.self);
        ResetVisualState(card);

        RectTransform rt = card.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            float startY = 600f;
            if (rootCanvas != null)
            {
                RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
                startY = (canvasRect.rect.height / 2f) + 250f;
            }
            rt.anchoredPosition = new Vector2(0, startY);

            Vector3 originalScale = Vector3.one;
            Sequence sequence = DOTween.Sequence();

            sequence.Append(rt.DOAnchorPos(Vector2.zero, 0.5f).SetEase(Ease.OutBack));
            sequence.Join(rt.DOScale(originalScale * 1.5f, 0.5f).SetEase(Ease.OutBack));
            sequence.Join(rt.DORotate(Vector3.zero, 0.3f));
            sequence.AppendInterval(0.6f);
            sequence.AppendCallback(() => { rt.DOScale(originalScale, 0.3f); });

            if (targetParent != null)
                sequence.Append(card.transform.DOMove(targetParent.position, 0.4f).SetEase(Ease.InQuad));

            sequence.OnComplete(() =>
            {
                if (targetParent != null)
                {
                    card.transform.SetParent(targetParent, false);

                    int finalIndex = fallbackIndex;
                    if (card.Network != null)
                        finalIndex = card.Network.fieldIndex.Value;

                    card.transform.SetSiblingIndex(finalIndex);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(targetParent as RectTransform);
                }

                card.transform.localScale = Vector3.one;
                card.transform.localRotation = Quaternion.identity;
                card.transform.localPosition = Vector3.zero;

                card.IsAnimating = false;
                DecrementBusyCount();
            });
        }
        else
            DecrementBusyCount();
    }

    public void PlayOpponentSpell(CardController card)
    {
        if (card == null) 
            return;

        if (AudioManager.Instance != null) 
            AudioManager.Instance.PlaySpellCast();

        GlobalBusyCount++;

        Canvas rootCanvas = card.GetComponentInParent<Canvas>();
        if (rootCanvas != null && rootCanvas.rootCanvas != null)
            rootCanvas = rootCanvas.rootCanvas;

        Transform showcaseParent = rootCanvas != null ? rootCanvas.transform : card.transform.root;
        card.transform.SetParent(showcaseParent, true);

        card.Info?.ShowCard(card.self);
        ResetVisualState(card);

        RectTransform rt = card.GetComponent<RectTransform>();
        if (rt == null)
        {
            Destroy(card.gameObject);
            DecrementBusyCount();
            return;
        }

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        float startY = 600f;
        if (rootCanvas != null)
        {
            RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
            startY = (canvasRect.rect.height / 2f) + 250f;
        }
        rt.anchoredPosition = new Vector2(0, startY);

        Vector3 originalScale = Vector3.one;
        Sequence sequence = DOTween.Sequence();

        sequence.Append(rt.DOAnchorPos(Vector2.zero, 0.5f).SetEase(Ease.OutBack));
        sequence.Join(rt.DOScale(originalScale * 1.6f, 0.5f).SetEase(Ease.OutBack));
        sequence.Join(rt.DORotate(Vector3.zero, 0.3f));
        sequence.AppendInterval(0.8f);

        CanvasGroup cg = card.GetComponent<CanvasGroup>();
        if (cg == null) 
            cg = card.gameObject.AddComponent<CanvasGroup>();

        sequence.Append(rt.DOScale(originalScale * 2f, 0.4f));
        sequence.Join(cg.DOFade(0f, 0.4f));

        sequence.OnComplete(() =>
        {
            Destroy(card.gameObject);
            DecrementBusyCount();
        });
    }

    public void PlayAttack(Transform attackerTransform, Transform targetTransform, System.Action onImpactCallback)
    {
        if (attackerTransform == null) 
            return;

        var controller = attackerTransform.GetComponent<CardController>();
        if (controller != null) 
            controller.IsAnimating = true;

        Transform originalParent = attackerTransform.parent;
        int originalIndex = attackerTransform.GetSiblingIndex();

        GameObject attackPlaceholder = new GameObject("AttackPlaceholder", typeof(RectTransform));
        attackPlaceholder.transform.SetParent(originalParent, false);
        attackPlaceholder.transform.SetSiblingIndex(originalIndex);

        RectTransform myRect = attackerTransform.GetComponent<RectTransform>();
        RectTransform phRect = attackPlaceholder.GetComponent<RectTransform>();
        if (myRect != null)
        {
            phRect.sizeDelta = myRect.sizeDelta;
            phRect.anchorMin = myRect.anchorMin;
            phRect.anchorMax = myRect.anchorMax;
            phRect.pivot = myRect.pivot;
            phRect.localScale = myRect.localScale;
        }

        Canvas rootCanvas = attackerTransform.GetComponentInParent<Canvas>();
        if (rootCanvas != null && rootCanvas.rootCanvas != null)
            rootCanvas = rootCanvas.rootCanvas;

        Transform topLevel = rootCanvas != null ? rootCanvas.transform : attackerTransform.root;
        attackerTransform.SetParent(topLevel, true);

        Vector3 impactPos = (targetTransform != null) ? targetTransform.position : attackerTransform.position;

        Sequence seq = DOTween.Sequence();
        seq.SetLink(attackerTransform.gameObject);

        seq.Append(attackerTransform.DOMove(impactPos, 0.4f).SetEase(Ease.InQuad));
        seq.Join(attackerTransform.DORotate(new Vector3(0, 0, 5f), 0.2f));

        seq.AppendCallback(() =>
        {
            if (AudioManager.Instance != null)
            {
                if (targetTransform != null && targetTransform.GetComponent<AttackedHero>() != null)
                    AudioManager.Instance.PlayHeroHit();
                else
                    AudioManager.Instance.PlayAttack();
            }

            onImpactCallback?.Invoke();
            if (targetTransform != null)
                targetTransform.DOShakePosition(0.3f, 15, 20);
        });

        seq.AppendCallback(() =>
        {
            if (attackPlaceholder != null && attackerTransform != null)
            {
                Vector3 returnTarget = attackPlaceholder.transform.position;
                attackerTransform.DOMove(returnTarget, 0.4f).SetEase(Ease.OutQuad);
                attackerTransform.DORotate(Vector3.zero, 0.4f).SetEase(Ease.OutQuad);
                attackerTransform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutQuad);
            }
        });

        seq.AppendInterval(0.4f);
        seq.OnComplete(() =>
        {
            if (attackPlaceholder != null) 
                Destroy(attackPlaceholder);

            if (attackerTransform != null)
            {
                attackerTransform.SetParent(originalParent, true);
                attackerTransform.SetSiblingIndex(originalIndex);
                attackerTransform.localPosition = Vector3.zero;
                attackerTransform.localRotation = Quaternion.identity;
                attackerTransform.localScale = Vector3.one;

                LayoutRebuilder.ForceRebuildLayoutImmediate(originalParent as RectTransform);
            }
            if (controller != null) 
                controller.IsAnimating = false;
        });
    }

    public void PlayDeath(Transform cardTransform, System.Action onComplete)
    {
        if (cardTransform == null) return;

        var le = cardTransform.GetComponent<LayoutElement>();
        if (le == null) le = cardTransform.gameObject.AddComponent<LayoutElement>();
        le.ignoreLayout = true;

        if (cardTransform.parent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardTransform.parent as RectTransform);

        cardTransform.DOKill();
        cardTransform.DOScale(Vector3.zero, 0.3f)
            .SetEase(Ease.InBack)
            .SetLink(cardTransform.gameObject)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void MoveToField(Transform cardTransform, Transform targetField, float duration)
    {
        if (cardTransform == null || targetField == null) 
            return;

        var canvasGO = GameObject.Find("Canvas");
        if (canvasGO != null)
            cardTransform.SetParent(canvasGO.transform, false);

        if (DOTween.IsTweening(cardTransform))
            DOTween.Kill(cardTransform, false);

        if (duration <= 0f)
            cardTransform.position = targetField.position;
        else
            cardTransform.DOMove(targetField.position, duration).SetLink(cardTransform.gameObject);
    }

    public void ShakeObject(Transform target)
    {
        if (target != null)
            target.DOShakePosition(0.3f, 15, 20);
    }

    private void DecrementBusyCount()
    {
        GlobalBusyCount--;
        if (GlobalBusyCount < 0) 
            GlobalBusyCount = 0;
    }

    private void ResetVisualState(CardController card)
    {
        var cg = card.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }
    }
}