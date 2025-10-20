using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AttackedCard : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (!GameManager.Instance.IsPlayerTurn)
            return;
       
        CardController attacker = eventData.pointerDrag.GetComponent<CardController>(), defender = GetComponent<CardController>();

        if (attacker && attacker.self.canAttack && defender.self.isPlaced)
        {
            if (GameManager.Instance.enemyFieldCards.Exists(x => x.self.IsProvocation) && !defender.self.IsProvocation)
                return;

            GameManager.Instance.CardsFight(attacker, defender);
        }
    }
}