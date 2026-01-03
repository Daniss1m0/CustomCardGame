using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class DeckManager : MonoBehaviour
{
    public static readonly int MAX_HAND_SIZE = 10, MAX_FIELD_SIZE = 7;
    [SerializeField] private int startPlayerHand = 3, startEnemyHand = 4;
    [SerializeField] private Transform playerHand, enemyHand, playerField, enemyField, networkCardRoot;
    [SerializeField] private GameObject networkCardPrefab, visualCardPrefab;
    [SerializeField] private TextMeshProUGUI playerDeckText, enemyDeckText;

    [System.Serializable]
    public class SpecialCardEntry
    {
        public string id;
        public CardData cardData;
    }

    [SerializeField] private List<SpecialCardEntry> specialCards = new();

    public Transform PlayerField => playerField;
    public Transform EnemyField => enemyField;
    public Transform PlayerHand => playerHand;
    public Transform EnemyHand => enemyHand;
    public GameObject VisualCardPrefab => visualCardPrefab;

    private void Shuffle<T>(List<T> list) { int n = list.Count; for (int i = n - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (list[i], list[j]) = (list[j], list[i]); } }
    private int GetCardDataIndex(Card card) { if (card == null || CardDatabase.AllCards == null) return -1; for (int i = 0; i < CardDatabase.AllCards.Count; i++) { object entryObj = CardDatabase.AllCards[i]; if (entryObj == null) continue; CardData cd = entryObj as CardData; if (cd != null) { if (!string.IsNullOrEmpty(cd.cardName) && cd.cardName == card.name) return i; continue; } if (entryObj is Card existingCard) if (!string.IsNullOrEmpty(existingCard.name) && existingCard.name == card.name) return i; } return -1; }
    private void DrawCards(List<Card> deck, Transform hand, ulong ownerClientId, int count = 1)
    {
        if (deck == null || hand == null || count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            if (deck.Count == 0) break;
            var card = deck[0];

            int currentHandCount = hand.childCount;
            var gm = GameManager.Instance;
            if (gm != null)
            {
                if (hand == playerHand) currentHandCount = gm.playerHandCards != null ? gm.playerHandCards.Count : hand.childCount;
                else if (hand == enemyHand) currentHandCount = gm.enemyHandCards != null ? gm.enemyHandCards.Count : hand.childCount;
            }

            if (currentHandCount >= MAX_HAND_SIZE)
            {
                deck.RemoveAt(0);
                continue;
            }

            SpawnAndRegisterCard(card, hand, ownerClientId, GetCardDataIndex(card));
            deck.RemoveAt(0);
        }

        if (NetworkManager.Singleton.IsServer && GameManager.Instance != null && GameManager.Instance.currentGame != null)
        {
            var tm = FindFirstObjectByType<TurnManager>();
            if (tm != null)
            {
                tm.SetDeckCounts(
                    GameManager.Instance.currentGame.playerDeck.Count,
                    GameManager.Instance.currentGame.enemyDeck.Count
                );
            }
        }
    }
    public void GiveNewCards(Game currentGame, ulong newTurnOwnerClientId) { if (currentGame == null) return; if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return; ulong playerOwnerClientId = NetworkManager.ServerClientId; var tm = FindFirstObjectByType<TurnManager>(); if (tm != null && tm.PlayerOwner.Value != 0UL) playerOwnerClientId = tm.PlayerOwner.Value; if (newTurnOwnerClientId == playerOwnerClientId) DrawCards(currentGame.playerDeck, playerHand, newTurnOwnerClientId, 1); else DrawCards(currentGame.enemyDeck, enemyHand, newTurnOwnerClientId, 1); }
    public bool GiveInitialHands(Game currentGame, ulong playerOwnerClientId, ulong otherClientId, bool randomStart = true) { Shuffle(currentGame.playerDeck); Shuffle(currentGame.enemyDeck); bool playerStarts = !randomStart || (Random.value < 0.5f); int playerCount = playerStarts ? startPlayerHand : startEnemyHand; int enemyCount = playerStarts ? startEnemyHand : startPlayerHand; if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) { DrawCards(currentGame.playerDeck, playerHand, playerOwnerClientId, playerCount); DrawCards(currentGame.enemyDeck, enemyHand, otherClientId, enemyCount); CardData coinData = GetSpecialCardData("coin"); int coinIndex = -1; if (coinData != null && CardDatabase.AllCards != null) { for (int i = 0; i < CardDatabase.AllCards.Count; i++) { object entryObj = CardDatabase.AllCards[i]; if (entryObj == null) continue; CardData cd = entryObj as CardData; if (cd != null) { if (cd == coinData || (!string.IsNullOrEmpty(cd.cardName) && cd.cardName == coinData.cardName)) { coinIndex = i; break; } continue; } if (entryObj is Card existingCard) if (!string.IsNullOrEmpty(existingCard.name) && existingCard.name == coinData.cardName) { coinIndex = i; break; } } if (coinIndex == -1 && CardDatabase.AllCards != null) { try { Card cardToAdd = coinData.isSpell ? (Card)new SpellCard(coinData) : new Card(coinData); CardDatabase.AllCards.Add(cardToAdd); coinIndex = CardDatabase.AllCards.Count - 1; } catch { } } } if (coinData != null) { Card coinCardInstance = coinData.isSpell ? (Card)new SpellCard(coinData) : new Card(coinData); if (playerStarts) SpawnAndRegisterCard(coinCardInstance, enemyHand, otherClientId, coinIndex); else SpawnAndRegisterCard(coinCardInstance, playerHand, playerOwnerClientId, coinIndex); } } return playerStarts; }

    public void UpdateDeckVisualsFromNetwork(int playerCount, int enemyCount)
    {
        if (playerDeckText != null)
            playerDeckText.text = playerCount.ToString();

        if (enemyDeckText != null)
            enemyDeckText.text = enemyCount.ToString();
    }

    private void SpawnAndRegisterCard(Card card, Transform hand, ulong ownerClientId, int cardDataIndex = -1)
    {
        if (card == null)
        {
            Debug.LogError("SpawnAndRegisterCard called with NULL card!");
            return;
        }

        if (networkCardPrefab == null || visualCardPrefab == null || hand == null)
            return;

        GameObject netInstance = Instantiate(networkCardPrefab);
        var netObj = netInstance.GetComponent<NetworkObject>();
        var cn = netInstance.GetComponent<CardNetwork>();
        var innerVisualOnNet = netInstance.GetComponentInChildren<CardController>(true);

        if (netObj == null || cn == null)
        {
            Destroy(netInstance);
            return;
        }

        try
        {
            if (NetworkManager.Singleton != null)
            {
                if (ownerClientId != NetworkManager.ServerClientId && ownerClientId != NetworkManager.Singleton.LocalClientId)
                    netObj.SpawnWithOwnership(ownerClientId);
                else
                    netObj.Spawn();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Spawn error: {e.Message}");
        }

        Transform root = networkCardRoot;
        if (root == null)
        {
            var found = GameObject.Find("Network Card Root");
            if (found != null) root = found.transform;
        }

        if (netObj.IsSpawned && root != null)
        {
            try
            {
                netObj.transform.SetParent(root, false);
                netObj.transform.localScale = Vector3.one;
                netObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            catch { }
        }

        ulong actualOwner = ownerClientId;
        if (netObj != null)
            actualOwner = netObj.OwnerClientId;

        try
        {
            cn.ownerClientIdNet.Value = actualOwner;
            cn.cardDataIndex.Value = cardDataIndex;

            cn.attack.Value = card.attack;
            cn.health.Value = card.health;
            cn.manaCost.Value = card.manaCost;
            cn.isSpell.Value = card.isSpell;

            cn.isPlaced.Value = false;
            cn.canAttack.Value = false;

            if (cn.abilitiesNet != null)
                cn.abilitiesNet.Value = CardController.AbilitiesToInt(card.abilities);
            else
                Debug.LogError("CardNetwork.abilitiesNet is NULL! Reset component on prefab.");

            if (card is SpellCard sc)
            {
                cn.spellType.Value = (int)sc.spell;
                cn.spellTarget.Value = (int)sc.spellTarget;
                cn.spellPower.Value = sc.spellPower;
            }
            else
            {
                cn.spellType.Value = (int)SpellType.None;
                cn.spellTarget.Value = (int)TargetType.None;
                cn.spellPower.Value = 0;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"NetVar setup error: {e.Message} \nStack: {e.StackTrace}");
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            try
            {
                string descToSend = card != null ? card.description : "";
                string cardIdToSend = card != null ? card.id : "";

                if (string.IsNullOrEmpty(cardIdToSend) && cardDataIndex >= 0 && CardDatabase.AllCards != null && cardDataIndex < CardDatabase.AllCards.Count)
                {
                    var entry = CardDatabase.AllCards[cardDataIndex];
                    if (entry != null) cardIdToSend = entry.id;
                }

                string logoToSend = card?.logo != null ? card.logo.name : "";
                int abilitiesMask = CardController.AbilitiesToInt(card.abilities);

                if (actualOwner != NetworkManager.ServerClientId)
                {
                    var ownerRpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { actualOwner } }
                    };

                    cn.CreateLocalCloneClientRpc(
                        cardDataIndex,
                        actualOwner,
                        card.attack,
                        card.health,
                        card.manaCost,
                        card.isSpell,
                        cardIdToSend,
                        logoToSend,
                        (card is SpellCard s1) ? (int)s1.spell : (int)SpellType.None,
                        (card is SpellCard s2) ? (int)s2.spellTarget : (int)TargetType.None,
                        (card is SpellCard s3) ? s3.spellPower : 0,
                        abilitiesMask,
                        descToSend,
                        ownerRpcParams
                    );
                }

                var otherTargetIds = new List<ulong>();
                foreach (var kv in NetworkManager.Singleton.ConnectedClients)
                {
                    var clientId = kv.Key;
                    if (clientId == NetworkManager.ServerClientId) continue;
                    if (clientId == ownerClientId) continue;
                    otherTargetIds.Add(clientId);
                }

                if (otherTargetIds.Count > 0)
                {
                    var othersRpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams { TargetClientIds = otherTargetIds.ToArray() }
                    };

                    cn.CreateLocalCloneClientRpc(
                        cardDataIndex,
                        actualOwner,
                        card.attack,
                        card.health,
                        card.manaCost,
                        card.isSpell,
                        cardIdToSend,
                        logoToSend,
                        (card is SpellCard s4) ? (int)s4.spell : (int)SpellType.None,
                        (card is SpellCard s5) ? (int)s5.spellTarget : (int)TargetType.None,
                        (card is SpellCard s6) ? s6.spellPower : 0,
                        abilitiesMask,
                        descToSend,
                        othersRpcParams
                    );
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"RPC error: {e.Message}");
            }
        }

        if (innerVisualOnNet != null)
            innerVisualOnNet.gameObject.SetActive(false);

        GameObject uiClone = null;
        try
        {
            uiClone = Instantiate(visualCardPrefab, hand, false);
            uiClone.SetActive(false);
            StartCoroutine(FinishLocalCloneRoutine(uiClone, hand, card, cardDataIndex, actualOwner, netInstance, innerVisualOnNet, cn));
        }
        catch
        {
            if (uiClone != null) Destroy(uiClone);
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySound(AudioManager.Instance.drawCardClip);
    }

    private void UpdateCardPosition(Transform cardTransform, int index, bool isPlaced)
    {
        if (cardTransform == null || !isPlaced) return;
        StartCoroutine(ForcePositionRoutine(cardTransform, index));
    }

    private IEnumerator ForcePositionRoutine(Transform t, int targetIndex)
    {
        for (int i = 0; i < 5; i++)
        {
            if (t == null) yield break;

            var dropPlace = t.parent != null ? t.parent.GetComponent<DropPlace>() : null;
            bool isOnField = dropPlace != null;

            if (t.parent != null && isOnField)
            {
                if (i == 0) LayoutRebuilder.ForceRebuildLayoutImmediate(t.parent as RectTransform);

                int max = t.parent.childCount - 1;
                if (max < 0) max = 0;
                int actualIndex = Mathf.Clamp(targetIndex, 0, max);

                if (t.GetSiblingIndex() != actualIndex)
                    t.SetSiblingIndex(actualIndex);

                LayoutRebuilder.MarkLayoutForRebuild(t.parent as RectTransform);
            }

            yield return null;
        }
    }

    private IEnumerator FinishLocalCloneRoutine(GameObject uiClone, Transform hand, Card card, int cardDataIndex, ulong ownerClientId, GameObject netInstance, CardController innerVisualOnNet, CardNetwork cn)
    {
        yield return null;

        if (uiClone == null)
            yield break;

        try
        {
            uiClone.SetActive(true);
            uiClone.transform.SetParent(hand, false);
            uiClone.transform.localScale = Vector3.one;
            uiClone.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            if (uiClone.TryGetComponent<RectTransform>(out var rtRoot))
            {
                rtRoot.pivot = new Vector2(0.5f, 0.5f);
                rtRoot.anchorMin = new Vector2(0.5f, 0.5f);
                rtRoot.anchorMax = new Vector2(0.5f, 0.5f);
                if (rtRoot.sizeDelta == Vector2.zero) 
                    rtRoot.sizeDelta = new Vector2(175f, 230f);

                rtRoot.anchoredPosition = Vector2.zero;
                rtRoot.localScale = Vector3.one;
            }
            CardController cloneController = uiClone.GetComponent<CardController>() ?? uiClone.GetComponentInChildren<CardController>();
            bool isOwner = NetworkManager.Singleton != null && ownerClientId == NetworkManager.Singleton.LocalClientId;
            if (cloneController != null)
            {
                cloneController.Init(card, isOwner);
                cloneController.SetNetworkData(card.attack, card.health, card.manaCost, card.isSpell, cardDataIndex, ownerClientId, CardController.AbilitiesToInt(card.abilities));

                if (card != null && !string.IsNullOrEmpty(card.description))
                    cloneController.self.description = card.description;

                try
                {
                    if (card is SpellCard origSpell && cloneController.self is SpellCard visualSpell)
                    {
                        bool needCopy = visualSpell.spell == SpellType.None;
                        visualSpell.spell = origSpell.spell;
                        visualSpell.spellTarget = origSpell.spellTarget;
                        visualSpell.spellPower = origSpell.spellPower;

                        cloneController.Info.UpdateStats(visualSpell);
                        cloneController.Info.UpdateDescription(visualSpell);
                    }
                    else
                        cloneController.Info.UpdateDescription(cloneController.self);
                }
                catch { }
                
                cloneController.LinkNetwork(cn);

                var cloneMove = uiClone.GetComponentInChildren<CardMovement>(true);
                if (cloneMove != null)
                {
                    cloneController.SetMovement(cloneMove);
                    cloneMove.defaultParent = hand;
                    cloneMove.tempParent = hand;
                    cloneMove.enabled = isOwner;
                }

                if (!uiClone.TryGetComponent<Graphic>(out var rootGraphic))
                {
                    var img = uiClone.AddComponent<Image>();
                    img.color = new Color(0f, 0f, 0f, 0f);
                    img.raycastTarget = true;
                }
                else 
                    rootGraphic.raycastTarget = true;

                CanvasGroup canvasGroup = uiClone.GetComponent<CanvasGroup>() ?? uiClone.AddComponent<CanvasGroup>();
                canvasGroup.blocksRaycasts = isOwner;
                canvasGroup.interactable = isOwner;
                if (!isOwner)
                {
                    canvasGroup.blocksRaycasts = false;
                    if (cloneMove != null) cloneMove.enabled = false;
                    cn.ownerClientIdNet.OnValueChanged += (oldV, newV) =>
                    {
                        bool nowOwner = NetworkManager.Singleton != null && newV == NetworkManager.Singleton.LocalClientId;
                        canvasGroup.blocksRaycasts = nowOwner;
                        if (cloneMove != null) cloneMove.enabled = nowOwner;
                        cloneController.OnNetworkOwnershipChanged(nowOwner);
                    };
                }
            }

            if (hand.TryGetComponent<RectTransform>(out var handRect))
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(handRect);
                try 
                { 
                    uiClone.transform.SetSiblingIndex(Mathf.Clamp(hand.childCount - 1, 0, hand.childCount)); 
                } 
                catch { }
            }

            var gm = GameManager.Instance;
            if (gm != null && cloneController != null)
            {
                if (isOwner) gm.playerHandCards.Add(cloneController);
                else gm.enemyHandCards.Add(cloneController);
                try { gm.CheckCardsForManaAvailability(); UIManager.Instance.UpdateHPAndMana(); } catch { }
            }
            if (innerVisualOnNet != null) 
                innerVisualOnNet.gameObject.SetActive(false);

            if (cn != null && cloneController != null)
            {
                cn.attack.OnValueChanged += (o, n) => { if (cloneController != null && cloneController.self != null) { cloneController.self.attack = n; cloneController.Info.UpdateStats(cloneController.self); } };
                cn.health.OnValueChanged += (o, n) => { if (cloneController != null && cloneController.self != null) { cloneController.self.health = n; cloneController.Info.UpdateStats(cloneController.self); if (n <= 0) { cloneController.OnDeath(); var gmInst = GameManager.Instance; if (gmInst != null) { if (isOwner) { if (gmInst.playerHandCards.Contains(cloneController)) gmInst.playerHandCards.Remove(cloneController); if (gmInst.playerFieldCards.Contains(cloneController)) gmInst.playerFieldCards.Remove(cloneController); } else { if (gmInst.enemyHandCards.Contains(cloneController)) gmInst.enemyHandCards.Remove(cloneController); if (gmInst.enemyFieldCards.Contains(cloneController)) gmInst.enemyFieldCards.Remove(cloneController); } } if (uiClone != null) Destroy(uiClone); } } };
                cn.manaCost.OnValueChanged += (o, n) => { if (cloneController != null && cloneController.self != null) { cloneController.self.manaCost = n; cloneController.Info.UpdateStats(cloneController.self); } };

                cn.canAttack.OnValueChanged += (o, n) => {
                    if (cloneController != null)
                    {
                        if (cloneController.self != null) cloneController.self.canAttack = n;
                        cloneController.SetCanAttackVisual(n);
                    }
                };

                cn.onCanAttackForceUpdate += (n) => {
                    if (cloneController != null)
                    {
                        if (cloneController.self != null) cloneController.self.canAttack = n;
                        cloneController.SetCanAttackVisual(n);
                    }
                };

                cn.ownerClientIdNet.OnValueChanged += (o, n) => { if (cloneController != null) { bool nowOwner = NetworkManager.Singleton != null && n == NetworkManager.Singleton.LocalClientId; cloneController.isPlayerCard = nowOwner; cloneController.OnNetworkOwnershipChanged(nowOwner); if (!cloneController.self.isPlaced) { if (nowOwner) cloneController.Info.ShowCard(cloneController.self); else cloneController.Info.HideCard(); } else cloneController.Info.ShowCard(cloneController.self); } };
                cn.abilitiesNet.OnValueChanged += (o, n) => { if (cloneController != null) cloneController.UpdateAbilitiesFromMask(n); };

                cn.fieldIndex.OnValueChanged += (o, n) => { UpdateCardPosition(uiClone != null ? uiClone.transform : null, n, cn.isPlaced.Value); };

                cn.isPlaced.OnValueChanged += (o, n) => {
                    if (cloneController == null) return;

                    if (n)
                        cloneController.OnPlacedNetworkSide(cn.ownerClientIdNet.Value);
                    else
                        cloneController.OnUnplacedNetworkSide(cn.ownerClientIdNet.Value);

                    if (n)
                    {
                        UpdateCardPosition(uiClone.transform, cn.fieldIndex.Value, true);

                        var gm2 = GameManager.Instance;
                        if (gm2 != null)
                        {
                            if (ownerClientId == (NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL))
                            {
                                if (gm2.playerHandCards.Contains(cloneController)) 
                                    gm2.playerHandCards.Remove(cloneController);

                                if (!gm2.playerFieldCards.Contains(cloneController)) 
                                    gm2.playerFieldCards.Add(cloneController);
                            }
                            else
                            {
                                if (gm2.enemyHandCards.Contains(cloneController)) 
                                    gm2.enemyHandCards.Remove(cloneController);

                                if (!gm2.enemyFieldCards.Contains(cloneController)) 
                                    gm2.enemyFieldCards.Add(cloneController);
                            }
                        }
                    }
                };

                cn.placedOnTurn.OnValueChanged += (o, n) => { if (cloneController == null) return; cloneController.placedOnTurn = n; };
                try { cloneController.placedOnTurn = cn.placedOnTurn.Value; } catch { }

                if (cn.isPlaced.Value)
                {
                    cloneController.self.isPlaced = true;
                    cloneController.Info.ShowCard(cloneController.self);
                    var dm2 = FindFirstObjectByType<DeckManager>();
                    Transform targetField = (ownerClientId == (NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL)) ? dm2.PlayerField : dm2.EnemyField;
                    if (targetField != null) { uiClone.transform.SetParent(targetField, false); UpdateCardPosition(uiClone.transform, cn.fieldIndex.Value, true); }
                    var gm2 = GameManager.Instance;
                    if (gm2 != null)
                    {
                        if (ownerClientId == (NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL)) { if (gm2.playerHandCards.Contains(cloneController)) gm2.playerHandCards.Remove(cloneController); if (!gm2.playerFieldCards.Contains(cloneController)) gm2.playerFieldCards.Add(cloneController); }
                        else { if (gm2.enemyHandCards.Contains(cloneController)) gm2.enemyHandCards.Remove(cloneController); if (!gm2.enemyFieldCards.Contains(cloneController)) gm2.enemyFieldCards.Add(cloneController); }
                    }
                }

                if (cn != null && cloneController != null)
                    cloneController.UpdateAbilitiesFromMask(cn.abilitiesNet.Value);
            }
        }
        catch { 
            if (uiClone != null) 
                Destroy(uiClone); 
        }
    }
    public SpecialCardEntry GetSpecialCardEntry(string id) { if (string.IsNullOrEmpty(id)) return null; foreach (var e in specialCards) if (e != null && e.id == id) return e; return null; }
    public CardData GetSpecialCardData(string id) { var e = GetSpecialCardEntry(id); return e.cardData; }
    private void ClearList(List<CardController> list) { if (list == null || list.Count == 0) return; for (int i = list.Count - 1; i >= 0; i--) { var c = list[i]; if (c != null) Destroy(c.gameObject); } list.Clear(); }
    public void ClearAll() { var gm = GameManager.Instance; if (gm == null) return; ClearList(gm.playerHandCards); ClearList(gm.playerFieldCards); ClearList(gm.enemyHandCards); ClearList(gm.enemyFieldCards); }
}