using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardAbility : MonoBehaviour
{
    public CardController CC;

    [SerializeField] private GameObject shield, taunt;

    //setup?

    public void OnCast()
    {
        foreach (var ability in CC.self.abilities)
        {
            switch (ability)
            {
                case Card.AbilityType.Charge:
                    CC.self.canAttack = true;
                    if (CC.isPlayerCard)
                        CC.info.SetHighlight(true);

                    break;

                case Card.AbilityType.Shield:
                    shield.SetActive(true);
                    break;

                case Card.AbilityType.Taunt:
                    taunt.SetActive(true);
                    break;
            }
        }
    }

    public void OnDamageDeal()
    {
        foreach (var ability in CC.self.abilities)
        {
            switch (ability)
            {
                case Card.AbilityType.DoubleAttack:
                    if (CC.self.timesDealedDamage == 1)
                    {
                        CC.self.canAttack = true;
                        if (CC.isPlayerCard)
                            CC.info.SetHighlight(true);
                    }
                    break;
            }
        }
    }

    public void OnTakeDamage(CardController attacker = null)
    {
        shield.SetActive(false);

        foreach (var ability in CC.self.abilities)
        {
            switch (ability)
            {
                case Card.AbilityType.Shield:
                    shield.SetActive(true);
                    break;

                case Card.AbilityType.CounterAttack:
                    if (attacker != null)
                        attacker.self.GetDamage(CC.self.attack);
                    break;
            }
        }
    }

    public void OnNewTurn()
    {
        CC.self.timesDealedDamage = 0;

        foreach (var ability in CC.self.abilities)
        {
            switch (ability)
            {
                case Card.AbilityType.Regeneration:
                    CC.self.health += 2;
                    CC.info.UpdateStats();
                    break;
            }
        }
    }
}