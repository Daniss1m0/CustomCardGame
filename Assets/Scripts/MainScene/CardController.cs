using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardController : MonoBehaviour
{
    public bool isPlayerCard;
    public Card self;

    [SerializeField] private CardInfo info;
    [SerializeField] private CardMovement movement;
    [SerializeField] private CardAbility ability;
    
    private GameManager gameManager;

    public CardInfo Info => info;
    public CardAbility Ability => ability;
    public CardMovement Movement => movement;

    public void Init(Card card, bool isPlayerCard)
    {
        self = card;
        this.isPlayerCard = isPlayerCard;
        gameManager = GameManager.Instance;

        if (movement == null) movement = GetComponent<CardMovement>();

        if (isPlayerCard)
        {
            info.ShowCard(self);
            GetComponent<AttackedCard>().enabled = false; //?
        }
        else
            info.HideCard();
    }

    public void OnCast()
    {
        if (self.isSpell && ((SpellCard)self).spellTarget != SpellCard.TargetType.None)
            return;

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

        if (self.HasAbility)
            ability.OnDamageDeal(self, isPlayerCard, info);
    }

    public void DestroyCard()
    {
        movement.OnEndDrag(null);
        //movement.StopAllActions();

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
            case SpellCard.SpellType.HealAlliesField:

                var allyCards = isPlayerCard ? gameManager.playerFieldCards : gameManager.enemyFieldCards;

                foreach (var card in allyCards)
                {
                    card.self.health += spellCard.spellPower;
                    card.info.UpdateStats(card.self);
                }
                
                break;

            case SpellCard.SpellType.DamageEnemiesField:

                var enemyCards = isPlayerCard ? new List<CardController>(gameManager.enemyFieldCards) : 
                                                new List<CardController>(gameManager.playerFieldCards);

                foreach (var card in enemyCards)
                    GiveDamageTo(card, spellCard.spellPower);

                break;

            case SpellCard.SpellType.HealHero:

                if (isPlayerCard)
                    gameManager.currentGame.player.hp += spellCard.spellPower;
                else
                    gameManager.currentGame.enemy.hp += spellCard.spellPower;

                UIManager.Instance.UpdateHPAndMana();

                break;

            case SpellCard.SpellType.DamageHero:

                if (isPlayerCard)
                    gameManager.currentGame.enemy.hp -= spellCard.spellPower;
                else
                    gameManager.currentGame.player.hp -= spellCard.spellPower;

                UIManager.Instance.UpdateHPAndMana();
                gameManager.CheckForResult();

                break;

            case SpellCard.SpellType.HealCard:

                target.self.health += spellCard.spellPower;

                break;

            case SpellCard.SpellType.DamageCard:

                GiveDamageTo(target, spellCard.spellPower);

                break;

            case SpellCard.SpellType.AddShield:
                
                if (!target.self.abilities.Exists(x => x == Card.AbilityType.Shield))
                    target.self.abilities.Add(Card.AbilityType.Shield);
                
                break;

            case SpellCard.SpellType.AddTaunt:
                
                if (!target.self.abilities.Exists(x => x == Card.AbilityType.Taunt))
                    target.self.abilities.Add(Card.AbilityType.Taunt);
                
                break;

            case SpellCard.SpellType.BuffAttack:
                
                target.self.attack += spellCard.spellPower;
                
                break;

            case SpellCard.SpellType.DebuffAttack:

                target.self.attack = Mathf.Max(0, target.self.attack - spellCard.spellPower);

                break;
        }

        if (target != null)
        {
            target.ability.OnCast(target.self, target.isPlayerCard, info);
            target.CheckForAlive();
        }

        DestroyCard();
    }
}