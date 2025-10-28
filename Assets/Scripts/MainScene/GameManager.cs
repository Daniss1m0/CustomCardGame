using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Game //Game class in separate file?
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
        list.Add(CardDatabase.AllCards[6].GetCopy()); //example manual add of a specific card
        
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

    public Game currentGame; // public Game CurrentGame { get; private set; }?

    [SerializeField] private TurnManager turnManager;
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private AttackedHero playerHero, enemyHero;
    [SerializeField] public AI enemyAI; //later private or just remove

    public List<CardController> playerHandCards = new List<CardController>(), enemyHandCards = new List<CardController>(),
                                playerFieldCards = new List<CardController>(), enemyFieldCards = new List<CardController>(); //?

    private int turn;

    public bool IsPlayerTurn => turn % 2 == 0;
    public AttackedHero PlayerHero => playerHero;
    public Transform EnemyField => deckManager?.EnemyField;


    private void Awake() 
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        StartGame();
    }

    private void StartGame()
    {
        turn = 0;

        currentGame = new Game();

        if (turnManager == null)
            turnManager = FindAnyObjectByType<TurnManager>();

        if (deckManager == null)
            deckManager = FindAnyObjectByType<DeckManager>();

        deckManager?.GiveInitialHands(currentGame);

        UIManager.Instance.StartGame();

        if (turnManager != null)
            turnManager.StartTurnLoop();
        else
            Debug.LogError("Add TurnManager to scene.");
    }

    public void RestartGame()
    {
        turnManager?.StopTurnLoop(); //unity null conditional operator?

        deckManager?.ClearAll();

        StartGame();
    }

    public void ChangeTurn()
    {
        turn++;
        UIManager.Instance.DisableTurnBtn();

        if (IsPlayerTurn)
        {
            deckManager?.GiveNewCards(currentGame);

            currentGame.player.IncreaseManaPool();
            currentGame.player.RestoreRoundMana();

            UIManager.Instance.UpdateHPAndMana();
        }
        else
        {
            currentGame.enemy.IncreaseManaPool();
            currentGame.enemy.RestoreRoundMana();
        }

        turnManager?.StartTurnLoop();
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

        UIManager.Instance.UpdateHPAndMana();
    }

    public void DamageHero(CardController card, bool isEnemyAttacked) 
    {
        if (isEnemyAttacked)
            currentGame.enemy.GetDamage(card.self.attack);
        else
            currentGame.player.GetDamage(card.self.attack);

        UIManager.Instance.UpdateHPAndMana();
        card.OnDamageDeal();
        CheckForResult();
    }

    public void CheckForResult() 
    {
        if (currentGame.enemy.hp == 0 || currentGame.player.hp == 0)
        {
            turnManager?.StopTurnLoop();

            UIManager.Instance.ShowResult();
        }
    }

    public void CheckCardsForManaAvailability()
    {
        foreach (var card in playerHandCards)
            card.Info.SetManaAvailability(currentGame.player.mana, card.self.manaCost);
    }

    public void HighlightTargets(CardController attacker,bool highlight)
    {
        List<CardController> targets = new List<CardController>();

        if (attacker.self.isSpell)
        {
            var spellCard = (SpellCard)attacker.self;

            switch (spellCard.spellTarget)
            {
                case SpellCard.TargetType.None:
                    targets.Clear();
                    break;

                case SpellCard.TargetType.AllyCard:
                    targets = playerFieldCards;
                    break;

                default:
                    targets = enemyFieldCards;
                    break;
            }
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
                card.Info.HighlightAsSpellTarget(highlight);
            else
                card.Info.HighlightAsTarget(highlight);
        }
    }
}
