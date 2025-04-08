using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardInfoScr : MonoBehaviour
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

        Logo.sprite = CC.Card.Logo;
        Logo.preserveAspect = true;
        Name.text = CC.Card.Name;

        if (CC.Card.IsSpell)
        {
            Attack.gameObject.SetActive(false);
            Health.gameObject.SetActive(false);
        }

        RefreshData();
    }

    public void RefreshData() 
    {
        Attack.text = CC.Card.Attack.ToString();
        Health.text = CC.Card.Health.ToString();
        Manacost.text = CC.Card.Manacost.ToString();
    }

    public void HighlightCard(bool highlight) 
    {
        HighlightedObj.SetActive(highlight);
    }

    public void HighlightManaAvaliability(int currentMana)
    {
        GetComponent<CanvasGroup>().alpha = currentMana >= CC.Card.Manacost ?
            1 :
            0.5f;
    }

    public void HighlightAsTarget(bool highlight)
    {
        GetComponent<Image>().color = highlight ?
            TargetCol :
            NormalCol;
    }


    public void HighlightAsSpellTarget(bool highlight)
    {
        GetComponent<Image>().color = highlight ?
            SpellTargetCol :
            NormalCol;
    }
}