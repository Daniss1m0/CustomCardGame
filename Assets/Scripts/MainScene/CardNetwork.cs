using Unity.Netcode;
using UnityEngine;

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
        visual = GetComponent<CardController>();
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

    private void UpdateVisual()
    {
        if (visual == null)
            return;
        //visual.SetNetworkData(Attack.Value, Health.Value, ManaCost.Value, IsSpell.Value, CardDataIndex.Value, OwnerClientIdNet.Value);
    }

    private void UpdateOwnership()
    {
        if (visual == null)
            return;
        bool isMine = OwnerClientIdNet.Value == NetworkManager.Singleton.LocalClientId;
       // visual.OnNetworkOwnershipChanged(isMine);
    }

    private void UpdateHighlight()
    {
        if (visual == null)
            return;
       // visual.SetCanAttackVisual(CanAttack.Value);
    }

    private void OnPlacedChanged()
    {
        if (visual == null)
            return;

        //if (IsPlaced.Value)
          //  visual.OnPlacedNetworkSide(OwnerClientIdNet.Value);
       // else
           // visual.OnUnplacedNetworkSide(OwnerClientIdNet.Value);
    }
}