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

    [SerializeField] private string lobbyName = "Card Game Lobby";

    private bool isBusy = false;
    private string lastLobbyId = "";
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
            options.SetProfile("Editor_Profile_" + GetHashCode()); // Allows testing
#else
            options.SetProfile("Build_Profile_" + Random.Range(0, 10000));
#endif
            await UnityServices.InitializeAsync(options);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;

        await CleanupGhostLobbies();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private async Task CleanupGhostLobbies()
    {
        try
        {
            var joinedLobbyIds = await LobbyService.Instance.GetJoinedLobbiesAsync();

            if (joinedLobbyIds != null && joinedLobbyIds.Count > 0)
                foreach (string lobbyId in joinedLobbyIds)
                    try
                    {
                        Lobby lobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);

                        if (lobby.HostId == AuthenticationService.Instance.PlayerId)
                            await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
                        else
                            await LobbyService.Instance.RemovePlayerAsync(lobbyId, AuthenticationService.Instance.PlayerId);

                        await Task.Delay(500);
                    }
                    catch (System.Exception)
                    {
                        try
                        {
                            await LobbyService.Instance.RemovePlayerAsync(lobbyId, AuthenticationService.Instance.PlayerId);
                        }
                        catch { }
                        await Task.Delay(500);
                    }
        }
        catch { }
    }

    private async void OnClientDisconnect(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer && clientId != NetworkManager.ServerClientId)
            if (currentLobby != null)
                try
                {
                    currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);

                    string myPlayerId = AuthenticationService.Instance.PlayerId;
                    foreach (var player in currentLobby.Players)
                        if (player.Id != myPlayerId)
                            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, player.Id);
                }
                catch { }
    }

    public async void FindMatch()
    {
        if (isBusy) 
            return;

        isBusy = true;

        await ResetNetworkState();

        try
        {
            QuickJoinLobbyOptions options = new();
            
            Lobby foundLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);

            if (foundLobby.Id == lastLobbyId)
            {
                await CreateMatch();
                return;
            }

            currentLobby = foundLobby;
            Debug.Log("JOINED LOBBY: " + currentLobby.Id);

            string joinCode = currentLobby.Data[JOIN_CODE_KEY].Value;

            bool success = await StartClientWithRelay(joinCode);

            if (!success)
            {
                lastLobbyId = currentLobby.Id;

                await LeaveLobby();

                await CreateMatch();
            }
        }
        catch (LobbyServiceException)
        {
            await CreateMatch();
        }
        catch
        {
            isBusy = false;
        }
    }

    private async Task CreateMatch()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);

            if (!NetworkManager.Singleton.StartHost())
            {
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

            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 2, options);
            Debug.Log("CREATED LOBBY: " + currentLobby.Id);

            if (heartbeatCoroutine != null)
                StopCoroutine(heartbeatCoroutine);

            heartbeatCoroutine = StartCoroutine(LobbyCoroutine(currentLobby.Id, 15));

            isBusy = false;
        }
        catch
        {
            await ResetNetworkState();
            isBusy = false;
        }
    }

    private async Task<bool> StartClientWithRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(joinAllocation.RelayServer.IpV4, (ushort)joinAllocation.RelayServer.Port, joinAllocation.AllocationIdBytes, joinAllocation.Key, joinAllocation.ConnectionData, joinAllocation.HostConnectionData);

            if (NetworkManager.Singleton.StartClient())
            {
                isBusy = false;
                return true;
            }
            else
                return false;
        }
        catch
        {
            return false;
        }
    }

    public void DisconnectAndReturnToMenu()
    {
        if (isBusy)
            return;

        if (currentLobby != null)
            lastLobbyId = currentLobby.Id;

        StartCoroutine(DisconnectSequence());
    }

    private IEnumerator DisconnectSequence()
    {
        isBusy = true;

        Task resetTask = ResetNetworkState();
        yield return new WaitUntil(() => resetTask.IsCompleted);

        SceneManager.LoadScene("MainMenu");
        isBusy = false;
    }

    private async Task LeaveLobby()
    {
        if (currentLobby == null)
            return;

        string lobbyId = currentLobby.Id;
        lastLobbyId = lobbyId;

        try
        {
            if (heartbeatCoroutine != null)
            {
                StopCoroutine(heartbeatCoroutine);
                heartbeatCoroutine = null;
            }

            string playerId = AuthenticationService.Instance.PlayerId;

            if (currentLobby.HostId == playerId)
                await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
            else
                await LobbyService.Instance.RemovePlayerAsync(lobbyId, playerId);
        }
        catch { }

        currentLobby = null;
    }

    private async Task ResetNetworkState()
    {
        await LeaveLobby();
        await CleanupGhostLobbies();
        await Task.Delay(100);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
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

    private IEnumerator LobbyCoroutine(string lobbyId, float waitTimeSecs)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSecs);
        while (true)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return delay;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainScene")
            FindMatch();
    }
}