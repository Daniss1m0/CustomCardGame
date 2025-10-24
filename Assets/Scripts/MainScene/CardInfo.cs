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
    
    private void Awake()
    {
        background = GetComponent<Image>();
    }

    public void ShowCard(Card card)
    {
        hideState.SetActive(false);
        logo.sprite = card.logo;
        logo.preserveAspect = true;
        nameTxt.text = card.name;

        if (card.isSpell)
        {
            attackTxt.gameObject.SetActive(false);
            healthTxt.gameObject.SetActive(false);
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
        attackTxt.text = card.attack.ToString();
        healthTxt.text = card.health.ToString();
        manaCostTxt.text = card.manaCost.ToString();
    }

    public void SetHighlight(bool highlight) 
    {
        highlightState.SetActive(highlight);
    }

    public void HighlightAsTarget(bool active)
    {
        background.color = active ? targetColor : normalColor;
    }

    public void HighlightAsSpellTarget(bool active)
    {
        background.color = active ? spellTargetColor : normalColor;
    }

    public void SetManaAvailability(int currentMana, int cardCost)
    {
        GetComponent<CanvasGroup>().alpha = currentMana >= cardCost ? 1 : 0.5f;
    }
}