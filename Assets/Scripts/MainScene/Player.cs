using Unity.VisualScripting;
using UnityEngine;

public class Player
{
    public int hp, mana, manaPool;

    public Player()
    {
        hp = 30;
        mana = manaPool = 1; // Enemy has more mana at start?
    }

    public void IncreaseManaPool()
    {
        if (manaPool < 10)
            manaPool++;
    }

    public void RestoreRoundMana()
    {
        mana = manaPool;
    }

    public void GetDamage(int dmg)
    {
        hp = Mathf.Max(0, hp - dmg);
    }
}