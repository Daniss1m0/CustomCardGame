using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardInfo : MonoBehaviour
{
    [SerializeField] private Color normalColor, targetColor, spellTargetColor;
    [SerializeField] private TextMeshProUGUI nameTxt, attackTxt, healthTxt, manaCostTxt;
    [SerializeField] private Image logo;
    [SerializeField] private GameObject hideState, highlightState;

    private Image background;
    private CanvasGroup canvasGroup;
    private Vector2 logoInitialSize;

    private void Awake()
    {
        background = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (logo != null)
        {
            var rt = logo.GetComponent<RectTransform>();
            if (rt != null) 
                logoInitialSize = rt.sizeDelta;
            else 
                logoInitialSize = Vector2.zero;
        }
    }

    public void ShowCard(Card card)
    {
        if (hideState != null) 
            hideState.SetActive(false);

        if (logo != null)
        {
            if (card != null && card.logo != null)
            {
                logo.enabled = true;
                logo.sprite = card.logo;
                logo.preserveAspect = true;
                var rt = logo.GetComponent<RectTransform>();
                if (rt != null && logoInitialSize != Vector2.zero)
                    rt.sizeDelta = logoInitialSize;
            }
            else
            {
                logo.enabled = false;
                logo.sprite = null;
            }
        }

        if (nameTxt != null) 
            nameTxt.text = card != null ? card.name : "";

        if (card != null && card.isSpell)
        {
            if (attackTxt != null && attackTxt.transform.parent != null)
                attackTxt.transform.parent.gameObject.SetActive(false);
            if (healthTxt != null && healthTxt.transform.parent != null)
                healthTxt.transform.parent.gameObject.SetActive(false);
        }

        UpdateStats(card);
    }

    public void HideCard()
    {
        if (hideState != null) 
            hideState.SetActive(true);

        if (nameTxt != null) 
            nameTxt.text = "";
        if (attackTxt != null) 
            attackTxt.text = "";
        if (healthTxt != null) 
            healthTxt.text = "";
        if (manaCostTxt != null) 
            manaCostTxt.text = "";

        if (logo != null) 
        { 
            logo.enabled = false; 
            logo.sprite = null; 
        }
    }

    public void UpdateStats(Card card)
    {
        if (attackTxt != null) 
            attackTxt.text = card != null ? card.attack.ToString() : "";
        if (healthTxt != null) 
            healthTxt.text = card != null ? card.health.ToString() : "";
        if (manaCostTxt != null) 
            manaCostTxt.text = card != null ? card.manaCost.ToString() : "";
    }

    public void SetHighlight(bool highlight)
    {
        if (highlightState != null) 
            highlightState.SetActive(highlight);
    }

    public void HighlightAsTarget(bool active)
    {
        HighlightHelper.SetTargetHighlight(background, active, normalColor, targetColor);
    }

    public void HighlightAsSpellTarget(bool active)
    {
        HighlightHelper.SetSpellTargetHighlight(background, active, normalColor, spellTargetColor);
    }

    public void SetAvailability(bool hasMana, bool isPlayerTurn)
    {
        if (!isPlayerTurn)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.5f;
                canvasGroup.blocksRaycasts = false;
            }
            if (highlightState != null) 
                highlightState.SetActive(false);

            return;
        }

        HighlightHelper.SetManaAvailability(canvasGroup, hasMana);

        bool playableNow = hasMana && isPlayerTurn;

        if (highlightState != null) 
            highlightState.SetActive(playableNow);
        if (canvasGroup != null) 
            canvasGroup.blocksRaycasts = playableNow;
    }
}
