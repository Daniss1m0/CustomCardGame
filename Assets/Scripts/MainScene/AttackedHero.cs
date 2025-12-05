using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.Netcode;

public class AttackedHero : MonoBehaviour, IDropHandler
{
    public Color normalCol, highlightCol;

    public enum HeroType
    {
        Player,
        Enemy
    }

    public HeroType type;

    public void OnDrop(PointerEventData eventData)
    {
        if (!GameManager.Instance.IsPlayerTurn)
            return;

        CardController card = eventData.pointerDrag.GetComponent<CardController>();

        if (card && card.self.canAttack && type == HeroType.Enemy && !GameManager.Instance.enemyFieldCards.Exists(x => x.self.IsProvocation))
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                if (NetworkManager.Singleton.IsServer)
                    GameManager.Instance.DamageHero(card, true);
                else
                {
                    if (card.Network != null)
                    {
                        card.Network.RequestAttackHeroServerRpc(true);

                        card.self.canAttack = false;
                        card.Info.SetHighlight(false);
                    }
                }
            }
            else
                GameManager.Instance.DamageHero(card, true);
        }
    }

    public void HighlightAsTarget(bool highlight)
    {
        var img = GetComponent<Image>();
        HighlightHelper.SetTargetHighlight(img, highlight, normalCol, highlightCol);
    }
}