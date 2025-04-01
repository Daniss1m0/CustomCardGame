using UnityEngine;
using UnityEngine.EventSystems;

public class AttackedHero : MonoBehaviour, IDropHandler
{
    public enum HeroType
    {
        ENEMY,
        PLAYER
    }

    public HeroType Type;
    public GameManagerScr GameManager;

    public void OnDrop(PointerEventData eventData)
    {
        if (!GameManager.IsPlayerTurn)
            return;

        CardInfoScr card = eventData.pointerDrag.GetComponent<CardInfoScr>();

        if (card && card.SelfCard.CanAttack && Type == HeroType.ENEMY)
        {
                card.SelfCard.CanAttack = false;
                GameManager.DamageHero(card, true);
        }
    }
}