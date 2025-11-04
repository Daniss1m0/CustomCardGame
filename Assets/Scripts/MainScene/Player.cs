using Unity.VisualScripting;
using UnityEngine;

public class Player
{
    public int hp, mana, manaPool, tempMana = 0;

    public int MaxManaForThisTurn => Mathf.Min(manaPool, 10) + tempMana;

    public Player()
    {
        hp = 30;
        mana = manaPool = 0; // Enemy has more mana at start?
        //tempMana = 0;
    }

    public void IncreaseManaPool()
    {
        if (manaPool < 10)
            manaPool++;
    }

    public void RestoreRoundMana()
    {
        mana = MaxManaForThisTurn;
    }

    public void GetDamage(int dmg)
    {
        hp = Mathf.Max(0, hp - dmg);
    }

    public void ClearTemporaryMana()
    {
        tempMana = 0;
    }
}