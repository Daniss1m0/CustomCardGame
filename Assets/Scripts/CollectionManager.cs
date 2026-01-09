using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Netcode; // For NetworkObject

public class CollectionManager : MonoBehaviour
{
    public int cardsPerPage;
    public GameObject cardPfb;
    public Transform cardGrd;
    public List<Button> pageBtns;

    private int currentPage = 0;
    private List<Card> allCards;
    private List<GameObject> currentCardObjs = new();

    private void Start()
    {
        if (cardGrd != null)
            for (int i = cardGrd.childCount - 1; i >= 0; i--)
                DestroyImmediate(cardGrd.GetChild(i).gameObject);

        if (CardDatabase.AllCards != null)
            allCards = new List<Card>(CardDatabase.AllCards);
        else
            allCards = new List<Card>();

        ShowPage(0);
        SetupPageBtns();
    }

    private void SetupPageBtns()
    {
        for (int i = 0; i < pageBtns.Count; i++)
        {
            int pageIndex = i;
            pageBtns[i].onClick.RemoveAllListeners();
            pageBtns[i].onClick.AddListener(() => ShowPage(pageIndex));
        }
    }

    private void ShowPage(int pageIndex)
    {
        currentPage = pageIndex;

        foreach (GameObject go in currentCardObjs)
            if (go != null)
                Destroy(go);

        currentCardObjs.Clear();

        if (allCards.Count == 0)
            return;

        int start = pageIndex * cardsPerPage;
        int end = Mathf.Min(start + cardsPerPage, allCards.Count);

        for (int i = start; i < end; i++)
        {
            GameObject cardGO = Instantiate(cardPfb, cardGrd, false);

            cardGO.transform.localScale = Vector3.one;
            cardGO.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            if (cardGO.TryGetComponent<RectTransform>(out var rt))
            {
                rt.anchoredPosition = Vector2.zero;
                rt.anchoredPosition3D = Vector3.zero;
            }

            SetupCardUI(cardGO, allCards[i]);
            currentCardObjs.Add(cardGO);
        }

        UpdatePageBtns();
    }

    private void UpdatePageBtns()
    {
        for (int i = 0; i < pageBtns.Count; i++)
        {
            if (!pageBtns[i].TryGetComponent<CanvasGroup>(out var canvasGroup))
                canvasGroup = pageBtns[i].gameObject.AddComponent<CanvasGroup>();

            pageBtns[i].gameObject.SetActive(true);

            bool isCurrent = (i == currentPage);

            canvasGroup.alpha = isCurrent ? 1f : 0.5f;
            pageBtns[i].interactable = !isCurrent;
        }
    }

    private void SetupCardUI(GameObject cardGO, Card card)
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
                cg.alpha = 1f;
        }
    }

    public void OnBackBtn() => UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
}