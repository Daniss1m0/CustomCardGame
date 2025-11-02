using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AttackedHero : MonoBehaviour, IDropHandler
{
    public Color normalCol, highlightCol;
    
    public enum HeroType
    {
        Player,
        Enemy
    }

    public HeroType type;

    public void OnDrop(PointerEventData eventData)
    {
        if (!GameManager.Instance.IsPlayerTurn)
            return;

        CardController card = eventData.pointerDrag.GetComponent<CardController>();

        if (card && card.self.canAttack && type == HeroType.Enemy && !GameManager.Instance.enemyFieldCards.Exists(x => x.self.IsProvocation))
                GameManager.Instance.DamageHero(card, true);
    }

    public void HighlightAsTarget(bool highlight)
    {
        var img = GetComponent<Image>();
        HighlightHelper.SetTargetHighlight(img, highlight, normalCol, highlightCol);
    }
}