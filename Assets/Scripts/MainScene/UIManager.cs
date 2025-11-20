using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public TextMeshProUGUI playerManaTxt, enemyManaTxt, playerHPTxt, enemyHPTxt, resultTxt, turnTimeTxt;
    public Button endTurnBtn;
    public GameObject result, optionsPanel;

    private void Awake()
    {
        if (!Instance)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void StartGame()
    {
        result.SetActive(false);
        UpdateHPAndMana();
    }

    public void UpdateHPAndMana()
    {
        playerManaTxt.text = GameManager.Instance.currentGame.player.mana.ToString();
        enemyManaTxt.text = GameManager.Instance.currentGame.enemy.mana.ToString();
        playerHPTxt.text = GameManager.Instance.currentGame.player.hp.ToString();
        enemyHPTxt.text = GameManager.Instance.currentGame.enemy.hp.ToString();
    }

    public void ShowResult()
    {
        result.SetActive(true);
        if (GameManager.Instance.currentGame.enemy.hp == 0)
            resultTxt.text = "WIN";
        else
            resultTxt.text = "LOSE";
    }

    public void UpdateTurnTime(int time)
    {
        turnTimeTxt.text = time > 0 ? time.ToString() : "-";
    }

    public void DisableTurnBtn()
    {
        endTurnBtn.interactable = GameManager.Instance.IsPlayerTurn;
    }

    public void OnOptionsButton()
    {
        optionsPanel.SetActive(!optionsPanel.activeSelf);
    }

    public void LoadDeckSelection()
    {
        SceneManager.LoadScene("DeckSelection");
    }

    public void SetEndTurnInteractable(bool state)
    {
        if (endTurnBtn != null)
            endTurnBtn.interactable = state;
    }
}
