using UnityEngine;
using Unity.Netcode;
using System.Collections;

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
    public NetworkVariable<int> spellType = new((int)SpellType.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); //?
    public NetworkVariable<int> spellTarget = new((int)TargetType.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> spellPower = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private CardController visual;

    private void Awake()
    {
        visual = GetComponentInChildren<CardController>();
    }

    public override void OnNetworkSpawn()
    {
        attack.OnValueChanged += (o, n) => UpdateVisual();
        health.OnValueChanged += (o, n) => UpdateVisual();
        manaCost.OnValueChanged += (o, n) => UpdateVisual();
        isPlaced.OnValueChanged += (o, n) => OnPlacedChanged();
        canAttack.OnValueChanged += (o, n) => UpdateHighlight();
        ownerClientIdNet.OnValueChanged += (o, n) => UpdateOwnership();
        cardDataIndex.OnValueChanged += (o, n) => UpdateVisual();
        spellType.OnValueChanged += (o, n) => ApplySpellFields();
        spellTarget.OnValueChanged += (o, n) => ApplySpellFields();
        spellPower.OnValueChanged += (o, n) => ApplySpellFields();

        UpdateVisual();
        UpdateOwnership();
        UpdateHighlight();
        OnPlacedChanged();
    }

    public override void OnNetworkDespawn()
    {
        attack.OnValueChanged -= (o, n) => UpdateVisual();
        health.OnValueChanged -= (o, n) => UpdateVisual();
        manaCost.OnValueChanged -= (o, n) => UpdateVisual();
        isPlaced.OnValueChanged -= (o, n) => OnPlacedChanged();
        canAttack.OnValueChanged -= (o, n) => UpdateHighlight();
        ownerClientIdNet.OnValueChanged -= (o, n) => UpdateOwnership();
        cardDataIndex.OnValueChanged -= (o, n) => UpdateVisual();
        spellType.OnValueChanged -= (o, n) => ApplySpellFields();
        spellTarget.OnValueChanged -= (o, n) => ApplySpellFields();
        spellPower.OnValueChanged -= (o, n) => ApplySpellFields();
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

                        if (foundData == null && special != null && special.sprite != null)
                            cloneController.self.logo = special.sprite;
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
                }
                var canvasGroup = uiClone.GetComponent<CanvasGroup>() ?? uiClone.AddComponent<CanvasGroup>();
                canvasGroup.blocksRaycasts = isOwner;
                canvasGroup.interactable = isOwner;
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
                    ctrl.SetCanAttackVisual(n);
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
        }
        catch
        {
            if (uiClone != null)
                Destroy(uiClone);
        }
    }
}
