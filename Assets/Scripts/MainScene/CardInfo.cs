using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardInfo : MonoBehaviour
{
    public CardController CC; //?

    [SerializeField] private Color normalColor, targetColor, spellTargetColor;
    [SerializeField] private TextMeshProUGUI nameTxt, attackTxt, healthTxt, manaCostTxt;
    [SerializeField] private Image logo;
    [SerializeField] private GameObject hideState, highlightState;

    private Image background;
    
    private void Awake()
    {
        background = GetComponent<Image>();
    }

    public void ShowCard()
    {
        hideState.SetActive(false);
        logo.sprite = CC.self.logo;
        logo.preserveAspect = true;
        nameTxt.text = CC.self.name;

        if (CC.self.isSpell)
        {
            attackTxt.gameObject.SetActive(false);
            healthTxt.gameObject.SetActive(false);
        }

        UpdateStats();
    }

    public void HideCard()
    {
        hideState.SetActive(true);
        nameTxt.text = "";
        attackTxt.text = "";
        healthTxt.text = "";
        manaCostTxt.text = "";
    }

    public void UpdateStats() 
    {
        attackTxt.text = CC.self.attack.ToString();
        healthTxt.text = CC.self.health.ToString();
        manaCostTxt.text = CC.self.manaCost.ToString();
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

    public void SetManaAvailability(int currentMana)
    {
        GetComponent<CanvasGroup>().alpha = currentMana >= CC.self.manaCost ? 1 : 0.5f;
    }
}