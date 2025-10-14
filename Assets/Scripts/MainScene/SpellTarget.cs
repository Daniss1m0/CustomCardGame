using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SpellTarget : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (!GameManager.Instance.IsPlayerTurn)
            return;

        CardController spell = eventData.pointerDrag.GetComponent<CardController>(),
                       target = GetComponent<CardController>();

        if (spell && spell.self.isSpell && spell.isPlayerCard && target.self.isPlaced && GameManager.Instance.currentGame.player.mana >= spell.self.manacost)
        {
            var spellCard = (SpellCard)spell.self;

            if ((spellCard.spellTarget == SpellCard.TargetType.ALLY_CARD_TARGET && target.isPlayerCard) ||
                (spellCard.spellTarget == SpellCard.TargetType.ENEMY_CARD_TARGET && !target.isPlayerCard))
            {
                GameManager.Instance.ReduceMana(true, spell.self.manacost);
                spell.UseSpell(target);
                GameManager.Instance.CheckCardsForManaAvailability();
            }
        }
    }
}