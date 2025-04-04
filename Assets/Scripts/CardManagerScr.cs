using System.Collections.Generic;
using UnityEngine;

public struct Card
{
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

    public string Name;
    public Sprite Logo;
    public int Attack, Defense, Manacost;
    public bool CanAttack;
    public bool IsPlaced;

    public List<AbilityType> Abilities;
    public int TimesDealedDamage;

    public bool IsAlive 
    {
        get 
        {
            return Defense > 0;
        }
    }

    public bool HasAbility
    {
        get
        {
            return Abilities.Count > 0;
        }
    }

    public bool IsProvocation
    {
        get
        {
            return Abilities.Exists(x => x == AbilityType.PROVOCATION);
        }
    }

    public Card(string name, string logoPath, int attack, int defense, int manacost, AbilityType abilityType = 0)
    {
        Name = name;
        Logo = Resources.Load<Sprite>(logoPath);
        Attack = attack;
        Defense = defense;
        Manacost = manacost;
        CanAttack = false;
        IsPlaced = false;

        Abilities = new List<AbilityType>();

        if (abilityType != 0)
            Abilities.Add(abilityType);

        TimesDealedDamage = 0;
    }

    public void GetDamage(int dmg) 
    {
        if (dmg > 0)
        {
            if (Abilities.Exists(x => x == AbilityType.SHIELD))
                Abilities.Remove(AbilityType.SHIELD);
            else
                Defense -= dmg;

        }    
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
        CardManager.AllCards.Add(new Card("ebalo", "Sprites/Cards/)", 5, 5, 6));
        CardManager.AllCards.Add(new Card("buldiga", "Sprites/Cards/Akane", 4, 3, 5));
        CardManager.AllCards.Add(new Card("hmm", "Sprites/Cards/gto", 3, 3, 4));
        CardManager.AllCards.Add(new Card("micro", "Sprites/Cards/igris", 2, 1, 2));
        CardManager.AllCards.Add(new Card("pominki", "Sprites/Cards/isagi", 8, 1, 7));
        CardManager.AllCards.Add(new Card("pomokka", "Sprites/Cards/lollol", 1, 1, 1));

        CardManager.AllCards.Add(new Card("provocation", "Sprites/Cards/satoru", 1, 2, 3, Card.AbilityType.PROVOCATION));
        CardManager.AllCards.Add(new Card("regeneration", "Sprites/Cards/torpin", 4, 2, 5, Card.AbilityType.REGENERATION_EACH_TURN));
        CardManager.AllCards.Add(new Card("doubleattack", "Sprites/Cards/yuki1", 3, 2, 4, Card.AbilityType.DOUBLE_ATTACK));
        CardManager.AllCards.Add(new Card("instantActive", "Sprites/Cards/yuki2", 2, 1, 2, Card.AbilityType.INSTANT_ACTIVE));
        CardManager.AllCards.Add(new Card("shield", "Sprites/Cards/beru", 5, 1, 7, Card.AbilityType.SHIELD));
        CardManager.AllCards.Add(new Card("counterAttack", "Sprites/Cards/rofls", 3, 1, 1, Card.AbilityType.COUNTER_ATTACK));
    }
}
