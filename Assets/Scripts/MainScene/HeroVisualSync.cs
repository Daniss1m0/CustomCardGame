using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class HeroVisualSync : NetworkBehaviour
{
    public Sprite[] allHeroS;
    public Image bottomHeroImg, topHeroImg;

    public NetworkVariable<int> hostHeroIdx = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> clientHeroIdx = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        hostHeroIdx.OnValueChanged += (oldV, newV) => UpdateUI();
        clientHeroIdx.OnValueChanged += (oldV, newV) => UpdateUI();

        if (IsServer)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;

        int myIdxToSend = HeroSelectionManager.SelectedHeroIdx;
        if (myIdxToSend == -1)
            myIdxToSend = PlayerPrefs.GetInt("SelectedHeroIndex", 0);

        SubmitHeroIndexServerRpc(myIdxToSend);

        UpdateUI();
    }

    public override void OnNetworkDespawn()
    {
        hostHeroIdx.OnValueChanged -= (oldV, newV) => UpdateUI();
        clientHeroIdx.OnValueChanged -= (oldV, newV) => UpdateUI();

        if (IsServer && NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;

        base.OnNetworkDespawn();
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (clientId != NetworkManager.ServerClientId)
            clientHeroIdx.Value = -1;
    }

    public void ResetClientVisuals()
    {
        if (IsServer)
            clientHeroIdx.Value = -1;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitHeroIndexServerRpc(int index, RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (senderId == NetworkManager.ServerClientId)
            hostHeroIdx.Value = index;
        else
            clientHeroIdx.Value = index;
    }

    private void UpdateUI()
    {
        if (IsServer)
        {
            UpdateAvatarVisuals(bottomHeroImg, hostHeroIdx.Value);
            UpdateAvatarVisuals(topHeroImg, clientHeroIdx.Value);
        }
        else
        {
            UpdateAvatarVisuals(bottomHeroImg, clientHeroIdx.Value);
            UpdateAvatarVisuals(topHeroImg, hostHeroIdx.Value);
        }
    }

    private void UpdateAvatarVisuals(Image targetImage, int heroIdx)
    {
        if (targetImage == null)
            return;

        if (heroIdx < 0)
        {
            targetImage.sprite = null;

            var c = targetImage.color;
            c.a = 0.5f;
            targetImage.color = c;
        }
        else
        {
            targetImage.sprite = GetSpriteSafe(heroIdx);

            var c = targetImage.color;
            c.a = 1f;
            targetImage.color = c;
        }
    }

    private Sprite GetSpriteSafe(int idx)
    {
        if (allHeroS == null || allHeroS.Length == 0)
            return null;

        if (idx >= 0 && idx < allHeroS.Length)
            return allHeroS[idx];

        return allHeroS[0];
    }
}