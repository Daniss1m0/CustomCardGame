using UnityEngine;

public class Player
{
    const int MAX_MANAPOOL = 10;
    
    public int hp, mana, manaPool;

    public Player()
    {
        hp = 30;
        mana = manaPool = 1;
    }

    public void RestoreRoundMana()
    {
        mana = manaPool;
    }

    public void IncreaseManapool()
    {
        manaPool = Mathf.Clamp(manaPool + 1, 0, MAX_MANAPOOL);
    }

    public void GetDamage(int damage)
    {
        hp = Mathf.Clamp(hp - damage, 0, int.MaxValue);
    }
}