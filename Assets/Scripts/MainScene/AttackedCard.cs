using UnityEngine;
using UnityEngine.EventSystems;

public class AttackedCard : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (!GameManager.Instance.IsPlayerTurn)
            return;

        CardController attacker = eventData.pointerDrag.GetComponent<CardController>();
        CardController defender = GetComponent<CardController>();

        if (attacker == null) 
            return;

        if (attacker.self.isPlaced && attacker.self.canAttack && defender.self.isPlaced)
        {
            if (GameManager.Instance.enemyFieldCards.Exists(x => x.self.IsProvocation) && !defender.self.IsProvocation)
                return;

            GameManager.Instance.CardsFight(attacker, defender);
            return;
        }

        if (!attacker.self.isPlaced && !attacker.self.isSpell)
        {
            var dropPlace = GetComponentInParent<DropPlace>();
            if (dropPlace != null)
                dropPlace.OnDrop(eventData);
        }
    }
}