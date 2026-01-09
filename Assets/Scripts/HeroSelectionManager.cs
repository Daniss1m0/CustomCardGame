using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HeroSelectionManager : MonoBehaviour
{
    public static int SelectedHeroIdx = -1;

    public Image heroImg;
    public Sprite[] heroS;

    private int currentHeroIdx = 0;

    private void Start()
    {
        currentHeroIdx = PlayerPrefs.GetInt("SelectedHeroIdx", 0);
        UpdateVisuals();
    }

    public void SelectHero(int idx)
    {
        if (idx >= 0 && idx < heroS.Length)
        {
            currentHeroIdx = idx;
            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        if (heroImg != null && heroS.Length > 0)
            heroImg.sprite = heroS[currentHeroIdx];
    }

    private void OnMainSceneLoaded(Scene scene, LoadSceneMode mode) //?
    {
        if (scene.name == "MainScene")
        {
            MatchmakingManager.Instance.FindMatch();

            SceneManager.sceneLoaded -= OnMainSceneLoaded;
        }
    }

    public void OnPlayBtn()
    {
        PlayerPrefs.SetInt("SelectedHeroIdx", currentHeroIdx);
        PlayerPrefs.Save();
        SelectedHeroIdx = currentHeroIdx;

        SceneManager.LoadScene("MainScene");

        SceneManager.sceneLoaded += OnMainSceneLoaded;
    }

    public void OnBackBtn() => SceneManager.LoadScene("MainMenu");
}