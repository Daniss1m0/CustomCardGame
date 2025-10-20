using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AttackedHero : MonoBehaviour, IDropHandler
{
    public Color normalCol, highlightCol;
    
    public enum HeroType
    {
        Enemy,
        Player
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
        GetComponent<Image>().color = highlight ? highlightCol : normalCol;
    }
}