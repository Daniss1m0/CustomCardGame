using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class HeroVisualSync : NetworkBehaviour
{
    public Sprite[] allHeroSprites;
    public Image bottomHeroImage, topHeroImage;
    public NetworkVariable<int> hostHeroIndex = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> clientHeroIndex = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        hostHeroIndex.OnValueChanged += (oldV, newV) => UpdateUI();
        clientHeroIndex.OnValueChanged += (oldV, newV) => UpdateUI();

        int mySavedIndex = PlayerPrefs.GetInt("SelectedHeroIndex", 0);

        SubmitHeroIndexServerRpc(mySavedIndex);

        UpdateUI();
    }

    public override void OnNetworkDespawn()
    {
        hostHeroIndex.OnValueChanged -= (oldV, newV) => UpdateUI();
        clientHeroIndex.OnValueChanged -= (oldV, newV) => UpdateUI();
        base.OnNetworkDespawn();
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
        if (allHeroSprites == null || allHeroSprites.Length == 0) 
            return;

        Sprite hostSprite = GetSpriteSafe(hostHeroIndex.Value);
        Sprite clientSprite = GetSpriteSafe(clientHeroIndex.Value);

        if (IsServer)
        {
            if (bottomHeroImage) 
                bottomHeroImage.sprite = hostSprite;

            if (topHeroImage) 
                topHeroImage.sprite = clientSprite;
        }
        else
        {
            if (bottomHeroImage) 
                bottomHeroImage.sprite = clientSprite;

            if (topHeroImage) 
                topHeroImage.sprite = hostSprite;
        }
    }

    private Sprite GetSpriteSafe(int index)
    {
        if (index >= 0 && index < allHeroSprites.Length)
            return allHeroSprites[index];

        return allHeroSprites.Length > 0 ? allHeroSprites[0] : null;
    }
}