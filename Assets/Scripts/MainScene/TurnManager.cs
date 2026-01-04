using System.Collections;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class TurnManager : NetworkBehaviour
{
    public int turnTimeDefault = 30;

    public NetworkVariable<ulong> currentTurnOwner = new(0UL, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> turnTimeRemaining = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> playerMana = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> enemyMana = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> playerHP = new(30, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> enemyHP = new(30, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<ulong> playerOwner = new(0UL, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> playerDeckCount = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> enemyDeckCount = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> restartVotes = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Coroutine serverTurnCoroutine;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
            turnTimeRemaining.Value = 0;

        turnTimeRemaining.OnValueChanged += OnTurnTimeRemainingChanged;
        playerMana.OnValueChanged += OnPlayerManaChanged;
        enemyMana.OnValueChanged += OnEnemyManaChanged;
        playerOwner.OnValueChanged += OnPlayerOwnerChanged;

        playerHP.OnValueChanged += (oldV, newV) => { };
        enemyHP.OnValueChanged += (oldV, newV) => { };

        playerDeckCount.OnValueChanged += (oldV, newV) => UpdateDeckVisuals();
        enemyDeckCount.OnValueChanged += (oldV, newV) => UpdateDeckVisuals();

        restartVotes.OnValueChanged += (oldV, newV) =>
        {
            if (UIManager.Instance != null)
                UIManager.Instance.UpdateRestartText(newV);
        };
    }

    public override void OnNetworkDespawn()
    {
        turnTimeRemaining.OnValueChanged -= OnTurnTimeRemainingChanged;
        playerMana.OnValueChanged -= OnPlayerManaChanged;
        enemyMana.OnValueChanged -= OnEnemyManaChanged;
        playerOwner.OnValueChanged -= OnPlayerOwnerChanged;

        playerDeckCount.OnValueChanged -= (oldV, newV) => UpdateDeckVisuals();
        enemyDeckCount.OnValueChanged -= (oldV, newV) => UpdateDeckVisuals();

        base.OnNetworkDespawn();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestRestartVoteServerRpc()
    {
        restartVotes.Value++;

        if (restartVotes.Value >= 2)
            PerformFullRestart();
    }

    private void PerformFullRestart()
    {
        restartVotes.Value = 0;
        StopServerTurnLoop();

        if (GameManager.Instance != null)
            GameManager.Instance.CleanupNetworkCards();

        TriggerRestartClientRpc();

        if (GameManager.Instance != null)
            GameManager.Instance.StartGame();
    }

    [ClientRpc]
    private void TriggerRestartClientRpc()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ResetClientState();
    }

    private void OnTurnTimeRemainingChanged(int oldV, int newV) { }
    private void OnPlayerManaChanged(int oldV, int newV) { }
    private void OnEnemyManaChanged(int oldV, int newV) { }
    private void OnPlayerOwnerChanged(ulong oldV, ulong newV) { }

    private void UpdateDeckVisuals()
    {
        var dm = FindFirstObjectByType<DeckManager>();
        if (dm != null)
            dm.UpdateDeckVisualsFromNetwork(playerDeckCount.Value, enemyDeckCount.Value);
    }

    public void SetDeckCounts(int playerCount, int enemyCount)
    {
        if (!IsServer) 
            return;

        playerDeckCount.Value = playerCount;
        enemyDeckCount.Value = enemyCount;
        UpdateDeckVisuals();
    }

    public void SetCurrentTurnOwner(ulong ownerClientId) { if (!IsServer) return; currentTurnOwner.Value = ownerClientId; }
    public void SetPlayerManaServer(int value) { if (!IsServer) return; playerMana.Value = value; }
    public void SetEnemyManaServer(int value) { if (!IsServer) return; enemyMana.Value = value; }
    public void SetPlayerHPServer(int value) { if (!IsServer) return; playerHP.Value = value; }
    public void SetEnemyHPServer(int value) { if (!IsServer) return; enemyHP.Value = value; }
    public void SetPlayerOwnerServer(ulong ownerClientId) { if (!IsServer) return; playerOwner.Value = ownerClientId; }

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
        turnTimeRemaining.Value = 0;
    }

    private IEnumerator ServerTurnLoop()
    {
        while (true)
        {
            turnTimeRemaining.Value = Mathf.Max(0, turnTimeDefault);
            while (turnTimeRemaining.Value > 0)
            {
                yield return new WaitForSeconds(1f);
                turnTimeRemaining.Value = Mathf.Max(0, turnTimeRemaining.Value - 1);
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
        if (sender != currentTurnOwner.Value) 
            return;

        if (GameManager.Instance != null) 
            GameManager.Instance.ChangeTurn();
    }

    [ClientRpc]
    public void NotifyClientsOwnerClientRpc(ulong ownerClientId)
    {
        bool amOwner = NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == ownerClientId;
        if (UIManager.Instance != null) 
            UIManager.Instance.SetEndTurnInteractable(amOwner);

        GameManager.Instance?.CheckCardsForManaAvailability();
    }
}