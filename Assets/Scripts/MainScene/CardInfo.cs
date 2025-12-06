using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardInfo : MonoBehaviour
{
    [SerializeField] private Color normalColor, targetColor, spellTargetColor;
    [SerializeField] private TextMeshProUGUI nameTxt, attackTxt, healthTxt, manaCostTxt, descriptionTxt;
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
        UpdateDescription(card);
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

    public void UpdateDescription(Card card)
    {
        if (descriptionTxt == null || card == null)
            return;

        string finalText = "";

        List<string> keywords = new List<string>();
        if (card.abilities != null)
        {
            foreach (var ab in card.abilities)
            {
                if (ab == AbilityType.None)
                    continue;

                string niceName = Regex.Replace(ab.ToString(), "(\\B[A-Z])", " $1");
                keywords.Add(niceName);
            }
        }

        if (keywords.Count > 0)
        {
            finalText += "<b>" + string.Join(", ", keywords) + "</b>";
            if (!string.IsNullOrEmpty(card.description))
                finalText += "\n";
        }

        if (!string.IsNullOrEmpty(card.description))
        {
            string rawDescription = card.description;

            if (card is SpellCard spellCard)
            {
                try
                {
                    rawDescription = string.Format(rawDescription, spellCard.spellPower);
                }
                catch (System.FormatException)
                {
                    Debug.LogWarning($"Description format error.");
                }
            }

            finalText += rawDescription;
        }

        descriptionTxt.text = finalText;
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
