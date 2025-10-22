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


    public void HideCardInfo()
    {
        hideState.SetActive(true);
        manaCostTxt.text = "";
    }

    public void ShowCardInfo()
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

        RefreshData();
    }

    public void RefreshData() 
    {
        attackTxt.text = CC.self.attack.ToString();
        healthTxt.text = CC.self.health.ToString();
        manaCostTxt.text = CC.self.manaCost.ToString();
    }

    public void HighlightCard(bool highlight) 
    {
        highlightState.SetActive(highlight);
    }

    public void HighlightManaAvaliability(int currentMana)
    {
        GetComponent<CanvasGroup>().alpha = currentMana >= CC.self.manaCost ? 1 : 0.5f;
    }

    public void HighlightAsTarget(bool highlight)
    {
        GetComponent<Image>().color = highlight ? targetColor : normalColor;
    }

    public void HighlightAsSpellTarget(bool highlight)
    {
        GetComponent<Image>().color = highlight ? spellTargetColor : normalColor;
    }
}