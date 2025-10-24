using System.Collections.Generic;
using UnityEngine;

public class Card
{
    public string name;
    public int attack, health, manaCost, timesDealedDamage;
    public bool canAttack, isPlaced, isSpell;
    public Sprite logo;

    public enum AbilityType
    {
        None,
        Charge,
        DoubleAttack,
        Taunt,
        Shield,
        Regeneration,
        CounterAttack
    }

    public List<AbilityType> abilities;

    public bool IsAlive => health > 0;
    public bool HasAbility => abilities.Count > 0;
    public bool IsProvocation => abilities.Exists(x => x == AbilityType.Taunt);

    public Card(string name, int manaCost, string logoPath, int attack, int health, AbilityType abilityType = 0)
    {
        this.name = name;
        this.manaCost = manaCost;
        logo = Resources.Load<Sprite>(logoPath);
        this.attack = attack;
        this.health = health;

        canAttack = false;
        isPlaced = false;

        abilities = new List<AbilityType>();
        if (abilityType != 0)
            abilities.Add(abilityType);

        timesDealedDamage = 0;
    }

    public Card(Card card)
    {
        name = card.name;
        manaCost = card.manaCost;
        logo = card.logo;
        attack = card.attack;
        health = card.health;

        canAttack = false;
        isPlaced = false;

        abilities = new List<AbilityType>(card.abilities);
        timesDealedDamage = 0;
    }

    public void GetDamage(int dmg) 
    {
        if (dmg <= 0)
            return;

        if (abilities.Exists(x => x == AbilityType.Shield))
            abilities.Remove(AbilityType.Shield);
        else
            health -= dmg;
    }

    public Card GetCopy() => new Card(this);
}

public class SpellCard : Card
{
    public int spellValue;
    
    public enum SpellType
    {
        None,
        HealHero,
        DamageHero,
        HealCard,
        DamageCard,
        AddShield,
        AddTaunt,
        BuffAttack,
        DebuffAttack,
        HealAlliesField,
        DamageEnemiesField
    }

    public enum TargetType
    {
        None,
        AllyCard,
        EnemyCard
    }

    public SpellType spell;
    public TargetType spellTarget;

    public SpellCard(string name, int manacost, string logoPath, SpellType spellType = 0, int spellValue = 0, TargetType targetType = 0) //manaCost = spellValue?
        : base(name, manacost, logoPath, 0, 0)
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

    public new SpellCard GetCopy() => new SpellCard(this);
}

public static class CardDatabase
{
    public static List<Card> AllCards = new List<Card>();
}

public class CardManager : MonoBehaviour
{
    public void Awake()
    {
        //CardDatabase.AllCards.Add(new Card("Absolwent", 5, 5, 6, "Sprites/Cards/Absolwent)"));
        CardDatabase.AllCards.Add(new Card("Asystent", 5, "Sprites/Cards/Asystent", 4, 3));
        CardDatabase.AllCards.Add(new Card("Inzynier", 4, "Sprites/Cards/Inzynier", 3, 3));
        CardDatabase.AllCards.Add(new Card("Student", 2, "Sprites/Cards/Student", 2, 3));

        CardDatabase.AllCards.Add(new Card("Magister", 3, "Sprites/Cards/Magister", 1, 2, Card.AbilityType.Taunt));
        CardDatabase.AllCards.Add(new Card("Rektor", 5, "Sprites/Cards/Rektor", 4, 2, Card.AbilityType.Regeneration));
        CardDatabase.AllCards.Add(new Card("Biblioteka", 4, "Sprites/Cards/Biblioteka", 3, 2, Card.AbilityType.DoubleAttack));
        CardDatabase.AllCards.Add(new Card("Uczen", 1, "Sprites/Cards/Uczen", 1, 2, Card.AbilityType.Charge));
        CardDatabase.AllCards.Add(new Card("Wykladowca", 7, "Sprites/Cards/Wykladowca", 5, 1, Card.AbilityType.Shield));
        CardDatabase.AllCards.Add(new Card("Doktorant", 1, "Sprites/Cards/Doktorant", 3, 1, Card.AbilityType.CounterAttack));

        CardDatabase.AllCards.Add(new SpellCard("Podrecznik", 2, "Sprites/Cards/Podrecznik",
            SpellCard.SpellType.HealAlliesField, 2, SpellCard.TargetType.None));
        CardDatabase.AllCards.Add(new SpellCard("Impreza", 2, "Sprites/Cards/Impreza",
            SpellCard.SpellType.DamageEnemiesField, 2, SpellCard.TargetType.None));
        CardDatabase.AllCards.Add(new SpellCard("Kawa", 2, "Sprites/Cards/Kawa",
            SpellCard.SpellType.HealHero, 2, SpellCard.TargetType.None));
        CardDatabase.AllCards.Add(new SpellCard("Kserowka", 2, "Sprites/Cards/Kserowka",
            SpellCard.SpellType.DamageHero, 2, SpellCard.TargetType.None));
        CardDatabase.AllCards.Add(new SpellCard("Obrona", 2, "Sprites/Cards/Obrona",
            SpellCard.SpellType.HealCard, 2, SpellCard.TargetType.AllyCard));
        CardDatabase.AllCards.Add(new SpellCard("Blad", 2, "Sprites/Cards/Blad",
            SpellCard.SpellType.DamageCard, 2, SpellCard.TargetType.EnemyCard));
        CardDatabase.AllCards.Add(new SpellCard("Projekt", 2, "Sprites/Cards/Projekt",
            SpellCard.SpellType.AddShield, 0, SpellCard.TargetType.AllyCard));
        CardDatabase.AllCards.Add(new SpellCard("Sesja", 2, "Sprites/Cards/Sesja",
            SpellCard.SpellType.AddTaunt, 0, SpellCard.TargetType.AllyCard));
        CardDatabase.AllCards.Add(new SpellCard("Staz", 2, "Sprites/Cards/Staz",
            SpellCard.SpellType.BuffAttack, 2, SpellCard.TargetType.AllyCard));
        CardDatabase.AllCards.Add(new SpellCard("Egzamin", 2, "Sprites/Cards/Egzamin",
            SpellCard.SpellType.DebuffAttack, 2, SpellCard.TargetType.EnemyCard));
    }
}
