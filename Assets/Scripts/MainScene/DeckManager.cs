using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static readonly int MAX_HAND_SIZE = 10;
    public static readonly int MAX_FIELD_SIZE = 7;

    [SerializeField] private int startPlayerHand = 3, startEnemyHand = 4;
    [SerializeField] private Transform playerHand, enemyHand, playerField, enemyField, networkCardRoot;

    [Header("Prefabs")]
    [SerializeField] private GameObject networkCardPrefab;
    [SerializeField] private GameObject visualCardPrefab;

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
        if (visualCardPrefab == null || hand == null)
        {
            Debug.LogError("visualCardPrefab or hand is not assigned.");
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

        GameObject instance = Instantiate(visualCardPrefab, hand, false);
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
        if (networkCardPrefab == null)
        {
            Debug.LogError("[SpawnAndRegisterCardNetworked] networkCardPrefab is not assigned.");
            return;
        }
        if (visualCardPrefab == null)
        {
            Debug.LogError("[SpawnAndRegisterCardNetworked] visualCardPrefab is not assigned.");
            return;
        }
        if (hand == null)
        {
            Debug.LogWarning("[SpawnAndRegisterCardNetworked] hand transform is null for card: " + (card != null ? card.name : "null"));
            return;
        }

        GameObject netInstance = Instantiate(networkCardPrefab);
        var netObj = netInstance.GetComponent<NetworkObject>();
        var cn = netInstance.GetComponent<CardNetwork>();
        var innerVisualOnNet = netInstance.GetComponentInChildren<CardController>(true);

        if (netObj == null || cn == null)
        {
            Debug.LogError("[SpawnAndRegisterCardNetworked] networkCardPrefab must contain NetworkObject and CardNetwork.");
            Destroy(netInstance);
            return;
        }

        bool spawned = false;
        try
        {
            if (NetworkManager.Singleton != null)
            {
                if (ownerClientId != NetworkManager.ServerClientId && ownerClientId != NetworkManager.Singleton.LocalClientId)
                    netObj.SpawnWithOwnership(ownerClientId);
                else
                    netObj.Spawn();

                spawned = true;
            }
            else
            {
                Debug.Log("[SpawnAndRegisterCardNetworked] NetworkManager not present — running offline/spawn without network.");
                spawned = true;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[SpawnAndRegisterCardNetworked] Spawn failed: " + ex);
            spawned = false;
        }

        Transform root = networkCardRoot;
        if (root == null)
        {
            var found = GameObject.Find("Network Card Root");
            if (found != null) root = found.transform;
        }

        if (spawned && root != null)
        {
            try
            {
                netObj.transform.SetParent(root, false);
                netObj.transform.localScale = Vector3.one;
                netObj.transform.localPosition = Vector3.zero;
                netObj.transform.localRotation = Quaternion.identity;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[SpawnAndRegisterCardNetworked] Failed to set parent after spawn: " + ex);
            }
        }
        else if (spawned)
            Debug.Log("[SpawnAndRegisterCardNetworked] networkCardRoot not assigned/found - leaving network instance at scene root.");

        try
        {
            cn.OwnerClientIdNet.Value = ownerClientId;
            cn.CardDataIndex.Value = cardDataIndex;
            cn.Attack.Value = card.attack;
            cn.Health.Value = card.health;
            cn.ManaCost.Value = card.manaCost;
            cn.IsSpell.Value = card.isSpell;
            cn.IsPlaced.Value = false;
            cn.CanAttack.Value = false;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[SpawnAndRegisterCardNetworked] Failed to set NetworkVariables: " + ex);
        }

        if (innerVisualOnNet != null)
            innerVisualOnNet.gameObject.SetActive(false);

        GameObject uiClone = null;
        try
        {
            uiClone = Instantiate(visualCardPrefab, hand, false);
            uiClone.SetActive(false);
            StartCoroutine(FinishLocalCloneRoutine(uiClone, hand, card, cardDataIndex, ownerClientId, netInstance, innerVisualOnNet, cn));
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[SpawnAndRegisterCardNetworked] Failed to create uiClone: " + ex);
            if (uiClone != null) Destroy(uiClone);
        }
    }

    private IEnumerator FinishLocalCloneRoutine(GameObject uiClone, Transform hand, Card card, int cardDataIndex, ulong ownerClientId, GameObject netInstance, CardController innerVisualOnNet, CardNetwork cn)
    {
        yield return null;
        if (uiClone == null) yield break;

        try
        {
            uiClone.SetActive(true);

            uiClone.transform.SetParent(hand, false);
            uiClone.transform.localScale = Vector3.one;
            uiClone.transform.localRotation = Quaternion.identity;
            uiClone.transform.localPosition = Vector3.zero;

            var rtRoot = uiClone.GetComponent<RectTransform>();
            if (rtRoot != null)
            {
                rtRoot.pivot = new Vector2(0.5f, 0.5f);
                rtRoot.anchorMin = new Vector2(0.5f, 0.5f);
                rtRoot.anchorMax = new Vector2(0.5f, 0.5f);
                if (rtRoot.sizeDelta == Vector2.zero) rtRoot.sizeDelta = new Vector2(176f, 230f);
                rtRoot.anchoredPosition = Vector2.zero;
                rtRoot.localScale = Vector3.one;
            }

            var cloneController = uiClone.GetComponent<CardController>() ?? uiClone.GetComponentInChildren<CardController>();
            bool isOwner = NetworkManager.Singleton != null && ownerClientId == NetworkManager.Singleton.LocalClientId;

            if (cloneController != null)
            {
                cloneController.Init(card, isOwner);

                cloneController.SetNetworkData(card.attack, card.health, card.manaCost, card.isSpell, cardDataIndex, ownerClientId);

                cloneController.LinkNetwork(cn);

                var cloneMove = uiClone.GetComponentInChildren<CardMovement>(true);
                if (cloneMove != null)
                {
                    cloneController.SetMovement(cloneMove);

                    cloneMove.defaultParent = hand;
                    cloneMove.tempParent = hand;
                }

                var rootGraphic = uiClone.GetComponent<UnityEngine.UI.Graphic>();
                if (rootGraphic == null)
                {
                    var img = uiClone.AddComponent<UnityEngine.UI.Image>();
                    img.color = new Color(0f, 0f, 0f, 0f);
                    img.raycastTarget = true;
                }
                else
                {
                    rootGraphic.raycastTarget = true;
                }

                var canvasGroup = uiClone.GetComponent<CanvasGroup>() ?? uiClone.AddComponent<CanvasGroup>();
                canvasGroup.blocksRaycasts = isOwner;
                canvasGroup.interactable = isOwner;
            }

            var handRect = hand.GetComponent<RectTransform>();
            if (handRect != null)
            {
                Canvas.ForceUpdateCanvases();
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(handRect);
                try { uiClone.transform.SetSiblingIndex(Mathf.Clamp(hand.childCount - 1, 0, hand.childCount)); } catch { }
            }

            var gm = GameManager.Instance;
            if (gm != null && cloneController != null)
            {
                if (isOwner)
                    gm.playerHandCards.Add(cloneController);
                else
                    gm.enemyHandCards.Add(cloneController);

                try
                {
                    gm.CheckCardsForManaAvailability();
                    UIManager.Instance?.UpdateHPAndMana();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("[FinishLocalCloneRoutine] Failed to refresh availability UI: " + ex);
                }
            }


            if (innerVisualOnNet != null)
                innerVisualOnNet.gameObject.SetActive(false);

            if (cn != null && cloneController != null)
            {
                cn.Attack.OnValueChanged += (oldV, newV) =>
                {
                    if (cloneController == null) return;
                    if (cloneController.self != null) cloneController.self.attack = newV;
                    cloneController.Info?.UpdateStats(cloneController.self);
                };

                cn.Health.OnValueChanged += (oldV, newV) =>
                {
                    if (cloneController == null) return;
                    if (cloneController.self != null) cloneController.self.health = newV;
                    cloneController.Info?.UpdateStats(cloneController.self);
                };

                cn.ManaCost.OnValueChanged += (oldV, newV) =>
                {
                    if (cloneController == null) return;
                    if (cloneController.self != null) cloneController.self.manaCost = newV;
                    cloneController.Info?.UpdateStats(cloneController.self);
                };

                cn.CanAttack.OnValueChanged += (oldV, newV) =>
                {
                    if (cloneController == null) return;
                    cloneController.SetCanAttackVisual(newV);
                };

                cn.OwnerClientIdNet.OnValueChanged += (oldV, newV) =>
                {
                    if (cloneController == null) return;
                    bool nowOwner = NetworkManager.Singleton != null && newV == NetworkManager.Singleton.LocalClientId;
                    cloneController.isPlayerCard = nowOwner;
                    cloneController.OnNetworkOwnershipChanged(nowOwner);

                    if (!cloneController.self.isPlaced)
                    {
                        if (nowOwner) cloneController.Info?.ShowCard(cloneController.self);
                        else cloneController.Info?.HideCard();
                    }
                    else
                    {
                        cloneController.Info?.ShowCard(cloneController.self);
                    }
                };

                cn.IsPlaced.OnValueChanged += (oldV, newV) =>
                {
                    if (cloneController == null) return;
                    cloneController.self.isPlaced = newV;
                    cloneController.Info?.ShowCard(cloneController.self);
                };
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[FinishLocalCloneRoutine] Error finishing uiClone setup: " + ex);
            if (uiClone != null)
                Destroy(uiClone);
        }
    }


    private void DrawCardsNetworked(List<Card> deck, Transform hand, ulong ownerClientId, int count = 1)
    {
        if (deck == null || hand == null || count <= 0)
            return;

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