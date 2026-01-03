using System.Collections;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class TurnManager : NetworkBehaviour
{
    public int turnTimeDefault = 30;

    public NetworkVariable<ulong> CurrentTurnOwner = new(0UL, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> TurnTimeRemaining = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> PlayerMana = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> EnemyMana = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> PlayerHP = new(30, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> EnemyHP = new(30, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<ulong> PlayerOwner = new(0UL, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> PlayerDeckCount = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> EnemyDeckCount = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Coroutine serverTurnCoroutine;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
            TurnTimeRemaining.Value = 0;

        TurnTimeRemaining.OnValueChanged += OnTurnTimeRemainingChanged;
        PlayerMana.OnValueChanged += OnPlayerManaChanged;
        EnemyMana.OnValueChanged += OnEnemyManaChanged;
        PlayerOwner.OnValueChanged += OnPlayerOwnerChanged;

        PlayerHP.OnValueChanged += (oldV, newV) => { };
        EnemyHP.OnValueChanged += (oldV, newV) => { };

        PlayerDeckCount.OnValueChanged += (oldV, newV) => UpdateDeckVisuals();
        EnemyDeckCount.OnValueChanged += (oldV, newV) => UpdateDeckVisuals();
    }

    public override void OnNetworkDespawn()
    {
        TurnTimeRemaining.OnValueChanged -= OnTurnTimeRemainingChanged;
        PlayerMana.OnValueChanged -= OnPlayerManaChanged;
        EnemyMana.OnValueChanged -= OnEnemyManaChanged;
        PlayerOwner.OnValueChanged -= OnPlayerOwnerChanged;

        PlayerDeckCount.OnValueChanged -= (oldV, newV) => UpdateDeckVisuals();
        EnemyDeckCount.OnValueChanged -= (oldV, newV) => UpdateDeckVisuals();

        base.OnNetworkDespawn();
    }

    private void OnTurnTimeRemainingChanged(int oldV, int newV) { }
    private void OnPlayerManaChanged(int oldV, int newV) { }
    private void OnEnemyManaChanged(int oldV, int newV) { }
    private void OnPlayerOwnerChanged(ulong oldV, ulong newV) { }

    private void UpdateDeckVisuals()
    {
        var dm = FindFirstObjectByType<DeckManager>();
        if (dm != null)
            dm.UpdateDeckVisualsFromNetwork(PlayerDeckCount.Value, EnemyDeckCount.Value);
    }

    public void SetDeckCounts(int playerCount, int enemyCount)
    {
        if (!IsServer)
            return;

        PlayerDeckCount.Value = playerCount;
        EnemyDeckCount.Value = enemyCount;

        UpdateDeckVisuals();
    }

    public void SetCurrentTurnOwner(ulong ownerClientId)
    {
        if (!IsServer) 
            return;

        CurrentTurnOwner.Value = ownerClientId;
    }

    public void SetPlayerManaServer(int value)
    {
        if (!IsServer) 
            return;

        PlayerMana.Value = value;
    }

    public void SetEnemyManaServer(int value)
    {
        if (!IsServer)
            return;

        EnemyMana.Value = value;
    }

    public void SetPlayerHPServer(int value)
    {
        if (!IsServer) 
            return;

        PlayerHP.Value = value;
    }

    public void SetEnemyHPServer(int value)
    {
        if (!IsServer) 
            return;

        EnemyHP.Value = value;
    }

    public void SetPlayerOwnerServer(ulong ownerClientId)
    {
        if (!IsServer) 
            return;

        PlayerOwner.Value = ownerClientId;
    }

    public void StartServerTurnLoop()
    {
        if (!IsServer) 
            return;

        if (serverTurnCoroutine != null) 
            StopCoroutine(serverTurnCoroutine);

        serverTurnCoroutine = StartCoroutine(ServerTurnLoop());
    }

    public void StopServerTurnLoop()
    {
        if (!IsServer) 
            return;

        if (serverTurnCoroutine != null)
        {
            StopCoroutine(serverTurnCoroutine);
            serverTurnCoroutine = null;
        }
        TurnTimeRemaining.Value = 0;
    }

    private IEnumerator ServerTurnLoop()
    {
        while (true)
        {
            TurnTimeRemaining.Value = Mathf.Max(0, turnTimeDefault);
            while (TurnTimeRemaining.Value > 0)
            {
                yield return new WaitForSeconds(1f);
                TurnTimeRemaining.Value = Mathf.Max(0, TurnTimeRemaining.Value - 1);
            }

            if (GameManager.Instance != null)
                GameManager.Instance.ChangeTurn();

            yield return null;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestEndTurnServerRpc(RpcParams rpcParams = default)
    {
        if (!IsServer) 
            return;

        ulong sender = rpcParams.Receive.SenderClientId;
        if (sender != CurrentTurnOwner.Value) 
            return;

        if (GameManager.Instance != null) 
            GameManager.Instance.ChangeTurn();
    }

    [ClientRpc]
    public void NotifyClientsOwnerClientRpc(ulong ownerClientId, ClientRpcParams clientRpcParams = default)
    {
        bool amOwner = NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == ownerClientId;
        if (UIManager.Instance != null)
            UIManager.Instance.SetEndTurnInteractable(amOwner);

        GameManager.Instance.CheckCardsForManaAvailability();
    }
}