using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI : MonoBehaviour
{
    public void MakeTurn()
    {
        StartCoroutine(EnemyTurn(GameManagerScr.Instance.enemyHandCards));
    }

    IEnumerator EnemyTurn(List<CardController> cards)
    {
        yield return new WaitForSeconds(1);

        int count = cards.Count == 1 ? 1 : Random.Range(0, cards.Count);

        for (int i = 0; i < count; i++)
        {
            if (GameManagerScr.Instance.enemyFieldCards.Count > 5 || GameManagerScr.Instance.currentGame.enemy.mana == 0 || GameManagerScr.Instance.enemyHandCards.Count == 0)
                break;

            List<CardController> cardsList = cards.FindAll(x => GameManagerScr.Instance.currentGame.enemy.mana >= x.Card.Manacost); 

            if (cardsList.Count == 0)
                break;

            if (cardsList[0].Card.IsSpell)
            {
                CastSpell(cardsList[0]);
                yield return new WaitForSeconds(.51f);
            }
            else
            {
                cardsList[0].GetComponent<CardMovement>().MoveToField(GameManagerScr.Instance.enemyField);
                yield return new WaitForSeconds(.51f);
                cardsList[0].transform.SetParent(GameManagerScr.Instance.enemyField);
                cardsList[0].OnCast();
            }
        }

        yield return new WaitForSeconds(1);

        while (GameManagerScr.Instance.enemyFieldCards.Exists(x => x.Card.CanAttack))
        {
            var activeCard = GameManagerScr.Instance.enemyFieldCards.FindAll(x => x.Card.CanAttack)[0];
            bool hasProvocation = GameManagerScr.Instance.playerFieldCards.Exists(x => x.Card.IsProvocation);

            if (hasProvocation || Random.Range(0, 2) == 0 && GameManagerScr.Instance.playerFieldCards.Count > 0)
            {
                CardController enemy;

                if (hasProvocation)
                    enemy = GameManagerScr.Instance.playerFieldCards.Find(x => x.Card.IsProvocation);
                else
                    enemy = GameManagerScr.Instance.playerFieldCards[Random.Range(0, GameManagerScr.Instance.playerFieldCards.Count)];

                Debug.Log(activeCard.Card.Name + "(" + activeCard.Card.Attack + ";" + activeCard.Card.Health + "))" + "---> " +
                enemy.Card.Name + " (" + enemy.Card.Attack + ";" + enemy.Card.Health + ")");

                activeCard.Movement.MoveToTarget(enemy.transform);
                yield return new WaitForSeconds(.75f);

                GameManagerScr.Instance.CardsFight(activeCard, enemy);
            }
            else
            {
                Debug.Log(activeCard.Card.Name + " (" + activeCard.Card.Attack + ") Attacked Hero");

                activeCard.GetComponent<CardMovement>().MoveToTarget(GameManagerScr.Instance.playerHero.transform);
                yield return new WaitForSeconds(.75f);

                GameManagerScr.Instance.DamageHero(activeCard, false);
            }

            yield return new WaitForSeconds(.2f);
        }

        yield return new WaitForSeconds(1);
        GameManagerScr.Instance.ChangeTurn();
    }

    void CastSpell(CardController card)
    {
        switch (((SpellCard)card.Card).SpellTarget)
        {
            case SpellCard.TargetType.NO_TARGET:
                
                switch (((SpellCard)card.Card).Spell)
                {
                    case SpellCard.SpellType.HEAL_ALLY_FIELD_CARDS:
                        if (GameManagerScr.Instance.enemyFieldCards.Count > 0)
                            StartCoroutine(CastCard(card));
                        break;
                    case SpellCard.SpellType.DAMAGE_ENEMY_FIELD_CARDS:
                        if (GameManagerScr.Instance.playerFieldCards.Count > 0)
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

                if (GameManagerScr.Instance.enemyFieldCards.Count > 0)
                    StartCoroutine(CastCard(card, GameManagerScr.Instance.enemyFieldCards[Random.Range(0, GameManagerScr.Instance.enemyFieldCards.Count)]));
                break;

            case SpellCard.TargetType.ENEMY_CARD_TARGET:

                if (GameManagerScr.Instance.playerFieldCards.Count > 0)
                    StartCoroutine(CastCard(card, GameManagerScr.Instance.playerFieldCards[Random.Range(0, GameManagerScr.Instance.playerFieldCards.Count)]));
                break;
        }
    }

    IEnumerator CastCard(CardController spell, CardController target = null)
    {
        if (((SpellCard)spell.Card).SpellTarget == SpellCard.TargetType.NO_TARGET)
        {
            spell.GetComponent<CardMovement>().MoveToField(GameManagerScr.Instance.enemyField);
            yield return new WaitForSeconds(.51f);

            spell.OnCast();
        }
        else
        {
            spell.Info.ShowCardInfo();
            spell.GetComponent<CardMovement>().MoveToTarget(target.transform);
            yield return new WaitForSeconds(.51f);

            GameManagerScr.Instance.enemyHandCards.Remove(spell);
            GameManagerScr.Instance.enemyFieldCards.Add(spell);
            GameManagerScr.Instance.ReduceMana(false, spell.Card.Manacost);

            spell.Card.IsPlaced = true;
            spell.UseSpell(target);
        }

        string targetStr = target == null ? "no_target" : target.Card.Name;
        Debug.Log("AI spell cast: " + spell.Card.Name + " target: " + targetStr);
    }
}
