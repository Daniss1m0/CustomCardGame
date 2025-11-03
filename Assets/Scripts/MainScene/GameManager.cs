using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Game currentGame; // public Game CurrentGame { get; private set; }?
    public List<CardController> playerHandCards = new(), enemyHandCards = new(),
                                playerFieldCards = new(), enemyFieldCards = new();

    [SerializeField] private TurnManager turnManager;
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private AttackedHero playerHero, enemyHero;

    [SerializeField] private MonoBehaviour playerControllerBehaviour;
    [SerializeField] private MonoBehaviour opponentControllerBehaviour;

    private int turn;
    private float lastChangeTime = -10f;
    private const float minTimeBetweenChange = 0.1f;

    public IPlayerController PlayerController => playerControllerBehaviour as IPlayerController;
    public IPlayerController OpponentController => opponentControllerBehaviour as IPlayerController;
    public bool IsPlayerTurn => turn % 2 == 0;
    public AttackedHero PlayerHero => playerHero;
    public Transform EnemyField => deckManager != null ? deckManager.EnemyField : null;

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
        currentGame = new Game();

        if (turnManager == null)
            turnManager = FindAnyObjectByType<TurnManager>();

        if (deckManager == null)
            deckManager = FindAnyObjectByType<DeckManager>();

        PlayerController?.Initialize(currentGame.player, true);
        OpponentController?.Initialize(currentGame.enemy, false);

        bool playerStarts = deckManager.GiveInitialHands(currentGame, randomStart: false);
        turn = playerStarts ? 0 : 1;

        UIManager.Instance.StartGame();

        if (turnManager != null)
            turnManager.StartTurnLoop();
        else
            Debug.LogError("Add TurnManager to scene.");
    }

    public void RestartGame()
    {
        turnManager.StopTurnLoop();

        deckManager.ClearAll();

        StartGame();
    }

    public void ChangeTurn()
    {
        if (Time.realtimeSinceStartup - lastChangeTime < minTimeBetweenChange)
            return;
        lastChangeTime = Time.realtimeSinceStartup;

        if (turnManager != null)
            turnManager.StopTurnLoop();

        turn++;
        UIManager.Instance.DisableTurnBtn();

        if (IsPlayerTurn)
        {
            currentGame.player.ClearTemporaryMana();

            deckManager.GiveNewCards(currentGame);

            currentGame.player.IncreaseManaPool();
            currentGame.player.RestoreRoundMana();

            UIManager.Instance.UpdateHPAndMana();
        }
        else
        {
            currentGame.enemy.ClearTemporaryMana();

            currentGame.enemy.IncreaseManaPool();
            currentGame.enemy.RestoreRoundMana();

            UIManager.Instance.UpdateHPAndMana();
        }

        if (turnManager != null)
            turnManager.StartTurnLoop();
    }
    //why?
    public void PlayCard(CardController card, bool isPlayerSide)
    {
        if (card == null) return;

        if (isPlayerSide != IsPlayerTurn)
        {
            Debug.LogWarning("PlayCard: not that side's turn.");
            return;
        }

        if (card.self.isPlaced)
        {
            Debug.LogWarning("PlayCard: card already placed.");
            return;
        }

        if (isPlayerSide && currentGame.player.mana < card.self.manaCost)
        {
            Debug.LogWarning("PlayCard: player not enough mana.");
            return;
        }
        if (!isPlayerSide && currentGame.enemy.mana < card.self.manaCost)
        {
            Debug.LogWarning("PlayCard: enemy not enough mana.");
            return;
        }

        card.OnCast();
    }

    public void CastSpell(CardController spell, CardController target, bool isPlayerSide)
    {
        if (spell == null) return;

        if (isPlayerSide != IsPlayerTurn)
        {
            Debug.LogWarning("CastSpell: not that side's turn.");
            return;
        }

        int currentMana = isPlayerSide ? currentGame.player.mana : currentGame.enemy.mana;
        if (currentMana < spell.self.manaCost)
        {
            Debug.LogWarning("CastSpell: not enough mana.");
            return;
        }

        if (isPlayerSide)
        {
            if (GameManager.Instance.playerHandCards.Contains(spell))
                GameManager.Instance.playerHandCards.Remove(spell);
            GameManager.Instance.playerFieldCards.Add(spell);
        }
        else
        {
            if (GameManager.Instance.enemyHandCards.Contains(spell))
                GameManager.Instance.enemyHandCards.Remove(spell);
            GameManager.Instance.enemyFieldCards.Add(spell);
            spell.Info?.ShowCard(spell.self);
        }

        ReduceMana(isPlayerSide, spell.self.manaCost);

        spell.self.isPlaced = true;

        spell.UseSpell(target);

        CheckCardsForManaAvailability();
    }


    public void Attack(CardController attacker, CardController defender)
    {
        if (attacker == null || defender == null) return;
        if (!attacker.self.canAttack) return;
        if (!defender.self.isPlaced) return;

        if (attacker.isPlayerCard)
        {
            if (enemyFieldCards.Exists(x => x.self.IsProvocation) && !defender.self.IsProvocation)
                return;
        }
        else
        {
            if (playerFieldCards.Exists(x => x.self.IsProvocation) && !defender.self.IsProvocation)
                return;
        }

        CardsFight(attacker, defender);
    }

    public void AttackHero(CardController attacker, bool targetIsEnemyHero)
    {
        if (attacker == null) 
            return;
        if (!attacker.self.canAttack) 
            return;

        if (targetIsEnemyHero)
        {
            if (enemyFieldCards.Exists(x => x.self.IsProvocation)) 
                return;
            DamageHero(attacker, true);
        }
        else
        {
            if (playerFieldCards.Exists(x => x.self.IsProvocation)) 
                return;
            DamageHero(attacker, false);
        }
    }

    public void EndTurnFromController()
    {
        ChangeTurn();
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
            turnManager.StopTurnLoop();

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
        List<CardController> targets = new();

        if (attacker.self.isSpell)
        {
            var spellCard = (SpellCard)attacker.self;

            switch (spellCard.spellTarget)
            {
                case TargetType.None:

                    targets.Clear();

                    break;

                case TargetType.AllyCard:

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
