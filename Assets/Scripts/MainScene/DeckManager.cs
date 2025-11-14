using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

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

    public bool GiveInitialHandsNetworked(Game currentGame, bool randomStart = true)
    {
        Shuffle(currentGame.playerDeck);
        Shuffle(currentGame.enemyDeck);

        bool playerStarts = randomStart ? (Random.value < 0.5f) : true;

        int playerCount = playerStarts ? startPlayerHand : startEnemyHand;
        int enemyCount = playerStarts ? startEnemyHand : startPlayerHand;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            DrawCardsNetworked(currentGame.playerDeck, playerHand, /*ownerClientId*/ NetworkManager.Singleton.LocalClientId, playerCount);
            DrawCardsNetworked(currentGame.enemyDeck, enemyHand, /*ownerClientId*/ GetOpponentClientId(), enemyCount);
        }
        else
        {
            DrawCards(currentGame.playerDeck, playerHand, true, playerCount);
            DrawCards(currentGame.enemyDeck, enemyHand, false, enemyCount);
        }

        return playerStarts;
    }

    private void SpawnAndRegisterCardNetworked(Card card, Transform hand, ulong ownerClientId, int cardDataIndex = -1)
    {
        if (cardPrefab == null || hand == null) { Debug.LogError("CardPrefab or hand is not assigned."); return; }
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) { Debug.LogError("SpawnAndRegisterCardNetworked called but this is not the server."); return; }

        GameObject instance = Instantiate(cardPrefab); // no parent
        var netObj = instance.GetComponent<NetworkObject>();
        var cn = instance.GetComponent<CardNetwork>();
        var visual = instance.GetComponentInChildren<CardController>(true);

        if (netObj == null || cn == null || visual == null)
        {
            Debug.LogError("Card prefab must contain NetworkObject, CardNetwork and CardController (in children).");
            Destroy(instance);
            return;
        }

        if (ownerClientId != NetworkManager.ServerClientId && ownerClientId != NetworkManager.Singleton.LocalClientId)
            netObj.SpawnWithOwnership(ownerClientId);
        else
            netObj.Spawn();

        cn.OwnerClientIdNet.Value = ownerClientId;
        cn.CardDataIndex.Value = cardDataIndex;
        cn.Attack.Value = card.attack;
        cn.Health.Value = card.health;
        cn.ManaCost.Value = card.manaCost;
        cn.IsSpell.Value = card.isSpell;

        if (visual != null && hand != null)
        {
            visual.transform.SetParent(hand, false);

            visual.transform.localScale = Vector3.one;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localPosition = Vector3.zero;

            var rt = visual.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);

                if (rt.sizeDelta == Vector2.zero)
                    rt.sizeDelta = new Vector2(175f, 230f);

                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
            }
            else
            {
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
            }

            var handRect = hand.GetComponent<RectTransform>();
            if (handRect != null)
            {
                Canvas.ForceUpdateCanvases();
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(handRect);

                try
                {
                    visual.transform.SetSiblingIndex(Mathf.Clamp(hand.childCount - 1, 0, hand.childCount));
                }
                catch {  }
            }

            visual.gameObject.SetActive(true);
            var cg = visual.GetComponent<CanvasGroup>();
            if (cg != null) { cg.alpha = 1f; cg.blocksRaycasts = true; cg.interactable = true; }

            foreach (var g in visual.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                if (!g.enabled) g.enabled = true;

            var canvas = visual.GetComponentInParent<Canvas>();
            var rt2 = visual.GetComponent<RectTransform>();
            string pos = rt2 != null ? $"anchored:{rt2.anchoredPosition} size:{rt2.sizeDelta}" : $"pos:{visual.transform.position}";
            Debug.Log($"Spawned card '{card.name}' owner:{ownerClientId} visualParent:{visual.transform.parent?.name ?? "null"} {pos} | Canvas:{(canvas ? canvas.name : "null")}");
        }

    }

    private void DrawCardsNetworked(List<Card> deck, Transform hand, ulong ownerClientId, int count = 1)
    {
        if (deck == null || hand == null || count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            if (deck.Count == 0) break;
            var card = deck[0];
            SpawnAndRegisterCardNetworked(card, hand, ownerClientId, /*index*/ GetCardDataIndex(card));
            deck.RemoveAt(0);
        }
    }

    private int GetCardDataIndex(Card card)
    {
        if (card == null || CardDatabase.AllCards == null)
            return -1;

        for (int i = 0; i < CardDatabase.AllCards.Count; i++)
        {
            var entry = CardDatabase.AllCards[i];
            if (entry == null)
                continue;

            if (!string.IsNullOrEmpty(entry.name) && entry.name == card.name)
                return i;
        }

        return -1;
    }

    private ulong GetOpponentClientId()
    {
        if (NetworkManager.Singleton == null)
            return NetworkManager.ServerClientId;

        foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
        {
            ulong clientId = kvp.Key;
            if (clientId != NetworkManager.Singleton.LocalClientId)
                return clientId;
        }

        return NetworkManager.ServerClientId;
    }
}