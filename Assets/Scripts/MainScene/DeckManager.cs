using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static readonly int MAX_HAND_SIZE = 10;
    public static readonly int MAX_FIELD_SIZE = 7;

    [SerializeField] private int startPlayerHand = 3, startEnemyHand = 4;
    [SerializeField] private Transform playerHand, enemyHand, playerField, enemyField;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private CardData coinCard;

    public Transform EnemyField => enemyField;

    private void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        for (int i = n - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public bool GiveInitialHands(Game currentGame, bool randomStart = true)
    {
        if (currentGame == null)
            return true;

        Shuffle(currentGame.playerDeck);
        Shuffle(currentGame.enemyDeck);

        bool playerStarts = randomStart ? (Random.value < 0.5f) : true;

        int playerCount = playerStarts ? startPlayerHand : startEnemyHand;
        int enemyCount = playerStarts ? startEnemyHand : startPlayerHand;

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
    /*
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
    */
    private void SpawnAndRegisterCard(Card card, Transform hand, bool isPlayer)
    {
        if (cardPrefab == null || hand == null)
        {
            Debug.LogError("CardPrefab or hand is not assigned.");
            return;
        }

        var gm = GameManager.Instance;
        if (gm == null)
            return;

        var handList = isPlayer ? gm.playerHandCards : gm.enemyHandCards;
        if (handList.Count >= MAX_HAND_SIZE)
        {
            Debug.Log($"{(isPlayer ? "Player" : "Enemy")} hand is full. Burning drawn card: {card.name}");
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