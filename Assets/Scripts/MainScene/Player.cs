using Unity.VisualScripting;
using UnityEngine;

public class Player
{
    public int hp, mana, manaPool, tempMana;

    public Player()
    {
        hp = 30;
        mana = manaPool = tempMana = 0;
    }

    public void IncreaseManaPool()
    {
        if (manaPool < 10)
            manaPool++;
    }

    public void RestoreRoundMana()
    {
        mana = manaPool + tempMana;
    }

    public void AddTempMana(int amount)
    {
        tempMana += amount;
        mana += amount;
    }

    public void ClearTempMana()
    {
        tempMana = 0;
    }

    public void GetDamage(int dmg)
    {
        hp = Mathf.Max(0, hp - dmg);
    }
}