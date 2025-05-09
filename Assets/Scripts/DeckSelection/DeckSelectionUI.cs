using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Не забудь!

public class DeckSelectionUI : MonoBehaviour
{
    public GameObject optionsPanel;
    public Image heroImage; // Картинка справа
    public Sprite[] deckSprites; // Сюда добавь спрайты для разных колод

    private int currentDeckIndex = 0;

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
        SceneManager.LoadScene("MainMenu");
    }

    public void SelectDeck(int index)
    {
        if (index >= 0 && index < deckSprites.Length)
        {
            currentDeckIndex = index;
            heroImage.sprite = deckSprites[index];
        }
    }
}
