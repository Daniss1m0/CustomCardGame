using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardInfo : MonoBehaviour
{
    public Color normalCol, targetCol, spellTargetCol;
    public GameObject hideObj, highlightedObj;
    public TextMeshProUGUI nameTxt, attackTxt, healthTxt, manacostTxt;
    public Image logo;
    public CardController CC;

    public void HideCardInfo()
    {
        hideObj.SetActive(true);
        manacostTxt.text = "";
    }

    public void ShowCardInfo()
    {
        hideObj.SetActive(false);

        logo.sprite = CC.card.logo;
        logo.preserveAspect = true;
        nameTxt.text = CC.card.name;

        if (CC.card.isSpell)
        {
            attackTxt.gameObject.SetActive(false);
            healthTxt.gameObject.SetActive(false);
        }

        RefreshData();
    }

    public void RefreshData() 
    {
        attackTxt.text = CC.card.attack.ToString();
        healthTxt.text = CC.card.health.ToString();
        manacostTxt.text = CC.card.manacost.ToString();
    }

    public void HighlightCard(bool highlight) 
    {
        highlightedObj.SetActive(highlight);
    }

    public void HighlightManaAvaliability(int currentMana)
    {
        GetComponent<CanvasGroup>().alpha = currentMana >= CC.card.manacost ? 1 : 0.5f;
    }

    public void HighlightAsTarget(bool highlight)
    {
        GetComponent<Image>().color = highlight ? targetCol : normalCol;
    }

    public void HighlightAsSpellTarget(bool highlight)
    {
        GetComponent<Image>().color = highlight ? spellTargetCol : normalCol;
    }
}