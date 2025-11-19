using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class TurnManager : NetworkBehaviour
{
    [Header("Turn settings")]
    [Tooltip("Seconds per turn on server")]
    public int turnTimeDefault = 30;

    public NetworkVariable<ulong> CurrentTurnOwner = new NetworkVariable<ulong>(
        0UL,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> TurnTimeRemaining = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Coroutine serverTurnCoroutine;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        TurnTimeRemaining.Value = 0;
        Debug.Log($"[TurnManager] OnNetworkSpawn. IsServer={IsServer}, IsClient={IsClient}. CurrentOwner={CurrentTurnOwner.Value}");
    }

    public void SetCurrentTurnOwner(ulong ownerClientId)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[TurnManager] SetCurrentTurnOwner called on non-server. Ignored.");
            return;
        }

        CurrentTurnOwner.Value = ownerClientId;
        Debug.Log($"[TurnManager] CurrentTurnOwner set to {ownerClientId}");
    }

    public void StartServerTurnLoop()
    {
        if (!IsServer)
        {
            Debug.LogWarning("[TurnManager] StartServerTurnLoop called on non-server. Ignored.");
            return;
        }

        if (serverTurnCoroutine != null)
            StopCoroutine(serverTurnCoroutine);

        serverTurnCoroutine = StartCoroutine(ServerTurnLoop());
        Debug.Log("[TurnManager] Server turn loop started.");
    }

    public void StopServerTurnLoop()
    {
        if (!IsServer) return;

        if (serverTurnCoroutine != null)
        {
            StopCoroutine(serverTurnCoroutine);
            serverTurnCoroutine = null;
        }

        TurnTimeRemaining.Value = 0;
        Debug.Log("[TurnManager] Server turn loop stopped.");
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

            Debug.Log("[TurnManager] Turn time expired on server -> calling GameManager.ChangeTurn()");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ChangeTurn();
            }
            else
            {
                Debug.LogWarning("[TurnManager] GameManager.Instance is null when trying to ChangeTurn.");
            }

            yield return null;
        }
    }

    public ulong GetOtherClientIdOrServerFallback()
    {
        if (NetworkManager.Singleton == null)
            return NetworkManager.ServerClientId;

        foreach (var kv in NetworkManager.Singleton.ConnectedClients)
        {
            var clientId = kv.Key;
            if (clientId != NetworkManager.ServerClientId)
                return clientId;
        }

        return NetworkManager.ServerClientId;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestEndTurnServerRpc(RpcParams rpcParams = default)
    {
        if (!IsServer)
            return;

        ulong sender = rpcParams.Receive.SenderClientId;
        Debug.Log($"[TurnManager] RequestEndTurnServerRpc received from {sender}. CurrentOwner={CurrentTurnOwner.Value}");

        if (sender != CurrentTurnOwner.Value)
        {
            Debug.LogWarning($"[TurnManager] RequestEndTurnServerRpc ignored from {sender} — not current owner.");
            return;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.ChangeTurn();
        else
            Debug.LogWarning("[TurnManager] GameManager.Instance is null — can't ChangeTurn()");
    }
}
