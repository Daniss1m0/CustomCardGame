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
        if (type == FieldType.EnemyField || type == FieldType.EnemyHand)
            return;

        if (type != FieldType.PlayerField)
            return;

        var dragObj = eventData.pointerDrag;
        if (dragObj == null) 
            return;

        var card = dragObj.GetComponent<CardController>();
        if (card == null) 
            return;

        if (card.self.isSpell)
        {
            var spell = card.self as SpellCard;
            if (spell != null && spell.spellTarget == TargetType.None)
            {
                if (!GameManager.Instance.IsPlayerTurn || !card.isPlayerCard) 
                    return;

                if (GameManager.Instance.currentGame.player.mana < card.self.manaCost) 
                    return;

                card.Movement.MoveToField(transform);
                GameManager.Instance.PlayCard(card, true);
                return;
            }
        }

        if (!card.self.isSpell)
        {
            var gm = GameManager.Instance;
            if (type == FieldType.PlayerField && gm.playerFieldCards.Count >= DeckManager.MAX_FIELD_SIZE)
            {
                Debug.Log("Player field is full.");
                return;
            }
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
        if (eventData.pointerDrag == null) return;

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