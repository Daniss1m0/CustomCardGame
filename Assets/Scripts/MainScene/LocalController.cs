using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LocalController : MonoBehaviour, IPlayerController
{
    private Player playerModel;
    private bool isLocal;
    private bool endPressed;

    public void Initialize(Player playerModel, bool isLocal)
    {
        this.playerModel = playerModel;
        this.isLocal = isLocal;
    }

    public IEnumerator PerformTurn()
    {
        endPressed = false;
        var btn = UIManager.Instance?.endTurnBtn;
        if (btn != null)
        {
            btn.interactable = true;
            btn.onClick.AddListener(OnEndClicked);
        }

        while (GameManager.Instance.IsPlayerTurn == isLocal && !endPressed)
            yield return null;

        if (btn != null)
        {
            btn.onClick.RemoveListener(OnEndClicked);
            btn.interactable = false;
        }

        yield break;
    }

    private void OnEndClicked()
    {
        endPressed = true;
        GameManager.Instance.EndTurnFromController();
    }
}
