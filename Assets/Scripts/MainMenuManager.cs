using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public Toggle fullscreenTgl;
    public Slider volumeSldr;
    public GameObject optionsPnl;

    private void Start()
    {
        bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", 0) == 1;

        Screen.fullScreen = isFullscreen;

        fullscreenTgl.isOn = isFullscreen;
        fullscreenTgl.onValueChanged.AddListener(SetFullscreen);

        float volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        AudioListener.volume = volume;

        volumeSldr.value = volume;
        volumeSldr.onValueChanged.AddListener(SetVolume);
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

    public void OnOptionsBtn() => optionsPnl.SetActive(!optionsPnl.activeSelf);

    public void OnPlayBtn() => SceneManager.LoadScene("HeroSelection");

    public void OnCollectionBtn() => SceneManager.LoadScene("Collection");

    public void OnQuitBtn() => Application.Quit();
}