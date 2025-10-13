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

            List<CardController> cardsList = cards.FindAll(x => GameManagerScr.Instance.currentGame.enemy.mana >= x.card.manacost); 

            if (cardsList.Count == 0)
                break;

            if (cardsList[0].card.isSpell)
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

        while (GameManagerScr.Instance.enemyFieldCards.Exists(x => x.card.canAttack))
        {
            var activeCard = GameManagerScr.Instance.enemyFieldCards.FindAll(x => x.card.canAttack)[0];
            bool hasProvocation = GameManagerScr.Instance.playerFieldCards.Exists(x => x.card.IsProvocation);

            if (hasProvocation || Random.Range(0, 2) == 0 && GameManagerScr.Instance.playerFieldCards.Count > 0)
            {
                CardController enemy;

                if (hasProvocation)
                    enemy = GameManagerScr.Instance.playerFieldCards.Find(x => x.card.IsProvocation);
                else
                    enemy = GameManagerScr.Instance.playerFieldCards[Random.Range(0, GameManagerScr.Instance.playerFieldCards.Count)];

                Debug.Log(activeCard.card.name + "(" + activeCard.card.attack + ";" + activeCard.card.health + "))" + "---> " +
                enemy.card.name + " (" + enemy.card.attack + ";" + enemy.card.health + ")");

                activeCard.Movement.MoveToTarget(enemy.transform);
                yield return new WaitForSeconds(.75f);

                GameManagerScr.Instance.CardsFight(activeCard, enemy);
            }
            else
            {
                Debug.Log(activeCard.card.name + " (" + activeCard.card.attack + ") Attacked Hero");

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
        switch (((SpellCard)card.card).spellTarget)
        {
            case SpellCard.TargetType.NO_TARGET:
                
                switch (((SpellCard)card.card).spell)
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
        if (((SpellCard)spell.card).spellTarget == SpellCard.TargetType.NO_TARGET)
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
            GameManagerScr.Instance.ReduceMana(false, spell.card.manacost);

            spell.card.isPlaced = true;
            spell.UseSpell(target);
        }

        string targetStr = target == null ? "no_target" : target.card.name;
        Debug.Log("AI spell cast: " + spell.card.name + " target: " + targetStr);
    }
}
