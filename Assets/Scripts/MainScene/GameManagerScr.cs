using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Game
{
    public Player player, enemy;
    public List<Card> enemyDeck, playerDeck;

    public Game()
    {
        enemyDeck = GiveDeckCard();
        playerDeck = GiveDeckCard();

        player = new Player();
        enemy = new Player();
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
    public Game CurrentGame;
    public Transform EnemyHand, PlayerHand, EnemyField, PlayerField;
    public GameObject CardPref;
    private int Turn, TurnTime = 30;
    public AttackedHero EnemyHero, PlayerHero;
    public AI EnemyAI;
    public List<CardController> PlayerHandCards = new List<CardController>(), PlayerFieldCards = new List<CardController>(), 
                                EnemyHandCards = new List<CardController>(), EnemyFieldCards = new List<CardController>();

    public bool IsPlayerTurn 
    {
        get
        {
            return Turn % 2 == 0;
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

        foreach (var card in PlayerHandCards)
            Destroy(card.gameObject);
        foreach (var card in PlayerFieldCards)
            Destroy(card.gameObject);
        foreach (var card in EnemyHandCards)
            Destroy(card.gameObject);
        foreach (var card in EnemyFieldCards)
            Destroy(card.gameObject);

        PlayerHandCards.Clear();
        PlayerFieldCards.Clear();
        EnemyHandCards.Clear();
        EnemyFieldCards.Clear();

        StartGame();
    }

    void StartGame()
    {
        Turn = 0;

        CurrentGame = new Game();

        GiveHandCards(CurrentGame.enemyDeck, EnemyHand);
        GiveHandCards(CurrentGame.playerDeck, PlayerHand);

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
        GameObject cardGO = Instantiate(CardPref, hand, false);
        CardController cardC = cardGO.GetComponent<CardController>();

        cardC.Init(card, hand == PlayerHand);

        if (cardC.IsPlayerCard)
            PlayerHandCards.Add(cardC);
        else
            EnemyHandCards.Add(cardC);
    }

    IEnumerator TurnFunc() 
    {
        TurnTime = 30;
        UIController.Instance.UpdateTurnTime(TurnTime);

        foreach (var card in PlayerFieldCards)
            card.Info.HighlightCard(false);

        CheckCardsForManaAvailability();

        if (IsPlayerTurn)
        {
            foreach (var card in PlayerFieldCards) 
            {
                card.Card.CanAttack = true;
                card.Info.HighlightCard(true);
                card.Ability.OnNewTurn();
            }

            while (TurnTime-- > 0)
            {
                UIController.Instance.UpdateTurnTime(TurnTime);
                yield return new WaitForSeconds(1);
            }

            ChangeTurn();
        }
        else
        {
            foreach (var card in EnemyFieldCards)
            {
                card.Card.CanAttack = true;
                card.Ability.OnNewTurn();
            }

            EnemyAI.MakeTurn();

            while (TurnTime-- > 0)
            {
                UIController.Instance.UpdateTurnTime(TurnTime);
                yield return new WaitForSeconds(1);
            }

            ChangeTurn();
        }
    }

    public void ChangeTurn()
    {
        StopAllCoroutines();
        Turn++;
        UIController.Instance.DisableTurnBtn();

        if (IsPlayerTurn)
        {
            GiveNewCards();

            CurrentGame.player.IncreaseManapool();
            CurrentGame.player.RestoreRoundMana();

            UIController.Instance.UpdateHPAndMana();
        }
        else
        {
            CurrentGame.enemy.IncreaseManapool();
            CurrentGame.enemy.RestoreRoundMana();
        }

        StartCoroutine(TurnFunc());
    }

    void GiveNewCards()
    {
        GiveCardToHand(CurrentGame.enemyDeck, EnemyHand);
        GiveCardToHand(CurrentGame.playerDeck, PlayerHand);
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
            CurrentGame.player.mana -= manacost;
        else
            CurrentGame.enemy.mana -= manacost;

        UIController.Instance.UpdateHPAndMana();
    }

    public void DamageHero(CardController card, bool isEnemyAttacked) 
    {
        if (isEnemyAttacked)
            CurrentGame.enemy.GetDamage(card.Card.Attack);
        else
            CurrentGame.player.GetDamage(card.Card.Attack);

        UIController.Instance.UpdateHPAndMana();
        card.OnDamageDeal();
        CheckForResult();
    }

    public void CheckForResult() 
    {
        if (CurrentGame.enemy.hp == 0 || CurrentGame.player.hp == 0)
        {
            StopAllCoroutines();
            UIController.Instance.ShowResult();
        }
    }

    public void CheckCardsForManaAvailability()
    {
        foreach (var card in PlayerHandCards)
            card.Info.HighlightManaAvaliability(CurrentGame.player.mana);
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
                targets = PlayerFieldCards;
            else
                targets = EnemyFieldCards;
        }
        else
        {
            if (EnemyFieldCards.Exists(x => x.Card.IsProvocation))
                targets = EnemyFieldCards.FindAll(x => x.Card.IsProvocation);
            else
            {
                targets = EnemyFieldCards;
                EnemyHero.HighlightAsTarget(highlight);
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
