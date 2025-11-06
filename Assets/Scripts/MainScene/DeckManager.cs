using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    [SerializeField] private int initialPlayerHand = 3;//mb dont need
    [SerializeField] private int initialEnemyHand = 4;
    [SerializeField] private Transform playerHand, enemyHand, playerField, enemyField;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private CardData coinCard;

    // Properties to access private fields
    public Transform PlayerHand => playerHand;
    public Transform EnemyHand => enemyHand;
    public Transform PlayerField => playerField;
    public Transform EnemyField => enemyField;
    public GameObject CardPrefab => cardPrefab;

    public bool GiveInitialHands(Game currentGame, bool randomStart = true)
    {
        if (currentGame == null)
            return true;

        bool playerStarts = randomStart ? (Random.value < 0.5f) : true;

        int playerCount = playerStarts ? initialPlayerHand : initialEnemyHand;
        int enemyCount = playerStarts ? initialEnemyHand : initialPlayerHand;

        DrawCards(currentGame.playerDeck, playerHand, true, playerCount);
        DrawCards(currentGame.enemyDeck, enemyHand, false, enemyCount);

        if (coinCard != null && coinCard.isSpell)
        {
            var coin = new SpellCard(coinCard);
            if (coin.spell == SpellType.GiveTempMana)
            {
                if (playerStarts)
                    SpawnAndRegisterCard(coin, enemyHand, false);
                else
                    SpawnAndRegisterCard(coin, playerHand, true);
            }
        }
        else if (coinCard == null)
            Debug.LogWarning("ÑoinCard not assigned.");

        return playerStarts;
    }

    public void GiveNewCards(Game currentGame)
    {
        if (currentGame == null) 
            return;

        DrawCards(currentGame.playerDeck, playerHand, true, 1);
        DrawCards(currentGame.enemyDeck, enemyHand, false, 1);
    }

    private void DrawCards(List<Card> deck, Transform hand, bool isPlayer, int count = 1)
    {
        if (deck == null || hand == null || count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            if (deck.Count == 0) 
                break;
            var card = deck[0];
            SpawnAndRegisterCard(card, hand, isPlayer);
            deck.RemoveAt(0);
        }
    }

    private void SpawnAndRegisterCard(Card card, Transform hand, bool isPlayer)
    {
        if (cardPrefab == null || hand == null)
        {
            Debug.LogError("CardPrefab or hand is not assigned.");
            return;
        }

        GameObject instance = Instantiate(cardPrefab, hand, false);
        var controller = instance.GetComponent<CardController>();
        if (controller == null)
        {
            Destroy(instance);
            return;
        }

        controller.Init(card, isPlayer);

        var gm = GameManager.Instance;
        if (gm == null) return;

        if (isPlayer)
            gm.playerHandCards.Add(controller);
        else
            gm.enemyHandCards.Add(controller);
    }

    private void ClearList(List<CardController> list)
    {
        if (list == null || list.Count == 0) return;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var c = list[i];
            if (c != null)
                Destroy(c.gameObject);
        }
        list.Clear();
    }

    public void ClearAll()
    {
        var gm = GameManager.Instance;
        if (gm == null) 
            return;

        ClearList(gm.playerHandCards);
        ClearList(gm.playerFieldCards);
        ClearList(gm.enemyHandCards);
        ClearList(gm.enemyFieldCards);
    }
}