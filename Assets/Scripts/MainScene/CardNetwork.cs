using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
    public NetworkVariable<int> fieldIndex = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> abilitiesNet = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public System.Action<bool> onCanAttackForceUpdate;

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
    private NetworkVariable<int>.OnValueChangedDelegate fieldIndexChangedHandler;
    private NetworkVariable<int>.OnValueChangedDelegate abilitiesChangedHandler;

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
        fieldIndexChangedHandler = (oldV, newV) => ApplySiblingIndex(newV);
        abilitiesChangedHandler = (oldV, newV) => ApplyAbilities(newV);

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
        fieldIndex.OnValueChanged += fieldIndexChangedHandler;
        abilitiesNet.OnValueChanged += abilitiesChangedHandler;

        UpdateVisual();
        UpdateOwnership();
        UpdateHighlight();
        ApplyAbilities(abilitiesNet.Value);
        if (isPlaced.Value)
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
        if (fieldIndexChangedHandler != null) 
            fieldIndex.OnValueChanged -= fieldIndexChangedHandler;
        if (abilitiesChangedHandler != null) 
            abilitiesNet.OnValueChanged -= abilitiesChangedHandler;
    }

    private void ApplySiblingIndex(int index)
    {
        if (visual)
        {
            var dropPlace = visual.transform.parent != null ? visual.transform.parent.GetComponent<DropPlace>() : null;
            bool isOnField = dropPlace != null;

            if (isOnField)
                visual.transform.SetSiblingIndex(index);
        }
    }

    private void OnPlacedChanged()
    {
        if (!visual) 
            return;

        if (isPlaced.Value) 
            visual.OnPlacedNetworkSide(ownerClientIdNet.Value);
        else 
            visual.OnUnplacedNetworkSide(ownerClientIdNet.Value);

        if (isPlaced.Value) 
            ApplySiblingIndex(fieldIndex.Value);
    }

    private void UpdateVisual()
    {
        if (visual)
        {
            visual.SetNetworkData(attack.Value, health.Value, manaCost.Value, isSpell.Value, cardDataIndex.Value, ownerClientIdNet.Value, abilitiesNet.Value);
            ApplySpellFields();
        }
    }

    private void ApplySpellFields()
    {
        if (visual?.self is SpellCard s)
        {
            s.spell = (SpellType)spellType.Value;
            s.spellTarget = (TargetType)spellTarget.Value;
            s.spellPower = spellPower.Value;

            visual.Info?.UpdateStats(s);

            visual.Info?.UpdateDescription(s);
        }
    }

    private void ApplyAbilities(int mask)
    {
        if (visual) 
            visual.UpdateAbilitiesFromMask(mask);
    }

    private void UpdateOwnership()
    {
        if (visual) 
        { 
            bool isMine = NetworkManager.Singleton != null && ownerClientIdNet.Value == NetworkManager.Singleton.LocalClientId; 
            visual.OnNetworkOwnershipChanged(isMine); 
        }
    }

    private void UpdateHighlight()
    {
        if (visual)
        {
            if (visual.self != null) 
                visual.self.canAttack = canAttack.Value;

            visual.SetCanAttackVisual(canAttack.Value);
        }
    }

    private void OnPlacedTurnChanged(int n)
    {
        if (visual) 
            visual.placedOnTurn = n;
    }

    [ClientRpc]
    public void CreateLocalCloneClientRpc(int cardDataIndexValue, ulong ownerClientId, int attackValue, int healthValue, int manaCostValue, bool isSpellValue, string cardIdValue, string logoNameValue, int spellTypeValue, int spellTargetValue, int spellPowerValue, int abilitiesValue, string descriptionValue, ClientRpcParams clientRpcParams = default)
    {
        StartCoroutine(CreateLocalCloneRoutine(cardDataIndexValue, ownerClientId, attackValue, healthValue, manaCostValue, isSpellValue, cardIdValue, logoNameValue, spellTypeValue, spellTargetValue, spellPowerValue, abilitiesValue, descriptionValue));
    }

    [ClientRpc]
    public void ForceCanAttackSyncClientRpc(bool state, ClientRpcParams clientRpcParams = default)
    {
        onCanAttackForceUpdate?.Invoke(state);
    }

    private void UpdateClonePosition(Transform t, int index, bool isPlaced)
    {
        if (t == null || !isPlaced) 
            return;

        StartCoroutine(ForceClonePosition(t, index));
    }

    private IEnumerator ForceClonePosition(Transform t, int targetIndex)
    {
        for (int i = 0; i < 5; i++)
        {
            if (t == null)
                yield break;

            var dropPlace = t.parent != null ? t.parent.GetComponent<DropPlace>() : null;
            bool isOnField = dropPlace != null;

            if (t.parent != null && isOnField)
            {
                if (i == 0)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(t.parent as RectTransform);

                int max = t.parent.childCount - 1;
                if (max < 0) 
                    max = 0;

                int actualIndex = Mathf.Clamp(targetIndex, 0, max);

                if (t.GetSiblingIndex() != actualIndex)
                    t.SetSiblingIndex(actualIndex);

                LayoutRebuilder.MarkLayoutForRebuild(t.parent as RectTransform);
            }

            yield return null;
        }
    }

    private IEnumerator CreateLocalCloneRoutine(int cardDataIndexValue, ulong ownerClientId, int attackValue, int healthValue, int manaCostValue, bool isSpellValue, string cardIdValue, string logoNameValue, int spellTypeValue, int spellTargetValue, int spellPowerValue, int abilitiesValue, string descriptionValue)
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

        Transform hand = null; float start2 = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start2 < timeout) { hand = ownerClientId == (NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL) ? dm.PlayerHand : dm.EnemyHand; if (hand != null) break; yield return null; }
        if (hand == null) 
            yield break;

        GameObject visualPrefab = null; float start3 = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start3 < timeout) { visualPrefab = dm.VisualCardPrefab; if (visualPrefab != null) break; yield return null; }
        if (visualPrefab == null) yield break;

        GameObject uiClone = null;
        try
        {
            uiClone = Instantiate(visualPrefab, hand, false); 
            uiClone.SetActive(true);
            var rtRoot = uiClone.GetComponent<RectTransform>();
            if (rtRoot != null)
            {
                rtRoot.pivot = new Vector2(0.5f, 0.5f); rtRoot.anchorMin = new Vector2(0.5f, 0.5f); rtRoot.anchorMax = new Vector2(0.5f, 0.5f);
                if (rtRoot.sizeDelta == Vector2.zero) rtRoot.sizeDelta = new Vector2(176f, 230f);
                rtRoot.anchoredPosition = Vector2.zero; rtRoot.localScale = Vector3.one;
            }
            var cloneController = uiClone.GetComponent<CardController>() ?? uiClone.GetComponentInChildren<CardController>(true);
            bool isOwner = NetworkManager.Singleton != null && ownerClientId == NetworkManager.Singleton.LocalClientId;
            if (cloneController != null)
            {
                cloneController.SetNetworkData(attackValue, healthValue, manaCostValue, isSpellValue, cardDataIndexValue, ownerClientId, abilitiesValue);
                if (!string.IsNullOrEmpty(cardIdValue))
                {
                    CardData foundData = null;
                    if (CardDatabase.CardDataById != null && CardDatabase.CardDataById.TryGetValue(cardIdValue, out var cd)) foundData = cd;
                    if (foundData == null) { var special = dm.GetSpecialCardEntry(cardIdValue); if (special != null && special.cardData != null) foundData = special.cardData; }
                    if (foundData != null) { if (!string.IsNullOrEmpty(foundData.cardName)) cloneController.self.name = foundData.cardName; if (foundData.logo != null) cloneController.self.logo = foundData.logo; }
                }
                if (!string.IsNullOrEmpty(logoNameValue)) { var sp = Resources.Load<Sprite>(logoNameValue); if (sp != null) cloneController.self.logo = sp; }
                if (cloneController.self is SpellCard sc) { sc.spell = (SpellType)spellTypeValue; sc.spellTarget = (TargetType)spellTargetValue; sc.spellPower = spellPowerValue; }
                
                if (!string.IsNullOrEmpty(descriptionValue))
                    cloneController.self.description = descriptionValue;

                cloneController.UpdateAbilitiesFromMask(abilitiesValue);
                cloneController.Init(cloneController.self, isOwner);
                cloneController.LinkNetwork(this);

                var cloneMove = uiClone.GetComponentInChildren<CardMovement>(true);
                if (cloneMove != null) { cloneController.SetMovement(cloneMove); cloneMove.defaultParent = hand; cloneMove.tempParent = hand; cloneMove.enabled = isOwner; }
                var canvasGroup = uiClone.GetComponent<CanvasGroup>() ?? uiClone.AddComponent<CanvasGroup>();
                canvasGroup.blocksRaycasts = isOwner; canvasGroup.interactable = isOwner;
                cloneController.self.canAttack = canAttack.Value; cloneController.SetCanAttackVisual(canAttack.Value);
                if (!isOwner)
                {
                    canvasGroup.blocksRaycasts = false; if (cloneMove != null) cloneMove.enabled = false;
                    ownerClientIdNet.OnValueChanged += (oldV, newV) => { bool nowOwner = NetworkManager.Singleton != null && newV == NetworkManager.Singleton.LocalClientId; canvasGroup.blocksRaycasts = nowOwner; if (cloneMove != null) cloneMove.enabled = nowOwner; cloneController.OnNetworkOwnershipChanged(nowOwner); };
                }
            }

            attack.OnValueChanged += (o, n) => { if (uiClone == null) return; var ctrl = uiClone.GetComponentInChildren<CardController>(); if (ctrl != null && ctrl.self != null) { ctrl.self.attack = n; ctrl.Info?.UpdateStats(ctrl.self); } };
            health.OnValueChanged += (o, n) => { if (uiClone == null) return; var ctrl = uiClone.GetComponentInChildren<CardController>(); if (ctrl != null && ctrl.self != null) { ctrl.self.health = n; ctrl.Info?.UpdateStats(ctrl.self); if (n <= 0) { var gm = GameManager.Instance; if (gm != null) { if (ownerClientId == (NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL)) { if (gm.playerHandCards.Contains(ctrl)) gm.playerHandCards.Remove(ctrl); if (gm.playerFieldCards.Contains(ctrl)) gm.playerFieldCards.Remove(ctrl); } else { if (gm.enemyHandCards.Contains(ctrl)) gm.enemyHandCards.Remove(ctrl); if (gm.enemyFieldCards.Contains(ctrl)) gm.enemyFieldCards.Remove(ctrl); } } if (uiClone != null) Destroy(uiClone); } } };
            manaCost.OnValueChanged += (o, n) => { if (uiClone == null) return; var ctrl = uiClone.GetComponentInChildren<CardController>(); if (ctrl != null && ctrl.self != null) { ctrl.self.manaCost = n; ctrl.Info?.UpdateStats(ctrl.self); } };

            canAttack.OnValueChanged += (o, n) => {
                if (uiClone == null) 
                    return;
                var ctrl = uiClone.GetComponentInChildren<CardController>();
                if (ctrl != null)
                {
                    if (ctrl.self != null) 
                        ctrl.self.canAttack = n;
                    ctrl.SetCanAttackVisual(n);
                }
            };

            onCanAttackForceUpdate += (n) => {
                if (uiClone == null) 
                    return;
                var ctrl = uiClone.GetComponentInChildren<CardController>();
                if (ctrl != null)
                {
                    if (ctrl.self != null) 
                        ctrl.self.canAttack = n;
                    ctrl.SetCanAttackVisual(n);
                }
            };

            ownerClientIdNet.OnValueChanged += (o, n) => { var ctrl = uiClone != null ? uiClone.GetComponentInChildren<CardController>() : null; if (ctrl == null) return; bool nowOwner = NetworkManager.Singleton != null && n == NetworkManager.Singleton.LocalClientId; ctrl.isPlayerCard = nowOwner; ctrl.OnNetworkOwnershipChanged(nowOwner); if (!ctrl.self.isPlaced) { if (nowOwner) ctrl.Info?.ShowCard(ctrl.self); else ctrl.Info?.HideCard(); } else ctrl.Info?.ShowCard(ctrl.self); };
            fieldIndex.OnValueChanged += (o, n) => { UpdateClonePosition(uiClone != null ? uiClone.transform : null, n, isPlaced.Value); };
            abilitiesNet.OnValueChanged += (o, n) => { if (uiClone == null) return; var ctrl = uiClone.GetComponentInChildren<CardController>(); if (ctrl != null) ctrl.UpdateAbilitiesFromMask(n); };

            isPlaced.OnValueChanged += (o, n) => {
                var ctrl = uiClone != null ? uiClone.GetComponentInChildren<CardController>() : null;
                if (ctrl == null)
                    return;

                if (n)
                    ctrl.OnPlacedNetworkSide(ownerClientIdNet.Value);
                else
                    ctrl.OnUnplacedNetworkSide(ownerClientIdNet.Value);

                if (n)
                {
                    var dm = FindFirstObjectByType<DeckManager>();
                    if (dm != null)
                    {
                        Transform targetField = (ownerClientId == (NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL)) ? dm.PlayerField : dm.EnemyField;
                        try
                        {
                            UpdateClonePosition(uiClone.transform, fieldIndex.Value, true);
                        }
                        catch { }
                    }

                    var gm2 = GameManager.Instance;
                    if (gm2 != null)
                    {
                        if (ownerClientId == (NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL))
                        {
                            if (gm2.playerHandCards.Contains(ctrl)) gm2.playerHandCards.Remove(ctrl);
                            if (!gm2.playerFieldCards.Contains(ctrl)) gm2.playerFieldCards.Add(ctrl);
                        }
                        else
                        {
                            if (gm2.enemyHandCards.Contains(ctrl)) gm2.enemyHandCards.Remove(ctrl);
                            if (!gm2.enemyFieldCards.Contains(ctrl)) gm2.enemyFieldCards.Add(ctrl);
                        }
                    }
                }
            };

            spellType.OnValueChanged += (o, n) => { var ctrl = uiClone != null ? uiClone.GetComponentInChildren<CardController>() : null; if (ctrl == null || ctrl.self == null) return; if (ctrl.self is SpellCard s) { s.spell = (SpellType)n; ctrl.Info?.UpdateStats(s); } };
            spellTarget.OnValueChanged += (o, n) => { var ctrl = uiClone != null ? uiClone.GetComponentInChildren<CardController>() : null; if (ctrl == null || ctrl.self == null) return; if (ctrl.self is SpellCard s) { s.spellTarget = (TargetType)n; ctrl.Info?.UpdateStats(s); } };
            spellPower.OnValueChanged += (o, n) => { var ctrl = uiClone != null ? uiClone.GetComponentInChildren<CardController>() : null; if (ctrl == null || ctrl.self == null) return; if (ctrl.self is SpellCard s) { s.spellPower = n; ctrl.Info?.UpdateStats(s); } };

            var gm = GameManager.Instance;
            var controllerToAdd = uiClone != null ? uiClone.GetComponentInChildren<CardController>() : null;
            if (gm != null && controllerToAdd != null) { if (ownerClientId == (NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL)) gm.playerHandCards.Add(controllerToAdd); else gm.enemyHandCards.Add(controllerToAdd); gm.CheckCardsForManaAvailability(); UIManager.Instance?.UpdateHPAndMana(); }
            if (isPlaced.Value)
            {
                var ctrl = uiClone != null ? uiClone.GetComponentInChildren<CardController>() : null;
                if (ctrl != null)
                {
                    ctrl.self.isPlaced = true; ctrl.Info?.ShowCard(ctrl.self);
                    if (ctrl.Ability != null) ctrl.Ability.OnApplyEffect(ctrl.self, ctrl.isPlayerCard, ctrl.Info);
                    var dm2 = FindFirstObjectByType<DeckManager>(); Transform targetField = (ownerClientId == (NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL)) ? dm2.PlayerField : dm2.EnemyField; if (targetField != null) { uiClone.transform.SetParent(targetField, false); UpdateClonePosition(uiClone.transform, fieldIndex.Value, true); }
                    var gm2 = GameManager.Instance; if (gm2 != null) { if (ownerClientId == (NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL)) { if (gm2.playerHandCards.Contains(ctrl)) gm2.playerHandCards.Remove(ctrl); if (!gm2.playerFieldCards.Contains(ctrl)) gm2.playerFieldCards.Add(ctrl); } else { if (gm2.enemyHandCards.Contains(ctrl)) gm2.enemyHandCards.Remove(ctrl); if (!gm2.enemyFieldCards.Contains(ctrl)) gm2.enemyFieldCards.Add(ctrl); } }
                }
            }
        }
        catch 
        { 
            if (uiClone != null) 
                Destroy(uiClone); 
        }
    }

    [ClientRpc]
    public void RemoveLocalCloneClientRpc(ClientRpcParams clientRpcParams = default)
    {
        StartCoroutine(RemoveLocalCloneRoutine());
    }

    private IEnumerator RemoveLocalCloneRoutine()
    {
        float timeout = 2f; float start = Time.realtimeSinceStartup; GameManager gm = null;
        while (Time.realtimeSinceStartup - start < timeout) 
        { 
            gm = GameManager.Instance; 
            if (gm != null) 
                break; 

            yield return null; 
        }
        if (gm == null) 
            yield break;
        gm.SanitizeLists();
        CardController found = null;
        foreach (var c in gm.playerHandCards) 
            if (c != null && c.Network == this) 
            { 
                found = c; 
                break; 
            }

        if (found == null) 
            foreach (var c in gm.enemyHandCards) if (c != null && c.Network == this) { found = c; break; }
        if (found == null) 
            foreach (var c in gm.playerFieldCards) if (c != null && c.Network == this) { found = c; break; }
        if (found == null) 
            foreach (var c in gm.enemyFieldCards) if (c != null && c.Network == this) { found = c; break; }
        if (found != null) 
        { 
            try { gm.playerHandCards.Remove(found); gm.enemyHandCards.Remove(found); gm.playerFieldCards.Remove(found); gm.enemyFieldCards.Remove(found); } catch { } try { Destroy(found.gameObject); } catch { } 
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void RequestPlaceCardServerRpc(bool isPlayerSide, int targetSlotIndex, RpcParams rpcParams = default)
    {
        if (!IsServer) 
            return;

        ulong sender = rpcParams.Receive.SenderClientId; if (ownerClientIdNet.Value != sender) 
            return;

        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.SanitizeLists();
            bool isHostCard = ownerClientIdNet.Value == NetworkManager.ServerClientId;
            int currentFieldCount = isHostCard ? gm.playerFieldCards.Count : gm.enemyFieldCards.Count;
            int maxField = DeckManager.MAX_FIELD_SIZE;
            if (currentFieldCount >= maxField) 
                return;

            isPlaced.Value = true;

            int currentAbilities = abilitiesNet.Value;
            bool hasCharge = (currentAbilities & (1 << (int)AbilityType.Charge)) != 0;

            canAttack.Value = hasCharge;
            placedOnTurn.Value = gm.CurrentTurn;
            fieldIndex.Value = targetSlotIndex;

            if (hasCharge)
                ForceCanAttackSyncClientRpc(true);

            gm.ReduceMana(isHostCard, manaCost.Value);
            if (visual != null)
            {
                if (isHostCard) { if (gm.playerHandCards.Contains(visual)) gm.playerHandCards.Remove(visual); if (!gm.playerFieldCards.Contains(visual)) { if (targetSlotIndex >= 0 && targetSlotIndex < gm.playerFieldCards.Count) gm.playerFieldCards.Insert(targetSlotIndex, visual); else gm.playerFieldCards.Add(visual); } }
                else { if (gm.enemyHandCards.Contains(visual)) gm.enemyHandCards.Remove(visual); if (!gm.enemyFieldCards.Contains(visual)) { if (targetSlotIndex >= 0 && targetSlotIndex < gm.enemyFieldCards.Count) gm.enemyFieldCards.Insert(targetSlotIndex, visual); else gm.enemyFieldCards.Add(visual); } }
                visual.transform.SetSiblingIndex(targetSlotIndex);
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void RequestAttackServerRpc(ulong targetNetObjId, RpcParams rpcParams = default)
    {
        if (!IsServer) 
            return;

        ulong sender = rpcParams.Receive.SenderClientId; 
        if (ownerClientIdNet.Value != sender) 
            return;

        if (!isPlaced.Value || !canAttack.Value) 
            return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetObjId, out var targetNetObj)) 
            return;

        var targetCardController = targetNetObj.GetComponentInChildren<CardController>();
        if (targetCardController != null && visual != null)
            GameManager.Instance.Attack(visual, targetCardController);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void RequestCastSpellServerRpc(int spellTypeValue, int spellTargetType, int spellPowerValue, ulong targetNetObjId, RpcParams rpcParams = default)
    {
        if (!IsServer) 
            return;

        ulong sender = rpcParams.Receive.SenderClientId; 
        if (ownerClientIdNet.Value != sender) 
            return;

        SpellType st = (SpellType)spellTypeValue; TargetType tt = (TargetType)spellTargetType;
        var gm = GameManager.Instance; if (gm != null) gm.SanitizeLists();
        if (gm != null) 
        { 
            bool isHostCard = ownerClientIdNet.Value == NetworkManager.ServerClientId; 
            gm.ReduceMana(isHostCard, manaCost.Value); 
        }
        ulong playerOwnerClientId = NetworkManager.ServerClientId; 
        var tm = FindFirstObjectByType<TurnManager>(); 
        if (tm != null && tm.PlayerOwner.Value != 0UL) 
            playerOwnerClientId = tm.PlayerOwner.Value;
        bool isPlayerSide = ownerClientIdNet.Value == playerOwnerClientId;
        switch (st)
        {
            case SpellType.GiveTempMana:
                if (gm != null && gm.currentGame != null) { if (isPlayerSide) gm.currentGame.player.AddTempMana(spellPowerValue); else gm.currentGame.enemy.AddTempMana(spellPowerValue); }
                if (gm != null) gm.UpdateStateNetworkIfServer();
                break;
            case SpellType.HealAlliesField: if (gm != null) { var list = isPlayerSide ? gm.playerFieldCards : gm.enemyFieldCards; foreach (var c in list) { if (c == null) continue; if (c.Network != null) c.Network.health.Value += spellPowerValue; else { c.self.health += spellPowerValue; c.Info?.UpdateStats(c.self); } } } break;
            case SpellType.DamageEnemiesField: if (gm != null) { var list = isPlayerSide ? new List<CardController>(gm.enemyFieldCards) : new List<CardController>(gm.playerFieldCards); foreach (var c in list) { if (c == null) continue; if (c.Network != null) { c.Network.health.Value = Mathf.Max(0, c.Network.health.Value - spellPowerValue); if (c.Network.health.Value <= 0) { var no = c.Network.GetComponent<NetworkObject>(); if (no != null) try { no.Despawn(true); } catch { } } } else { c.self.GetDamage(spellPowerValue); c.CheckForAlive(); } } } break;
            case SpellType.HealHero:
                if (gm != null && gm.currentGame != null)
                {
                    if (isPlayerSide) gm.currentGame.player.hp += spellPowerValue; else gm.currentGame.enemy.hp += spellPowerValue;
                    gm.UpdateStateNetworkIfServer();
                    UIManager.Instance?.UpdateHPAndMana();
                }
                break;
            case SpellType.DamageHero:
                if (gm != null && gm.currentGame != null)
                {
                    if (isPlayerSide) gm.currentGame.enemy.hp -= spellPowerValue; else gm.currentGame.player.hp -= spellPowerValue;
                    gm.UpdateStateNetworkIfServer();
                    UIManager.Instance?.UpdateHPAndMana(); gm.CheckForResult();
                }
                break;
            case SpellType.HealCard: if (tt == TargetType.AllyCard && targetNetObjId != 0) { if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetObjId, out var targetNetObj)) { var targetCardNetwork = targetNetObj.GetComponent<CardNetwork>(); if (targetCardNetwork != null) targetCardNetwork.health.Value += spellPowerValue; } } break;
            case SpellType.DamageCard: if (tt == TargetType.EnemyCard && targetNetObjId != 0) { if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetObjId, out var targetNetObj)) { var targetCardNetwork = targetNetObj.GetComponent<CardNetwork>(); if (targetCardNetwork != null) { targetCardNetwork.health.Value = Mathf.Max(0, targetCardNetwork.health.Value - spellPowerValue); if (targetCardNetwork.health.Value <= 0) try { targetNetObj.Despawn(true); } catch { } } } } break;
            case SpellType.AddShield: if (tt == TargetType.AllyCard && targetNetObjId != 0) { if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetObjId, out var tno)) { var tc = tno.GetComponentInChildren<CardController>(); var cn = tno.GetComponent<CardNetwork>(); if (tc != null) { if (!tc.self.abilities.Exists(x => x == AbilityType.Shield)) { tc.self.abilities.Add(AbilityType.Shield); if (cn != null) cn.abilitiesNet.Value = CardController.AbilitiesToInt(tc.self.abilities); } } } } break;
            case SpellType.AddTaunt: if (tt == TargetType.AllyCard && targetNetObjId != 0) { if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetObjId, out var tno2)) { var tc2 = tno2.GetComponentInChildren<CardController>(); var cn2 = tno2.GetComponent<CardNetwork>(); if (tc2 != null) { if (!tc2.self.abilities.Exists(x => x == AbilityType.Taunt)) { tc2.self.abilities.Add(AbilityType.Taunt); if (cn2 != null) cn2.abilitiesNet.Value = CardController.AbilitiesToInt(tc2.self.abilities); } } } } break;
            case SpellType.BuffAttack: if (tt == TargetType.AllyCard && targetNetObjId != 0) { if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetObjId, out var tno3)) { var tnet3 = tno3.GetComponent<CardNetwork>(); if (tnet3 != null) tnet3.attack.Value += spellPowerValue; } } break;
            case SpellType.DebuffAttack: if (tt == TargetType.EnemyCard && targetNetObjId != 0) { if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetObjId, out var tno4)) { var tnet4 = tno4.GetComponent<CardNetwork>(); if (tnet4 != null) tnet4.attack.Value = Mathf.Max(0, tnet4.attack.Value - spellPowerValue); } } break;
        }
        RemoveLocalCloneClientRpc();
        var casterNO = GetComponent<NetworkObject>();
        if (casterNO != null)
        {
            CardController found = null; var g = GameManager.Instance;
            if (g != null) { found = g.playerHandCards.Find(x => x.Network == this) ?? g.enemyHandCards.Find(x => x.Network == this) ?? g.playerFieldCards.Find(x => x.Network == this) ?? g.enemyFieldCards.Find(x => x.Network == this); if (found != null) { try { if (g.playerHandCards.Contains(found)) g.playerHandCards.Remove(found); if (g.enemyHandCards.Contains(found)) g.enemyHandCards.Remove(found); if (g.playerFieldCards.Contains(found)) g.playerFieldCards.Remove(found); if (g.enemyFieldCards.Contains(found)) g.enemyFieldCards.Remove(found); } catch { } try { Destroy(found.gameObject); } catch { } } }
            try { casterNO.Despawn(true); } catch { }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void RequestAttackHeroServerRpc(bool isAttackingEnemyHero, RpcParams rpcParams = default)
    {
        if (!IsServer) 
            return;

        if (rpcParams.Receive.SenderClientId != ownerClientIdNet.Value) 
            return;

        if (!canAttack.Value || !isPlaced.Value) 
            return;

        var gm = GameManager.Instance;
        if (gm != null)
        {
            CardController attackerCard = null;
            if (gm.playerFieldCards.Exists(x => x.Network == this)) 
                attackerCard = gm.playerFieldCards.Find(x => x.Network == this);
            else if (gm.enemyFieldCards.Exists(x => x.Network == this)) 
                attackerCard = gm.enemyFieldCards.Find(x => x.Network == this);

            if (attackerCard != null)
            {
                bool isClientOwner = ownerClientIdNet.Value != NetworkManager.ServerClientId;
                bool finalTargetIsEnemy = isAttackingEnemyHero;
                if (isClientOwner) 
                    finalTargetIsEnemy = !isAttackingEnemyHero;

                gm.AttackHero(attackerCard, finalTargetIsEnemy);
            }
        }
    }
}