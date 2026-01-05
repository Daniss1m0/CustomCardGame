using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Netcode;

public class CollectionManager : MonoBehaviour
{
    public int cardsPerPage;
    public GameObject cardPrefab;
    public Transform cardGrid;
    public List<Button> pageButtons;

    private int currentPage = 0;
    private List<Card> allCards;
    private List<GameObject> currentCardObjects = new();

    void Start()
    {
        if (cardGrid != null)
            for (int i = cardGrid.childCount - 1; i >= 0; i--)
                DestroyImmediate(cardGrid.GetChild(i).gameObject);

        if (CardDatabase.AllCards == null || CardDatabase.AllCards.Count == 0)
        {
            var cardManager = FindFirstObjectByType<CardManager>();
            if (cardManager != null)
                cardManager.Awake();
        }

        if (CardDatabase.AllCards != null)
            allCards = new List<Card>(CardDatabase.AllCards);
        else
            allCards = new List<Card>();

        ShowPage(0);
        SetupButtons();
    }

    void SetupButtons()
    {
        for (int i = 0; i < pageButtons.Count; i++)
        {
            int pageIndex = i;
            pageButtons[i].onClick.RemoveAllListeners();
            pageButtons[i].onClick.AddListener(() => ShowPage(pageIndex));
        }
    }

    void ShowPage(int pageIndex)
    {
        currentPage = pageIndex;

        foreach (GameObject go in currentCardObjects)
            if (go != null) 
                Destroy(go);

        currentCardObjects.Clear();

        if (allCards == null || allCards.Count == 0) 
            return;

        int start = pageIndex * cardsPerPage;
        int end = Mathf.Min(start + cardsPerPage, allCards.Count);

        for (int i = start; i < end; i++)
        {
            GameObject cardGO = Instantiate(cardPrefab, cardGrid, false);

            cardGO.transform.localScale = Vector3.one;
            cardGO.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            if (cardGO.TryGetComponent<RectTransform>(out var rt))
            {
                rt.anchoredPosition = Vector2.zero;
                rt.anchoredPosition3D = Vector3.zero;
            }

            SetupCardUI(cardGO, allCards[i]);
            currentCardObjects.Add(cardGO);
        }

        UpdatePageButtons();
    }

    void UpdatePageButtons()
    {
        for (int i = 0; i < pageButtons.Count; i++)
        {
            if (!pageButtons[i].TryGetComponent<CanvasGroup>(out var canvasGroup))
                canvasGroup = pageButtons[i].gameObject.AddComponent<CanvasGroup>();

            pageButtons[i].gameObject.SetActive(true);

            bool isCurrent = (i == currentPage);

            canvasGroup.alpha = isCurrent ? 1f : 0.5f;
            pageButtons[i].interactable = !isCurrent;
        }
    }

    void SetupCardUI(GameObject cardGO, Card card)
    {
        if (cardGO.TryGetComponent<CardNetwork>(out var netScript)) 
            DestroyImmediate(netScript);

        if (cardGO.TryGetComponent<NetworkObject>(out var netObj)) 
            DestroyImmediate(netObj);

        CardController controller = cardGO.GetComponent<CardController>();
        CardInfo info;

        if (controller != null)
        {
            info = controller.Info;
            Destroy(controller.GetComponent<CardMovement>());
            Destroy(controller.GetComponent<CardAbility>());
            Destroy(controller.GetComponent<AttackedCard>());
            Destroy(controller.GetComponent<SpellTarget>());
            Destroy(controller);
        }
        else
            info = cardGO.GetComponent<CardInfo>();

        if (info != null)
        {
            info.ShowCard(card);
            info.UpdateStats(card);
            info.UpdateDescription(card);

            info.SetAvailability(true, false);
            info.SetHighlight(false);

            if (cardGO.TryGetComponent<CanvasGroup>(out var cg))
            {
                cg.blocksRaycasts = false;
                cg.alpha = 1f;
            }
        }
    }

    public void LoadMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}