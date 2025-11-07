using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum FieldType
{
    PlayerHand,
    PlayerField,
    EnemyHand,
    EnemyField
}

public class DropPlace : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public FieldType type;

    public void OnDrop(PointerEventData eventData)
    {
        if (type != FieldType.PlayerField)
            return;

        CardController card = eventData.pointerDrag.GetComponent<CardController>();
        if (card == null) 
            return;

        if (!card.self.isSpell && GameManager.Instance.playerFieldCards.Count >= DeckManager.MAX_FIELD_SIZE)
        {
            Debug.Log("Player field is full.");
            return;
        }

        if (card && GameManager.Instance.IsPlayerTurn && GameManager.Instance.currentGame.player.mana >= card.self.manaCost && !card.self.isPlaced)
        {
            if (!card.self.isSpell)
                card.Movement.defaultParent = transform;

            GameManager.Instance.PlayCard(card, true);
        }
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null || type == FieldType.EnemyField || type == FieldType.EnemyHand || type == FieldType.PlayerHand)
            return;

        CardMovement card = eventData.pointerDrag.GetComponent<CardMovement>();

        if (card)
            card.tempParent = transform;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        CardMovement card = eventData.pointerDrag.GetComponent<CardMovement>();

        if (card && card.tempParent == transform)
            card.tempParent = card.defaultParent;
    }
}