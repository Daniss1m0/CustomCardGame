using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Netcode;

public class AttackedCard : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (!GameManager.Instance.IsMyTurn)
            return;

        CardController attacker = eventData.pointerDrag.GetComponent<CardController>();
        CardController defender = GetComponent<CardController>();

        if (attacker == null)
            return;

        if (attacker.self.isPlaced && attacker.self.canAttack && defender.self.isPlaced)
        {
            if (GameManager.Instance.enemyFieldCards.Exists(x => x.self.IsProvocation) && !defender.self.IsProvocation)
                return;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                if (attacker.Network != null && defender.Network != null)
                {
                    ulong targetId = defender.Network.NetworkObjectId;

                    attacker.Network.RequestAttackServerRpc(targetId);

                    attacker.self.canAttack = false;
                    attacker.Info.SetHighlight(false);
                }
            }
            else
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