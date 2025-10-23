using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardController : MonoBehaviour
{
    public bool isPlayerCard;
    public Card self; //?

    [SerializeField] private CardInfo info; //public
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

        if (isPlayerCard)
        {
            info.ShowCard(self);
            GetComponent<AttackedCard>().enabled = false; //?
        }
        else
            info.HideCard();

        ability.Setup(self.abilities);
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

        UIController.Instance.UpdateHPAndMana();
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

    public void UseSpell(CardController target)
    {
        var spellCard = (SpellCard)self;

        switch (spellCard.spell)
        {
            case SpellCard.SpellType.HealAlliesField:

                var allyCards = isPlayerCard ? gameManager.playerFieldCards : gameManager.enemyFieldCards;

                foreach (var card in allyCards)
                {
                    card.self.health += spellCard.spellValue;
                    card.info.UpdateStats(card.self);
                }
                
                break;

            case SpellCard.SpellType.DamageEnemiesField:

                var enemyCards = isPlayerCard ? new List<CardController>(gameManager.enemyFieldCards) : 
                                                new List<CardController>(gameManager.playerFieldCards);

                foreach (var card in enemyCards)
                    GiveDamageTo(card, spellCard.spellValue);

                break;

            case SpellCard.SpellType.HealHero:

                if (isPlayerCard)
                    gameManager.currentGame.player.hp += spellCard.spellValue;
                else
                    gameManager.currentGame.enemy.hp += spellCard.spellValue;

                UIController.Instance.UpdateHPAndMana();

                break;

            case SpellCard.SpellType.DamageHero:

                if (isPlayerCard)
                    gameManager.currentGame.enemy.hp -= spellCard.spellValue;
                else
                    gameManager.currentGame.player.hp -= spellCard.spellValue;

                UIController.Instance.UpdateHPAndMana();
                gameManager.CheckForResult();

                break;

            case SpellCard.SpellType.HealCard:
                target.self.health += spellCard.spellValue;
                break;

            case SpellCard.SpellType.DamageCard:
                
                GiveDamageTo(target, spellCard.spellValue);
                
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
                
                target.self.attack += spellCard.spellValue;
                
                break;

            case SpellCard.SpellType.DebuffAttack:

                target.self.attack = Mathf.Max(0, target.self.attack - spellCard.spellValue);

                break;
        }

        if (target != null)
        {
            target.ability.OnCast(target.self, target.isPlayerCard, info);
            target.CheckForAlive();
        }

        DestroyCard();
    }

    void GiveDamageTo(CardController target, int damage)
    {
        target.self.GetDamage(damage);
        target.CheckForAlive();
        target.OnTakeDamage();
    }

    void RemoveFromList(List<CardController> list)
    {
        if(list.Contains(this))
            list.Remove(this); //?
    }

    public void DestroyCard()
    {
        movement.OnEndDrag(null);
        //movement.StopAllActions();

        RemoveFromList(gameManager.enemyFieldCards); // gameManager.playerHandCards.Remove(this);
        RemoveFromList(gameManager.enemyHandCards);
        RemoveFromList(gameManager.playerFieldCards);
        RemoveFromList(gameManager.playerHandCards);

        Destroy(gameObject);
    }

    public void CheckForAlive()
    {
        if (self.IsAlive)
            info.UpdateStats(self);
        else
            DestroyCard();
    }
}