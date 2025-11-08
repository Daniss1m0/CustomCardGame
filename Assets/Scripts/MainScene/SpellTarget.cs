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

        var dragObj = eventData.pointerDrag;
        if (dragObj == null) 
            return;

        var spell = dragObj.GetComponent<CardController>();
        var target = GetComponent<CardController>();
        if (spell == null || !spell.self.isSpell || !spell.isPlayerCard) 
            return;

        var spellCard = spell.self as SpellCard;
        if (spellCard == null) 
            return;

        if (spellCard.spellTarget == TargetType.None)
        {
            if (GameManager.Instance.currentGame.player.mana < spell.self.manaCost) 
                return;

            spell.Info?.ShowCard(spell.self);
            spell.Movement?.MoveToField(GameManager.Instance.PlayerHero != null ? GameManager.Instance.PlayerHero.transform.parent : transform);
            GameManager.Instance.PlayCard(spell, true);
            return;
        }

        if (target == null || !target.self.isPlaced) 
            return;

        if ((spellCard.spellTarget == TargetType.AllyCard && target.isPlayerCard) || 
            (spellCard.spellTarget == TargetType.EnemyCard && !target.isPlayerCard))
            GameManager.Instance.CastSpell(spell, target, true);
    }
}
