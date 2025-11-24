using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class CardNetwork : NetworkBehaviour
{
    public NetworkVariable<int> Attack = new(0, NetworkVariableReadPermission.Everyone);
    public NetworkVariable<int> Health = new(1, NetworkVariableReadPermission.Everyone);
    public NetworkVariable<int> ManaCost = new(0, NetworkVariableReadPermission.Everyone);
    public NetworkVariable<bool> IsSpell = new(false, NetworkVariableReadPermission.Everyone);
    public NetworkVariable<bool> IsPlaced = new(false, NetworkVariableReadPermission.Everyone);
    public NetworkVariable<bool> CanAttack = new(false, NetworkVariableReadPermission.Everyone);
    public NetworkVariable<ulong> OwnerClientIdNet = new(0UL, NetworkVariableReadPermission.Everyone);
    public NetworkVariable<int> CardDataIndex = new(-1, NetworkVariableReadPermission.Everyone);

    private CardController visual;

    private void Awake()
    {
        visual = GetComponentInChildren<CardController>();
    }

    public override void OnNetworkSpawn()
    {
        Attack.OnValueChanged += OnAttackChanged;
        Health.OnValueChanged += OnHealthChanged;
        ManaCost.OnValueChanged += OnManaChanged;
        IsPlaced.OnValueChanged += OnPlacedChangedHandler;
        CanAttack.OnValueChanged += OnCanAttackChanged;
        OwnerClientIdNet.OnValueChanged += OnOwnerChanged;
        CardDataIndex.OnValueChanged += OnCardDataIndexChanged;

        UpdateVisual();
        UpdateOwnership();
        UpdateHighlight();
        OnPlacedChanged();
    }

    public override void OnNetworkDespawn()
    {
        Attack.OnValueChanged -= OnAttackChanged;
        Health.OnValueChanged -= OnHealthChanged;
        ManaCost.OnValueChanged -= OnManaChanged;
        IsPlaced.OnValueChanged -= OnPlacedChangedHandler;
        CanAttack.OnValueChanged -= OnCanAttackChanged;
        OwnerClientIdNet.OnValueChanged -= OnOwnerChanged;
        CardDataIndex.OnValueChanged -= OnCardDataIndexChanged;
    }

    private void OnAttackChanged(int oldV, int newV) => UpdateVisual();
    private void OnHealthChanged(int oldV, int newV) => UpdateVisual();
    private void OnManaChanged(int oldV, int newV) => UpdateVisual();
    private void OnCanAttackChanged(bool oldV, bool newV) => UpdateHighlight();
    private void OnOwnerChanged(ulong oldV, ulong newV) => UpdateOwnership();
    private void OnCardDataIndexChanged(int oldV, int newV) => UpdateVisual();
    private void OnPlacedChangedHandler(bool oldV, bool newV) => OnPlacedChanged();
    
    private void OnPlacedChanged()
    {
        if (visual == null)
            return;

        if (IsPlaced.Value)
            visual.OnPlacedNetworkSide(OwnerClientIdNet.Value);
        else
            visual.OnUnplacedNetworkSide(OwnerClientIdNet.Value);
    }
    private void UpdateVisual()
    {
        if (visual == null)
            return;

        visual.SetNetworkData(Attack.Value, Health.Value, ManaCost.Value, IsSpell.Value, CardDataIndex.Value, OwnerClientIdNet.Value);
    }

    private void UpdateOwnership()
    {
        if (visual == null)
            return;

        bool isMine = OwnerClientIdNet.Value == NetworkManager.Singleton.LocalClientId;
        visual.OnNetworkOwnershipChanged(isMine);
    }

    private void UpdateHighlight()
    {
        if (visual == null)
            return;

        visual.SetCanAttackVisual(CanAttack.Value);
    }

    [ClientRpc]
    public void CreateLocalCloneClientRpc(int cardDataIndex, ulong ownerClientId, int attack, int health, int manaCost, bool isSpell, ClientRpcParams clientRpcParams = default)
    {
        var dm = FindFirstObjectByType<DeckManager>();
        if (dm == null)
        {
            Debug.LogWarning("DeckManager not found on client when creating local clone.");
            return;
        }

        Transform hand = ownerClientId == (NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL)
                          ? dm.PlayerHand
                          : dm.EnemyHand;

        if (hand == null)
        {
            Debug.LogWarning("Hand transform is null for local clone. owner=" + ownerClientId + " local=" + (NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL));
            return;
        }

        var visualPrefab = dm.VisualCardPrefab;
        if (visualPrefab == null)
        {
            Debug.LogWarning("visualCardPrefab not assigned in DeckManager on client.");
            return;
        }

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
                if (rtRoot.sizeDelta == Vector2.zero) rtRoot.sizeDelta = new Vector2(176f, 230f);
                rtRoot.anchoredPosition = Vector2.zero;
                rtRoot.localScale = Vector3.one;
            }

            var cloneController = uiClone.GetComponent<CardController>() ?? uiClone.GetComponentInChildren<CardController>(true);
            bool isOwner = NetworkManager.Singleton != null && ownerClientId == NetworkManager.Singleton.LocalClientId;

            if (cloneController != null)
            {
                cloneController.SetNetworkData(attack, health, manaCost, isSpell, cardDataIndex, ownerClientId);

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

            Attack.OnValueChanged += (oldV, newV) =>
            {
                if (uiClone == null) return;
                var ctrl = uiClone.GetComponentInChildren<CardController>();
                if (ctrl != null && ctrl.self != null) { ctrl.self.attack = newV; ctrl.Info?.UpdateStats(ctrl.self); }
            };

            Health.OnValueChanged += (oldV, newV) =>
            {
                if (uiClone == null) return;
                var ctrl = uiClone.GetComponentInChildren<CardController>();
                if (ctrl != null && ctrl.self != null) { ctrl.self.health = newV; ctrl.Info?.UpdateStats(ctrl.self); }
            };

            ManaCost.OnValueChanged += (oldV, newV) =>
            {
                if (uiClone == null) return;
                var ctrl = uiClone.GetComponentInChildren<CardController>();
                if (ctrl != null && ctrl.self != null) { ctrl.self.manaCost = newV; ctrl.Info?.UpdateStats(ctrl.self); }
            };

            CanAttack.OnValueChanged += (oldV, newV) =>
            {
                if (uiClone == null) return;
                var ctrl = uiClone.GetComponentInChildren<CardController>();
                if (ctrl != null) ctrl.SetCanAttackVisual(newV);
            };

            OwnerClientIdNet.OnValueChanged += (oldV, newV) =>
            {
                var ctrl = uiClone != null ? uiClone.GetComponentInChildren<CardController>() : null;
                if (ctrl == null) return;
                bool nowOwner = NetworkManager.Singleton != null && newV == NetworkManager.Singleton.LocalClientId;
                ctrl.isPlayerCard = nowOwner;
                ctrl.OnNetworkOwnershipChanged(nowOwner);
                if (!ctrl.self.isPlaced)
                {
                    if (nowOwner) ctrl.Info?.ShowCard(ctrl.self);
                    else ctrl.Info?.HideCard();
                }
                else
                    ctrl.Info?.ShowCard(ctrl.self);
            };

            IsPlaced.OnValueChanged += (oldV, newV) =>
            {
                var ctrl = uiClone != null ? uiClone.GetComponentInChildren<CardController>() : null;
                if (ctrl == null) return;
                ctrl.self.isPlaced = newV;
                ctrl.Info?.ShowCard(ctrl.self);
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
        catch (System.Exception ex)
        {
            Debug.LogError("Failed: " + ex);
            if (uiClone != null) Destroy(uiClone);
        }
    }
}