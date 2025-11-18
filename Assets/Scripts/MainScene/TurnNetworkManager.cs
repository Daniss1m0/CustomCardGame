using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class TurnNetworkManager : NetworkBehaviour
{
    public static TurnNetworkManager Instance { get; private set; }

    public NetworkVariable<ulong> CurrentTurnOwner = new NetworkVariable<ulong>(
        0UL,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake() => Instance = this;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"[TurnNetworkManager] OnNetworkSpawn. IsServer={IsServer}, IsClient={IsClient}. CurrentTurnOwner={CurrentTurnOwner.Value}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestEndTurnServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        Debug.Log($"[TurnNetworkManager] RequestEndTurnServerRpc received from {sender}. CurrentTurnOwner = {CurrentTurnOwner.Value}");

        if (sender != CurrentTurnOwner.Value)
        {
            Debug.LogWarning($"[TurnNetworkManager] EndTurn request from {sender} ignored. Expected {CurrentTurnOwner.Value}.");
            return;
        }

        Debug.Log($"[TurnNetworkManager] EndTurn request accepted from {sender}. Executing ChangeTurn on server.");
        GameManager.Instance.ChangeTurn();

        ulong newOwner = GameManager.Instance.IsPlayerTurn ? NetworkManager.ServerClientId : GetOtherClientIdOrServerFallback();
        CurrentTurnOwner.Value = newOwner;

        NotifyClientsChangeTurnClientRpc(newOwner);
    }

    [ClientRpc]
    private void NotifyClientsChangeTurnClientRpc(ulong newOwnerClientId, ClientRpcParams clientRpcParams = default)
    {
        if (IsServer) return;
        Debug.Log($"[TurnNetworkManager] Client received ChangeTurn ClientRpc. NewOwner={newOwnerClientId}");
        GameManager.Instance.ChangeTurn();
    }

    private ulong GetOtherClientIdOrServerFallback()
    {
        if (NetworkManager.Singleton == null)
            return NetworkManager.ServerClientId;

        foreach (var kv in NetworkManager.Singleton.ConnectedClients)
        {
            var clientId = kv.Key;
            if (clientId != NetworkManager.Singleton.LocalClientId)
                return clientId;
        }

        return NetworkManager.ServerClientId;
    }
}
