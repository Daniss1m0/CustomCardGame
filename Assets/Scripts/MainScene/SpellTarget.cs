using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SpellTarget : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (!GameManagerScr.Instance.IsPlayerTurn)
            return;

        CardController spell = eventData.pointerDrag.GetComponent<CardController>(),
                       target = GetComponent<CardController>();

        if (spell && spell.card.isSpell && spell.isPlayerCard && target.card.isPlaced && GameManagerScr.Instance.currentGame.player.mana >= spell.card.manacost)
        {
            var spellCard = (SpellCard)spell.card;

            if ((spellCard.spellTarget == SpellCard.TargetType.ALLY_CARD_TARGET && target.isPlayerCard) ||
                (spellCard.spellTarget == SpellCard.TargetType.ENEMY_CARD_TARGET && !target.isPlayerCard))
            {
                GameManagerScr.Instance.ReduceMana(true, spell.card.manacost);
                spell.UseSpell(target);
                GameManagerScr.Instance.CheckCardsForManaAvailability();
            }
        }
    }
}