using UnityEngine;

public abstract class SpellRune : ScriptableObject
{
    [Header("Basic Info")]
    public Sprite runeIcon;
    public KeyCode inputKey;
    
    /*
    public bool IsCompatibleWith(SpellRune otherRune)
    {
        // 基本規則：元素符和行為符是基礎，修飾符和特性符是附加的
        if (runeType == RuneType.Element)
        {
            // 元素符只能與行為符組合
            return otherRune.runeType == RuneType.Behavior;
        }
        else if (runeType == RuneType.Behavior)
        {
            // 行為符可以與元素符、修飾符和特性符組合
            return otherRune.runeType == RuneType.Element || 
                   otherRune.runeType == RuneType.Modifier || 
                   otherRune.runeType == RuneType.Trait;
        }
        else if (runeType == RuneType.Modifier || runeType == RuneType.Trait)
        {
            // 修飾符和特性符只能與行為符組合
            return otherRune.runeType == RuneType.Behavior;
        }
        
        return false;
    }
    */
} 