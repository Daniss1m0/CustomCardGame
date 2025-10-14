using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AttackedHero : MonoBehaviour, IDropHandler
{
    public Color normalCol, targetCol;
    
    public enum HeroType
    {
        ENEMY,
        PLAYER
    }

    public HeroType type;

    public void OnDrop(PointerEventData eventData)
    {
        if (!GameManager.Instance.IsPlayerTurn)
            return;

        CardController card = eventData.pointerDrag.GetComponent<CardController>();

        if (card && card.card.canAttack && type == HeroType.ENEMY && !GameManager.Instance.enemyFieldCards.Exists(x => x.card.IsProvocation))
                GameManager.Instance.DamageHero(card, true);
    }

    public void HighlightAsTarget(bool highlight)
    {
        GetComponent<Image>().color = highlight ? targetCol : normalCol;
    }
}