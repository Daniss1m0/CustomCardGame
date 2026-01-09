using System.Collections.Generic;
using UnityEngine;

public enum AbilityType
{
    None,
    Charge,
    DoubleAttack,
    Taunt,
    Shield,
    Regeneration,
    CounterAttack //Or more
}

public class Card
{
    public string id, name, description;
    public int attack, health, manaCost, timesDealedDamage;
    public bool canAttack, isPlaced, isSpell;
    public Sprite logo;
    public List<AbilityType> abilities;

    public bool IsAlive => health > 0;
    public bool HasAbility => abilities.Count > 0;
    public bool IsProvocation => abilities.Exists(x => x == AbilityType.Taunt);

    public Card(CardData data)
    {
        id = data.cardId;
        name = data.cardName;
        description = data.description;
        manaCost = data.manaCost;
        logo = data.logo;
        attack = data.attack;
        health = data.health;
        isSpell = data.isSpell;

        canAttack = false;
        isPlaced = false;
        abilities = new List<AbilityType>(data.abilities ?? new List<AbilityType>());
        timesDealedDamage = 0;
    }

    public Card(Card card)
    {
        id = card.id;
        name = card.name;
        description = card.description;
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

    public virtual Card GetCopy() => new(this);
}

public enum SpellType
{
    None,
    GiveTempMana,
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

public class SpellCard : Card
{
    public int spellPower;
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

    public override Card GetCopy() => new SpellCard(this);
}
