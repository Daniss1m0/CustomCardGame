using UnityEngine;
using UnityEngine.SceneManagement;

public class DeckSelectionUI : MonoBehaviour
{
    public GameObject optionsPanel;

    public void OnPlayButton()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void OnOptionsButton()
    {
        optionsPanel.SetActive(!optionsPanel.activeSelf);
    }

    public void LoadMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
