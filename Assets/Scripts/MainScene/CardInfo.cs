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
        manacostTxt.text = CC.self.manacost.ToString();
    }

    public void HighlightCard(bool highlight) 
    {
        highlightedObj.SetActive(highlight);
    }

    public void HighlightManaAvaliability(int currentMana)
    {
        GetComponent<CanvasGroup>().alpha = currentMana >= CC.self.manacost ? 1 : 0.5f;
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