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
        InitializationOptions options = new();

#if UNITY_EDITOR
        options.SetProfile("Editor_Profile");
#else
        options.SetProfile("Build_Profile_" + Random.Range(0, 10000));
#endif

        await UnityServices.InitializeAsync(options);

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
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && clientId != NetworkManager.ServerClientId)
        {
            if (currentLobby != null)
            {
                try
                {
                    currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);

                    string myPlayerId = AuthenticationService.Instance.PlayerId;

                    foreach (var player in currentLobby.Players)
                    {
                        if (player.Id != myPlayerId)
                        {
                            Debug.Log($"[Host] Kicking disconnected player {player.Id} from lobby.");
                            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, player.Id);
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to cleanup lobby on disconnect: {e.Message}");
                }
            }
        }
    }

    public async void FindMatch()
    {
        Debug.Log("Looking for a lobby...");

        // FIX: Перед началом убеждаемся, что старый NetworkManager выключен
        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("NetworkManager was still running. Shutting down...");
            NetworkManager.Singleton.Shutdown();
            // Ждем, пока он реально выключится
            while (NetworkManager.Singleton.IsListening)
                await Task.Yield();
        }

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
            CreateMatch();
        }
    }

    private async void CreateMatch()
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

            // FIX: Дополнительная проверка перед стартом хоста
            if (NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();

            NetworkManager.Singleton.StartHost();

            CreateLobbyOptions options = new()
            {
                Data = new Dictionary<string, DataObject>
                {
                    { JOIN_CODE_KEY, new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
                }
            };

            currentLobby = await LobbyService.Instance.CreateLobbyAsync("My Card Game", 2, options);

            Debug.Log("Created lobby: " + currentLobby.Id);

            heartbeatCoroutine = StartCoroutine(HeartbeatLobbyCoroutine(currentLobby.Id, 15));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create match: {e.Message}");
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

            // FIX: Дополнительная проверка перед стартом клиента
            if (NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();

            NetworkManager.Singleton.StartClient();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to join match: {e.Message}");
        }
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

    private async void LeaveLobby()
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
                Debug.Log("Left lobby successfully.");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error leaving lobby: {e.Message}");
            }
            finally
            {
                currentLobby = null;
            }
        }
    }

    public void DisconnectAndReturnToMenu()
    {
        LeaveLobby();
        StartCoroutine(DisconnectSequence());
    }

    private IEnumerator DisconnectSequence()
    {
        if (heartbeatCoroutine != null)
        {
            StopCoroutine(heartbeatCoroutine);
            heartbeatCoroutine = null;
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            yield return new WaitForSeconds(0.5f);
        }

        SceneManager.LoadScene("MainMenu");
    }
}