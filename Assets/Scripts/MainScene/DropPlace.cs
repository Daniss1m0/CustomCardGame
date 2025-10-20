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

        if (card && GameManager.Instance.IsPlayerTurn && GameManager.Instance.currentGame.player.mana >= card.self.manaCost && !card.self.isPlaced)
        {
            if (!card.self.isSpell)
                card.movement.defaultParent = transform;

            card.OnCast();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null || type == FieldType.EnemyField || type == FieldType.EnemyHand || type == FieldType.PlayerHand)
            return;

        CardMovement card = eventData.pointerDrag.GetComponent<CardMovement>();

        if (card)
            card.defaultTempCardParent = transform;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        CardMovement card = eventData.pointerDrag.GetComponent<CardMovement>();

        if (card && card.defaultTempCardParent == transform)
            card.defaultTempCardParent = card.defaultParent;
    }
}