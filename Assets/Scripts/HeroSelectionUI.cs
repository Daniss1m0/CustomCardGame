using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HeroSelectionUI : MonoBehaviour
{
    public Image heroImage;
    public Sprite[] heroSprites;

    public void OnPlayButton()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void SelectDeck(int index)
    {
        if (index >= 0 && index < heroSprites.Length)
            heroImage.sprite = heroSprites[index];
    }
}
