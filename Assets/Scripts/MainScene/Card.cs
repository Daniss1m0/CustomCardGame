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

    public Card(CardData data)
    {
        name = data.cardName;
        manaCost = data.manaCost;
        logo = data.logo;
        attack = data.attack;
        health = data.health;
        isSpell = data.isSpell;

        canAttack = false;
        isPlaced = false;

        abilities = new List<AbilityType>(data.abilities ?? new List<AbilityType>()); //??
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

        abilities = new List<AbilityType>(card.abilities ?? new List<AbilityType>());
        timesDealedDamage = 0;
    }

    public void GetDamage(int dmg)
    {
        if (dmg <= 0)
            return;

        if (abilities != null && abilities.Exists(x => x == AbilityType.Shield))
            abilities.Remove(AbilityType.Shield);
        else
            health -= dmg;
    }

    public Card GetCopy() => new(this);
}

public class SpellCard : Card
{
    public int spellPower;

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

    public SpellCard(CardData data) : base(data)
    {
        isSpell = true;
        spellPower = data.spellPower;
        spell = data.spellType;
        spellTarget = data.spellTarget;
    }

    public SpellCard(SpellCard card) : base(card)
    {
        isSpell = true;
        spellPower = card.spellPower;
        spell = card.spell;
        spellTarget = card.spellTarget;
    }

    public new SpellCard GetCopy() => new(this);
}
