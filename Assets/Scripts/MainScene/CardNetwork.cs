using System.Collections;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class CardNetwork : NetworkBehaviour
{
    public NetworkVariable<int> attack = new();
    public NetworkVariable<int> health = new();
    public NetworkVariable<int> manaCost = new();
    public NetworkVariable<bool> isSpell = new();
    public NetworkVariable<bool> isPlaced = new();
    public NetworkVariable<bool> canAttack = new();
    public NetworkVariable<int> cardDataIndex = new();
    public NetworkVariable<ulong> ownerClientIdNet = new();
    public NetworkVariable<int> spellType = new((int)SpellType.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> spellTarget = new((int)TargetType.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> spellPower = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> placedOnTurn = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private CardController visual;

    private NetworkVariable<int>.OnValueChangedDelegate attackChangedHandler;
    private NetworkVariable<int>.OnValueChangedDelegate healthChangedHandler;
    private NetworkVariable<int>.OnValueChangedDelegate manaChangedHandler;
    private NetworkVariable<bool>.OnValueChangedDelegate isPlacedChangedHandler;
    private NetworkVariable<bool>.OnValueChangedDelegate canAttackChangedHandler;
    private NetworkVariable<int>.OnValueChangedDelegate cardDataIndexChangedHandler;
    private NetworkVariable<ulong>.OnValueChangedDelegate ownerChangedHandler;
    private NetworkVariable<int>.OnValueChangedDelegate spellTypeChangedHandler;
    private NetworkVariable<int>.OnValueChangedDelegate spellTargetChangedHandler;
    private NetworkVariable<int>.OnValueChangedDelegate spellPowerChangedHandler;
    private NetworkVariable<int>.OnValueChangedDelegate placedOnTurnChangedHandler;

    private void Awake()
    {
        visual = GetComponentInChildren<CardController>(true);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        attackChangedHandler = (oldV, newV) => UpdateVisual();
        healthChangedHandler = (oldV, newV) => UpdateVisual();
        manaChangedHandler = (oldV, newV) => UpdateVisual();
        isPlacedChangedHandler = (oldV, newV) => OnPlacedChanged();
        canAttackChangedHandler = (oldV, newV) => UpdateHighlight();
        ownerChangedHandler = (oldV, newV) => UpdateOwnership();
        cardDataIndexChangedHandler = (oldV, newV) => UpdateVisual();
        spellTypeChangedHandler = (oldV, newV) => ApplySpellFields();
        spellTargetChangedHandler = (oldV, newV) => ApplySpellFields();
        spellPowerChangedHandler = (oldV, newV) => ApplySpellFields();
        placedOnTurnChangedHandler = (oldV, newV) => OnPlacedTurnChanged(newV);

        attack.OnValueChanged += attackChangedHandler;
        health.OnValueChanged += healthChangedHandler;
        manaCost.OnValueChanged += manaChangedHandler;
        isPlaced.OnValueChanged += isPlacedChangedHandler;
        canAttack.OnValueChanged += canAttackChangedHandler;
        ownerClientIdNet.OnValueChanged += ownerChangedHandler;
        cardDataIndex.OnValueChanged += cardDataIndexChangedHandler;
        spellType.OnValueChanged += spellTypeChangedHandler;
        spellTarget.OnValueChanged += spellTargetChangedHandler;
        spellPower.OnValueChanged += spellPowerChangedHandler;
        placedOnTurn.OnValueChanged += placedOnTurnChangedHandler;

        UpdateVisual();
        UpdateOwnership();
        UpdateHighlight();
        OnPlacedChanged();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (attackChangedHandler != null) 
            attack.OnValueChanged -= attackChangedHandler;
        if (healthChangedHandler != null) 
            health.OnValueChanged -= healthChangedHandler;
        if (manaChangedHandler != null) 
            manaCost.OnValueChanged -= manaChangedHandler;
        if (isPlacedChangedHandler != null) 
            isPlaced.OnValueChanged -= isPlacedChangedHandler;
        if (canAttackChangedHandler != null) 
            canAttack.OnValueChanged -= canAttackChangedHandler;
        if (ownerChangedHandler != null) 
            ownerClientIdNet.OnValueChanged -= ownerChangedHandler;
        if (cardDataIndexChangedHandler != null) 
            cardDataIndex.OnValueChanged -= cardDataIndexChangedHandler;
        if (spellTypeChangedHandler != null) 
            spellType.OnValueChanged -= spellTypeChangedHandler;
        if (spellTargetChangedHandler != null) 
            spellTarget.OnValueChanged -= spellTargetChangedHandler;
        if (spellPowerChangedHandler != null) 
            spellPower.OnValueChanged -= spellPowerChangedHandler;
        if (placedOnTurnChangedHandler != null) 
            placedOnTurn.OnValueChanged -= placedOnTurnChangedHandler;
    }

    private void OnPlacedChanged()
    {
        if (visual == null)
            return;

        if (isPlaced.Value)
            visual.OnPlacedNetworkSide(ownerClientIdNet.Value);
        else
            visual.OnUnplacedNetworkSide(ownerClientIdNet.Value);
    }

    private void UpdateVisual()
    {
        if (visual == null)
            return;

        visual.SetNetworkData(attack.Value, health.Value, manaCost.Value, isSpell.Value, cardDataIndex.Value, ownerClientIdNet.Value);
        ApplySpellFields();
    }

    private void ApplySpellFields()
    {
        if (visual == null || visual.self == null)
            return;

        if (visual.self is SpellCard s)
        {
            s.spell = (SpellType)spellType.Value;
            s.spellTarget = (TargetType)spellTarget.Value;
            s.spellPower = spellPower.Value;
            visual.Info?.UpdateStats(s);
        }
    }

    private void UpdateOwnership()
    {
        if (visual == null)
            return;

        bool isMine = NetworkManager.Singleton != null && ownerClientIdNet.Value == NetworkManager.Singleton.LocalClientId;
        visual.OnNetworkOwnershipChanged(isMine);
        var cg = visual.GetComponent<CanvasGroup>();
        if (cg != null)
            cg.blocksRaycasts = isMine;
    }

    private void UpdateHighlight()
    {
        if (visual == null)
            return;

        visual.SetCanAttackVisual(canAttack.Value);
    }

    private void OnPlacedTurnChanged(int newPlacedOnTurn)
    {
        if (visual == null)
            return;

        visual.placedOnTurn = newPlacedOnTurn;
    }

    [ClientRpc]
    public void CreateLocalCloneClientRpc(int cardDataIndexValue, ulong ownerClientId, int attackValue, int healthValue, int manaCostValue, bool isSpellValue, string cardIdValue, string logoNameValue, int spellTypeValue, int spellTargetValue, int spellPowerValue, ClientRpcParams clientRpcParams = default)
    {
        StartCoroutine(CreateLocalCloneRoutine(cardDataIndexValue, ownerClientId, attackValue, healthValue, manaCostValue, isSpellValue, cardIdValue, logoNameValue, spellTypeValue, spellTargetValue, spellPowerValue));
    }

    private IEnumerator CreateLocalCloneRoutine(int cardDataIndexValue, ulong ownerClientId, int attackValue, int healthValue, int manaCostValue, bool isSpellValue, string cardIdValue, string logoNameValue, int spellTypeValue, int spellTargetValue, int spellPowerValue)
    {
        float timeout = 2f;
        float start = Time.realtimeSinceStartup;
        DeckManager dm = null;
        while (Time.realtimeSinceStartup - start < timeout)
        {
            dm = FindFirstObjectByType<DeckManager>();
            if (dm != null)
                break;

            yield return null;
        }

        if (dm == null)
            yield break;

        Transform hand = null;
        float start2 = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start2 < timeout)
        {
            hand = ownerClientId == (NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL) ? dm.PlayerHand : dm.EnemyHand;
            if (hand != null)
                break;

            yield return null;
        }
        if (hand == null)
            yield break;

        GameObject visualPrefab = null;
        float start3 = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start3 < timeout)
        {
            visualPrefab = dm.VisualCardPrefab;
            if (visualPrefab != null)
                break;

            yield return null;
        }
        if (visualPrefab == null)
            yield break;

        GameObject uiClone = null;
        try
        {
            uiClone = Instantiate(visualPrefab, hand, false);
            uiClone.SetActive(true);
            var rtRoot = uiClone.GetComponent<RectTransform>();
            if (rtRoot != null)
            {
                rtRoot.pivot = new Vector2(0.5f, 0.5f);
                rtRoot.anchorMin = new Vector2(0.5f, 0.5f);
                rtRoot.anchorMax = new Vector2(0.5f, 0.5f);
                if (rtRoot.sizeDelta == Vector2.zero)
                    rtRoot.sizeDelta = new Vector2(176f, 230f);
                rtRoot.anchoredPosition = Vector2.zero;
                rtRoot.localScale = Vector3.one;
            }
            var cloneController = uiClone.GetComponent<CardController>() ?? uiClone.GetComponentInChildren<CardController>(true);
            bool isOwner = NetworkManager.Singleton != null && ownerClientId == NetworkManager.Singleton.LocalClientId;
            if (cloneController != null)
            {
                cloneController.SetNetworkData(attackValue, healthValue, manaCostValue, isSpellValue, cardDataIndexValue, ownerClientId);

                if (!string.IsNullOrEmpty(cardIdValue))
                {
                    CardData foundData = null;
                    if (CardDatabase.CardDataById != null && CardDatabase.CardDataById.TryGetValue(cardIdValue, out var cd))
                        foundData = cd;

                    if (foundData == null)
                    {
                        var special = dm.GetSpecialCardEntry(cardIdValue);
                        if (special != null && special.cardData != null)
                            foundData = special.cardData;
                    }

                    if (foundData != null)
                    {
                        if (!string.IsNullOrEmpty(foundData.cardName))
                            cloneController.self.name = foundData.cardName;
                        if (foundData.logo != null)
                            cloneController.self.logo = foundData.logo;
                    }
                }

                if (!string.IsNullOrEmpty(logoNameValue))
                {
                    var sp = Resources.Load<Sprite>(logoNameValue);
                    if (sp != null)
                        cloneController.self.logo = sp;
                }
                if (cloneController.self is SpellCard sc)
                {
                    sc.spell = (SpellType)spellTypeValue;
                    sc.spellTarget = (TargetType)spellTargetValue;
                    sc.spellPower = spellPowerValue;
                }
                cloneController.Init(cloneController.self, isOwner);
                cloneController.LinkNetwork(this);
                var cloneMove = uiClone.GetComponentInChildren<CardMovement>(true);
                if (cloneMove != null)
                {
                    cloneController.SetMovement(cloneMove);
                    cloneMove.defaultParent = hand;
                    cloneMove.tempParent = hand;
                    cloneMove.enabled = isOwner;
                }
                var canvasGroup = uiClone.GetComponent<CanvasGroup>() ?? uiClone.AddComponent<CanvasGroup>();
                canvasGroup.blocksRaycasts = isOwner;
                canvasGroup.interactable = isOwner;
                cloneController.self.canAttack = canAttack.Value;
                cloneController.SetCanAttackVisual(canAttack.Value);
                if (!isOwner)
                {
                    canvasGroup.blocksRaycasts = false;
                    if (cloneMove != null) cloneMove.enabled = false;
                    ownerClientIdNet.OnValueChanged += (oldV, newV) =>
                    {
                        bool nowOwner = NetworkManager.Singleton != null && newV == NetworkManager.Singleton.LocalClientId;
                        canvasGroup.blocksRaycasts = nowOwner;
                        if (cloneMove != null) cloneMove.enabled = nowOwner;
                        cloneController.OnNetworkOwnershipChanged(nowOwner);
                    };
                }
            }

            attack.OnValueChanged += (o, n) =>
            {
                if (uiClone == null)
                    return;

                var ctrl = uiClone.GetComponentInChildren<CardController>();
                if (ctrl != null && ctrl.self != null)
                {
                    ctrl.self.attack = n;
                    ctrl.Info?.UpdateStats(ctrl.self);
                }
            };
            health.OnValueChanged += (o, n) =>
            {
                if (uiClone == null)
                    return;

                var ctrl = uiClone.GetComponentInChildren<CardController>();
                if (ctrl != null && ctrl.self != null)
                {
                    ctrl.self.health = n;
                    ctrl.Info?.UpdateStats(ctrl.self);
                    if (n <= 0)
                    {
                        var gm = GameManager.Instance;
                        if (gm != null)
                        {
                            if (ownerClientId == (NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL))
                            {
                                if (gm.playerHandCards.Contains(ctrl)) gm.playerHandCards.Remove(ctrl);
                                if (gm.playerFieldCards.Contains(ctrl)) gm.playerFieldCards.Remove(ctrl);
                            }
                            else
                            {
                                if (gm.enemyHandCards.Contains(ctrl)) gm.enemyHandCards.Remove(ctrl);
                                if (gm.enemyFieldCards.Contains(ctrl)) gm.enemyFieldCards.Remove(ctrl);
                            }
                        }
                        if (uiClone != null) Destroy(uiClone);
                    }
                }
            };
            manaCost.OnValueChanged += (o, n) =>
            {
                if (uiClone == null)
                    return;

                var ctrl = uiClone.GetComponentInChildren<CardController>();
                if (ctrl != null && ctrl.self != null)
                {
                    ctrl.self.manaCost = n;
                    ctrl.Info?.UpdateStats(ctrl.self);
                }
            };
            canAttack.OnValueChanged += (o, n) =>
            {
                if (uiClone == null)
                    return;

                var ctrl = uiClone.GetComponentInChildren<CardController>();
                if (ctrl != null)
                {
                    if (ctrl.self != null) ctrl.self.canAttack = n;
                    ctrl.SetCanAttackVisual(n);
                }
            };
            ownerClientIdNet.OnValueChanged += (o, n) =>
            {
                var ctrl = uiClone != null ? uiClone.GetComponentInChildren<CardController>() : null;
                if (ctrl == null)
                    return;

                bool nowOwner = NetworkManager.Singleton != null && n == NetworkManager.Singleton.LocalClientId;
                ctrl.isPlayerCard = nowOwner;
                ctrl.OnNetworkOwnershipChanged(nowOwner);
                if (!ctrl.self.isPlaced)
                {
                    if (nowOwner)
                        ctrl.Info?.ShowCard(ctrl.self);
                    else
                        ctrl.Info?.HideCard();
                }
                else
                    ctrl.Info?.ShowCard(ctrl.self);
            };
            isPlaced.OnValueChanged += (o, n) =>
            {
                var ctrl = uiClone != null ? uiClone.GetComponentInChildren<CardController>() : null;
                if (ctrl == null)
                    return;

                ctrl.self.isPlaced = n;
                ctrl.Info?.ShowCard(ctrl.self);
                if (n)
                {
                    var dm = FindFirstObjectByType<DeckManager>();
                    if (dm != null)
                    {
                        Transform targetField = (ownerClientId == (NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL)) ? dm.PlayerField : dm.EnemyField;
                        try
                        {
                            uiClone.transform.SetParent(targetField, false);
                        }
                        catch { }
                    }
                    var gm2 = GameManager.Instance;
                    if (gm2 != null)
                    {
                        if (ownerClientId == (NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL))
                        {
                            if (gm2.playerHandCards.Contains(ctrl))
                                gm2.playerHandCards.Remove(ctrl);
                            if (!gm2.playerFieldCards.Contains(ctrl))
                                gm2.playerFieldCards.Add(ctrl);
                        }
                        else
                        {
                            if (gm2.enemyHandCards.Contains(ctrl))
                                gm2.enemyHandCards.Remove(ctrl);
                            if (!gm2.enemyFieldCards.Contains(ctrl))
                                gm2.enemyFieldCards.Add(ctrl);
                        }
                    }
                }
            };
            spellType.OnValueChanged += (o, n) =>
            {
                var ctrl = uiClone != null ? uiClone.GetComponentInChildren<CardController>() : null;
                if (ctrl == null || ctrl.self == null)
                    return;

                if (ctrl.self is SpellCard s)
                {
                    s.spell = (SpellType)n;
                    ctrl.Info?.UpdateStats(s);
                }
            };
            spellTarget.OnValueChanged += (o, n) =>
            {
                var ctrl = uiClone != null ? uiClone.GetComponentInChildren<CardController>() : null;
                if (ctrl == null || ctrl.self == null)
                    return;

                if (ctrl.self is SpellCard s)
                {
                    s.spellTarget = (TargetType)n;
                    ctrl.Info?.UpdateStats(s);
                }
            };
            spellPower.OnValueChanged += (o, n) =>
            {
                var ctrl = uiClone != null ? uiClone.GetComponentInChildren<CardController>() : null;
                if (ctrl == null || ctrl.self == null)
                    return;

                if (ctrl.self is SpellCard s)
                {
                    s.spellPower = n;
                    ctrl.Info?.UpdateStats(s);
                }
            };

            var gm = GameManager.Instance;
            var controllerToAdd = uiClone != null ? uiClone.GetComponentInChildren<CardController>() : null;
            if (gm != null && controllerToAdd != null)
            {
                if (ownerClientId == (NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL))
                    gm.playerHandCards.Add(controllerToAdd);
                else
                    gm.enemyHandCards.Add(controllerToAdd);

                gm.CheckCardsForManaAvailability();
                UIManager.Instance?.UpdateHPAndMana();
            }

            if (isPlaced.Value)
            {
                var ctrl = uiClone != null ? uiClone.GetComponentInChildren<CardController>() : null;
                if (ctrl != null)
                {
                    ctrl.self.isPlaced = true;
                    ctrl.Info?.ShowCard(ctrl.self);
                    var dm2 = FindFirstObjectByType<DeckManager>();
                    Transform targetField = (ownerClientId == (NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL)) ? dm2.PlayerField : dm2.EnemyField;
                    if (targetField != null)
                    {
                        uiClone.transform.SetParent(targetField, false);
                    }
                    var gm2 = GameManager.Instance;
                    if (gm2 != null)
                    {
                        if (ownerClientId == (NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL))
                        {
                            if (gm2.playerHandCards.Contains(ctrl))
                                gm2.playerHandCards.Remove(ctrl);
                            if (!gm2.playerFieldCards.Contains(ctrl))
                                gm2.playerFieldCards.Add(ctrl);
                        }
                        else
                        {
                            if (gm2.enemyHandCards.Contains(ctrl))
                                gm2.enemyHandCards.Remove(ctrl);
                            if (!gm2.enemyFieldCards.Contains(ctrl))
                                gm2.enemyFieldCards.Add(ctrl);
                        }
                    }
                }
            }
        }
        catch
        {
            if (uiClone != null)
                Destroy(uiClone);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void RequestPlaceCardServerRpc(bool isPlayerSide, RpcParams rpcParams = default)
    {
        if (!IsServer)
            return;

        ulong sender = rpcParams.Receive.SenderClientId;
        if (ownerClientIdNet.Value != sender)
            return;

        isPlaced.Value = true;
        canAttack.Value = false;
        placedOnTurn.Value = GameManager.Instance != null ? GameManager.Instance.CurrentTurn : 0;

        var gm = GameManager.Instance;
        if (gm != null && visual != null)
        {
            if (ownerClientIdNet.Value == NetworkManager.ServerClientId)
            {
                if (gm.playerHandCards.Contains(visual))
                    gm.playerHandCards.Remove(visual);
                if (!gm.playerFieldCards.Contains(visual))
                    gm.playerFieldCards.Add(visual);
            }
            else
            {
                if (gm.enemyHandCards.Contains(visual)) gm.enemyHandCards.Remove(visual);
                if (!gm.enemyFieldCards.Contains(visual)) gm.enemyFieldCards.Add(visual);
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void RequestAttackServerRpc(ulong targetNetObjId, RpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong sender = rpcParams.Receive.SenderClientId;
        if (ownerClientIdNet.Value != sender) return;
        if (!isPlaced.Value || !canAttack.Value) return;
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetObjId, out var targetNetObj)) return;
        var targetCardNetwork = targetNetObj.GetComponent<CardNetwork>();
        if (targetCardNetwork == null) return;
        int atk = attack.Value;
        targetCardNetwork.health.Value = Mathf.Max(0, targetCardNetwork.health.Value - atk);
        canAttack.Value = false;
        if (targetCardNetwork.health.Value <= 0) try { targetNetObj.Despawn(true); } catch { }
        if (health.Value <= 0)
        {
            var selfNO = this.GetComponent<NetworkObject>();
            if (selfNO != null) try { selfNO.Despawn(true); } catch { }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void RequestCastSpellServerRpc(int spellTypeValue, int spellTargetType, int spellPowerValue, ulong targetNetObjId, RpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong sender = rpcParams.Receive.SenderClientId;
        if (ownerClientIdNet.Value != sender) return;
        spellType.Value = spellTypeValue;
        spellTarget.Value = spellTargetType;
        spellPower.Value = spellPowerValue;
        SpellType st = (SpellType)spellTypeValue;
        TargetType tt = (TargetType)spellTargetType;
        if (st == SpellType.DamageCard && tt == TargetType.EnemyCard && targetNetObjId != 0)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetObjId, out var targetNetObj))
            {
                var targetCardNetwork = targetNetObj.GetComponent<CardNetwork>();
                if (targetCardNetwork != null)
                {
                    targetCardNetwork.health.Value = Mathf.Max(0, targetCardNetwork.health.Value - spellPowerValue);
                    if (targetCardNetwork.health.Value <= 0) try { targetNetObj.Despawn(true); } catch { }
                }
            }
        }
    }
}
