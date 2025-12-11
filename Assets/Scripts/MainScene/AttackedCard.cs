using UnityEngine;
using UnityEngine.EventSystems;

public class AttackedCard : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (!GameManager.Instance.IsMyTurn)
            return;

        CardController attacker = eventData.pointerDrag.GetComponent<CardController>();
        CardController defender = GetComponent<CardController>();

        if (attacker == null || defender == null)
            return;

        if (attacker.self.isPlaced && attacker.self.canAttack && defender.self.isPlaced)
        {
            if (GameManager.Instance.enemyFieldCards.Exists(x => x.self.IsProvocation) && !defender.self.IsProvocation)
                return;

            if (attacker.Network != null && defender.Network != null)
            {
                ulong targetId = defender.Network.NetworkObjectId;

                attacker.Network.RequestAttackServerRpc(targetId);

                attacker.self.canAttack = false;
                attacker.Info.SetHighlight(false);
            }
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