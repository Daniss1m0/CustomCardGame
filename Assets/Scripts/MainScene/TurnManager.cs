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

    private IEnumerator TurnFunc() //be possible to SetHighlight(false) in PlayerHandCards when its not player's turn 
    {
        int turnTime = turnTimeDefault;
        UIManager.Instance.UpdateTurnTime(turnTime);

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
        {
            foreach (var card in GameManager.Instance.enemyFieldCards)
            {
                card.self.canAttack = true;
                card.Ability.OnNewTurn(card.self, card.Info);
            }

            if (GameManager.Instance.enemyAI != null)
                StartCoroutine(GameManager.Instance.enemyAI.PerformTurn());
            else
                Debug.LogWarning("TurnManager: enemyAI (AIController) is not assigned in GameManager.");
        }

        while (turnTime-- > 0)
        {
            UIManager.Instance.UpdateTurnTime(turnTime);
            yield return new WaitForSeconds(1f);
        }

        GameManager.Instance.ChangeTurn();
    }

}
