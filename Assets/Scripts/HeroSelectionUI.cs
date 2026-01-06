using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HeroSelectionUI : MonoBehaviour
{
    public Image heroImage;
    public Sprite[] heroSprites;

    private int currentHeroIndex = 0;

    private void Start()
    {
        currentHeroIndex = PlayerPrefs.GetInt("SelectedHeroIndex", 0);
        UpdateVisuals();
    }

    public void OnPlayButton()
    {
        PlayerPrefs.SetInt("SelectedHeroIndex", currentHeroIndex);
        PlayerPrefs.Save();

        SceneManager.LoadScene("MainScene");
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void SelectHero(int index)
    {
        if (index >= 0 && index < heroSprites.Length)
        {
            currentHeroIndex = index;
            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        if (heroImage != null && heroSprites.Length > 0)
            heroImage.sprite = heroSprites[currentHeroIndex];
    }
}