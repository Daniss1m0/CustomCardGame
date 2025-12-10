using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using DG.Tweening;

public class CardController : MonoBehaviour
{
    public static int GlobalAnimationBusyCount = 0;

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
            ability.OnApplyEffect(self, isPlayerCard, info);
    }

    public void OnCast(int slotIndex = -1)
    {
        if (self.isSpell)
        {
            if (self is SpellCard spellCard)
            {
                if (spellCard.spellTarget != TargetType.None)
                    return;
            }
            else
                return;
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

                gameManager.SanitizeLists();

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
                        linkedNetwork.abilitiesNet.Value = AbilitiesToInt(self.abilities);

                        int targetIndex = slotIndex;
                        if (targetIndex == -1)
                        {
                            targetIndex = isPlayerCard ? gameManager.playerFieldCards.Count : gameManager.enemyFieldCards.Count;
                        }
                        linkedNetwork.fieldIndex.Value = targetIndex;
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

                        if (!self.isSpell && !gameManager.playerFieldCards.Contains(this))
                        {
                            if (slotIndex != -1 && slotIndex <= gameManager.playerFieldCards.Count)
                                gameManager.playerFieldCards.Insert(slotIndex, this);
                            else
                                gameManager.playerFieldCards.Add(this);
                        }

                        gameManager.ReduceMana(true, self.manaCost);
                        gameManager.CheckCardsForManaAvailability();
                    }
                    else
                    {
                        if (gameManager.enemyHandCards.Contains(this))
                            gameManager.enemyHandCards.Remove(this);

                        if (!self.isSpell && !gameManager.enemyFieldCards.Contains(this))
                        {
                            if (slotIndex != -1 && slotIndex <= gameManager.enemyFieldCards.Count)
                                gameManager.enemyFieldCards.Insert(slotIndex, this);
                            else
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
                {
                    self.isPlaced = true;
                    if (slotIndex != -1)
                        transform.SetSiblingIndex(slotIndex);
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
                        int targetIdx = slotIndex == -1 ? 999 : slotIndex;
                        linkedNetwork.RequestPlaceCardServerRpc(isPlayerCard, targetIdx);
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
            {
                if (slotIndex != -1 && slotIndex <= gameManager.playerFieldCards.Count)
                    gameManager.playerFieldCards.Insert(slotIndex, this);
                else
                    gameManager.playerFieldCards.Add(this);
            }
            gameManager.ReduceMana(true, self.manaCost);
            gameManager.CheckCardsForManaAvailability();
        }

        self.isPlaced = true;
        if (slotIndex != -1) 
            transform.SetSiblingIndex(slotIndex);

        if (self.HasAbility) 
            ability.OnCast(self, isPlayerCard, info);

        if (self.isSpell) 
            UseSpell(null);

        UIManager.Instance.UpdateHPAndMana();
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
            movement.ForceCleanupAnimation();
            movement.enabled = false;
        }

        var cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        Info?.SetHighlight(false);

        transform.DOKill();

        transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).SetLink(gameObject).OnComplete(() =>
        {
            DestroyCard();
        });
    }

    public void DestroyCard()
    {
        Transform parentTransform = transform.parent;

        movement.OnEndDrag(null);
        if (movement != null) 
            movement.ForceCleanupAnimation();

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
        var spellCard = self as SpellCard;
        if (spellCard == null)
        {
            Debug.LogError("Not a spell card");
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
            target.ability.OnApplyEffect(target.self, target.isPlayerCard, info);
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
            ability.OnApplyEffect(self, isPlayerCard, info);
        
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

        Info?.UpdateStats(self);
        Info?.UpdateDescription(self);

        bool isMine = NetworkManager.Singleton != null && ownerClientId == NetworkManager.Singleton.LocalClientId;
        if (!self.isPlaced) { bool showForNonOwnerCoin = false; if (!isMine && !string.IsNullOrEmpty(self.name)) { var n = self.name.ToLower(); if (n == "coin" || n.Contains("coin")) showForNonOwnerCoin = true; } if (isMine || showForNonOwnerCoin) Info?.ShowCard(self); else Info?.HideCard(); } else Info?.ShowCard(self);
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
        if (!isPlayerCard)
        {
            Info?.SetHighlight(false);
            return;
        }

        Info?.SetHighlight(canAttack);
    }

    public void OnPlacedNetworkSide(ulong ownerClientId)
    {
        self.isPlaced = true;

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
            AnimateOpponentPlay(targetParent, targetIndex);
        else
        {
            if (targetParent != null)
            {
                transform.SetParent(targetParent, false);
                transform.SetSiblingIndex(targetIndex);

                LayoutRebuilder.ForceRebuildLayoutImmediate(targetParent as RectTransform);
            }

            ResetVisualState();
            Info?.ShowCard(self);
        }

        if (ability != null)
            ability.OnApplyEffect(self, isPlayerCard, info);
    }

    private void AnimateOpponentPlay(Transform targetParent, int fallbackIndex)
    {
        GlobalAnimationBusyCount++;

        IsAnimating = true;

        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null && rootCanvas.rootCanvas != null)
            rootCanvas = rootCanvas.rootCanvas;

        Transform showcaseParent = rootCanvas != null ? rootCanvas.transform : transform.root;
        transform.SetParent(showcaseParent, true);
        Info?.ShowCard(self);
        ResetVisualState();

        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null)
            return;

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        float startY = 600f;
        if (rootCanvas != null)
        {
            RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
            startY = (canvasRect.rect.height / 2f) + 250f;
        }

        rt.anchoredPosition = new Vector2(0, startY);

        Vector3 originalScale = Vector3.one;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(rt.DOAnchorPos(Vector2.zero, 0.5f).SetEase(Ease.OutBack));

        sequence.Join(rt.DOScale(originalScale * 1.5f, 0.5f).SetEase(Ease.OutBack));
        sequence.Join(rt.DORotate(Vector3.zero, 0.3f));

        sequence.AppendInterval(0.6f);

        sequence.AppendCallback(() => { rt.DOScale(originalScale, 0.3f); });

        if (targetParent != null)
            sequence.Append(transform.DOMove(targetParent.position, 0.4f).SetEase(Ease.InQuad));

        sequence.OnComplete(() =>
        {
            if (targetParent != null)
            {
                transform.SetParent(targetParent, false);

                int finalIndex = fallbackIndex;
                if (linkedNetwork != null)
                    finalIndex = linkedNetwork.fieldIndex.Value;

                transform.SetSiblingIndex(finalIndex);

                LayoutRebuilder.ForceRebuildLayoutImmediate(targetParent as RectTransform);
            }

            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            transform.localPosition = Vector3.zero;

            IsAnimating = false;

            GlobalAnimationBusyCount--;
            if (GlobalAnimationBusyCount < 0) 
                GlobalAnimationBusyCount = 0;
        });
    }

    public void AnimateOpponentSpellAndDestroy()
    {
        GlobalAnimationBusyCount++;

        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null && rootCanvas.rootCanvas != null)
            rootCanvas = rootCanvas.rootCanvas;

        Transform showcaseParent = rootCanvas != null ? rootCanvas.transform : transform.root;
        transform.SetParent(showcaseParent, true);

        Info?.ShowCard(self);
        ResetVisualState();

        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null)
        {
            Destroy(gameObject);
            return;
        }

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        float startY = 600f;
        if (rootCanvas != null)
        {
            RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
            startY = (canvasRect.rect.height / 2f) + 250f;
        }
        rt.anchoredPosition = new Vector2(0, startY);

        Vector3 originalScale = Vector3.one;
        Sequence sequence = DOTween.Sequence();

        sequence.Append(rt.DOAnchorPos(Vector2.zero, 0.5f).SetEase(Ease.OutBack));
        sequence.Join(rt.DOScale(originalScale * 1.6f, 0.5f).SetEase(Ease.OutBack));
        sequence.Join(rt.DORotate(Vector3.zero, 0.3f));

        sequence.AppendInterval(0.8f);

        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) 
            cg = gameObject.AddComponent<CanvasGroup>();

        sequence.Append(rt.DOScale(originalScale * 2f, 0.4f));
        sequence.Join(cg.DOFade(0f, 0.4f));

        sequence.OnComplete(() =>
        {
            GlobalAnimationBusyCount--;
            if (GlobalAnimationBusyCount < 0) 
                GlobalAnimationBusyCount = 0;

            Destroy(gameObject);
        });
    }

    private void ResetVisualState()
    {
        var cg = GetComponent<CanvasGroup>();
        if (cg != null)
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
            Info?.ShowCard(self);
        else
            Info?.HideCard();
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

    private void OnDestroy()
    {
        if (linkedNetwork != null)
            linkedNetwork.onNetworkDespawn -= OnNetworkDespawnHandler;
    }

    public void PlayAttackAnimation(bool targetIsHero, bool isEnemyHero, ulong targetCardObjectId)
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
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetCardObjectId, out var targetObj))
            {
                var visualCard = targetObj.GetComponentInChildren<CardController>();
                if (visualCard != null)
                    targetTransform = visualCard.transform;
            }
        }

        if (targetTransform != null && movement != null)
            movement.AnimateAttack(targetTransform, null);
    }
}