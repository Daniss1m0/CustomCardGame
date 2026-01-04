using System.Collections.Generic;
using UnityEngine;

public static class CardDatabase
{
    public static List<Card> AllCards = new();
    public static Dictionary<string, CardData> CardDataById = new();
}

public class CardManager : MonoBehaviour
{
    public List<CardData> allCardData = new();

    public void Awake()
    {
        if (CardDatabase.AllCards != null && CardDatabase.AllCards.Count > 0)
            return;

        CardDatabase.AllCards = new List<Card>();
        CardDatabase.CardDataById = new Dictionary<string, CardData>();
        foreach (var data in allCardData)
        {
            if (data == null)
                continue;

            if (!string.IsNullOrEmpty(data.cardId))
                if (!CardDatabase.CardDataById.ContainsKey(data.cardId))
                    CardDatabase.CardDataById.Add(data.cardId, data);

            if (data.isSpell)
                CardDatabase.AllCards.Add(new SpellCard(data));
            else
                CardDatabase.AllCards.Add(new Card(data));
        }
    }
}
