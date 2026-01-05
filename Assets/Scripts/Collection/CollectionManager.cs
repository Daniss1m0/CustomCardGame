using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Netcode;

public class CollectionManager : MonoBehaviour
{
    public int ñardsPerPage;
    public GameObject ñardPrefab;
    public Transform ñardGrid;
    public List<Button> pageButtons;

    private int currentPage = 0;
    private List<Card> allCards;
    private List<GameObject> currentCardObjects = new();

    void Start()
    {
        if (ñardGrid != null)
            for (int i = ñardGrid.childCount - 1; i >= 0; i--)
                DestroyImmediate(ñardGrid.GetChild(i).gameObject);

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

        int start = pageIndex * ñardsPerPage;
        int end = Mathf.Min(start + ñardsPerPage, allCards.Count);

        for (int i = start; i < end; i++)
        {
            GameObject cardGO = Instantiate(ñardPrefab, ñardGrid, false);

            cardGO.transform.localScale = Vector3.one;
            cardGO.transform.localPosition = Vector3.zero;
            cardGO.transform.localRotation = Quaternion.identity;

            var rt = cardGO.GetComponent<RectTransform>();
            if (rt != null)
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
            var canvasGroup = pageButtons[i].GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = pageButtons[i].gameObject.AddComponent<CanvasGroup>();

            pageButtons[i].gameObject.SetActive(true);

            bool isCurrent = (i == currentPage);

            canvasGroup.alpha = isCurrent ? 1f : 0.5f;
            pageButtons[i].interactable = !isCurrent;
        }
    }

    void SetupCardUI(GameObject cardGO, Card card)
    {
        var netScript = cardGO.GetComponent<CardNetwork>();
        if (netScript != null) 
            DestroyImmediate(netScript);

        var netObj = cardGO.GetComponent<NetworkObject>();
        if (netObj != null) 
            DestroyImmediate(netObj);

        CardController controller = cardGO.GetComponent<CardController>();
        CardInfo info = null;

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

            var cg = cardGO.GetComponent<CanvasGroup>();
            if (cg != null)
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