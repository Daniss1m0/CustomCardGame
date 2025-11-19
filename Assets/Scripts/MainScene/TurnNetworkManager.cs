using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class TurnNetworkManager : NetworkBehaviour
{
    public static TurnNetworkManager Instance { get; private set; }

    [Header("Turn settings")]
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

    private void Awake() => Instance = this;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        TurnTimeRemaining.Value = 0;
    }

    public void StartServerTurnLoop()
    {
        if (!IsServer)
        {
            Debug.LogWarning("[TurnNetworkManager] StartServerTurnLoop called on non-server. Ignored.");
            return;
        }

        if (serverTurnCoroutine != null)
            StopCoroutine(serverTurnCoroutine);

        serverTurnCoroutine = StartCoroutine(ServerTurnLoop());
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
    }

    private IEnumerator ServerTurnLoop()
    {
        while (true)
        {
            TurnTimeRemaining.Value = turnTimeDefault;

            while (TurnTimeRemaining.Value > 0)
            {
                yield return new WaitForSeconds(1f);
                TurnTimeRemaining.Value = Mathf.Max(0, TurnTimeRemaining.Value - 1);
            }

            Debug.Log("[TurnNetworkManager] Server turn timer expired -> requesting server ChangeTurn.");
            GameManager.Instance.ChangeTurn();
            
            yield return null;
        }
    }

    public ulong GetOtherClientIdOrServerFallback()
    {
        if (NetworkManager.Singleton == null)
            return NetworkManager.ServerClientId;

        foreach (var kv in NetworkManager.Singleton.ConnectedClients)
        {
            if (kv.Key != NetworkManager.ServerClientId)
                return kv.Key;
        }

        return NetworkManager.ServerClientId;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestEndTurnServerRpc(RpcParams rpcParams = default)
    {
        if (!IsServer)
            return;

        ulong sender = rpcParams.Receive.SenderClientId;
        Debug.Log($"[TurnNetworkManager] RequestEndTurnServerRpc from {sender}. CurrentTurnOwner={CurrentTurnOwner.Value}");

        if (sender != CurrentTurnOwner.Value)
        {
            Debug.LogWarning($"[TurnNetworkManager] RequestEndTurnServerRpc ignored from {sender} — not current owner.");
            return;
        }

        GameManager.Instance.ChangeTurn();
    }
}
