using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameManager : MonoBehaviour
{
    private const float MIN_TIME_BETWEEN_CHANGE = 0.1f;

    public static GameManager Instance;

    public Game currentGame;
    public List<CardController> playerHandCards = new(), enemyHandCards = new(), playerFieldCards = new(), enemyFieldCards = new();

    [SerializeField] private DeckManager deckManager;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private AttackedHero playerHero, enemyHero;

    private int turn;
    private float lastChangeTime = -10f;
    private bool localIsOwnerTurn = false, pendingRestartSync = false;

    public int CurrentTurn => turn;
    public bool IsPlayerTurn => turn % 2 == 0;
    public bool IsGameOver { get; private set; } = false;
    public AttackedHero PlayerHero => playerHero;
    public AttackedHero EnemyHero => enemyHero;

    public bool IsMyTurn
    {
        get
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
                return localIsOwnerTurn;

            return IsPlayerTurn;
        }
    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (CardDatabase.AllCards != null && CardDatabase.AllCards.Count > 0)
            currentGame ??= new Game();
    }

    private void Start()
    {
        currentGame ??= new Game();

        StartCoroutine(SubscribeToTurnNetworkVars());

        StartCoroutine(ServerWaitForPlayersAndStart());
    }

    private IEnumerator ServerWaitForPlayersAndStart()
    {
        while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            yield return null;

        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log("Server started. Waiting for opponent...");

            while (NetworkManager.Singleton.ConnectedClientsList.Count < 2)
                yield return new WaitForSeconds(0.5f);

            Debug.Log("Opponent connected!");

            if (turnManager == null)
                turnManager = FindFirstObjectByType<TurnManager>();

            if (turnManager != null)
            {
                var no = turnManager.GetComponent<NetworkObject>();
                if (no != null && !no.IsSpawned)
                    try
                    {
                        no.Spawn();
                        Debug.Log("TurnManager Spawned.");
                    }
                    catch (System.Exception e) { Debug.LogWarning($"Failed to spawn TurnManager: {e}"); }
            }

            yield return new WaitForSeconds(0.5f);

            StartGame();
        }
    }

    public void StartGame()
    {
        IsGameOver = false;

        currentGame = new Game();

        if (deckManager == null)
            deckManager = FindAnyObjectByType<DeckManager>();

        if (turnManager == null)
            turnManager = FindFirstObjectByType<TurnManager>();

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            UIManager.Instance.StartGame();
            return;
        }

        ulong playerSideOwnerClientId = NetworkManager.ServerClientId;
        ulong otherClientId = GetAnyOtherClientId();

        bool playerStarts = deckManager.GiveInitialHands(currentGame, playerSideOwnerClientId, otherClientId, randomStart: true);
        turn = playerStarts ? 0 : 1;

        ulong startingTurnOwner = playerStarts ? playerSideOwnerClientId : otherClientId;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && deckManager != null)
            deckManager.GiveNewCards(currentGame, startingTurnOwner);

        if (turnManager != null)
        {
            turnManager.currentTurnOwner.Value = startingTurnOwner;
            turnManager.NotifyClientsOwnerClientRpc(startingTurnOwner);
            turnManager.StartServerTurnLoop();
            try
            {
                turnManager.SetPlayerOwnerServer(playerSideOwnerClientId);
            }
            catch { }
        }

        if (startingTurnOwner == playerSideOwnerClientId)
        {
            currentGame.player.IncreaseManaPool();
            currentGame.player.RestoreRoundMana();
        }
        else
        {
            currentGame.enemy.IncreaseManaPool();
            currentGame.enemy.RestoreRoundMana();
        }

        UpdateStateNetworkIfServer();
        UIManager.Instance.UpdateHPAndMana();

        if (UIManager.Instance != null)
            UIManager.Instance.StartGame();

        if (turnManager != null)
            turnManager.NotifyClientsOwnerClientRpc(turnManager.currentTurnOwner.Value);
    }

    public void ResetClientState()
    {
        IsGameOver = false;

        if (UIManager.Instance != null)
            UIManager.Instance.StartGame();

        currentGame = new Game();

        pendingRestartSync = true;

        if (deckManager != null)
            deckManager.ClearAll();

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateHPAndMana();
    }

    public void ChangeTurn()
    {
        SanitizeLists();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            return;

        if (Time.realtimeSinceStartup - lastChangeTime < MIN_TIME_BETWEEN_CHANGE)
            return;

        lastChangeTime = Time.realtimeSinceStartup;

        foreach (var c in playerFieldCards)
            if (c.Info != null)
                c.Info.SetHighlight(false);

        foreach (var c in enemyFieldCards)
            if (c.Info != null)
                c.Info.SetHighlight(false);

        var prevActiveField = IsPlayerTurn ? playerFieldCards : enemyFieldCards;
        foreach (var c in prevActiveField)
        {
            if (c == null || c.self == null)
                continue;

            c.self.canAttack = false;

            c.Info.SetHighlight(false);

            if (c.Network != null && NetworkManager.Singleton.IsServer)
                c.Network.canAttack.Value = false;
        }

        turn++;
        UIManager.Instance.DisableTurnBtn();

        if (NetworkManager.Singleton.IsServer)
        {
            ulong playerOwnerClientId = NetworkManager.ServerClientId;
            if (turnManager != null && turnManager.playerOwner.Value != 0UL)
                playerOwnerClientId = turnManager.playerOwner.Value;

            ulong otherClientId = GetOtherClientOf(playerOwnerClientId);
            ulong newOwner = IsPlayerTurn ? playerOwnerClientId : otherClientId;

            if (turnManager != null)
            {
                turnManager.currentTurnOwner.Value = newOwner;
                turnManager.NotifyClientsOwnerClientRpc(newOwner);
                turnManager.StopServerTurnLoop();
                turnManager.StartServerTurnLoop();
            }

            currentGame.player.ClearTempMana();
            currentGame.enemy.ClearTempMana();
            deckManager.GiveNewCards(currentGame, newOwner);

            if (newOwner == playerOwnerClientId)
            {
                currentGame.player.IncreaseManaPool();
                currentGame.player.RestoreRoundMana();
            }
            else
            {
                currentGame.enemy.IncreaseManaPool();
                currentGame.enemy.RestoreRoundMana();
            }

            UIManager.Instance.UpdateHPAndMana();
            UpdateStateNetworkIfServer();
        }

        List<CardController> activeField = IsPlayerTurn ? playerFieldCards : enemyFieldCards;
        foreach (var card in activeField)
        {
            if (card == null || card.self == null)
                continue;

            card.OnNewTurn();

            if (!card.self.isPlaced)
                continue;

            if (card.placedOnTurn < CurrentTurn)
            {
                card.self.canAttack = true;
                card.SetCanAttackVisual(true);
                if (card.Network != null && NetworkManager.Singleton.IsServer)
                    card.Network.canAttack.Value = true;
            }
            else
            {
                if (!card.self.abilities.Contains(AbilityType.Charge))
                {
                    card.self.canAttack = false;
                    card.SetCanAttackVisual(false);
                    if (card.Network != null && NetworkManager.Singleton.IsServer)
                        card.Network.canAttack.Value = false;
                }
            }
        }
    }

    public bool PlayCard(CardController card, bool isPlayerSide, int slotIndex = -1)
    {
        SanitizeLists();

        if (card == null) 
            return false;

        if (isPlayerSide != IsPlayerTurn) 
            return false;

        if (card.self.isPlaced) 
            return false;

        var fieldCount = isPlayerSide ? playerFieldCards.Count : enemyFieldCards.Count;
        if (!card.self.isSpell && fieldCount >= (deckManager != null ? DeckManager.MAX_FIELD_SIZE : 7))
            return false;

        if (isPlayerSide && currentGame.player.mana < card.self.manaCost)
            return false;

        if (!isPlayerSide && currentGame.enemy.mana < card.self.manaCost)
            return false;

        card.OnCast(slotIndex);
        return true;
    }

    public void CastSpell(CardController spell, CardController target, bool isPlayerSide)
    {
        if (spell == null) 
            return;

        if (isPlayerSide != IsPlayerTurn) 
            return;

        int currentMana = isPlayerSide ? currentGame.player.mana : currentGame.enemy.mana;
        if (currentMana < spell.self.manaCost) 
            return;

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
            spell.Info.ShowCard(spell.self);
        }

        ReduceMana(isPlayerSide, spell.self.manaCost);
        spell.self.isPlaced = true;
        spell.UseSpell(target);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            UpdateStateNetworkIfServer();

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
            else if (playerFieldCards.Exists(x => x.self.IsProvocation) && !defender.self.IsProvocation)
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

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            UpdateStateNetworkIfServer();

        UIManager.Instance.UpdateHPAndMana();
        CheckCardsForManaAvailability();
    }

    public void DamageHero(CardController card, bool isEnemyAttacked)
    {
        if (isEnemyAttacked)
            currentGame.enemy.GetDamage(card.self.attack);
        else
            currentGame.player.GetDamage(card.self.attack);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            UpdateStateNetworkIfServer();

        UIManager.Instance.UpdateHPAndMana();
        card.OnDamageDeal();
        CheckForResult();
    }

    public void CheckCardsForManaAvailability()
    {
        SanitizeLists();
        bool playerCanAct;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            playerCanAct = localIsOwnerTurn;
        else
            playerCanAct = IsPlayerTurn;

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

    public void CheckForResult()
    {
        if (currentGame == null || IsGameOver)
            return;

        if (currentGame.enemy.hp <= 0 || currentGame.player.hp <= 0)
        {
            IsGameOver = true;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                turnManager.StopServerTurnLoop();

            Transform deadHeroTransform = null;

            if (currentGame.player.hp <= 0 && playerHero != null)
                deadHeroTransform = playerHero.transform;
            else if (currentGame.enemy.hp <= 0 && enemyHero != null)
                deadHeroTransform = enemyHero.transform;

            if (AnimationManager.Instance != null && deadHeroTransform != null)
                AnimationManager.Instance.PlayHeroDeath(deadHeroTransform, () =>
                {
                    UIManager.Instance.ShowResult();
                });
            else
                UIManager.Instance.ShowResult();
        }
    }

    public void HighlightTargets(CardController attacker, bool highlight)
    {
        List<CardController> targets = new();
        if (attacker.self.isSpell)
        {
            if (attacker.self is not SpellCard spellCard)
                return;

            switch (spellCard.spellTarget)
            {
                case TargetType.None: targets.Clear(); break;
                case TargetType.AllyCard: targets = playerFieldCards; break;
                default: targets = enemyFieldCards; break;
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

    public void SendRestartVote()
    {
        if (turnManager == null)
            turnManager = FindFirstObjectByType<TurnManager>();

        if (turnManager != null && NetworkManager.Singleton.IsListening)
            turnManager.RequestRestartVoteServerRpc();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void CleanupNetworkCards()
    {
        List<CardController> allCards = new();
        allCards.AddRange(playerHandCards);
        allCards.AddRange(enemyHandCards);
        allCards.AddRange(playerFieldCards);
        allCards.AddRange(enemyFieldCards);

        foreach (var card in allCards)
        {
            if (card != null && card.Network != null)
            {
                if (card.Network.TryGetComponent<NetworkObject>(out var no))
                    if (no.IsSpawned)
                        no.Despawn(true);
            }
            else if (card != null)
            {
                Destroy(card.gameObject);
            }
        }
        SanitizeLists();
    }

    public void SanitizeLists()
    {
        playerHandCards.RemoveAll(x => x == null || x.gameObject == null);
        playerFieldCards.RemoveAll(x => x == null || x.gameObject == null);
        enemyHandCards.RemoveAll(x => x == null || x.gameObject == null);
        enemyFieldCards.RemoveAll(x => x == null || x.gameObject == null);
    }

    private ulong GetAnyOtherClientId()
    {
        if (NetworkManager.Singleton == null)
            return NetworkManager.ServerClientId;

        foreach (var kv in NetworkManager.Singleton.ConnectedClients)
        {
            var id = kv.Key;
            if (id != NetworkManager.ServerClientId)
                return id;
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

        if (turnManager.TryGetComponent<NetworkObject>(out var no))
            while (!no.IsSpawned)
                yield return null;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            currentGame ??= new Game();

        if (turnManager != null)
        {
            turnManager.currentTurnOwner.OnValueChanged += OnCurrentTurnOwnerChanged;
            turnManager.turnTimeRemaining.OnValueChanged += OnTurnTimeChanged;

            turnManager.playerMana.OnValueChanged += (oldV, newV) => ApplyNetworkStateValues();
            turnManager.enemyMana.OnValueChanged += (oldV, newV) => ApplyNetworkStateValues();
            turnManager.playerHP.OnValueChanged += (oldV, newV) => ApplyNetworkStateValues();
            turnManager.enemyHP.OnValueChanged += (oldV, newV) => ApplyNetworkStateValues();

            turnManager.playerOwner.OnValueChanged += (oldV, newV) => ApplyNetworkStateValues();

            OnCurrentTurnOwnerChanged(0, turnManager.currentTurnOwner.Value);
            OnTurnTimeChanged(0, turnManager.turnTimeRemaining.Value);

            ApplyNetworkStateValues();
        }
    }

    private void OnCurrentTurnOwnerChanged(ulong oldOwner, ulong newOwner)
    {
        void turnChangeAction()
        {
            bool amOwner = NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == newOwner;
            localIsOwnerTurn = amOwner;

            if (UIManager.Instance != null)
                UIManager.Instance.SetEndTurnInteractable(amOwner);

            CheckCardsForManaAvailability();
        }

        if (AnimationManager.Instance != null)
            AnimationManager.Instance.EnqueueVisual(turnChangeAction);
        else
            turnChangeAction();
    }

    private void OnTurnTimeChanged(int oldTime, int newTime)
    {
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateTurnTime(newTime);
    }

    public void UpdateStateNetworkIfServer()
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
        }
        catch { }
        try
        {
            turnManager.SetEnemyManaServer(currentGame.enemy.mana);
        }
        catch { }

        try
        {
            turnManager.SetPlayerHPServer(currentGame.player.hp);
        }
        catch { }
        try
        {
            turnManager.SetEnemyHPServer(currentGame.enemy.hp);
        }
        catch { }
    }

    public void UpdateManaNetworkIfServerPublic()
    {
        UpdateStateNetworkIfServer();
    }

    private void ApplyNetworkStateValues()
    {
        if (turnManager == null || NetworkManager.Singleton == null)
            return;

        void updateStatsAction()
        {
            currentGame ??= new Game();

            ulong playerOwnerClientId = turnManager.playerOwner.Value;
            if (playerOwnerClientId == 0 && NetworkManager.Singleton != null)
                playerOwnerClientId = NetworkManager.ServerClientId;

            bool localIsPlayerOwner = NetworkManager.Singleton.LocalClientId == playerOwnerClientId;

            int pMana = turnManager.playerMana.Value;
            int eMana = turnManager.enemyMana.Value;
            int pHP = turnManager.playerHP.Value;
            int eHP = turnManager.enemyHP.Value;

            if (pendingRestartSync)
            {
                if (pHP > 0 && eHP > 0)
                    pendingRestartSync = false;
                else
                    return;
            }

            if (localIsPlayerOwner)
            {
                currentGame.player.mana = pMana;
                currentGame.enemy.mana = eMana;
                currentGame.player.hp = pHP;
                currentGame.enemy.hp = eHP;
            }
            else
            {
                currentGame.player.mana = eMana;
                currentGame.enemy.mana = pMana;
                currentGame.player.hp = eHP;
                currentGame.enemy.hp = pHP;
            }

            UIManager.Instance.UpdateHPAndMana();
            CheckCardsForManaAvailability();
            CheckForResult();
        }

        if (AnimationManager.Instance != null)
            AnimationManager.Instance.EnqueueVisual(updateStatsAction);
        else
            updateStatsAction();
    }

    private ulong GetOtherClientOf(ulong clientId)
    {
        if (NetworkManager.Singleton == null)
            return NetworkManager.ServerClientId;

        foreach (var kv in NetworkManager.Singleton.ConnectedClients)
        {
            var id = kv.Key;
            if (id != NetworkManager.ServerClientId)
                return id;
        }
        return NetworkManager.ServerClientId;
    }

    private void OnDestroy()
    {
        if (turnManager != null)
        {
            turnManager.currentTurnOwner.OnValueChanged -= OnCurrentTurnOwnerChanged;
            turnManager.turnTimeRemaining.OnValueChanged -= OnTurnTimeChanged;
        }

        if (Instance == this)
            Instance = null;
    }
}