using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIController : MonoBehaviour, IPlayerController
{
    public IEnumerator PerformTurn()
    {
        yield return StartCoroutine(EnemyTurn());
    }

    private IEnumerator EnemyTurn()
    {
        var cards = GameManager.Instance.enemyHandCards;

        yield return new WaitForSeconds(1f);

        int count = cards.Count == 1 ? 1 : Random.Range(0, cards.Count);

        for (int i = 0; i < count; i++)
        {
            if (GameManager.Instance.enemyFieldCards.Count > 5 ||
                GameManager.Instance.currentGame.enemy.mana == 0 ||
                GameManager.Instance.enemyHandCards.Count == 0)
                break;

            List<CardController> playable = cards.FindAll(x => GameManager.Instance.currentGame.enemy.mana >= x.self.manaCost);
            if (playable.Count == 0) break;

            var c = playable[0];

            if (c == null)
            {
                Debug.LogWarning("AIController: playable[0] is null, skipping.");
                continue;
            }

            if (c.self == null)
            {
                Debug.LogWarning("AIController: card.self is null for card controller " + c.name);
                continue;
            }

            if (c.self.isSpell)
            {
                var spellCard = (SpellCard)c.self;
                switch (spellCard.spellTarget)
                {
                    case SpellCard.TargetType.None:
                        yield return StartCoroutine(CastCardRoutine(c, null));
                        break;

                    case SpellCard.TargetType.AllyCard:
                        if (GameManager.Instance.enemyFieldCards.Count > 0)
                        {
                            var allyTarget = GameManager.Instance.enemyFieldCards[Random.Range(0, GameManager.Instance.enemyFieldCards.Count)];
                            yield return StartCoroutine(CastCardRoutine(c, allyTarget));
                        }
                        else
                        {
                            Debug.Log("AIController: no ally targets for " + c.self.name);
                        }
                        break;

                    case SpellCard.TargetType.EnemyCard:
                        if (GameManager.Instance.playerFieldCards.Count > 0)
                        {
                            var enemyTarget = GameManager.Instance.playerFieldCards[Random.Range(0, GameManager.Instance.playerFieldCards.Count)];
                            yield return StartCoroutine(CastCardRoutine(c, enemyTarget));
                        }
                        else
                        {
                            Debug.Log("AIController: no enemy targets for " + c.self.name);
                        }
                        break;
                }
            }
            else
            {
                if (c.Movement != null)
                    c.Movement.MoveToField(GameManager.Instance.EnemyField);
                else
                    Debug.LogWarning("AIController: Movement is null for " + c.name);

                yield return new WaitForSeconds(.51f);

                if (GameManager.Instance.EnemyField != null)
                    c.transform.SetParent(GameManager.Instance.EnemyField);
                c.OnCast();
            }
        }

        yield return new WaitForSeconds(1f);

        while (GameManager.Instance.enemyFieldCards.Exists(x => x.self.canAttack))
        {
            var activeCard = GameManager.Instance.enemyFieldCards.Find(x => x.self.canAttack);
            if (activeCard == null) break;

            bool hasProvocation = GameManager.Instance.playerFieldCards.Exists(x => x.self.IsProvocation);

            if ((hasProvocation) || (Random.Range(0, 2) == 0 && GameManager.Instance.playerFieldCards.Count > 0))
            {
                CardController target;
                if (hasProvocation)
                    target = GameManager.Instance.playerFieldCards.Find(x => x.self.IsProvocation);
                else
                    target = GameManager.Instance.playerFieldCards[Random.Range(0, GameManager.Instance.playerFieldCards.Count)];

                if (target == null)
                {
                    Debug.LogWarning("AIController: selected attack target is null");
                    break;
                }

                Debug.Log(activeCard.self.name + "(" + activeCard.self.attack + ";" + activeCard.self.health + "))" + "---> " +
                          target.self.name + " (" + target.self.attack + ";" + target.self.health + ")");

                if (activeCard.Movement != null)
                    activeCard.Movement.MoveToTarget(target.transform);
                yield return new WaitForSeconds(.75f);

                GameManager.Instance.CardsFight(activeCard, target);
            }
            else
            {
                Debug.Log(activeCard.self.name + " (" + activeCard.self.attack + ") Attacked Hero");

                if (GameManager.Instance.PlayerHero != null)
                {
                    if (activeCard.Movement != null)
                        activeCard.Movement.MoveToTarget(GameManager.Instance.PlayerHero.transform);
                    yield return new WaitForSeconds(.75f);
                    GameManager.Instance.DamageHero(activeCard, false);
                }
                else
                {
                    Debug.LogWarning("AIController: PlayerHero is null");
                }
            }

            yield return new WaitForSeconds(.2f);
        }

        yield return new WaitForSeconds(1f);

        GameManager.Instance.ChangeTurn();
    }

    private IEnumerator CastCardRoutine(CardController spell, CardController target = null)
    {
        if (spell == null)
        {
            Debug.LogWarning("AIController.CastCardRoutine: spell is null");
            yield break;
        }

        var spellCard = spell.self as SpellCard;
        if (spellCard == null)
        {
            Debug.LogWarning("AIController.CastCardRoutine: spell.self is not SpellCard for " + spell.name);
            yield break;
        }

        if (spellCard.spellTarget != SpellCard.TargetType.None && target == null)
        {
            Debug.LogWarning("AIController.CastCardRoutine: target is null for targeted spell " + spell.self.name);
            yield break;
        }

        if (spellCard.spellTarget == SpellCard.TargetType.None)
        {
            if (spell.Movement != null)
                spell.Movement.MoveToField(GameManager.Instance.EnemyField);
            else
                Debug.LogWarning("AIController.CastCardRoutine: Movement is null on spell " + spell.name);

            yield return new WaitForSeconds(.51f);
            spell.OnCast();
        }
        else
        {
            spell.Info?.ShowCard(spell.self);

            if (target == null)
            {
                Debug.LogWarning("AIController.CastCardRoutine: unexpected null target for " + spell.self.name);
                yield break;
            }

            if (spell.Movement != null)
                spell.Movement.MoveToTarget(target.transform);
            else
                Debug.LogWarning("AIController.CastCardRoutine: Movement is null on spell " + spell.name);

            yield return new WaitForSeconds(.51f);

            if (GameManager.Instance.enemyHandCards.Contains(spell))
                GameManager.Instance.enemyHandCards.Remove(spell);
            GameManager.Instance.enemyFieldCards.Add(spell);
            GameManager.Instance.ReduceMana(false, spell.self.manaCost);

            spell.self.isPlaced = true;
            spell.UseSpell(target);
        }

        string targetStr = target == null ? "no_target" : target.self.name;
        Debug.Log("AI spell cast: " + spell.self.name + " target: " + targetStr);
    }
}
