using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardController : MonoBehaviour
{
    public bool isPlayerCard;
    public Card self; //?

    [SerializeField] public CardInfo info; //?
    [SerializeField] public CardMovement movement;
    [SerializeField] public CardAbility ability;
    
    private GameManager gameManager;

    public void Init(Card card, bool isPlayerCard)
    {
        self = card;
        this.isPlayerCard = isPlayerCard;
        gameManager = GameManager.Instance;

        if (isPlayerCard)
        {
            info.ShowCardInfo();
            GetComponent<AttackedCard>().enabled = false;
        }
        else
            info.HideCardInfo();

        // ? ability.Setup(card.abilities);
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
            info.ShowCardInfo();
        }

        self.isPlaced = true;

        if (self.HasAbility)
            ability.OnCast();

        if (self.isSpell)
            UseSpell(null);

        UIController.Instance.UpdateHPAndMana();
    }

    public void OnTakeDamage(CardController attacker = null)
    {
        CheckForAlive();
        ability.OnDamageTake(attacker);
    }

    public void OnDamageDeal()
    {
        self.timesDealedDamage++;
        self.canAttack = false;
        info.HighlightCard(false);

        if (self.HasAbility)
            ability.OnDamageDeal();
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
                    card.info.RefreshData();
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
                
                target.self.attack = Mathf.Clamp(target.self.attack - spellCard.spellValue, 0, int.MaxValue);
                
                break;
        }

        if (target != null)
        {
            target.ability.OnCast();
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

    public void CheckForAlive()
    {
        if (self.IsAlive)
            info.RefreshData();
        else
            DestroyCard();
    }

    void RemoveFromList(List<CardController> list)
    {
        if(list.Contains(this))
            list.Remove(this); //?
    }

    public void DestroyCard() // upper?
    {
        movement.OnEndDrag(null);
        //movement.StopAllActions();

        RemoveFromList(gameManager.enemyFieldCards);
        RemoveFromList(gameManager.enemyHandCards);
        RemoveFromList(gameManager.playerFieldCards);
        RemoveFromList(gameManager.playerHandCards);

        Destroy(gameObject);
    }
}