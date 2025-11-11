using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NetworkUIManager : MonoBehaviour
{
    [SerializeField] string defaultHostIP = "127.0.0.1";
    [SerializeField] ushort defaultPort = 7777;

    [SerializeField] private GameObject menuPanel;

    public void StartHostButton()
    {
        StartHost(defaultHostIP, defaultPort);
        HideMenuUI();
    }

    public void StartClientButton()
    {
        StartClient(defaultHostIP, defaultPort);
        HideMenuUI();
    }

    private void HideMenuUI()
    {
        if (menuPanel != null)
            menuPanel.SetActive(false);
    }

    void StartHost(string ip, ushort port)
    {
        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        utp.SetConnectionData(ip, port, "0.0.0.0");
        NetworkManager.Singleton.StartHost();
        Debug.Log("HOST");
    }

    void StartClient(string ip, ushort port)
    {
        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        utp.SetConnectionData(ip, port);
        NetworkManager.Singleton.StartClient();
        Debug.Log($"CLIENT requested -> {ip}:{port}");
    }
}