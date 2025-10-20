using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
    public static UIController Instance;

    public TextMeshProUGUI PlayerMana, EnemyMana;
    public TextMeshProUGUI PlayerHP, EnemyHP;

    public GameObject ResultGO;
    public TextMeshProUGUI ResultTxt;

    public TextMeshProUGUI TurnTime;
    public Button EndTurnBtn;

    public GameObject optionsPanel;

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

    public void RefreshUIReferences()
    {
        PlayerMana = GameObject.Find("PlayerMana").GetComponent<TextMeshProUGUI>();
        EnemyMana = GameObject.Find("EnemyMana").GetComponent<TextMeshProUGUI>();
        PlayerHP = GameObject.Find("PlayerHP").GetComponent<TextMeshProUGUI>();
        EnemyHP = GameObject.Find("EnemyHP").GetComponent<TextMeshProUGUI>();
        ResultGO = GameObject.Find("ResultGO");
        ResultTxt = GameObject.Find("ResultTxt").GetComponent<TextMeshProUGUI>();
        TurnTime = GameObject.Find("TurnTime").GetComponent<TextMeshProUGUI>();
        EndTurnBtn = GameObject.Find("EndTurnBtn").GetComponent<Button>();
        optionsPanel = GameObject.Find("OptionsPanel");
    }

    public void StartGame()
    {
        EndTurnBtn.interactable = true;
        ResultGO.SetActive(false);
        UpdateHPAndMana();
    }

    public void UpdateHPAndMana()
    {
        PlayerMana.text = GameManager.Instance.currentGame.player.mana.ToString();
        EnemyMana.text = GameManager.Instance.currentGame.enemy.mana.ToString();
        PlayerHP.text = GameManager.Instance.currentGame.player.hp.ToString();
        EnemyHP.text = GameManager.Instance.currentGame.enemy.hp.ToString();
    }

    public void ShowResult()
    {
        ResultGO.SetActive(true);
        if (GameManager.Instance.currentGame.enemy.hp == 0)
            ResultTxt.text = "WIN";
        else
            ResultTxt.text = "LOSE";
    }

    public void UpdateTurnTime(int time)
    {
        TurnTime.text = time.ToString();
    }

    public void DisableTurnBtn()
    {
        EndTurnBtn.interactable = GameManager.Instance.IsPlayerTurn;
    }

    public void OnOptionsButton()
    {
        optionsPanel.SetActive(!optionsPanel.activeSelf);
    }

    public void LoadDeckSelection()
    {
        SceneManager.LoadScene("DeckSelection");
    }
}
