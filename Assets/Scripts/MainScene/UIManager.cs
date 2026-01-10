using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public TextMeshProUGUI playerManaTxt, enemyManaTxt, playerHPTxt, enemyHPTxt, resultTxt, turnTimeTxt, readyStatusTxt, restartBtnTxt;
    public Button endTurnBtn, readyBtn, restartBtn, surrenderBtn;
    public GameObject surrenderPanel, surrenderPanelBtn, readyPanel, result;

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
            if (readyBtn != null)
                readyBtn.interactable = true;
            if (readyStatusTxt != null)
                readyStatusTxt.text = "READY";
        }

        if (surrenderBtn != null)
            surrenderBtn.onClick.AddListener(OnSurrenderButton);

        if (surrenderPanelBtn != null)
            surrenderPanelBtn.SetActive(false);

        if (surrenderPanel != null)
            surrenderPanel.SetActive(false);
    }

    public void StartGame()
    {
        readyPanel.SetActive(false);

        result.SetActive(false);
        UpdateHPAndMana();

        surrenderPanelBtn.SetActive(true);
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

        if (restartBtnTxt != null)
            restartBtnTxt.text = "RESTART?";

        surrenderPanelBtn.SetActive(false);

        surrenderPanel.SetActive(false);
    }

    public void OnSurrenderButton()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameOver)
        {
            surrenderPanel.SetActive(false);

            GameManager.Instance.Surrender();
        }
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

    public void UpdateRestartText(int votes)
    {
        if (restartBtnTxt != null)
        {
            if (votes == 0)
                restartBtnTxt.text = "RESTART?";
            else
                restartBtnTxt.text = $"RESTART: {votes}/2";
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

    public void OnReadyButton()
    {
        if (readyBtn != null)
            readyBtn.interactable = false;

        GameManager.Instance.SendPlayerReady();

        if (readyStatusTxt != null)
            readyStatusTxt.text = "READY...";
    }

    public void UpdateReadyStatus(int readyCount)
    {
        if (readyStatusTxt != null && readyPanel.activeSelf)
            readyStatusTxt.text = $"READY ({readyCount}/2)";
    }

    public void OnRestartButton()
    {
        if (restartBtn != null)
            restartBtn.interactable = false;

        if (GameManager.Instance != null)
            GameManager.Instance.SendRestartVote();
    }

    public void OnSurrenderPanelButton() => surrenderPanel.SetActive(!surrenderPanel.activeSelf);

    public void OnBackToMenuButton() => MatchmakingManager.Instance.DisconnectAndReturnToMenu();
}