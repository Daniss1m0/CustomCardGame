using System.Collections.Generic;
using UnityEngine;

public class Card
{
    public string name;
    public int attack, health, manacost, timesDealedDamage;
    public bool canAttack, isPlaced, isSpell;
    public Sprite logo;

    public enum AbilityType
    {
        NO_ABILITY,
        INSTANT_ACTIVE,
        DOUBLE_ATTACK,
        SHIELD,
        PROVOCATION,
        REGENERATION_EACH_TURN,
        COUNTER_ATTACK
    }

    public List<AbilityType> abilities;

    public bool IsAlive 
    {
        get 
        {
            return health > 0;
        }
    }

    public bool HasAbility
    {
        get
        {
            return abilities.Count > 0;
        }
    }

    public bool IsProvocation
    {
        get
        {
            return abilities.Exists(x => x == AbilityType.PROVOCATION);
        }
    }

    public Card(string name, int attack, int health, int manacost, string logoPath, AbilityType abilityType = 0)
    {
        this.name = name;
        this.attack = attack;
        this.health = health;
        this.manacost = manacost;
        canAttack = false;
        isPlaced = false;
        logo = Resources.Load<Sprite>(logoPath);

        abilities = new List<AbilityType>();

        if (abilityType != 0)
            abilities.Add(abilityType);

        timesDealedDamage = 0;
    }

    public Card(Card card)
    {
        name = card.name;
        logo = card.logo;
        attack = card.attack;
        health = card.health;
        manacost = card.manacost;
        canAttack = false;
        isPlaced = false;

        abilities = new List<AbilityType>(card.abilities);

        timesDealedDamage = 0;
    }

    public void GetDamage(int dmg) 
    {
        if (dmg > 0)
        {
            if (abilities.Exists(x => x == AbilityType.SHIELD))
                abilities.Remove(AbilityType.SHIELD);
            else
                health -= dmg;
        }    
    }

    public Card GetCopy()
    {
        return new Card(this);
    }
}

public class SpellCard : Card
{
    public int spellValue;
    
    public enum SpellType
    {
        NO_SPELL,
        HEAL_ALLY_FIELD_CARDS,
        DAMAGE_ENEMY_FIELD_CARDS,
        HEAL_ALLY_HERO,
        DAMAGE_ENEMY_HERO,
        HEAL_ALLY_CARD,
        DAMAGE_ENEMY_CARD,
        SHIELD_ON_ALLY_CARD,
        PROVOCATION_ON_ALLY_CARD,
        BUFF_CARD_DAMAGE,
        DEBUFF_CARD_DAMAGE
    }

    public enum TargetType
    {
        NO_TARGET,
        ALLY_CARD_TARGET,
        ENEMY_CARD_TARGET
    }

    public SpellType spell;
    public TargetType spellTarget;

    public SpellCard(string name, string logoPath, int manacost, SpellType spellType = 0, int spellValue = 0, TargetType targetType = 0) 
        : base(name, 0, 0, manacost, logoPath)
    {
        isSpell = true;

        this.spellValue = spellValue;
        spell = spellType;
        spellTarget = targetType;
    }

    public SpellCard(SpellCard card) : base(card)
    {
        isSpell = true;

        spellValue = card.spellValue;
        spell = card.spell;
        spellTarget = card.spellTarget;
    }

    public new SpellCard GetCopy()
    {
        return new SpellCard(this);
    }
}

public static class CardManager
{
    public static List<Card> AllCards = new List<Card>();
}

public class CardManagerScr : MonoBehaviour
{
    public void Awake()
    {
        //CardManager.AllCards.Add(new Card("Absolwent", "Sprites/Cards/Absolwent)", 5, 5, 6));
        CardManager.AllCards.Add(new Card("Asystent", 4, 3, 5, "Sprites/Cards/Asystent"));
        CardManager.AllCards.Add(new Card("Inzynier", 3, 3, 4, "Sprites/Cards/Inzynier"));
        CardManager.AllCards.Add(new Card("Student", 2, 1, 2, "Sprites/Cards/Student"));

        CardManager.AllCards.Add(new Card("Magister", 1, 2, 3, "Sprites/Cards/Magister", Card.AbilityType.PROVOCATION));
        CardManager.AllCards.Add(new Card("Rektor", 4, 2, 5, "Sprites/Cards/Rektor", Card.AbilityType.REGENERATION_EACH_TURN));
        CardManager.AllCards.Add(new Card("Biblioteka", 3, 2, 4, "Sprites/Cards/Biblioteka", Card.AbilityType.DOUBLE_ATTACK));
        CardManager.AllCards.Add(new Card("Uczen", 2, 1, 2, "Sprites/Cards/Uczen", Card.AbilityType.INSTANT_ACTIVE));
        CardManager.AllCards.Add(new Card("Wykladowca", 5, 1, 7, "Sprites/Cards/Wykladowca", Card.AbilityType.SHIELD));
        CardManager.AllCards.Add(new Card("Doktorant", 3, 1, 1, "Sprites/Cards/Doktorant", Card.AbilityType.COUNTER_ATTACK));

        CardManager.AllCards.Add(new SpellCard("Podrecznik", "Sprites/Cards/Podrecznik", 2,
            SpellCard.SpellType.HEAL_ALLY_FIELD_CARDS, 2, SpellCard.TargetType.NO_TARGET));
        CardManager.AllCards.Add(new SpellCard("Impreza", "Sprites/Cards/Impreza", 2,
            SpellCard.SpellType.DAMAGE_ENEMY_FIELD_CARDS, 2, SpellCard.TargetType.NO_TARGET));
        CardManager.AllCards.Add(new SpellCard("Kawa", "Sprites/Cards/Kawa", 2,
            SpellCard.SpellType.HEAL_ALLY_HERO, 2, SpellCard.TargetType.NO_TARGET));
        CardManager.AllCards.Add(new SpellCard("Kserowka", "Sprites/Cards/Kserowka", 2,
            SpellCard.SpellType.DAMAGE_ENEMY_HERO, 2, SpellCard.TargetType.NO_TARGET));
        CardManager.AllCards.Add(new SpellCard("Obrona", "Sprites/Cards/Obrona", 2,
            SpellCard.SpellType.HEAL_ALLY_CARD, 2, SpellCard.TargetType.ALLY_CARD_TARGET));
        CardManager.AllCards.Add(new SpellCard("Blad", "Sprites/Cards/Blad", 2,
            SpellCard.SpellType.DAMAGE_ENEMY_CARD, 2, SpellCard.TargetType.ENEMY_CARD_TARGET));
        CardManager.AllCards.Add(new SpellCard("Projekt", "Sprites/Cards/Projekt", 2,
            SpellCard.SpellType.SHIELD_ON_ALLY_CARD, 0, SpellCard.TargetType.ALLY_CARD_TARGET));
        CardManager.AllCards.Add(new SpellCard("Sesja", "Sprites/Cards/Sesja", 2,
            SpellCard.SpellType.PROVOCATION_ON_ALLY_CARD, 0, SpellCard.TargetType.ALLY_CARD_TARGET));
        CardManager.AllCards.Add(new SpellCard("Staz", "Sprites/Cards/Staz", 2,
            SpellCard.SpellType.BUFF_CARD_DAMAGE, 2, SpellCard.TargetType.ALLY_CARD_TARGET));
        CardManager.AllCards.Add(new SpellCard("Egzamin", "Sprites/Cards/Egzamin", 2,
            SpellCard.SpellType.DEBUFF_CARD_DAMAGE, 2, SpellCard.TargetType.ENEMY_CARD_TARGET));
    }
}
