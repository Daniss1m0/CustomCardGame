using System.Collections.Generic;
using UnityEngine;

public class CardAbility : MonoBehaviour
{
    [SerializeField] private GameObject shield, taunt;

    public void OnCast(Card card, bool isPlayerCard, CardInfo info = null)
    {
        foreach (var ability in card.abilities)
        {
            switch (ability)
            {
                case Card.AbilityType.Charge:
                   
                    card.canAttack = true;
                    if (isPlayerCard && info != null)
                        info.SetHighlight(true);
                    
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

    public void OnDamageDeal(Card card, bool isPlayerCard, CardInfo info = null)
    {
        foreach (var ability in card.abilities)
        {
            switch (ability)
            {
                case Card.AbilityType.DoubleAttack:
                    
                    if (card.timesDealedDamage == 1)
                    {
                        card.canAttack = true;
                        if (isPlayerCard && info != null)
                            info.SetHighlight(true);
                    }
                    
                    break;
            }
        }
    }

    public void OnTakeDamage(Card card, CardController attacker)
    {
        shield.SetActive(false);

        foreach (var ability in card.abilities)
        {
            switch (ability)
            {
                case Card.AbilityType.Shield:
                    
                    shield.SetActive(true);

                    break;

                case Card.AbilityType.CounterAttack:
                    
                    if (attacker != null)
                        attacker.self.GetDamage(card.attack);
                    
                    break;
            }
        }
    }

    public void OnNewTurn(Card card, CardInfo info = null)
    {
        card.timesDealedDamage = 0;

        foreach (var ability in card.abilities)
        {
            switch (ability)
            {
                case Card.AbilityType.Regeneration:
                    
                    card.health += 2;
                    info.UpdateStats(card);
                    
                    break;
            }
        }
    }
}
