using System.Collections.Generic;
using UnityEngine;

public static class CardDatabase //?
{
    public static List<Card> AllCards = new List<Card>();
}

public class CardManager : MonoBehaviour
{
    public List<CardData> allCardData = new List<CardData>();

    public void Awake()
    {
        if (CardDatabase.AllCards != null && CardDatabase.AllCards.Count > 0) 
            return;

        CardDatabase.AllCards = new List<Card>();

        foreach (var data in allCardData)
        {
            if (data == null) 
                continue;

            if (data.isSpell)
                CardDatabase.AllCards.Add(new SpellCard(data));
            else
                CardDatabase.AllCards.Add(new Card(data));
        }

        Debug.Log($"Loaded {CardDatabase.AllCards.Count} cards from CardData.");
    }
}
