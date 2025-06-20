using UnityEngine;

[CreateAssetMenu(menuName = "Magic/Runes/Element")]
public class ElementRune : SpellRune
{
    public ElementType elementType;
}

[CreateAssetMenu(menuName = "Magic/Runes/Behavior")]
public class BehaviorRune : SpellRune
{
    public BehaviorType behaviorType;
}

[CreateAssetMenu(menuName = "Magic/Runes/Modifier")]
public class ModifierRune : SpellRune
{
    public ModifierType modifierType;

    [Header("Modifier Values")]
    public float powerModifier = 1f;
    public float durationModifier = 1f;
    public float rangeModifier = 1f;
    public float speedModifier = 1f;
    public float sizeModifier = 1f;

    public float GetModifierValue(ModifierType type)
    {
        switch (type)
        {
            case ModifierType.Power:
                return powerModifier;
            case ModifierType.Duration:
                return durationModifier;
            case ModifierType.Range:
                return rangeModifier;
            case ModifierType.Speed:
                return speedModifier;
            case ModifierType.Size:
                return sizeModifier;
            default:
                return 1f;
        }
    }
}

[CreateAssetMenu(menuName = "Magic/Runes/Trait")]
public class TraitRune : SpellRune
{
    public TraitType traitType;
}