using System.Collections.Generic;
using UnityEngine;

public struct Card
{
    public string Name;
    public Sprite Logo;
    public int Attack, Defense;
    public bool CanAttack;

    public bool IsAlive 
    {
        get 
        {
            return Defense > 0;
        }
    }

    public Card(string name, string logoPath, int attack, int defense)
    {
        Name = name;
        Logo = Resources.Load<Sprite>(logoPath);
        Attack = attack;
        Defense = defense;
        CanAttack = false;
    }

    public void ChangeAttackState(bool can) 
    { 
        CanAttack = can; 
    }

    public void GetDamage(int dmg) 
    {
        Defense -= dmg;
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
        CardManager.AllCards.Add(new Card("ebalo", "Sprites/Cards/anime", 5, 5));
        CardManager.AllCards.Add(new Card("buldiga", "Sprites/Cards/anime2", 4, 3));
        CardManager.AllCards.Add(new Card("hmm", "Sprites/Cards/gto", 3, 3));
        CardManager.AllCards.Add(new Card("micro", "Sprites/Cards/isagi", 2, 1));
        CardManager.AllCards.Add(new Card("pominki", "Sprites/Cards/Miku", 8, 1));
        CardManager.AllCards.Add(new Card("pomoika", "Sprites/Cards/yuki2", 1, 1));
    }
}
