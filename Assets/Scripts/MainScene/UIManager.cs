using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public TextMeshProUGUI playerManaTxt, enemyManaTxt, playerHPTxt, enemyHPTxt, resultTxt, turnTimeTxt, restartBtnText;
    public Button endTurnBtn, restartBtn;
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

    private void Start()
    {
        if (result != null)
            result.SetActive(false);
    }

    public void StartGame()
    {
        result.SetActive(false);
        UpdateHPAndMana();
    }

    public void UpdateHPAndMana()
    {
        if (GameManager.Instance == null || GameManager.Instance.currentGame == null)
            return;

        playerManaTxt.text = GameManager.Instance.currentGame.player.mana.ToString();
        enemyManaTxt.text = GameManager.Instance.currentGame.enemy.mana.ToString();
        playerHPTxt.text = GameManager.Instance.currentGame.player.hp.ToString();
        enemyHPTxt.text = GameManager.Instance.currentGame.enemy.hp.ToString();
    }

    public void ShowResult()
    {
        result.SetActive(true);

        if (GameManager.Instance != null && GameManager.Instance.currentGame != null)
        {
            if (GameManager.Instance.currentGame.enemy.hp <= 0)
                resultTxt.text = "WIN";
            else
                resultTxt.text = "LOSE";
        }

        if (restartBtn != null)
            restartBtn.interactable = true;

        if (restartBtnText != null)
            restartBtnText.text = "RESTART?";
    }

    public void UpdateRestartText(int votes)
    {
        if (restartBtnText != null)
        {
            if (votes == 0)
                restartBtnText.text = "RESTART?";
            else
                restartBtnText.text = $"RESTART: {votes}/2";
        }
    }

    public void UpdateTurnTime(int time)
    {
        turnTimeTxt.text = time > 0 ? time.ToString() : "-";
    }

    public void DisableTurnBtn()
    {
        bool isMyTurn = GameManager.Instance.IsPlayerTurn;

        if (endTurnBtn != null)
        {
            endTurnBtn.interactable = isMyTurn;
            UpdateEndTurnTextAlpha(isMyTurn);
        }
    }

    public void SetEndTurnInteractable(bool state)
    {
        if (endTurnBtn != null)
        {
            endTurnBtn.interactable = state;
            UpdateEndTurnTextAlpha(state);
        }
    }

    private void UpdateEndTurnTextAlpha(bool interactable)
    {
        if (endTurnBtn == null) 
            return;

        var txt = endTurnBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null)
        {
            txt.alpha = interactable ? 1f : 0.1f;
        }
    }

    public void OnOptionsButton()
    {
        optionsPanel.SetActive(!optionsPanel.activeSelf);
    }

    public void OnRestartButton()
    {
        if (restartBtn != null)
            restartBtn.interactable = false;

        if (GameManager.Instance != null)
            GameManager.Instance.SendRestartVote();
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}