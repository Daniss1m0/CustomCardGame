using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public GameObject optionsPanel;

    public void OnPlayButton()
    {
        SceneManager.LoadScene("HeroSelection");
    }

    public void OnCollectionButton()
    {
        SceneManager.LoadScene("Collection");
    }

    public void OnQuitButton()
    {
        Application.Quit();
    }

    public void OnOptionsButton()
    {
        optionsPanel.SetActive(!optionsPanel.activeSelf);
    }
}
