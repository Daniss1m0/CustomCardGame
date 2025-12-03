using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class CardController : MonoBehaviour
{
    public bool isPlayerCard;
    public Card self;

    [SerializeField] private CardInfo info;
    [SerializeField] private CardMovement movement;
    [SerializeField] private CardAbility ability;

    private GameManager gameManager;
    private CardNetwork linkedNetwork;

    [HideInInspector] public int placedOnTurn = -1;
    private bool pendingServerAction = false;

    public CardInfo Info => info;
    public CardMovement Movement => movement;
    public CardAbility Ability => ability;
    public CardNetwork Network => linkedNetwork;

    public void Init(Card card, bool isPlayerCard)
    {
        self = card;
        this.isPlayerCard = isPlayerCard;
        gameManager = GameManager.Instance;
        if (movement == null)
            movement = GetComponent<CardMovement>();

        if (isPlayerCard)
            info.ShowCard(self);
        else
            info.HideCard();
    }

    public void OnCast()
    {
        if (self.isSpell)
        {
            if (self is SpellCard spellCard)
                if (spellCard.spellTarget != TargetType.None)
                {
                    Debug.Log($"Spell requires target: {spellCard.spellTarget}");
                    return;
                }
            else
            {
                Debug.LogError($"Card {self.name} has isSpell=true but is not SpellCard!");
                return;
            }
        }

        if (gameManager == null) 
            gameManager = GameManager.Instance;

        if (linkedNetwork != null && NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                if (gameManager != null)
                {
                    bool sideIsPlayerTurn = gameManager.IsPlayerTurn == isPlayerCard;
                    if (!sideIsPlayerTurn)
                        return;
                }

                int currentMana = isPlayerCard ? gameManager.currentGame.player.mana : gameManager.currentGame.enemy.mana;
                if (currentMana < self.manaCost)
                    return;

                var dm = FindAnyObjectByType<DeckManager>();

                if (isPlayerCard)
                    gameManager.playerFieldCards.RemoveAll(c => c == null || c.gameObject == null || c.Equals(null));
                else
                    gameManager.enemyFieldCards.RemoveAll(c => c == null || c.gameObject == null || c.Equals(null));

                int fieldCount = isPlayerCard ? gameManager.playerFieldCards.Count : gameManager.enemyFieldCards.Count;

                if (!self.isSpell && fieldCount >= (dm != null ? DeckManager.MAX_FIELD_SIZE : 7))
                {
                    Debug.LogError($"Field is full! Count: {fieldCount}");
                    return;
                }

                var cg = GetComponent<CanvasGroup>();
                if (cg != null) 
                { 
                    cg.blocksRaycasts = true; 
                    cg.interactable = true; 
                }

                try
                {
                    if (!self.isSpell)
                    {
                        linkedNetwork.isPlaced.Value = true;
                        linkedNetwork.canAttack.Value = false;
                        linkedNetwork.placedOnTurn.Value = (gameManager != null ? gameManager.CurrentTurn : 0);
                    }
                }
                catch (System.Exception e) 
                { 
                    Debug.LogError($"Network Variable Error: {e.Message}"); 
                }

                if (gameManager != null)
                {
                    if (isPlayerCard)
                    {
                        if (gameManager.playerHandCards.Contains(this))
                            gameManager.playerHandCards.Remove(this);

                        if (!self.isSpell)
                        {
                            if (!gameManager.playerFieldCards.Contains(this))
                                gameManager.playerFieldCards.Add(this);
                        }

                        gameManager.ReduceMana(true, self.manaCost);
                        gameManager.CheckCardsForManaAvailability();
                    }
                    else
                    {
                        if (gameManager.enemyHandCards.Contains(this))
                            gameManager.enemyHandCards.Remove(this);

                        if (!self.isSpell)
                        {
                            if (!gameManager.enemyFieldCards.Contains(this))
                                gameManager.enemyFieldCards.Add(this);

                            if (dm != null) 
                                transform.SetParent(dm.EnemyField, false);
                        }

                        gameManager.ReduceMana(false, self.manaCost);
                        info.ShowCard(self);
                    }
                }

                placedOnTurn = gameManager != null ? gameManager.CurrentTurn : -1;
                self.canAttack = false;
                info.SetHighlight(false);

                if (!self.isSpell)
                    self.isPlaced = true;

                if (self.HasAbility)
                    ability.OnCast(self, isPlayerCard, info);

                if (self.isSpell)
                    UseSpell(null);
            }
            else
            {
                try
                {
                    if (self.isSpell)
                    {
                        var sc = (SpellCard)self;
                        linkedNetwork.RequestCastSpellServerRpc((int)sc.spell, (int)sc.spellTarget, sc.spellPower, 0);
                        pendingServerAction = true;
                        if (movement != null) 
                        { 
                            movement.OnEndDrag(null); 
                            movement.enabled = false; 
                        }
                        var cg = GetComponent<CanvasGroup>();
                        if (cg != null) 
                        { 
                            cg.interactable = false; 
                            cg.blocksRaycasts = false; 
                        }
                    }
                    else
                    {
                        info.SetHighlight(false);
                        linkedNetwork.RequestPlaceCardServerRpc(isPlayerCard);
                    }
                }
                catch { }
            }
            return;
        }

        placedOnTurn = gameManager != null ? gameManager.CurrentTurn : -1;
        self.canAttack = false;
        info.SetHighlight(false);

        if (isPlayerCard)
        {
            gameManager.playerHandCards.Remove(this);
            if (!gameManager.playerFieldCards.Contains(this))
                gameManager.playerFieldCards.Add(this);
            gameManager.ReduceMana(true, self.manaCost);
            gameManager.CheckCardsForManaAvailability();
        }
        else
        {
            gameManager.enemyHandCards.Remove(this);
            if (!gameManager.enemyFieldCards.Contains(this))
                gameManager.enemyFieldCards.Add(this);
            gameManager.ReduceMana(false, self.manaCost);
            info.ShowCard(self);
        }

        self.isPlaced = true;
        if (self.HasAbility) 
            ability.OnCast(self, isPlayerCard, info);
        if (self.isSpell) 
            UseSpell(null);

        UIManager.Instance.UpdateHPAndMana();
    }

    public void OnTakeDamage(CardController attacker = null)
    {
        CheckForAlive();
        ability.OnTakeDamage(self, attacker);
    }

    public void OnDamageDeal()
    {
        self.timesDealedDamage++;
        self.canAttack = false;
        info.SetHighlight(false);
        if (linkedNetwork != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            try 
            { 
                linkedNetwork.canAttack.Value = false; 
            } catch { }

        if (self.HasAbility) 
            ability.OnDamageDeal(self, isPlayerCard, info);
    }

    public void DestroyCard()
    {
        movement.OnEndDrag(null);
        if (gameManager != null)
        {
            gameManager.playerHandCards.Remove(this);
            gameManager.enemyHandCards.Remove(this);
            gameManager.playerFieldCards.Remove(this);
            gameManager.enemyFieldCards.Remove(this);
        }
        Destroy(gameObject);
    }

    public void CheckForAlive()
    {
        if (self.IsAlive) 
            info.UpdateStats(self);
        else 
            DestroyCard();
    }

    void GiveDamageTo(CardController target, int damage)
    {
        target.self.GetDamage(damage);
        target.CheckForAlive();
        target.OnTakeDamage();
    }

    public void UseSpell(CardController target)
    {
        var spellCard = self as SpellCard;
        if (spellCard == null) 
        {
            Debug.LogError("UseSpell fail: not a spell card"); 
            DestroyCard(); 
            return; 
        }

        if (linkedNetwork != null && NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
        {
            ulong targetNetObjId = 0;
            if (target != null && target.Network != null) 
                targetNetObjId = target.Network.NetworkObject.NetworkObjectId;
            linkedNetwork.RequestCastSpellServerRpc((int)spellCard.spell, (int)spellCard.spellTarget, spellCard.spellPower, targetNetObjId);
            pendingServerAction = true;
            if (movement != null) 
            { 
                movement.OnEndDrag(null); 
                movement.enabled = false; 
            }
            var cg = GetComponent<CanvasGroup>();
            if (cg != null) 
            { 
                cg.interactable = false; 
                cg.blocksRaycasts = false; 
            }
            return;
        }

        switch (spellCard.spell)
        {
            case SpellType.GiveTempMana:
                if (isPlayerCard) 
                    gameManager.currentGame.player.AddTempMana(spellCard.spellPower);
                else 
                    gameManager.currentGame.enemy.AddTempMana(spellCard.spellPower);
                UIManager.Instance.UpdateHPAndMana();
                GameManager.Instance.CheckCardsForManaAvailability();
                break;
            case SpellType.HealAlliesField:
                var allyCards = isPlayerCard ? gameManager.playerFieldCards : gameManager.enemyFieldCards;
                foreach (var card in allyCards) 
                { 
                    card.self.health += spellCard.spellPower; 
                    card.info.UpdateStats(card.self); 
                }
                break;
            case SpellType.DamageEnemiesField:
                var enemyCards = isPlayerCard ? new List<CardController>(gameManager.enemyFieldCards) : new List<CardController>(gameManager.playerFieldCards);
                foreach (var card in enemyCards) 
                    GiveDamageTo(card, spellCard.spellPower);
                break;
            case SpellType.HealHero:
                if (isPlayerCard) 
                    gameManager.currentGame.player.hp += spellCard.spellPower;
                else 
                    gameManager.currentGame.enemy.hp += spellCard.spellPower;
                UIManager.Instance.UpdateHPAndMana();
                break;
            case SpellType.DamageHero:
                if (isPlayerCard) 
                    gameManager.currentGame.enemy.hp -= spellCard.spellPower;
                else 
                    gameManager.currentGame.player.hp -= spellCard.spellPower;
                UIManager.Instance.UpdateHPAndMana();
                gameManager.CheckForResult();
                break;
            case SpellType.HealCard:
                if (target != null) 
                    target.self.health += spellCard.spellPower;
                break;
            case SpellType.DamageCard:
                if (target != null) 
                    GiveDamageTo(target, spellCard.spellPower);
                break;
            case SpellType.AddShield:
                if (!target.self.abilities.Exists(x => x == AbilityType.Shield)) 
                    target.self.abilities.Add(AbilityType.Shield);
                break;
            case SpellType.AddTaunt:
                if (!target.self.abilities.Exists(x => x == AbilityType.Taunt)) 
                    target.self.abilities.Add(AbilityType.Taunt);
                break;
            case SpellType.BuffAttack:
                if (target != null) 
                    target.self.attack += spellCard.spellPower;
                break;
            case SpellType.DebuffAttack:
                if (target != null) 
                    target.self.attack = Mathf.Max(0, target.self.attack - spellCard.spellPower);
                break;
        }

        if (target != null)
        {
            target.ability.OnApplyEffect(target.self, target.isPlayerCard, info);
            target.CheckForAlive();
        }

        if (linkedNetwork != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            linkedNetwork.RemoveLocalCloneClientRpc();
            if (linkedNetwork.TryGetComponent<NetworkObject>(out var no)) 
                no.Despawn(true);
        }
        DestroyCard();
    }

    public void SetNetworkData(int attack, int health, int manaCost, bool isSpell, int cardDataIndex, ulong ownerClientId)
    {
        CardData dataToUse = null;
        try
        {
            var all = CardDatabase.AllCards;
            if (cardDataIndex >= 0 && all != null && cardDataIndex < all.Count)
            {
                object entryObj = (object)all[cardDataIndex];
                CardData cd = entryObj as CardData;
                if (cd != null)
                    dataToUse = cd;
                else
                {
                    Card existing = entryObj as Card;
                    if (existing != null)
                    {
                        var tmp = ScriptableObject.CreateInstance<CardData>();
                        try { tmp.cardName = existing.name; } catch { tmp.cardName = "NetCard"; }
                        try { tmp.isSpell = existing.isSpell; } catch { tmp.isSpell = isSpell; }
                        try { tmp.logo = existing.logo; } catch { }
                        try { tmp.manaCost = existing.manaCost; } catch { tmp.manaCost = manaCost; }
                        try { tmp.attack = existing.attack; } catch { tmp.attack = attack; }
                        try { tmp.health = existing.health; } catch { tmp.health = health; }
                        try { tmp.abilities = new List<AbilityType>(existing.abilities ?? new List<AbilityType>()); } catch { tmp.abilities = new List<AbilityType>(); }
                        dataToUse = tmp;
                    }
                    else
                        dataToUse = null;
                }
            }
        }
        catch { dataToUse = null; }

        if (dataToUse == null)
        {
            dataToUse = ScriptableObject.CreateInstance<CardData>();
            dataToUse.cardName = "NetCard";
            dataToUse.isSpell = isSpell;
            dataToUse.manaCost = manaCost;
            dataToUse.attack = attack;
            dataToUse.health = health;
        }

        bool finalIsSpell = isSpell;

        if (finalIsSpell)
            self = new SpellCard(dataToUse);
        else
            self = new Card(dataToUse);

        self.attack = attack;
        self.health = health;
        self.manaCost = manaCost;
        self.isSpell = isSpell;

        Info?.UpdateStats(self);

        bool isMine = NetworkManager.Singleton != null && ownerClientId == NetworkManager.Singleton.LocalClientId;

        if (!self.isPlaced)
        {
            bool showForNonOwnerCoin = false;
            if (!isMine && !string.IsNullOrEmpty(self.name))
            {
                var n = self.name.ToLower();
                if (n == "coin" || n.Contains("coin"))
                    showForNonOwnerCoin = true;
            }
            if (isMine || showForNonOwnerCoin)
                Info?.ShowCard(self);
            else
                Info?.HideCard();
        }
        else
            Info?.ShowCard(self);

        isPlayerCard = isMine;
    }

    public void OnNetworkOwnershipChanged(bool isOwner)
    {
        if (Movement != null) 
            Movement.enabled = isOwner;
        var cg = GetComponent<CanvasGroup>();
        if (cg != null) 
            cg.blocksRaycasts = isOwner;
        var attacked = GetComponent<AttackedCard>();
        if (attacked != null) 
            attacked.enabled = isOwner;

        if (!self.isPlaced)
        {
            if (isOwner) 
                Info?.ShowCard(self);
            else 
                Info?.HideCard();
        }
        else 
            Info?.ShowCard(self);

        if (isOwner && pendingServerAction)
        {
            pendingServerAction = false;
            if (movement != null) 
                movement.enabled = true;
            if (cg != null) 
            { 
                cg.interactable = true; 
                cg.blocksRaycasts = true; 
            }
        }
    }

    public void SetCanAttackVisual(bool canAttack) 
    { 
        Info?.SetHighlight(canAttack); 
    }

    public void OnPlacedNetworkSide(ulong ownerClientId)
    {
        self.isPlaced = true; 
        Info?.ShowCard(self);
        placedOnTurn = GameManager.Instance != null ? GameManager.Instance.CurrentTurn : -1;
        var dm = FindAnyObjectByType<DeckManager>();
        Transform targetParent = null;
        if (dm != null) 
            targetParent = (ownerClientId == (NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL)) ? dm.PlayerField : dm.EnemyField;
        if (targetParent != null) 
            transform.SetParent(targetParent, false);
    }

    public void OnUnplacedNetworkSide(ulong ownerClientId)
    {
        self.isPlaced = false;
        var dm = FindAnyObjectByType<DeckManager>();
        Transform handParent = null;
        if (dm != null) 
            handParent = (ownerClientId == (NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL)) ? dm.PlayerHand : dm.EnemyHand;
        if (handParent != null) 
            transform.SetParent(handParent, false);
        bool isMine = NetworkManager.Singleton != null && ownerClientId == NetworkManager.Singleton.LocalClientId;
        if (isMine) 
            Info?.ShowCard(self); else Info?.HideCard();
    }

    public void LinkNetwork(CardNetwork cn) 
    { 
        linkedNetwork = cn; 
    }

    public void SetMovement(CardMovement m) 
    { 
        if (m != null) 
            movement = m; 
    }
}