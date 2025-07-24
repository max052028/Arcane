using UnityEngine;

public abstract class Rune : ScriptableObject
{
    [Header("Basic Info")]
    public Sprite runeIcon;
    public KeyCode inputKey;
} 

[CreateAssetMenu(menuName = "Magic/Runes/Element")]
public class ElementRune : Rune
{
    public ElementType elementType;
}

[CreateAssetMenu(menuName = "Magic/Runes/Behavior")]
public class BehaviorRune : Rune
{
    public BehaviorType behaviorType;
}

[CreateAssetMenu(menuName = "Magic/Runes/Modifier")]
public class ModifierRune : Rune
{
    public ModifierType modifierType;
    public float modifierValue;
}