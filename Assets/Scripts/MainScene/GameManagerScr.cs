using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Game
{
    public Player player, enemy;
    public List<Card> playerDeck, enemyDeck;

    public Game()
    {
        player = new Player();
        enemy = new Player();

        playerDeck = GiveDeckCard();
        enemyDeck = GiveDeckCard();
    }

    List<Card> GiveDeckCard()
    {
        List<Card> list = new List<Card>();
        list.Add(CardManager.AllCards[6].GetCopy());
        for (int i = 0; i < 20; i++)
        {
            var card = CardManager.AllCards[Random.Range(0, CardManager.AllCards.Count)];
            if (card.IsSpell)
                list.Add(((SpellCard)card).GetCopy());
            else
                list.Add(card.GetCopy());
        }
        return list;
    }
}

public class GameManagerScr : MonoBehaviour
{
    public static GameManagerScr Instance;

    public GameObject cardPref;
    public Game currentGame;
    public Transform playerHand, enemyHand, playerField, enemyField;
    public AttackedHero playerHero, enemyHero;
    public AI enemyAI;
    public List<CardController> playerHandCards = new List<CardController>(), enemyHandCards = new List<CardController>(),
                                playerFieldCards = new List<CardController>(), enemyFieldCards = new List<CardController>();
    
    private int turn, turnTime = 30;

    public bool IsPlayerTurn 
    {
        get
        {
            return turn % 2 == 0;
        }
    }

    private void Awake() 
    {
        if (Instance == null)
            Instance = this;
    }

    void Start()
    {
        StartGame();
    }

    public void RestartGame()
    {
        StopAllCoroutines();

        foreach (var card in playerHandCards)
            Destroy(card.gameObject);
        foreach (var card in playerFieldCards)
            Destroy(card.gameObject);
        foreach (var card in enemyHandCards)
            Destroy(card.gameObject);
        foreach (var card in enemyFieldCards)
            Destroy(card.gameObject);

        playerHandCards.Clear();
        playerFieldCards.Clear();
        enemyHandCards.Clear();
        enemyFieldCards.Clear();

        StartGame();
    }

    void StartGame()
    {
        turn = 0;

        currentGame = new Game();

        GiveHandCards(currentGame.playerDeck, playerHand);
        GiveHandCards(currentGame.enemyDeck, enemyHand);

        UIController.Instance.StartGame();

        StartCoroutine(TurnFunc());
    }

    void GiveHandCards(List<Card> deck, Transform hand)
    {
        int i = 0;
        while (i++ < 4)
            GiveCardToHand(deck, hand);
    }

    void GiveCardToHand(List<Card> deck, Transform hand)
    {
        if (deck.Count == 0)
            return;

        CreateCardPref(deck[0], hand);

        deck.RemoveAt(0);
    }

    void CreateCardPref(Card card, Transform hand)
    {
        GameObject cardGO = Instantiate(cardPref, hand, false);
        CardController cardC = cardGO.GetComponent<CardController>();

        cardC.Init(card, hand == playerHand);

        if (cardC.IsPlayerCard)
            playerHandCards.Add(cardC);
        else
            enemyHandCards.Add(cardC);
    }

    IEnumerator TurnFunc() 
    {
        turnTime = 30;
        UIController.Instance.UpdateTurnTime(turnTime);

        foreach (var card in playerFieldCards)
            card.Info.HighlightCard(false);

        CheckCardsForManaAvailability();

        if (IsPlayerTurn)
        {
            foreach (var card in playerFieldCards) 
            {
                card.Card.CanAttack = true;
                card.Info.HighlightCard(true);
                card.Ability.OnNewTurn();
            }

            while (turnTime-- > 0)
            {
                UIController.Instance.UpdateTurnTime(turnTime);
                yield return new WaitForSeconds(1);
            }

            ChangeTurn();
        }
        else
        {
            foreach (var card in enemyFieldCards)
            {
                card.Card.CanAttack = true;
                card.Ability.OnNewTurn();
            }

            enemyAI.MakeTurn();

            while (turnTime-- > 0)
            {
                UIController.Instance.UpdateTurnTime(turnTime);
                yield return new WaitForSeconds(1);
            }

            ChangeTurn();
        }
    }

    public void ChangeTurn()
    {
        StopAllCoroutines();
        turn++;
        UIController.Instance.DisableTurnBtn();

        if (IsPlayerTurn)
        {
            GiveNewCards();

            currentGame.player.IncreaseManapool();
            currentGame.player.RestoreRoundMana();

            UIController.Instance.UpdateHPAndMana();
        }
        else
        {
            currentGame.enemy.IncreaseManapool();
            currentGame.enemy.RestoreRoundMana();
        }

        StartCoroutine(TurnFunc());
    }

    void GiveNewCards()
    {
        GiveCardToHand(currentGame.enemyDeck, enemyHand);
        GiveCardToHand(currentGame.playerDeck, playerHand);
    }

    public void CardsFight(CardController attacker, CardController defender)
    {
        defender.Card.GetDamage(attacker.Card.Attack);
        attacker.OnDamageDeal();
        defender.OnTakeDamage(attacker);

        attacker.Card.GetDamage(defender.Card.Attack);
        attacker.OnTakeDamage();

        attacker.CheckForAlive();
        defender.CheckForAlive();
    }

    public void ReduceMana(bool playerMana, int manacost)
    {
        if (playerMana)
            currentGame.player.mana -= manacost;
        else
            currentGame.enemy.mana -= manacost;

        UIController.Instance.UpdateHPAndMana();
    }

    public void DamageHero(CardController card, bool isEnemyAttacked) 
    {
        if (isEnemyAttacked)
            currentGame.enemy.GetDamage(card.Card.Attack);
        else
            currentGame.player.GetDamage(card.Card.Attack);

        UIController.Instance.UpdateHPAndMana();
        card.OnDamageDeal();
        CheckForResult();
    }

    public void CheckForResult() 
    {
        if (currentGame.enemy.hp == 0 || currentGame.player.hp == 0)
        {
            StopAllCoroutines();
            UIController.Instance.ShowResult();
        }
    }

    public void CheckCardsForManaAvailability()
    {
        foreach (var card in playerHandCards)
            card.Info.HighlightManaAvaliability(currentGame.player.mana);
    }

    public void HighlightTargets(CardController attacker,bool highlight)
    {
        List<CardController> targets = new List<CardController>();

        if (attacker.Card.IsSpell)
        {
            var spellCard = (SpellCard)attacker.Card;

            if (spellCard.SpellTarget == SpellCard.TargetType.NO_TARGET)
                targets = new List<CardController>();
            else if (spellCard.SpellTarget == SpellCard.TargetType.ALLY_CARD_TARGET)
                targets = playerFieldCards;
            else
                targets = enemyFieldCards;
        }
        else
        {
            if (enemyFieldCards.Exists(x => x.Card.IsProvocation))
                targets = enemyFieldCards.FindAll(x => x.Card.IsProvocation);
            else
            {
                targets = enemyFieldCards;
                enemyHero.HighlightAsTarget(highlight);
            }   
        }

        foreach (var card in targets)
        {
            if (attacker.Card.IsSpell)
                card.Info.HighlightAsSpellTarget(highlight);
            else
                card.Info.HighlightAsTarget(highlight);
        }
    }
}
