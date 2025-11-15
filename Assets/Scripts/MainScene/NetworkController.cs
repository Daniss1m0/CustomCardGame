using System.Collections;
using UnityEngine;

public class NetworkController : MonoBehaviour, IPlayerController
{
    private Player model;
    private bool isLocal;

    public void Initialize(Player playerModel, bool isLocal)
    {
        model = playerModel;
        this.isLocal = isLocal;
    }

    public IEnumerator PerformTurn()
    {
        while (GameManager.Instance.IsPlayerTurn == !isLocal)
            yield return null;
    }

    public void OnRemotePlayCard(CardController card)
    {
        GameManager.Instance.PlayCard(card, false);
    }

    public void OnRemoteAttack(int attackerId, int targetId)
    {
        //GameManager.Attack(...)?
    }

    public void OnRemoteEndTurn()
    {
        GameManager.Instance.EndTurnFromController();
    }
}
