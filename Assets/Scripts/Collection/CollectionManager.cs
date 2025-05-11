using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CollectionManager : MonoBehaviour
{
    public GameObject CardPrefab;
    public Transform CardGrid;
    public int CardsPerPage = 12;
    public List<Button> PageButtons;
    public GameObject optionsPanel;

    private static IReadOnlyList<Card> allCards;
    private static bool isInitialized = false;

    private int currentPage = 0;
    private List<GameObject> currentCardObjects = new List<GameObject>();

    void Start()
    {
        if (!isInitialized)
        {
            allCards = new List<Card>(CardManager.AllCards).AsReadOnly();
            isInitialized = true;
        }

        ShowPage(0);
        SetupButtons();
    }

    void SetupButtons()
    {
        for (int i = 0; i < PageButtons.Count; i++)
        {
            int pageIndex = i;
            PageButtons[i].onClick.AddListener(() => ShowPage(pageIndex));
        }
    }

    void ShowPage(int pageIndex)
    {
        currentPage = pageIndex;

        foreach (GameObject go in currentCardObjects)
        {
            Destroy(go);
        }
        currentCardObjects.Clear();

        int start = pageIndex * CardsPerPage;
        int end = Mathf.Min(start + CardsPerPage, allCards.Count);

        for (int i = start; i < end; i++)
        {
            GameObject cardGO = Instantiate(CardPrefab, CardGrid);
            SetupCardUI(cardGO, allCards[i]);
            currentCardObjects.Add(cardGO);
        }

        UpdatePageButtons();
    }

    void UpdatePageButtons()
    {
        for (int i = 0; i < PageButtons.Count; i++)
        {
            var canvasGroup = PageButtons[i].GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = PageButtons[i].gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = (i == currentPage) ? 1f : 0.5f;
        }
    }

    void SetupCardUI(GameObject cardGO, Card card)
    {
        CardInfoUI cardInfo = cardGO.GetComponent<CardInfoUI>();
        cardInfo.SetName(card.Name);
        cardInfo.SetLogo(card.Logo);
        cardInfo.SetStats(card.Attack, card.Health, card.Manacost);

        Destroy(cardGO.GetComponent<CardMovement>());
        Destroy(cardGO.GetComponent<CardAbility>());
    }

    public void LoadMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public void OnOptionsButton()
    {
        optionsPanel.SetActive(!optionsPanel.activeSelf);
    }
}
