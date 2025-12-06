using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Cards/CardData")]
public class CardData : ScriptableObject
{
    [Header("Identifiers")]
    public string cardId;

    [Header("Basic")]
    public string cardName;
    public int manaCost;
    public Sprite logo;
    public bool isSpell = false;
    [TextArea(2, 3)]
    public string description;

    [Header("Stats (for non-spell)")]
    public int attack;
    public int health;

    [Header("Abilities")]
    public List<AbilityType> abilities = new();

    [Header("Spell (if isSpell)")]
    public SpellType spellType = SpellType.None;
    public TargetType spellTarget = TargetType.None;
    public int spellPower = 0;
}
