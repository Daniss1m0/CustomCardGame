using System.Collections;
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
        if (self.isSpell && ((SpellCard)self).spellTarget != TargetType.None)
            return;

        if (gameManager == null)
            gameManager = GameManager.Instance;
        placedOnTurn = gameManager != null ? gameManager.CurrentTurn : -1;
        self.canAttack = false;
        info.SetHighlight(false);
        if (linkedNetwork != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            linkedNetwork.canAttack.Value = false;

        if (isPlayerCard)
        {
            gameManager.playerHandCards.Remove(this);
            gameManager.playerFieldCards.Add(this);
            gameManager.ReduceMana(true, self.manaCost);
            gameManager.CheckCardsForManaAvailability();
        }
        else
        {
            gameManager.enemyHandCards.Remove(this);
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
            try { linkedNetwork.canAttack.Value = false; } catch { }

        if (self.HasAbility)
            ability.OnDamageDeal(self, isPlayerCard, info);
    }

    public void DestroyCard()
    {
        movement.OnEndDrag(null);
        gameManager.playerHandCards.Remove(this);
        gameManager.enemyHandCards.Remove(this);
        gameManager.playerFieldCards.Remove(this);
        gameManager.enemyFieldCards.Remove(this);
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
        var spellCard = (SpellCard)self;
        switch (spellCard.spell)
        {
            case SpellType.GiveTempMana:
                Player targetPlayer = isPlayerCard ? gameManager.currentGame.player : gameManager.currentGame.enemy;
                targetPlayer.AddTempMana(spellCard.spellPower);
                UIManager.Instance.UpdateHPAndMana();
                GameManager.Instance.CheckCardsForManaAvailability();
                break;
            case SpellType.HealAlliesField:
                var allyCards = isPlayerCard ? gameManager.playerFieldCards : gameManager.enemyFieldCards;
                foreach (var card in allyCards) { card.self.health += spellCard.spellPower; card.info.UpdateStats(card.self); }
                break;
            case SpellType.DamageEnemiesField:
                var enemyCards = isPlayerCard ? new List<CardController>(gameManager.enemyFieldCards) : new List<CardController>(gameManager.playerFieldCards);
                foreach (var card in enemyCards) GiveDamageTo(card, spellCard.spellPower);
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
                target.self.health += spellCard.spellPower;
                break;
            case SpellType.DamageCard:
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
                target.self.attack += spellCard.spellPower;
                break;
            case SpellType.DebuffAttack:
                target.self.attack = Mathf.Max(0, target.self.attack - spellCard.spellPower);
                break;
        }
        if (target != null)
        {
            target.ability.OnApplyEffect(target.self, target.isPlayerCard, info);
            target.CheckForAlive();
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
        if (dataToUse.isSpell)
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
        {
            var t = dm.GetType().GetProperty("PlayerField");
            var e = dm.GetType().GetProperty("EnemyField");
            if (ownerClientId == (NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL))
            {
                if (t != null)
                    targetParent = t.GetValue(dm) as Transform;
                else
                {
                    var ph = dm.GetType().GetProperty("PlayerHand");
                    targetParent = ph != null ? ph.GetValue(dm) as Transform : null;
                }
            }
            else
            {
                if (e != null)
                    targetParent = e.GetValue(dm) as Transform;
                else
                {
                    var eh = dm.GetType().GetProperty("EnemyHand");
                    targetParent = eh != null ? eh.GetValue(dm) as Transform : null;
                }
            }
        }
        if (targetParent != null)
            transform.SetParent(targetParent, false);
    }

    public void OnUnplacedNetworkSide(ulong ownerClientId)
    {
        self.isPlaced = false;
        var dm = FindAnyObjectByType<DeckManager>();
        Transform handParent = null;
        if (dm != null)
        {
            var ph = dm.GetType().GetProperty("PlayerHand");
            var eh = dm.GetType().GetProperty("EnemyHand");
            if (ownerClientId == (NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL))
                if (ph != null)
                    handParent = ph.GetValue(dm) as Transform;
            else
                if (eh != null)
                    handParent = eh.GetValue(dm) as Transform;
        }
        if (handParent != null)
            transform.SetParent(handParent, false);

        bool isMine = NetworkManager.Singleton != null && ownerClientId == NetworkManager.Singleton.LocalClientId;
        if (isMine)
            Info?.ShowCard(self);
        else
            Info?.HideCard();
    }

    public void LinkNetwork(CardNetwork cn)
    {
        linkedNetwork = cn;
    }

    public void SetMovement(CardMovement m)
    {
        if (m == null)
            return;

        movement = m;
    }
}
