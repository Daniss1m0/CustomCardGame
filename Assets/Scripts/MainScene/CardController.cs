using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardController : MonoBehaviour
{
    public bool isPlayerCard;
    public Card self; //?
    public CardInfo info;
    public CardMovement movement;
    public CardAbility ability;
    
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
    }

    public void OnCast()
    {
        if (self.isSpell && ((SpellCard)self).spellTarget != SpellCard.TargetType.NO_TARGET)
            return;

        if (isPlayerCard)
        {
            gameManager.playerHandCards.Remove(this);
            gameManager.playerFieldCards.Add(this);
            gameManager.ReduceMana(true, self.manacost);
            gameManager.CheckCardsForManaAvailability();
        }
        else 
        {
            gameManager.enemyHandCards.Remove(this);
            gameManager.enemyFieldCards.Add(this);
            gameManager.ReduceMana(false, self.manacost);
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
            case SpellCard.SpellType.HEAL_ALLY_FIELD_CARDS:

                var allyCards = isPlayerCard ? gameManager.playerFieldCards : gameManager.enemyFieldCards;

                foreach (var card in allyCards)
                {
                    card.self.health += spellCard.spellValue;
                    card.info.RefreshData();
                }
                
                break;

            case SpellCard.SpellType.DAMAGE_ENEMY_FIELD_CARDS:

                var enemyCards = isPlayerCard ? new List<CardController>(gameManager.enemyFieldCards) : new List<CardController>(gameManager.playerFieldCards);

                foreach (var card in enemyCards)
                    GiveDamageTo(card, spellCard.spellValue);

                break;

            case SpellCard.SpellType.HEAL_ALLY_HERO:

                if (isPlayerCard)
                    gameManager.currentGame.player.hp += spellCard.spellValue;
                else
                    gameManager.currentGame.enemy.hp += spellCard.spellValue;

                UIController.Instance.UpdateHPAndMana();

                break;

            case SpellCard.SpellType.DAMAGE_ENEMY_HERO:

                if (isPlayerCard)
                    gameManager.currentGame.enemy.hp -= spellCard.spellValue;
                else
                    gameManager.currentGame.player.hp -= spellCard.spellValue;

                UIController.Instance.UpdateHPAndMana();
                gameManager.CheckForResult();

                break;

            case SpellCard.SpellType.HEAL_ALLY_CARD:
                target.self.health += spellCard.spellValue;
                break;

            case SpellCard.SpellType.DAMAGE_ENEMY_CARD:
                
                GiveDamageTo(target, spellCard.spellValue);
                
                break;

            case SpellCard.SpellType.SHIELD_ON_ALLY_CARD:
                
                if (!target.self.abilities.Exists(x => x == Card.AbilityType.SHIELD))
                    target.self.abilities.Add(Card.AbilityType.SHIELD);
                
                break;

            case SpellCard.SpellType.PROVOCATION_ON_ALLY_CARD:
                
                if (!target.self.abilities.Exists(x => x == Card.AbilityType.PROVOCATION))
                    target.self.abilities.Add(Card.AbilityType.PROVOCATION);
                
                break;

            case SpellCard.SpellType.BUFF_CARD_DAMAGE:
                
                target.self.attack += spellCard.spellValue;
                
                break;

            case SpellCard.SpellType.DEBUFF_CARD_DAMAGE:
                
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

    void GiveDamageTo(CardController card, int damage)
    {
        card.self.GetDamage(damage);
        card.CheckForAlive();
        card.OnTakeDamage();
    }

    public void CheckForAlive()
    {
        if (self.IsAlive)
            info.RefreshData();
        else
            DestroyCard();
    }

    public void DestroyCard()
    {
        movement.OnEndDrag(null);

        RemoveCardFromList(gameManager.enemyFieldCards);
        RemoveCardFromList(gameManager.enemyHandCards);
        RemoveCardFromList(gameManager.playerFieldCards);
        RemoveCardFromList(gameManager.playerHandCards);

        Destroy(gameObject);
    }

    void RemoveCardFromList(List<CardController> list)
    {
        if (list.Exists(x => x == this))
            list.Remove(this);
    }
}