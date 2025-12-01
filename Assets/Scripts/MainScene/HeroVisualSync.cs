using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class HeroVisualSync : NetworkBehaviour
{
    public Sprite hostAvatarSprite, clientAvatarSprite;
    public Image bottomHeroImage, topHeroImage;

    public override void OnNetworkSpawn()
    {
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (IsServer)
        {
            if (bottomHeroImage) 
                bottomHeroImage.sprite = hostAvatarSprite;

            if (topHeroImage) 
                topHeroImage.sprite = clientAvatarSprite;
        }
        else
        {
            if (bottomHeroImage) 
                bottomHeroImage.sprite = clientAvatarSprite;

            if (topHeroImage) 
                topHeroImage.sprite = hostAvatarSprite;
        }
    }
}