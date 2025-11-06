using UnityEngine;
using System.Collections;

public class TurnManager : MonoBehaviour
{
    public int turnTimeDefault = 30;
    private Coroutine turnCoroutine;

    public void StartTurnLoop()
    {
        if (turnCoroutine != null) 
            StopCoroutine(turnCoroutine);

        turnCoroutine = StartCoroutine(TurnFunc());
    }

    public void StopTurnLoop()
    {
        if (turnCoroutine != null) 
            StopCoroutine(turnCoroutine);
        
        turnCoroutine = null;
    }

    private IEnumerator TurnFunc()
    {
        int turnTime = turnTimeDefault;
        UIManager.Instance.UpdateTurnTime(turnTime);

        UIManager.Instance.DisableTurnBtn();

        foreach (var card in GameManager.Instance.playerFieldCards)
            card.Info.SetHighlight(false);

        GameManager.Instance.CheckCardsForManaAvailability();

        if (GameManager.Instance.IsPlayerTurn)
            foreach (var card in GameManager.Instance.playerFieldCards)
            {
                card.self.canAttack = true;
                card.Info.SetHighlight(true);
                card.Ability.OnNewTurn(card.self, card.Info);
            }
        else
            foreach (var card in GameManager.Instance.enemyFieldCards)
            {
                card.self.canAttack = true;
                card.Ability.OnNewTurn(card.self, card.Info);
            }

        IPlayerController controller = GameManager.Instance.IsPlayerTurn ? GameManager.Instance.PlayerController : GameManager.Instance.OpponentController;
        if (controller != null)
            StartCoroutine(controller.PerformTurn());
        else
            Debug.LogWarning("Ñontroller not assigned for current side.");

        while (turnTime > 0)
        {
            yield return new WaitForSeconds(1f);
            turnTime--;
            UIManager.Instance.UpdateTurnTime(turnTime);
        }

        GameManager.Instance.ChangeTurn();
    }
}
