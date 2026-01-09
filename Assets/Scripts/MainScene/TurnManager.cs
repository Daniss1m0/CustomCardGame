using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class TurnManager : NetworkBehaviour
{
    public int turnTimeDefault = 30;
    public NetworkVariable<int> turnTimeRemaining = new(0), playerMana = new(0), enemyMana = new(0), playerHP = new(30), enemyHP = new(30), playerDeckCount = new(0), enemyDeckCount = new(0), restartVotes = new(0),  readyPlayersCount = new(0);
    public NetworkVariable<ulong> currentTurnOwner = new(0UL), playerOwner = new(0UL);

    private HashSet<ulong> readyPlayerIds = new();
    private Coroutine serverTurnCoroutine;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            turnTimeRemaining.Value = 0;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        }

        playerDeckCount.OnValueChanged += (oldV, newV) => UpdateDeckVisuals();
        enemyDeckCount.OnValueChanged += (oldV, newV) => UpdateDeckVisuals();

        readyPlayersCount.OnValueChanged += (oldV, newV) =>
        {
            if (newV >= 2)
                GameManager.Instance.StartGame();

            UIManager.Instance.UpdateReadyStatus(newV);
        };

        restartVotes.OnValueChanged += (oldV, newV) =>
        {
            UIManager.Instance.UpdateRestartText(newV);
        };
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;

        turnTimeRemaining.OnValueChanged -= OnTurnTimeRemainingChanged;
        playerMana.OnValueChanged -= OnPlayerManaChanged;
        enemyMana.OnValueChanged -= OnEnemyManaChanged;
        playerOwner.OnValueChanged -= OnPlayerOwnerChanged;

        playerDeckCount.OnValueChanged -= (oldV, newV) => UpdateDeckVisuals();
        enemyDeckCount.OnValueChanged -= (oldV, newV) => UpdateDeckVisuals();
    }

    private void OnClientDisconnect(ulong clientId)
    {
        OnClientDisconnectPublic(clientId);
    }

    public void OnClientDisconnectPublic(ulong clientId)
    {
        if (!IsServer) 
            return;

        if (readyPlayerIds.Contains(clientId))
        {
            readyPlayerIds.Remove(clientId);
            readyPlayersCount.Value = readyPlayerIds.Count;
        }
    }

    private void PerformFullRestart()
    {
        restartVotes.Value = 0;
        readyPlayerIds.Clear();
        readyPlayersCount.Value = 0;
        StopServerTurnLoop();

        GameManager.Instance.CleanupNetworkCards();

        TriggerRestartClientRpc();

        GameManager.Instance.StartGame();
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

    public void SetCurrentTurnOwner(ulong ownerClientId) 
    { 
        if (!IsServer)
            return;

        currentTurnOwner.Value = ownerClientId;
    }

    public void SetPlayerManaServer(int value) 
    { 
        if (!IsServer)
            return;

        playerMana.Value = value;
    }

    public void SetEnemyManaServer(int value) 
    { 
        if (!IsServer)
            return;

        enemyMana.Value = value;
    }

    public void SetPlayerHPServer(int value) 
    {
        if (!IsServer)
            return;
        
        playerHP.Value = value;
    }

    public void SetEnemyHPServer(int value) 
    {
        if (!IsServer)
            return;

        enemyHP.Value = value;
    }

    public void SetPlayerOwnerServer(ulong ownerClientId) 
    {
        if (!IsServer)
            return;
        
        playerOwner.Value = ownerClientId;
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

            GameManager.Instance.ChangeTurn();

            yield return null;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PlayerReadyServerRpc(RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (!readyPlayerIds.Contains(senderId))
        {
            readyPlayerIds.Add(senderId);
            readyPlayersCount.Value = readyPlayerIds.Count;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestRestartVoteServerRpc()
    {
        restartVotes.Value++;

        if (restartVotes.Value >= 2)
            PerformFullRestart();
    }

    [ClientRpc]
    private void TriggerRestartClientRpc()
    {
        GameManager.Instance.ResetClientState();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestEndTurnServerRpc(RpcParams rpcParams = default)
    {
        if (!IsServer)
            return;

        ulong sender = rpcParams.Receive.SenderClientId;
        if (sender != currentTurnOwner.Value)
            return;

        GameManager.Instance.ChangeTurn();
    }

    [ClientRpc]
    public void NotifyClientsOwnerClientRpc(ulong ownerClientId)
    {
        bool amOwner = NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == ownerClientId;
        
        UIManager.Instance.SetEndTurnInteractable(amOwner);

        GameManager.Instance.CheckCardsForManaAvailability();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PlayerSurrenderServerRpc(RpcParams rpcParams = default)
    {
        if (!IsServer)
            return;

        ulong senderId = rpcParams.Receive.SenderClientId;

        GameManager.Instance.HandleSurrenderServer(senderId);
    }
}