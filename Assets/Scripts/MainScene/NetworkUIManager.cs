using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NetworkUIManager : MonoBehaviour
{
    [SerializeField] string defaultHostIP = "127.0.0.1";
    [SerializeField] ushort defaultPort = 7777;
    [SerializeField] private GameObject networkUIPanel;

    public void StartHostButton()
    {
        StartHost(defaultHostIP, defaultPort);
        StartCoroutine(WaitHostStartAndRun());
    }

    IEnumerator WaitHostStartAndRun()
    {
        while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            yield return null;

        HideNetworkUI();

        if (GameManager.Instance != null)
            GameManager.Instance.StartGame();
    }

    public void StartClientButton()
    {
        StartClient(defaultHostIP, defaultPort);

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            HideNetworkUI();
        }
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void HideNetworkUI()
    {
        if (networkUIPanel != null)
            networkUIPanel.SetActive(false);
    }

    void StartHost(string ip, ushort port)
    {
        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        utp.SetConnectionData(ip, port, "0.0.0.0");
        NetworkManager.Singleton.StartHost();
        Debug.Log("StartHost requested");
    }

    void StartClient(string ip, ushort port)
    {
        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        utp.SetConnectionData(ip, port);
        NetworkManager.Singleton.StartClient();
        Debug.Log($"StartClient requested -> {ip}:{port}");
    }
}
