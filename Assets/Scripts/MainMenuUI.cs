using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public GameObject optionsPanel;

    public Toggle fullscreenToggle;
    public Slider volumeSlider;

    private void Start()
    {
        bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        Screen.fullScreen = isFullscreen;

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = isFullscreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        float volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        AudioListener.volume = volume;

        if (volumeSlider != null)
        {
            volumeSlider.value = volume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
    }

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