using System.Collections.Generic;
using UnityEngine;

public class Game
{
    public Player player, enemy;
    public List<Card> playerDeck, enemyDeck;

    public Game()
    {
        player = new Player();
        enemy = new Player();

        enemy.manaPool = 0; //later will be compensation
        enemy.mana = 0; //still problem with enemy mana UI

        playerDeck = GiveDeckCard();
        enemyDeck = GiveDeckCard();
    }

    private List<Card> GiveDeckCard()
    {
        List<Card> list = new List<Card>();
        list.Add(CardDatabase.AllCards[8].GetCopy()); //example manual add of a specific card

        for (int i = 0; i < 20; i++)
        {
            var card = CardDatabase.AllCards[Random.Range(0, CardDatabase.AllCards.Count)];

            if (card.isSpell)
                list.Add(((SpellCard)card).GetCopy());
            else
                list.Add(card.GetCopy());
        }
        return list;
    }
}
