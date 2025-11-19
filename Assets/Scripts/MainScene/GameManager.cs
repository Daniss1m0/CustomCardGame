using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private const float MIN_TIME_BETWEEN_CHANGE = 0.1f;

    public static GameManager Instance;

    public Game currentGame; // public Game CurrentGame { get; private set; }?
    public List<CardController> playerHandCards = new(), enemyHandCards = new(),
                                playerFieldCards = new(), enemyFieldCards = new();

    [SerializeField] private DeckManager deckManager;
    [SerializeField] private AttackedHero playerHero, enemyHero;
    [SerializeField] private MonoBehaviour playerControllerBehaviour, opponentControllerBehaviour;

    private int turn;
    private float lastChangeTime = -10f;

    public IPlayerController PlayerController => playerControllerBehaviour as IPlayerController;
    public IPlayerController OpponentController => opponentControllerBehaviour as IPlayerController;
    public AttackedHero PlayerHero => playerHero;
    public Transform EnemyField => deckManager != null ? deckManager.EnemyField : null;
    public bool IsPlayerTurn => turn % 2 == 0;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        StartCoroutine(SubscribeToTurnNetworkVars());
    }

    private IEnumerator SubscribeToTurnNetworkVars()
    {
        while (TurnManager.Instance == null)
            yield return null;

        TurnManager.Instance.CurrentTurnOwner.OnValueChanged += OnCurrentTurnOwnerChanged;
        TurnManager.Instance.TurnTimeRemaining.OnValueChanged += OnTurnTimeChanged;

        OnCurrentTurnOwnerChanged(0, TurnManager.Instance.CurrentTurnOwner.Value);
        OnTurnTimeChanged(0, TurnManager.Instance.TurnTimeRemaining.Value);
    }

    private void OnCurrentTurnOwnerChanged(ulong oldOwner, ulong newOwner)
    {
        bool amOwner = NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == newOwner;
        Debug.Log($"[GameManager] CurrentTurnOwner changed -> {newOwner}. localIsOwner={amOwner}");

        if (UIManager.Instance != null)
            UIManager.Instance.SetEndTurnInteractable(amOwner);

        CheckCardsForManaAvailability();
    }

    private void OnTurnTimeChanged(int oldTime, int newTime)
    {
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateTurnTime(newTime);
    }

    public void StartGame()
    {
        currentGame = new Game();

        if (deckManager == null)
            deckManager = FindAnyObjectByType<DeckManager>();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            bool playerStarts = deckManager.GiveInitialHandsNetworked(currentGame, randomStart: true);
            turn = playerStarts ? 0 : 1;

            ulong ownerClientId = playerStarts ? NetworkManager.ServerClientId : GetAnyOtherClientId();
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.CurrentTurnOwner.Value = ownerClientId;
                Debug.Log($"[GameManager] CurrentTurnOwner set to {ownerClientId} (playerStarts={playerStarts})");

                TurnManager.Instance.StartServerTurnLoop();
            }
            else
                Debug.LogWarning("[GameManager] TurnNetworkManager.Instance is null when trying to set CurrentTurnOwner/start loop.");

            if (playerStarts)
            {
                currentGame.player.IncreaseManaPool();
                currentGame.player.RestoreRoundMana();
            }
            else
            {
                currentGame.enemy.IncreaseManaPool();
                currentGame.enemy.RestoreRoundMana();
            }

            UIManager.Instance?.UpdateHPAndMana();
        }
        else
        {
            bool playerStarts = deckManager.GiveInitialHands(currentGame, randomStart: true);
            turn = playerStarts ? 0 : 1;

            if (playerStarts)
            {
                currentGame.player.IncreaseManaPool();
                currentGame.player.RestoreRoundMana();
            }
            else
            {
                currentGame.enemy.IncreaseManaPool();
                currentGame.enemy.RestoreRoundMana();
            }

            UIManager.Instance?.UpdateHPAndMana();
        }

        UIManager.Instance?.StartGame();
    }

    public void RestartGame()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            TurnManager.Instance?.StopServerTurnLoop();

        deckManager.ClearAll();

        StartGame();
    }

    public void ChangeTurn()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[GameManager] Client attempted to call ChangeTurn() directly — ignored. Use EndTurnFromController() to request server.");
            return;
        }

        if (Time.realtimeSinceStartup - lastChangeTime < MIN_TIME_BETWEEN_CHANGE)
            return;
        lastChangeTime = Time.realtimeSinceStartup;

        foreach (var c in playerFieldCards)
        {
            if (c == null || c.self == null || c.Info == null)
                continue;

            c.self.canAttack = false;
            c.Info.SetHighlight(false);
        }
        foreach (var c in enemyFieldCards)
        {
            if (c == null || c.self == null || c.Info == null)
                continue;

            c.self.canAttack = false;
            c.Info.SetHighlight(false);
        }

        turn++;
        UIManager.Instance?.DisableTurnBtn();

        if (IsPlayerTurn)
        {
            currentGame.player.ClearTempMana();

            deckManager.GiveNewCards(currentGame);

            currentGame.player.IncreaseManaPool();
            currentGame.player.RestoreRoundMana();

            UIManager.Instance?.UpdateHPAndMana();
        }
        else
        {
            currentGame.enemy.ClearTempMana();

            currentGame.enemy.IncreaseManaPool();
            currentGame.enemy.RestoreRoundMana();

            UIManager.Instance?.UpdateHPAndMana();
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            ulong newOwner = IsPlayerTurn ? NetworkManager.ServerClientId : GetAnyOtherClientId();
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.CurrentTurnOwner.Value = newOwner;
                Debug.Log($"[GameManager] ChangeTurn -> new CurrentTurnOwner set to {newOwner}");
            }
        }
    }

    public void PlayCard(CardController card, bool isPlayerSide)
    {
        if (card == null) return;

        if (isPlayerSide != IsPlayerTurn)
        {
            Debug.LogWarning("Not that side's turn.");
            return;
        }

        if (card.self.isPlaced)
        {
            Debug.LogWarning("Card already placed.");
            return;
        }

        var fieldCount = isPlayerSide ? playerFieldCards.Count : enemyFieldCards.Count;
        if (!card.self.isSpell && fieldCount >= (deckManager != null ? DeckManager.MAX_FIELD_SIZE : 7))
        {
            Debug.LogWarning("Field is full.");
            return;
        }

        if (isPlayerSide && currentGame.player.mana < card.self.manaCost)
        {
            Debug.LogWarning("Player not enough mana.");
            return;
        }
        if (!isPlayerSide && currentGame.enemy.mana < card.self.manaCost)
        {
            Debug.LogWarning("Enemy not enough mana.");
            return;
        }

        card.OnCast();
    }

    public void CastSpell(CardController spell, CardController target, bool isPlayerSide)
    {
        if (spell == null)
            return;

        if (isPlayerSide != IsPlayerTurn)
        {
            Debug.LogWarning("Not that side's turn.");
            return;
        }

        int currentMana = isPlayerSide ? currentGame.player.mana : currentGame.enemy.mana;
        if (currentMana < spell.self.manaCost)
        {
            Debug.LogWarning("Not enough mana.");
            return;
        }

        if (isPlayerSide)
        {
            if (playerHandCards.Contains(spell))
                playerHandCards.Remove(spell);

            playerFieldCards.Add(spell);
        }
        else
        {
            if (enemyHandCards.Contains(spell))
                enemyHandCards.Remove(spell);

            enemyFieldCards.Add(spell);

            spell.Info?.ShowCard(spell.self);
        }

        ReduceMana(isPlayerSide, spell.self.manaCost);

        spell.self.isPlaced = true;

        spell.UseSpell(target);

        CheckCardsForManaAvailability();
    }

    public void Attack(CardController attacker, CardController defender)
    {
        if (attacker == null || defender == null)
            return;
        if (!attacker.self.canAttack)
            return;
        if (!defender.self.isPlaced)
            return;

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
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                ChangeTurn();
                return;
            }

            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.RequestEndTurnServerRpc();
                return;
            }
            else
                return;
        }

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

        UIManager.Instance?.UpdateHPAndMana();
        CheckCardsForManaAvailability();
    }

    public void DamageHero(CardController card, bool isEnemyAttacked)
    {
        if (isEnemyAttacked)
            currentGame.enemy.GetDamage(card.self.attack);
        else
            currentGame.player.GetDamage(card.self.attack);

        UIManager.Instance?.UpdateHPAndMana();
        card.OnDamageDeal();
        CheckForResult();
    }

    public void CheckForResult()
    {
        if (currentGame == null) return;

        if (currentGame.enemy.hp == 0 || currentGame.player.hp == 0)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                TurnManager.Instance?.StopServerTurnLoop();

            UIManager.Instance?.ShowResult();
        }
    }

    public void CheckCardsForManaAvailability()
    {
        bool playerCanAct = IsPlayerTurn;

        foreach (var card in playerHandCards)
        {
            if (card == null || card.Info == null)
                continue;

            bool hasMana = currentGame != null && currentGame.player.mana >= card.self.manaCost;
            card.Info.SetAvailability(hasMana, playerCanAct);
        }

        foreach (var card in playerFieldCards)
        {
            if (card == null || card.Info == null)
                continue;

            card.Info.SetHighlight(card.self.canAttack);
        }
    }

    public void HighlightTargets(CardController attacker, bool highlight)
    {
        List<CardController> targets = new();

        if (attacker.self.isSpell)
        {
            var spellCard = attacker.self as SpellCard;
            if (spellCard == null)
            {
                Debug.LogWarning("HighlightTargets: attacker marked as isSpell but not a SpellCard instance.");
                return;
            }

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

    private ulong GetAnyOtherClientId()
    {
        if (NetworkManager.Singleton == null)
            return NetworkManager.ServerClientId;

        var list = NetworkManager.Singleton.ConnectedClientsList;
        foreach (var client in list)
        {
            if (client.ClientId != NetworkManager.ServerClientId)
                return client.ClientId;
        }

        return NetworkManager.ServerClientId;
    }
}
