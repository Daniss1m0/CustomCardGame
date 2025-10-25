using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    [SerializeField] private Transform playerHand, enemyHand, playerField, enemyField;
    [SerializeField] private GameObject cardPrefab;

    // Properties to access private fields
    public Transform PlayerHand => playerHand;
    public Transform EnemyHand => enemyHand;
    public Transform PlayerField => playerField;
    public Transform EnemyField => enemyField;
    public GameObject CardPrefab => cardPrefab;

    public void GiveInitialHands(Game currentGame)
    {
        if (currentGame == null) return;
        GiveHandCards(currentGame.playerDeck, playerHand, true);
        GiveHandCards(currentGame.enemyDeck, enemyHand, false);
    }

    public void GiveNewCards(Game currentGame)
    {
        if (currentGame == null) return;
        GiveCardToHand(currentGame.playerDeck, playerHand, true);
        GiveCardToHand(currentGame.enemyDeck, enemyHand, false);
    }

    private void GiveHandCards(List<Card> deck, Transform hand, bool isPlayer)
    {
        for (int i = 0; i < 4; i++)
            GiveCardToHand(deck, hand, isPlayer);
    }

    private void GiveCardToHand(List<Card> deck, Transform hand, bool isPlayer) //too much functions?
    {
        if (deck == null || deck.Count == 0) return;

        CreateCardPrefab(deck[0], hand, isPlayer);
        deck.RemoveAt(0);
    }

    private void CreateCardPrefab(Card card, Transform hand, bool isPlayer)
    {
        if (cardPrefab == null || hand == null)
        {
            Debug.LogError("CardPrefab or hand is not assigned.");
            return;
        }

        GameObject tempCard = Instantiate(cardPrefab, hand, false);
        CardController cardController = tempCard.GetComponent<CardController>();
        if (cardController == null)
        {
            Destroy(tempCard);
            return;
        }

        cardController.Init(card, isPlayer);

        if (GameManager.Instance == null)
            return;

        if (isPlayer)
            GameManager.Instance.playerHandCards.Add(cardController);
        else
            GameManager.Instance.enemyHandCards.Add(cardController);
    }

    private void ClearList(List<CardController> list)
    {
        foreach (var c in new List<CardController>(list))
        {
            if (c != null)
                Destroy(c.gameObject);
        }
    }

    public void ClearAll()
    {
        if (GameManager.Instance == null) return;

        ClearList(GameManager.Instance.playerHandCards);
        ClearList(GameManager.Instance.playerFieldCards);
        ClearList(GameManager.Instance.enemyHandCards);
        ClearList(GameManager.Instance.enemyFieldCards);

        GameManager.Instance.playerHandCards.Clear();
        GameManager.Instance.playerFieldCards.Clear();
        GameManager.Instance.enemyHandCards.Clear();
        GameManager.Instance.enemyFieldCards.Clear();
    }
}
