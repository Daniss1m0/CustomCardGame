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

    private CardController visual;

    private void Awake()
    {
        visual = GetComponentInChildren<CardController>();
    }

    public override void OnNetworkSpawn()
    {
        attack.OnValueChanged += OnAttackChanged;
        health.OnValueChanged += OnHealthChanged;
        manaCost.OnValueChanged += OnManaChanged;
        isPlaced.OnValueChanged += OnPlacedChangedHandler;
        canAttack.OnValueChanged += OnCanAttackChanged;
        ownerClientIdNet.OnValueChanged += OnOwnerChanged;
        cardDataIndex.OnValueChanged += OnCardDataIndexChanged;

        UpdateVisual();
        UpdateOwnership();
        UpdateHighlight();
        OnPlacedChanged();
    }

    public override void OnNetworkDespawn()
    {
        attack.OnValueChanged -= OnAttackChanged;
        health.OnValueChanged -= OnHealthChanged;
        manaCost.OnValueChanged -= OnManaChanged;
        isPlaced.OnValueChanged -= OnPlacedChangedHandler;
        canAttack.OnValueChanged -= OnCanAttackChanged;
        ownerClientIdNet.OnValueChanged -= OnOwnerChanged;
        cardDataIndex.OnValueChanged -= OnCardDataIndexChanged;
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
    }

    private void UpdateOwnership()
    {
        if (visual == null)
            return;

        bool isMine = ownerClientIdNet.Value == NetworkManager.Singleton.LocalClientId;
        visual.OnNetworkOwnershipChanged(isMine);
    }

    private void UpdateHighlight()
    {
        if (visual == null)
            return;

        visual.SetCanAttackVisual(canAttack.Value);
    }

    [ClientRpc]
    public void CreateLocalCloneClientRpc(int cardDataIndexValue, ulong ownerClientId, int attackValue, int healthValue, int manaCostValue, bool isSpellValue, ClientRpcParams clientRpcParams = default)
    {
        var dm = FindFirstObjectByType<DeckManager>();
        if (dm == null)
        {
            Debug.LogWarning("DeckManager not found.");
            return;
        }

        Transform hand = ownerClientId == (NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL) ? dm.PlayerHand : dm.EnemyHand;

        if (hand == null)
        {
            Debug.LogWarning("Hand transform is null.");
            return;
        }

        var visualPrefab = dm.VisualCardPrefab;
        if (visualPrefab == null)
        {
            Debug.LogWarning("visualCardPrefab not assigned in DeckManager.");
            return;
        }

        GameObject uiClone = null;
        try
        {
            uiClone = Instantiate(visualPrefab, hand, false);
            uiClone.SetActive(true);

            var rtRoot = uiClone.GetComponent<RectTransform>(); //?
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

            attack.OnValueChanged += (oldV, newV) =>
            {
                if (uiClone == null) 
                    return;

                var ctrl = uiClone.GetComponentInChildren<CardController>();
                if (ctrl != null && ctrl.self != null) 
                { 
                    ctrl.self.attack = newV; 
                    ctrl.Info?.UpdateStats(ctrl.self); 
                }
            };

            health.OnValueChanged += (oldV, newV) =>
            {
                if (uiClone == null) 
                    return;

                var ctrl = uiClone.GetComponentInChildren<CardController>();
                if (ctrl != null && ctrl.self != null) 
                { 
                    ctrl.self.health = newV; 
                    ctrl.Info?.UpdateStats(ctrl.self); 
                }
            };

            manaCost.OnValueChanged += (oldV, newV) =>
            {
                if (uiClone == null) 
                    return;

                var ctrl = uiClone.GetComponentInChildren<CardController>();
                if (ctrl != null && ctrl.self != null) 
                { 
                    ctrl.self.manaCost = newV; 
                    ctrl.Info?.UpdateStats(ctrl.self); 
                }
            };

            canAttack.OnValueChanged += (oldV, newV) =>
            {
                if (uiClone == null) 
                    return;

                var ctrl = uiClone.GetComponentInChildren<CardController>();
                if (ctrl != null) 
                    ctrl.SetCanAttackVisual(newV);
            };

            ownerClientIdNet.OnValueChanged += (oldV, newV) =>
            {
                var ctrl = uiClone != null ? uiClone.GetComponentInChildren<CardController>() : null;
                if (ctrl == null) 
                    return;

                bool nowOwner = NetworkManager.Singleton != null && newV == NetworkManager.Singleton.LocalClientId;
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

            isPlaced.OnValueChanged += (oldV, newV) =>
            {
                var ctrl = uiClone != null ? uiClone.GetComponentInChildren<CardController>() : null;
                if (ctrl == null) 
                    return;

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
            if (uiClone != null) 
                Destroy(uiClone);
        }
    }

}