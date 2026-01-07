using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

public class MatchmakingManager : MonoBehaviour
{
    public static MatchmakingManager Instance;

    private const string JOIN_CODE_KEY = "j";

    private bool isBusy = false;
    private Coroutine heartbeatCoroutine;
    private Lobby currentLobby;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    private async void Start()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            InitializationOptions options = new();
#if UNITY_EDITOR
            options.SetProfile("Editor_Profile_" + GetHashCode());
#else
            options.SetProfile("Build_Profile_" + Random.Range(0, 10000));
#endif
            await UnityServices.InitializeAsync(options);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
    }

    private async void OnClientDisconnect(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer && clientId != NetworkManager.ServerClientId)
        {
            if (currentLobby != null)
            {
                Debug.Log($"[Host] Client {clientId} disconnected. Removing from Lobby...");
                try
                {
                    currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);

                    string myPlayerId = AuthenticationService.Instance.PlayerId;
                    foreach (var player in currentLobby.Players)
                    {
                        if (player.Id != myPlayerId)
                        {
                            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, player.Id);
                            Debug.Log($"[Host] Kicked player {player.Id} from Lobby.");
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[Host] Failed to remove player from lobby: {e.Message}");
                }
            }
        }
    }

    public async void FindMatch()
    {
        if (isBusy) 
            return;

        isBusy = true;

        Debug.Log("Resetting network before search...");
        await ResetNetworkState();

        Debug.Log("Looking for a lobby...");
        try
        {
            QuickJoinLobbyOptions options = new();
            currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);

            Debug.Log("Joined lobby: " + currentLobby.Id);

            string joinCode = currentLobby.Data[JOIN_CODE_KEY].Value;
            await StartClientWithRelay(joinCode);
        }
        catch (LobbyServiceException)
        {
            Debug.Log("No lobbies found. Creating a new one...");
            await CreateMatch();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Matchmaking Error: {e.Message}");
            isBusy = false;
        }
    }

    public void DisconnectAndReturnToMenu()
    {
        if (isBusy) return;
        StartCoroutine(DisconnectSequence());
    }

    private async Task CreateMatch()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            if (NetworkManager.Singleton.StartHost())
                Debug.Log("Host started via Relay.");
            else
            {
                Debug.LogError("Failed to StartHost (NetworkManager refused).");
                isBusy = false;
                return;
            }

            CreateLobbyOptions options = new()
            {
                Data = new Dictionary<string, DataObject>
                {
                    { JOIN_CODE_KEY, new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
                }
            };

            currentLobby = await LobbyService.Instance.CreateLobbyAsync("My Card Game", 2, options);
            Debug.Log("Created lobby: " + currentLobby.Id);

            if (heartbeatCoroutine != null) StopCoroutine(heartbeatCoroutine);
            heartbeatCoroutine = StartCoroutine(HeartbeatLobbyCoroutine(currentLobby.Id, 15));

            isBusy = false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CreateMatch failed: {e.Message}");
            await ResetNetworkState();
            isBusy = false;
        }
    }

    private async Task StartClientWithRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            if (NetworkManager.Singleton.StartClient())
                Debug.Log("Client started via Relay.");
            else
                Debug.LogError("Failed to StartClient.");

            isBusy = false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"StartClient failed: {e.Message}");
            await ResetNetworkState();
            isBusy = false;
        }
    }

    private async Task ResetNetworkState()
    {
        if (currentLobby != null)
        {
            try
            {
                if (heartbeatCoroutine != null)
                {
                    StopCoroutine(heartbeatCoroutine);
                    heartbeatCoroutine = null;
                }

                string playerId = AuthenticationService.Instance.PlayerId;
                await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, playerId);
            }
            catch {}
            currentLobby = null;
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            Debug.Log("Shutting down NetworkManager...");
            NetworkManager.Singleton.Shutdown();

            float timeout = 1f;
            float timer = 0f;
            while (NetworkManager.Singleton.IsListening && timer < timeout)
            {
                timer += Time.unscaledDeltaTime;
                await Task.Yield();
            }
        }
    }

    private IEnumerator DisconnectSequence()
    {
        isBusy = true;

        Task resetTask = ResetNetworkState();
        yield return new WaitUntil(() => resetTask.IsCompleted);

        SceneManager.LoadScene("MainMenu");
        isBusy = false;
    }

    private IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);
        while (true)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return delay;
        }
    }
}