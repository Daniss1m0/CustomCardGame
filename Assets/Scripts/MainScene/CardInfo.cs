using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardInfo : MonoBehaviour
{
    public CardController CC;

    public Image Logo;
    public TextMeshProUGUI Name, Attack, Health, Manacost;
    public GameObject HideObj, HighlightedObj;
    public Color NormalCol, TargetCol, SpellTargetCol;

    public void HideCardInfo()
    {
        HideObj.SetActive(true);   
        Manacost.text = "";
    }

    public void ShowCardInfo()
    {
        HideObj.SetActive(false);

        Logo.sprite = CC.card.logo;
        Logo.preserveAspect = true;
        Name.text = CC.card.name;

        if (CC.card.isSpell)
        {
            Attack.gameObject.SetActive(false);
            Health.gameObject.SetActive(false);
        }

        RefreshData();
    }

    public void RefreshData() 
    {
        Attack.text = CC.card.attack.ToString();
        Health.text = CC.card.health.ToString();
        Manacost.text = CC.card.manacost.ToString();
    }

    public void HighlightCard(bool highlight) 
    {
        HighlightedObj.SetActive(highlight);
    }

    public void HighlightManaAvaliability(int currentMana)
    {
        GetComponent<CanvasGroup>().alpha = currentMana >= CC.card.manacost ? 1 : 0.5f;
    }

    public void HighlightAsTarget(bool highlight)
    {
        GetComponent<Image>().color = highlight ? TargetCol : NormalCol;
    }


    public void HighlightAsSpellTarget(bool highlight)
    {
        GetComponent<Image>().color = highlight ? SpellTargetCol : NormalCol;
    }
}