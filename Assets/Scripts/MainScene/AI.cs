using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI : MonoBehaviour
{
    public void MakeTurn()
    {
        StartCoroutine(EnemyTurn(GameManager.Instance.enemyHandCards));
    }

    IEnumerator EnemyTurn(List<CardController> cards)
    {
        yield return new WaitForSeconds(1);

        int count = cards.Count == 1 ? 1 : Random.Range(0, cards.Count);

        for (int i = 0; i < count; i++)
        {
            if (GameManager.Instance.enemyFieldCards.Count > 5 || GameManager.Instance.currentGame.enemy.mana == 0 || GameManager.Instance.enemyHandCards.Count == 0)
                break;

            List<CardController> cardsList = cards.FindAll(x => GameManager.Instance.currentGame.enemy.mana >= x.self.manacost); 

            if (cardsList.Count == 0)
                break;

            if (cardsList[0].self.isSpell)
            {
                CastSpell(cardsList[0]);
                yield return new WaitForSeconds(.51f);
            }
            else
            {
                cardsList[0].GetComponent<CardMovement>().MoveToField(GameManager.Instance.EnemyField);
                yield return new WaitForSeconds(.51f);
                cardsList[0].transform.SetParent(GameManager.Instance.EnemyField);
                cardsList[0].OnCast();
            }
        }

        yield return new WaitForSeconds(1);

        while (GameManager.Instance.enemyFieldCards.Exists(x => x.self.canAttack))
        {
            var activeCard = GameManager.Instance.enemyFieldCards.FindAll(x => x.self.canAttack)[0];
            bool hasProvocation = GameManager.Instance.playerFieldCards.Exists(x => x.self.IsProvocation);

            if (hasProvocation || Random.Range(0, 2) == 0 && GameManager.Instance.playerFieldCards.Count > 0)
            {
                CardController enemy;

                if (hasProvocation)
                    enemy = GameManager.Instance.playerFieldCards.Find(x => x.self.IsProvocation);
                else
                    enemy = GameManager.Instance.playerFieldCards[Random.Range(0, GameManager.Instance.playerFieldCards.Count)];

                Debug.Log(activeCard.self.name + "(" + activeCard.self.attack + ";" + activeCard.self.health + "))" + "---> " +
                enemy.self.name + " (" + enemy.self.attack + ";" + enemy.self.health + ")");

                activeCard.movement.MoveToTarget(enemy.transform);
                yield return new WaitForSeconds(.75f);

                GameManager.Instance.CardsFight(activeCard, enemy);
            }
            else
            {
                Debug.Log(activeCard.self.name + " (" + activeCard.self.attack + ") Attacked Hero");

                activeCard.GetComponent<CardMovement>().MoveToTarget(GameManager.Instance.PlayerHero.transform);
                yield return new WaitForSeconds(.75f);

                GameManager.Instance.DamageHero(activeCard, false);
            }

            yield return new WaitForSeconds(.2f);
        }

        yield return new WaitForSeconds(1);
        GameManager.Instance.ChangeTurn();
    }

    void CastSpell(CardController card)
    {
        switch (((SpellCard)card.self).spellTarget)
        {
            case SpellCard.TargetType.NO_TARGET:
                
                switch (((SpellCard)card.self).spell)
                {
                    case SpellCard.SpellType.HEAL_ALLY_FIELD_CARDS:
                        if (GameManager.Instance.enemyFieldCards.Count > 0)
                            StartCoroutine(CastCard(card));
                        break;
                    case SpellCard.SpellType.DAMAGE_ENEMY_FIELD_CARDS:
                        if (GameManager.Instance.playerFieldCards.Count > 0)
                            StartCoroutine(CastCard(card));
                        break;
                    case SpellCard.SpellType.HEAL_ALLY_HERO:
                        StartCoroutine(CastCard(card));
                        break;
                    case SpellCard.SpellType.DAMAGE_ENEMY_HERO:
                        StartCoroutine(CastCard(card));
                        break;
                }
                break;

            case SpellCard.TargetType.ALLY_CARD_TARGET:

                if (GameManager.Instance.enemyFieldCards.Count > 0)
                    StartCoroutine(CastCard(card, GameManager.Instance.enemyFieldCards[Random.Range(0, GameManager.Instance.enemyFieldCards.Count)]));
                break;

            case SpellCard.TargetType.ENEMY_CARD_TARGET:

                if (GameManager.Instance.playerFieldCards.Count > 0)
                    StartCoroutine(CastCard(card, GameManager.Instance.playerFieldCards[Random.Range(0, GameManager.Instance.playerFieldCards.Count)]));
                break;
        }
    }

    IEnumerator CastCard(CardController spell, CardController target = null)
    {
        if (((SpellCard)spell.self).spellTarget == SpellCard.TargetType.NO_TARGET)
        {
            spell.GetComponent<CardMovement>().MoveToField(GameManager.Instance.EnemyField);
            yield return new WaitForSeconds(.51f);

            spell.OnCast();
        }
        else
        {
            spell.info.ShowCardInfo();
            spell.GetComponent<CardMovement>().MoveToTarget(target.transform);
            yield return new WaitForSeconds(.51f);

            GameManager.Instance.enemyHandCards.Remove(spell);
            GameManager.Instance.enemyFieldCards.Add(spell);
            GameManager.Instance.ReduceMana(false, spell.self.manacost);

            spell.self.isPlaced = true;
            spell.UseSpell(target);
        }

        string targetStr = target == null ? "no_target" : target.self.name;
        Debug.Log("AI spell cast: " + spell.self.name + " target: " + targetStr);
    }
}
