using System.Collections;
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
    private const float TEMP_PARENT_DELAY = 0.06f;

    public FieldType type;

    private Coroutine setTempCoroutine;
    private GameObject pendingDragObj;

    public void OnDrop(PointerEventData eventData)
    {
        var dragObj = eventData.pointerDrag;
        if (dragObj == null)
            return;

        var card = dragObj.GetComponent<CardController>();
        if (card == null) 
            return;

        GameManager.Instance.SanitizeLists();

        if (card.self.isSpell)
        {
            var spell = card.self as SpellCard;
            if (spell != null && spell.spellTarget == TargetType.None)
            {
                if (!GameManager.Instance.IsMyTurn || !card.isPlayerCard)
                    return;

                if (GameManager.Instance.currentGame.player.mana < card.self.manaCost)
                    return;

                if (GameManager.Instance.PlayCard(card, true, -1))
                    card.Movement.MoveToField(transform);
                
                return;
            }
            return;
        }

        int dropIndex = -1;
        bool foundTemp = false;
        foreach (Transform child in transform)
            if (child.name == "CardTemp")
            {
                dropIndex = child.GetSiblingIndex();
                foundTemp = true;
                break;
            }

        if (!foundTemp)
            dropIndex = transform.childCount;

        if (card && GameManager.Instance.IsMyTurn && GameManager.Instance.currentGame.player.mana >= card.self.manaCost && !card.self.isPlaced)
        {
            bool success = GameManager.Instance.PlayCard(card, true, dropIndex);

            if (success)
                if (!card.self.isSpell && card.Movement != null)
                    card.Movement.defaultParent = transform;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null || type == FieldType.EnemyField || type == FieldType.EnemyHand || type == FieldType.PlayerHand)
            return;

        CardMovement cardMovement = eventData.pointerDrag.GetComponent<CardMovement>();
        CardController cardController = eventData.pointerDrag.GetComponent<CardController>();

        if (cardMovement == null)
            return;

        if (type == FieldType.PlayerField && cardController != null && !cardController.self.isSpell)
            if (GameManager.Instance.playerFieldCards.Count >= DeckManager.MAX_FIELD_SIZE)
                return;

        if (cardMovement.tempParent == transform)
            return;

        if (setTempCoroutine != null)
        {
            StopCoroutine(setTempCoroutine);
            setTempCoroutine = null;
            pendingDragObj = null;
        }

        pendingDragObj = eventData.pointerDrag;
        setTempCoroutine = StartCoroutine(DelayedSetTempParent(cardMovement, pendingDragObj));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) 
            return;

        CardMovement card = eventData.pointerDrag.GetComponent<CardMovement>();

        if (setTempCoroutine != null && pendingDragObj == eventData.pointerDrag)
        {
            StopCoroutine(setTempCoroutine);
            setTempCoroutine = null;
            pendingDragObj = null;
        }

        if (card && card.tempParent == transform)
            card.tempParent = card.defaultParent;
    }

    private IEnumerator DelayedSetTempParent(CardMovement cardMovement, GameObject dragObj)
    {
        yield return new WaitForSecondsRealtime(TEMP_PARENT_DELAY);

        if (dragObj == null || cardMovement == null)
        {
            setTempCoroutine = null;
            pendingDragObj = null;
            yield break;
        }

        if (cardMovement.tempParent == transform)
        {
            setTempCoroutine = null;
            pendingDragObj = null;
            yield break;
        }

        cardMovement.tempParent = transform;
        setTempCoroutine = null;
        pendingDragObj = null;
    }
}