using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class HeroVisualSync : NetworkBehaviour
{
    public Sprite[] allHeroSprites;
    public Image bottomHeroImage, topHeroImage;

    public NetworkVariable<int> hostHeroIndex = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> clientHeroIndex = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        hostHeroIndex.OnValueChanged += (oldV, newV) => UpdateUI();
        clientHeroIndex.OnValueChanged += (oldV, newV) => UpdateUI();

        if (IsServer)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;

        int myIndexToSend = HeroSelectionManager.SelectedHeroIdx;
        if (myIndexToSend == -1)
            myIndexToSend = PlayerPrefs.GetInt("SelectedHeroIndex", 0);

        SubmitHeroIndexServerRpc(myIndexToSend);

        UpdateUI();
    }

    public override void OnNetworkDespawn()
    {
        hostHeroIndex.OnValueChanged -= (oldV, newV) => UpdateUI();
        clientHeroIndex.OnValueChanged -= (oldV, newV) => UpdateUI();

        if (IsServer && NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;

        base.OnNetworkDespawn();
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (clientId != NetworkManager.ServerClientId)
            clientHeroIndex.Value = -1;
    }

    public void ResetClientVisuals()
    {
        if (IsServer)
            clientHeroIndex.Value = -1;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitHeroIndexServerRpc(int index, RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (senderId == NetworkManager.ServerClientId)
            hostHeroIndex.Value = index;
        else
            clientHeroIndex.Value = index;
    }

    private void UpdateUI()
    {
        if (IsServer)
        {
            UpdateAvatarVisuals(bottomHeroImage, hostHeroIndex.Value);
            UpdateAvatarVisuals(topHeroImage, clientHeroIndex.Value);
        }
        else
        {
            UpdateAvatarVisuals(bottomHeroImage, clientHeroIndex.Value);
            UpdateAvatarVisuals(topHeroImage, hostHeroIndex.Value);
        }
    }

    private void UpdateAvatarVisuals(Image targetImage, int heroIndex)
    {
        if (targetImage == null)
            return;

        if (heroIndex < 0)
        {
            targetImage.sprite = null;

            var c = targetImage.color;
            c.a = 0.5f;
            targetImage.color = c;
        }
        else
        {
            targetImage.sprite = GetSpriteSafe(heroIndex);

            var c = targetImage.color;
            c.a = 1f;
            targetImage.color = c;
        }
    }

    private Sprite GetSpriteSafe(int index)
    {
        if (allHeroSprites == null || allHeroSprites.Length == 0)
            return null;

        if (index >= 0 && index < allHeroSprites.Length)
            return allHeroSprites[index];

        return allHeroSprites[0];
    }
}