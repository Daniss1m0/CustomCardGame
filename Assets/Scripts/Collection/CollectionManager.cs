using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CollectionManager : MonoBehaviour
{
    public GameObject CardPrefab;
    public Transform CardGrid;

    void Start()
    {
        LoadCards();
    }

    void LoadCards()
    {
        foreach (Card card in CardManager.AllCards)
        {
            GameObject cardGO = Instantiate(CardPrefab, CardGrid);
            SetupCardUI(cardGO, card);
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
}