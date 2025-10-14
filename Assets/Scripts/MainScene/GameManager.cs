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

    private List<Card> GiveDeckCard()
    {
        List<Card> list = new List<Card>();
        list.Add(CardDatabase.AllCards[6].GetCopy());
        
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

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Game currentGame;

    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform playerHand, enemyHand, playerField, enemyField;
    [SerializeField] private AttackedHero playerHero, enemyHero;
    [SerializeField] private AI enemyAI;
    
    public List<CardController> playerHandCards = new List<CardController>(), enemyHandCards = new List<CardController>(),
                                playerFieldCards = new List<CardController>(), enemyFieldCards = new List<CardController>(); //?
    
    private int turn, turnTime = 30;

    public bool IsPlayerTurn => turn % 2 == 0;
    public AttackedHero PlayerHero => playerHero;
    public Transform EnemyField => enemyField;


    private void Awake() 
    {
        if (Instance == null)
            Instance = this;
    }

    private void Start()
    {
        StartGame();
    }

    private void StartGame()
    {
        turn = 0;

        currentGame = new Game();

        GiveHandCards(currentGame.playerDeck, playerHand);
        GiveHandCards(currentGame.enemyDeck, enemyHand);

        UIController.Instance.StartGame();

        StartCoroutine(TurnFunc());
    }

    private void ClearCards(List<CardController> cards)
    {
        foreach (var card in cards)
            Destroy(card.gameObject);
    }

    public void RestartGame()
    {
        StopAllCoroutines();

        ClearCards(playerHandCards);
        ClearCards(playerFieldCards);
        ClearCards(enemyHandCards);
        ClearCards(enemyFieldCards);

        playerHandCards.Clear();
        playerFieldCards.Clear();
        enemyHandCards.Clear();
        enemyFieldCards.Clear();

        StartGame();
    }

    private void GiveCardToHand(List<Card> deck, Transform hand)
    {
        if (deck.Count == 0)
            return;

        CreateCardPrefab(deck[0], hand);
        deck.RemoveAt(0);
    }

    private void GiveHandCards(List<Card> deck, Transform hand)
    {
        for (int i = 0; i < 4; i++)
            GiveCardToHand(deck, hand);
    }

    private void CreateCardPrefab(Card card, Transform hand)
    {
        GameObject tempCard = Instantiate(cardPrefab, hand, false);
        CardController cardController = tempCard.GetComponent<CardController>();

        cardController.Init(card, hand == playerHand);

        if (cardController.isPlayerCard)
            playerHandCards.Add(cardController);
        else
            enemyHandCards.Add(cardController);
    }

    private IEnumerator TurnFunc() 
    {
        turnTime = 30;
        UIController.Instance.UpdateTurnTime(turnTime);

        foreach (var card in playerFieldCards)
            card.info.HighlightCard(false);

        CheckCardsForManaAvailability();

        if (IsPlayerTurn)
        {
            foreach (var card in playerFieldCards) 
            {
                card.self.canAttack = true;
                card.info.HighlightCard(true);
                card.ability.OnNewTurn();
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
                card.self.canAttack = true;
                card.ability.OnNewTurn();
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
        defender.self.GetDamage(attacker.self.attack);
        attacker.OnDamageDeal();
        defender.OnTakeDamage(attacker);

        attacker.self.GetDamage(defender.self.attack);
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
            currentGame.enemy.GetDamage(card.self.attack);
        else
            currentGame.player.GetDamage(card.self.attack);

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
            card.info.HighlightManaAvaliability(currentGame.player.mana);
    }

    public void HighlightTargets(CardController attacker,bool highlight)
    {
        List<CardController> targets = new List<CardController>();

        if (attacker.self.isSpell)
        {
            var spellCard = (SpellCard)attacker.self;

            if (spellCard.spellTarget == SpellCard.TargetType.NO_TARGET)
                targets = new List<CardController>();
            else if (spellCard.spellTarget == SpellCard.TargetType.ALLY_CARD_TARGET)
                targets = playerFieldCards;
            else
                targets = enemyFieldCards;
        }
        else
        {
            if (enemyFieldCards.Exists(x => x.self.IsProvocation))
                targets = enemyFieldCards.FindAll(x => x.self.IsProvocation);
            else
            {
                targets = enemyFieldCards;
                enemyHero.HighlightAsTarget(highlight);
            }   
        }

        foreach (var card in targets)
        {
            if (attacker.self.isSpell)
                card.info.HighlightAsSpellTarget(highlight);
            else
                card.info.HighlightAsTarget(highlight);
        }
    }
}
