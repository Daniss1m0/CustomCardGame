using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class CardController : MonoBehaviour
{
    public bool isPlayerCard;
    public Card self;

    [SerializeField] private CardInfo info;
    [SerializeField] private CardMovement movement;
    [SerializeField] private CardAbility ability;

    private bool isDead = false;
    private GameManager gameManager;
    private CardNetwork linkedNetwork;

    [HideInInspector] public int placedOnTurn = -1;
    private bool pendingServerAction = false;

    public CardInfo Info => info;
    public CardMovement Movement => movement;
    public CardAbility Ability => ability;
    public CardNetwork Network => linkedNetwork;
    public bool IsAnimating { get; set; }

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

        info.UpdateDescription(self);

        if (ability != null)
            ability.OnApplyEffect(self);
    }

    public void OnCast(int slotIdx = -1)
    {
        if (self.isSpell && self is SpellCard spellCard && spellCard.spellTarget != TargetType.None)
            return;

        if (gameManager == null) 
            gameManager = GameManager.Instance;

        if (linkedNetwork == null) 
            return;

        if (NetworkManager.Singleton.IsServer)
        {
            bool sideIsPlayerTurn = gameManager.IsPlayerTurn == isPlayerCard;
            if (!sideIsPlayerTurn) 
                return;

            int currentMana = isPlayerCard ? gameManager.currentGame.player.mana : gameManager.currentGame.enemy.mana;
            if (currentMana < self.manaCost) 
                return;

            var dm = FindAnyObjectByType<DeckManager>();
            gameManager.SanitizeLists();

            int fieldCount = isPlayerCard ? gameManager.playerFieldCards.Count : gameManager.enemyFieldCards.Count;
            if (!self.isSpell && fieldCount >= (dm != null ? DeckManager.MAX_FIELD_SIZE : 7)) 
                return;

            try
            {
                if (!self.isSpell)
                {
                    linkedNetwork.placedOnTurn.Value = gameManager.CurrentTurn;
                    linkedNetwork.abilitiesNet.Value = AbilitiesToInt(self.abilities);

                    int targetIdx = slotIdx;
                    if (targetIdx == -1)
                        targetIdx = isPlayerCard ? gameManager.playerFieldCards.Count : gameManager.enemyFieldCards.Count;

                    linkedNetwork.fieldIndex.Value = targetIdx;
                    linkedNetwork.canAttack.Value = false;
                    linkedNetwork.isPlaced.Value = true;
                }
            }
            catch { }

            if (isPlayerCard)
            {
                if (gameManager.playerHandCards.Contains(this)) 
                    gameManager.playerHandCards.Remove(this);

                if (!self.isSpell && !gameManager.playerFieldCards.Contains(this))
                {
                    if (slotIdx != -1 && slotIdx <= gameManager.playerFieldCards.Count) 
                        gameManager.playerFieldCards.Insert(slotIdx, this);
                    else 
                        gameManager.playerFieldCards.Add(this);
                }
                gameManager.ReduceMana(true, self.manaCost);
            }
            else
            {
                if (gameManager.enemyHandCards.Contains(this)) 
                    gameManager.enemyHandCards.Remove(this);

                if (!self.isSpell && !gameManager.enemyFieldCards.Contains(this))
                {
                    if (slotIdx != -1 && slotIdx <= gameManager.enemyFieldCards.Count) 
                        gameManager.enemyFieldCards.Insert(slotIdx, this);
                    else 
                        gameManager.enemyFieldCards.Add(this);
                    if (dm != null) 
                        transform.SetParent(dm.EnemyField, false);
                }
                gameManager.ReduceMana(false, self.manaCost);
            }

            placedOnTurn = gameManager.CurrentTurn;
            self.canAttack = false;
            info.SetHighlight(false);

            if (!self.isSpell)
            {
                self.isPlaced = true;
                if (slotIdx != -1) 
                    transform.SetSiblingIndex(slotIdx);
            }

            if (self.HasAbility) 
                ability.OnCast(self, isPlayerCard, info);

            if (self.abilities.Contains(AbilityType.Charge))
            {
                self.canAttack = true;
                linkedNetwork.canAttack.Value = true;
                info.SetHighlight(true);
            }

            if (self.isSpell) 
                UseSpell(null);
        }
        else
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
                if (TryGetComponent<CanvasGroup>(out var cg)) 
                { 
                    cg.interactable = false; 
                    cg.blocksRaycasts = false; 
                }
            }
            else
            {
                info.SetHighlight(false);
                int targetIdx = slotIdx == -1 ? 999 : slotIdx;
                linkedNetwork.RequestPlaceCardServerRpc(targetIdx);
            }
        }
    }

    public void OnTakeDamage(CardController attacker = null)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && linkedNetwork != null)
        {
            if (linkedNetwork.health.Value != self.health)
                linkedNetwork.health.Value = self.health;

            int currentMask = AbilitiesToInt(self.abilities);

            if (linkedNetwork.abilitiesNet.Value != currentMask)
                linkedNetwork.abilitiesNet.Value = currentMask;

            CheckForAlive();
        }
        else
            CheckForAlive();

        if (ability != null)
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
            }
            catch { }

        if (self.HasAbility)
            ability.OnDamageDeal(self, isPlayerCard, info);

        if (self.canAttack && linkedNetwork != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            try
            {
                linkedNetwork.canAttack.Value = true;
                linkedNetwork.ForceCanAttackSyncClientRpc(true);
            }
            catch { }
    }

    public void OnDeath()
    {
        if (isDead)
            return;

        isDead = true;

        if (movement != null)
        {
            movement.OnEndDrag(null);
            movement.enabled = false;
        }

        if (TryGetComponent<CanvasGroup>(out var cg))
        {
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        Info.SetHighlight(false);

        if (AnimationManager.Instance != null)
            AnimationManager.Instance.PlayDeath(transform, DestroyCard);
        else
            DestroyCard();
    }

    private void OnDestroy()
    {
        if (linkedNetwork != null)
            linkedNetwork.onNetworkDespawn -= OnNetworkDespawnHandler;
    }

    public void DestroyCard()
    {
        Transform parentTransform = transform.parent;

        movement.OnEndDrag(null);

        if (gameManager != null)
        {
            gameManager.playerHandCards.Remove(this);
            gameManager.enemyHandCards.Remove(this);
            gameManager.playerFieldCards.Remove(this);
            gameManager.enemyFieldCards.Remove(this);
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && linkedNetwork != null)
            if (linkedNetwork.TryGetComponent<NetworkObject>(out var no))
                if (no.IsSpawned)
                    no.Despawn(true);

        Destroy(gameObject);

        if (parentTransform != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentTransform as RectTransform);
    }

    public void CheckForAlive()
    {
        if (self.IsAlive)
            info.UpdateStats(self);
        else
            OnDeath();
    }

    void GiveDamageTo(CardController target, int damage)
    {
        target.self.GetDamage(damage);
        target.OnTakeDamage();
        target.CheckForAlive();
    }

    public void UseSpell(CardController target)
    {
        if (self is not SpellCard spellCard)
        {
            DestroyCard();
            return;
        }

        if (AudioManager.Instance != null) 
            AudioManager.Instance.PlaySpellCast();

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
            if (TryGetComponent<CanvasGroup>(out var cg))
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

                    if (card.Network != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                        card.Network.health.Value = card.self.health;
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
                {
                    target.self.health += spellCard.spellPower;
                    target.info.UpdateStats(target.self);

                    if (target.Network != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                        target.Network.health.Value = target.self.health;
                }

                break;

            case SpellType.DamageCard:

                if (target != null)
                    GiveDamageTo(target, spellCard.spellPower);

                break;

            case SpellType.AddShield:

                if (!target.self.abilities.Exists(x => x == AbilityType.Shield))
                {
                    target.self.abilities.Add(AbilityType.Shield);
                    if (target.Network != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                        target.Network.abilitiesNet.Value = AbilitiesToInt(target.self.abilities);
                }

                break;

            case SpellType.AddTaunt:

                if (!target.self.abilities.Exists(x => x == AbilityType.Taunt))
                {
                    target.self.abilities.Add(AbilityType.Taunt);
                    if (target.Network != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                        target.Network.abilitiesNet.Value = AbilitiesToInt(target.self.abilities);
                }

                break;

            case SpellType.BuffAttack:

                if (target != null)
                {
                    target.self.attack += spellCard.spellPower;
                    target.info.UpdateStats(target.self);

                    if (target.Network != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                        target.Network.attack.Value = target.self.attack;
                }

                break;

            case SpellType.DebuffAttack:

                if (target != null)
                {
                    target.self.attack = Mathf.Max(0, target.self.attack - spellCard.spellPower);
                    target.info.UpdateStats(target.self);

                    if (target.Network != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                        target.Network.attack.Value = target.self.attack;
                }

                break;
        }

        if (target != null)
        {
            target.ability.OnApplyEffect(target.self);
            target.CheckForAlive();
        }

        if (gameManager != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            gameManager.UpdateStateNetworkIfServer();

        if (linkedNetwork != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            linkedNetwork.RemoveLocalCloneClientRpc();

        DestroyCard();
    }

    public void OnNewTurn()
    {
        self.timesDealedDamage = 0;

        int healthBefore = self.health;

        if (ability != null)
            ability.OnNewTurn(self, info);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && linkedNetwork != null)
            if (self.health != healthBefore)
                linkedNetwork.health.Value = self.health;
    }

    public static int AbilitiesToInt(List<AbilityType> abilities)
    {
        int mask = 0;
        if (abilities == null)
            return mask;

        foreach (var a in abilities)
        {
            if (a == AbilityType.None)
                continue;

            mask |= (1 << (int)a);
        }
        return mask;
    }

    public static List<AbilityType> IntToAbilities(int mask)
    {
        var list = new List<AbilityType>();
        if (mask == 0)
            return list;

        foreach (AbilityType a in System.Enum.GetValues(typeof(AbilityType)))
        {
            if (a == AbilityType.None)
                continue;

            if ((mask & (1 << (int)a)) != 0)
                list.Add(a);
        }
        return list;
    }

    public void UpdateAbilitiesFromMask(int mask)
    {
        self.abilities = IntToAbilities(mask);
        if (ability != null)
            ability.OnApplyEffect(self);

        info.UpdateDescription(self);
    }

    public void SetNetworkData(int attack, int health, int manaCost, bool isSpell, int cardDataIndex, ulong ownerClientId, int abilitiesMask)
    {
        CardData dataToUse = null;
        try
        {
            var all = CardDatabase.AllCards;
            if (cardDataIndex >= 0 && all != null && cardDataIndex < all.Count)
            {
                object entryObj = all[cardDataIndex];
                CardData cd = entryObj as CardData;
                if (cd != null)
                    dataToUse = cd;
                else
                {
                    if (entryObj is Card existing)
                    {
                        var tmp = ScriptableObject.CreateInstance<CardData>();
                        tmp.cardName = existing.name;
                        tmp.isSpell = existing.isSpell;
                        tmp.logo = existing.logo;
                        tmp.manaCost = existing.manaCost;
                        tmp.attack = existing.attack;
                        tmp.health = existing.health;
                        tmp.abilities = new List<AbilityType>(existing.abilities ?? new List<AbilityType>());

                        dataToUse = tmp;
                    }
                    else
                        dataToUse = null;
                }
            }
        }
        catch
        {
            dataToUse = null;
        }

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

        UpdateAbilitiesFromMask(abilitiesMask);

        Info.UpdateStats(self);
        Info.UpdateDescription(self);

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
                Info.ShowCard(self);
            else
                Info.HideCard();
        }
        else
            Info.ShowCard(self);

        isPlayerCard = isMine;
    }

    public void OnNetworkOwnershipChanged(bool isOwner)
    {
        if (Movement != null)
            Movement.enabled = isOwner;

        if (TryGetComponent<CanvasGroup>(out var cg))
            cg.blocksRaycasts = isOwner;

        if (TryGetComponent<AttackedCard>(out var attacked))
            attacked.enabled = isOwner;

        if (!self.isPlaced)
        {
            if (isOwner)
                Info.ShowCard(self);
            else
                Info.HideCard();
        }
        else
            Info.ShowCard(self);

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
        if (!isPlayerCard)
        {
            Info.SetHighlight(false);
            return;
        }

        Info.SetHighlight(canAttack);
    }

    public void OnPlacedNetworkSide(ulong ownerClientId)
    {
        self.isPlaced = true;

        if (AudioManager.Instance != null) 
            AudioManager.Instance.PlayPlaceCard();

        int targetIndex = 0;
        if (linkedNetwork != null)
            targetIndex = linkedNetwork.fieldIndex.Value;

        if (placedOnTurn <= 0 && GameManager.Instance != null)
            placedOnTurn = GameManager.Instance.CurrentTurn;

        var dm = FindAnyObjectByType<DeckManager>();
        Transform targetParent = null;

        ulong localId = (NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0UL);
        bool isLocalPlayer = (ownerClientId == localId);

        if (dm != null)
            targetParent = isLocalPlayer ? dm.PlayerField : dm.EnemyField;

        bool isEnemyTurn = GameManager.Instance != null && !GameManager.Instance.IsPlayerTurn;
        bool turnMatches = placedOnTurn == (GameManager.Instance != null ? GameManager.Instance.CurrentTurn : -99);

        if (!isLocalPlayer && !self.isSpell && (isEnemyTurn || turnMatches))
        {
            if (AnimationManager.Instance != null)
                AnimationManager.Instance.EnqueueVisual(() =>
                {
                    if (this != null)
                        AnimationManager.Instance.PlayOpponentDraw(this, targetParent, targetIndex);
                });
        }
        else
        {
            if (targetParent != null)
            {
                transform.SetParent(targetParent, false);
                transform.SetSiblingIndex(targetIndex);

                LayoutRebuilder.ForceRebuildLayoutImmediate(targetParent as RectTransform);
            }

            ResetVisualState();
            Info.ShowCard(self);
        }

        if (ability != null)
            ability.OnApplyEffect(self);
    }

    public void AnimateOpponentSpellAndDestroy()
    {
        if (AnimationManager.Instance != null)
            AnimationManager.Instance.PlayOpponentSpell(this);
        else
            Destroy(gameObject);
    }

    private void ResetVisualState()
    {
        if (TryGetComponent<CanvasGroup>(out var cg))
        {
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }
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
            Info.ShowCard(self);
        else
            Info.HideCard();
    }

    public void LinkNetwork(CardNetwork cn)
    {
        linkedNetwork = cn;
        if (linkedNetwork != null)
            linkedNetwork.onNetworkDespawn += OnNetworkDespawnHandler;
    }

    public void SetMovement(CardMovement m)
    {
        if (m != null)
            movement = m;
    }

    private void OnNetworkDespawnHandler(ulong id)
    {
        OnDeath();
    }

    public void PlayAttackAnimation(bool targetIsHero, bool isEnemyHero, ulong targetCardObjId)
    {
        Transform targetTransform = null;

        if (targetIsHero)
        {
            var heroes = FindObjectsByType<AttackedHero>(FindObjectsSortMode.None);
            foreach (var h in heroes)
            {
                bool isVisualEnemyHero = h.type == AttackedHero.HeroType.Enemy;

                if (isEnemyHero && h.type == AttackedHero.HeroType.Enemy)
                    targetTransform = h.transform;
                else if (!isEnemyHero && h.type == AttackedHero.HeroType.Player)
                    targetTransform = h.transform;
            }
        }
        else
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetCardObjId, out var targetObj))
            {
                var visualCard = targetObj.GetComponentInChildren<CardController>();
                if (visualCard != null)
                    targetTransform = visualCard.transform;
            }
        }

        if (movement != null && AnimationManager.Instance != null)
            AnimationManager.Instance.PlayAttack(transform, targetTransform, null);
    }
}