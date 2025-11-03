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

        CardController spell = eventData.pointerDrag.GetComponent<CardController>();
        CardController target = GetComponent<CardController>();

        if (spell == null || target == null) 
            return;

        if (!spell.self.isSpell || !spell.isPlayerCard || !target.self.isPlaced) 
            return;

        if (GameManager.Instance.currentGame.player.mana < spell.self.manaCost) 
            return;

        var spellCard = (SpellCard)spell.self;

        if ((spellCard.spellTarget == TargetType.AllyCard && target.isPlayerCard) ||
            (spellCard.spellTarget == TargetType.EnemyCard && !target.isPlayerCard))
        {
            GameManager.Instance.CastSpell(spell, target, true);

            GameManager.Instance.CheckCardsForManaAvailability();
        }
    }
}
