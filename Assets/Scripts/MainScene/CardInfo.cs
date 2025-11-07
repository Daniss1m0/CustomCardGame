using System.Collections;
using System.Collections.Generic;
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

    private void Awake()
    {
        background = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void ShowCard(Card card)
    {
        hideState.SetActive(false);
        
        if (logo != null)
        {
            logo.sprite = card.logo;
            logo.preserveAspect = true;
        }

        nameTxt.text = card.name;

        if (card.isSpell)
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
        hideState.SetActive(true);
        nameTxt.text = "";
        attackTxt.text = "";
        healthTxt.text = "";
        manaCostTxt.text = "";
    }

    public void UpdateStats(Card card)
    {
        if (attackTxt != null) 
            attackTxt.text = card.attack.ToString();
        if (healthTxt != null) 
            healthTxt.text = card.health.ToString();
        if (manaCostTxt != null) 
            manaCostTxt.text = card.manaCost.ToString();
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
