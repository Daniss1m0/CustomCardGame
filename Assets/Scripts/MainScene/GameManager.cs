using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameManager : MonoBehaviour
{
    private const float MIN_TIME_BETWEEN_CHANGE = 0.1f;

    public static GameManager Instance;

    public Game currentGame; // public Game CurrentGame { get; private set; }?
    public List<CardController> playerHandCards = new(), enemyHandCards = new(), playerFieldCards = new(), enemyFieldCards = new();

    [SerializeField] private DeckManager deckManager;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private AttackedHero playerHero, enemyHero;
    [SerializeField] private MonoBehaviour playerControllerBehaviour, opponentControllerBehaviour;

    private int turn;
    private float lastChangeTime = -10f;

    public int CurrentTurn => turn;
    public bool IsPlayerTurn => turn % 2 == 0;
    public AttackedHero PlayerHero => playerHero;
    public Transform EnemyField => deckManager != null ? deckManager.EnemyField : null;
    public IPlayerController PlayerController => playerControllerBehaviour as IPlayerController;
    public IPlayerController OpponentController => opponentControllerBehaviour as IPlayerController;

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

    public void StartGame()
    {
        currentGame = new Game();

        if (deckManager == null)
            deckManager = FindAnyObjectByType<DeckManager>();

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("StartGame called on non-server. Clients should wait for server initialization.");
            UIManager.Instance?.StartGame();
            return;
        }

        bool playerStarts = deckManager.GiveInitialHands(currentGame, randomStart: true);
        turn = playerStarts ? 0 : 1;

        ulong ownerClientId = playerStarts ? NetworkManager.ServerClientId : GetAnyOtherClientId();
        if (turnManager != null)
        {
            turnManager.CurrentTurnOwner.Value = ownerClientId;
            turnManager.NotifyClientsOwnerClientRpc(ownerClientId);
            turnManager.StartServerTurnLoop();
            try { turnManager.SetPlayerOwnerServer(ownerClientId); } catch { }
        }

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

        UpdateManaNetworkIfServer();

        UIManager.Instance?.UpdateHPAndMana();
        UIManager.Instance?.StartGame();

        if (turnManager != null)
            turnManager.NotifyClientsOwnerClientRpc(turnManager.CurrentTurnOwner.Value);
    }

    public void RestartGame()
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("RestartGame can only be called on the server.");
            return;
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            turnManager?.StopServerTurnLoop();

        deckManager.ClearAll();

        StartGame();
    }


    public void ChangeTurn()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
            return;

        if (Time.realtimeSinceStartup - lastChangeTime < MIN_TIME_BETWEEN_CHANGE)
            return;

        lastChangeTime = Time.realtimeSinceStartup;

        foreach (var c in playerFieldCards)
        {
            if (c == null || c.self == null || c.Info == null)
                continue;

            c.Info.SetHighlight(false);
        }
        foreach (var c in enemyFieldCards)
        {
            if (c == null || c.self == null || c.Info == null)
                continue;

            c.Info.SetHighlight(false);
        }
        
        var prevActiveField = IsPlayerTurn ? playerFieldCards : enemyFieldCards;
        foreach (var c in prevActiveField)
        {
            if (c == null || c.self == null || c.Info == null)
                continue;

            c.self.canAttack = false;
            c.Info.SetHighlight(false);

            if (c.Network != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                c.Network.CanAttack.Value = false;
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

        UpdateManaNetworkIfServer();

        List<CardController> activeField = IsPlayerTurn ? playerFieldCards : enemyFieldCards;
        foreach (var card in activeField)
        {
            if (card == null || card.self == null || card.Info == null)
                continue;

            if (!card.self.isPlaced)
            {
                card.self.canAttack = false;
                card.Info.SetHighlight(false);
                if (card.Network != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                    card.Network.CanAttack.Value = false;
                
                continue;
            }

            if (card.placedOnTurn < CurrentTurn)
            {
                card.self.canAttack = true;
                card.Info.SetHighlight(true);
                if (card.Network != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                    card.Network.CanAttack.Value = true;
            }
            else
            {
                card.self.canAttack = false;
                card.Info.SetHighlight(false);
                if (card.Network != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                    card.Network.CanAttack.Value = false;
            }
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            ulong newOwner = IsPlayerTurn ? NetworkManager.ServerClientId : GetAnyOtherClientId();
            if (turnManager != null)
            {
                turnManager.CurrentTurnOwner.Value = newOwner;
                turnManager.NotifyClientsOwnerClientRpc(newOwner);
                turnManager.StopServerTurnLoop();
                turnManager.StartServerTurnLoop();
            }
        }
    }

    public void PlayCard(CardController card, bool isPlayerSide)
    {
        if (card == null) 
            return;

        if (isPlayerSide != IsPlayerTurn)
            return;

        if (card.self.isPlaced)
            return;

        var fieldCount = isPlayerSide ? playerFieldCards.Count : enemyFieldCards.Count;
        if (!card.self.isSpell && fieldCount >= (deckManager != null ? DeckManager.MAX_FIELD_SIZE : 7))
        {
            Debug.LogWarning("Field is full.");
            return;
        }

        if (isPlayerSide && currentGame.player.mana < card.self.manaCost)
            return;

        if (!isPlayerSide && currentGame.enemy.mana < card.self.manaCost)
            return;

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
            if (enemyFieldCards.Exists(x => x.self.IsProvocation) && !defender.self.IsProvocation)
                return;
        else
            if (playerFieldCards.Exists(x => x.self.IsProvocation) && !defender.self.IsProvocation)
                return;

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

            if (turnManager != null)
            {
                turnManager.RequestEndTurnServerRpc();
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

        UpdateManaNetworkIfServer();

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
        if (currentGame == null) 
            return;

        if (currentGame.enemy.hp == 0 || currentGame.player.hp == 0)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                turnManager?.StopServerTurnLoop();

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

    private IEnumerator SubscribeToTurnNetworkVars()
    {
        if (turnManager == null)
            turnManager = FindFirstObjectByType<TurnManager>();

        while (turnManager == null)
        {
            yield return null;
            turnManager = FindFirstObjectByType<TurnManager>();
        }

        if (turnManager.TryGetComponent<Unity.Netcode.NetworkObject>(out var no))
        {
            while (!no.IsSpawned)
                yield return null;
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (currentGame == null)
                currentGame = new Game();
        }

        if (turnManager != null)
        {
            turnManager.CurrentTurnOwner.OnValueChanged += OnCurrentTurnOwnerChanged;
            turnManager.TurnTimeRemaining.OnValueChanged += OnTurnTimeChanged;

            turnManager.PlayerMana.OnValueChanged += (oldV, newV) => ApplyNetworkManaValues();
            turnManager.EnemyMana.OnValueChanged += (oldV, newV) => ApplyNetworkManaValues();
            turnManager.PlayerOwner.OnValueChanged += (oldV, newV) => ApplyNetworkManaValues();

            OnCurrentTurnOwnerChanged(0, turnManager.CurrentTurnOwner.Value);
            OnTurnTimeChanged(0, turnManager.TurnTimeRemaining.Value);

            ApplyNetworkManaValues();
        }
    }

    private void OnCurrentTurnOwnerChanged(ulong oldOwner, ulong newOwner)
    {
        bool amOwner = NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == newOwner;

        if (UIManager.Instance != null)
            UIManager.Instance.SetEndTurnInteractable(amOwner);

        CheckCardsForManaAvailability();
    }

    private void OnTurnTimeChanged(int oldTime, int newTime)
    {
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateTurnTime(newTime);
    }

    private void UpdateManaNetworkIfServer()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) 
            return;

        if (turnManager == null) 
            turnManager = FindFirstObjectByType<TurnManager>();

        if (turnManager == null) 
            return;

        try 
        { 
            turnManager.SetPlayerManaServer(currentGame.player.mana); 
        } catch { }

        try 
        { 
            turnManager.SetEnemyManaServer(currentGame.enemy.mana); 
        } catch { }
    }

    private void ApplyNetworkManaValues()
    {
        if (turnManager == null || NetworkManager.Singleton == null) 
            return;

        if (currentGame == null)
            currentGame = new Game();

        ulong playerOwnerClientId = turnManager.PlayerOwner.Value;
        if (playerOwnerClientId == 0 && NetworkManager.Singleton != null)
            playerOwnerClientId = NetworkManager.ServerClientId;

        bool localIsPlayerOwner = NetworkManager.Singleton.LocalClientId == playerOwnerClientId;

        int playerManaNet = turnManager.PlayerMana.Value;
        int enemyManaNet = turnManager.EnemyMana.Value;

        if (localIsPlayerOwner)
        {
            currentGame.player.mana = playerManaNet;
            currentGame.enemy.mana = enemyManaNet;
        }
        else
        {
            currentGame.player.mana = enemyManaNet;
            currentGame.enemy.mana = playerManaNet;
        }

        UIManager.Instance?.UpdateHPAndMana();
        CheckCardsForManaAvailability();
    }
}
