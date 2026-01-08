using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public TextMeshProUGUI playerManaTxt, enemyManaTxt, playerHPTxt, enemyHPTxt, resultTxt, turnTimeTxt, readyStatusText, restartBtnText;
    public Button endTurnBtn, readyButton, restartBtn, surrenderButton;
    public GameObject readyPanel, result, surrenderPanel;

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

        if (readyPanel != null)
        {
            readyPanel.SetActive(true);
            if (readyButton != null) 
                readyButton.interactable = true;
            if (readyStatusText != null) 
                readyStatusText.text = "READY";
        }

        if (surrenderButton != null)
            surrenderButton.onClick.AddListener(OnSurrenderButton);
    }

    public void OnSurrenderButton()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameOver)
        {
            if (surrenderPanel != null)
                surrenderPanel.SetActive(false);

            GameManager.Instance.Surrender();
        }
    }

    public void StartGame()
    {
        if (readyPanel != null)
            readyPanel.SetActive(false);

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
            txt.alpha = interactable ? 1f : 0.1f;
    }

    public void OnSurrenderPanelButton()
    {
        surrenderPanel.SetActive(!surrenderPanel.activeSelf);
    }

    public void OnReadyButton()
    {
        if (readyButton != null)
            readyButton.interactable = false;

        if (GameManager.Instance != null)
            GameManager.Instance.SendPlayerReady();

        if (readyStatusText != null)
            readyStatusText.text = "READY...";
    }

    public void UpdateReadyStatus(int readyCount)
    {
        if (readyStatusText != null && readyPanel.activeSelf)
            readyStatusText.text = $"READY ({readyCount}/2)";
    }

    public void OnRestartButton()
    {
        if (restartBtn != null)
            restartBtn.interactable = false;

        if (GameManager.Instance != null)
            GameManager.Instance.SendRestartVote();
    }

    public void OnBackToMenuButton()
    {
        if (MatchmakingManager.Instance != null)
            MatchmakingManager.Instance.DisconnectAndReturnToMenu();
    }
}