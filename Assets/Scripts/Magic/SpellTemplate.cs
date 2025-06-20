using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Spell Template", menuName = "Magic/Spell Template")]
public class SpellTemplate : ScriptableObject
{
    [System.Serializable]
    public class RuneSequence
    {
        public List<ElementRune> elementRunes;    // 複數元素符
        public BehaviorRune behaviorRune;      // 單一行為符
        public List<ModifierRune> modifierRunes;   // 可選的複數修飾符
        public TraitRune traitRune;         // 可選的單一特徵符
        public string description;
        public GameObject spellEffectPrefab;
        
        [Header("Spell Properties")]
        public float baseDamage = 10f;
        public float baseDuration = 1f;
        public float baseRange = 5f;
        public float baseSpeed = 10f;
        public float baseSize = 1f;
        public float recoveryTime = 1f;  // 添加僵直時間
    }
    
    [Header("Basic Info")]
    public string spellName;
    public string description;
    public Sprite spellIcon;
    
    [Header("Valid Combinations")]
    public List<RuneSequence> validSequences = new List<RuneSequence>();
    
    public bool IsValidSequence(List<SpellRune> runes)
    {
        if (runes.Count < 2) return false; // 至少需要一個元素符和一個行為符
        
        // 檢查是否包含元素符和行為符
        List<ElementRune> elementRunes = new List<ElementRune>();
        BehaviorRune behaviorRune = null;
        
        foreach (var rune in runes)
        {
            if (rune is ElementRune)
            {
                    elementRunes.Add(rune as ElementRune);
            }
            else if (rune is BehaviorRune)
            {
                    if (behaviorRune != null) return false; // 不能有多個行為符
                    behaviorRune = rune as BehaviorRune;
            }
        }
        
        // 必須至少有一個元素符和一個行為符
        if (elementRunes.Count == 0 || behaviorRune == null) return false;
        
        // 檢查是否匹配任何有效序列
        foreach (var sequence in validSequences)
        {
            if (sequence.behaviorRune == behaviorRune)
            {
                // 檢查元素符是否匹配
                bool elementsMatch = CheckElementsMatch(elementRunes, sequence.elementRunes);
                if (elementsMatch)
                    return true;
            }
        }
        
        return false;
    }
    
    private bool CheckElementsMatch(List<ElementRune> inputElements, List<ElementRune> templateElements)
    {
        if (templateElements == null || templateElements.Count == 0)
            Debug.LogError("Template elements is null or empty");
            return false; // 模板必須定義至少一個元素符
            
        // 檢查每個元素符是否匹配
        /*
        foreach (var templateElement in templateElements)
        {
            bool found = false;
            foreach (var inputElement in inputElements)
            {
                if (inputElement == templateElement)
                {
                    found = true;
                    break;
                }
            }
            if (!found) return false;
        }
        */
        return inputElements == templateElements;
    }
    
    private bool CheckModifiersMatch(List<ModifierRune> inputModifiers, List<ModifierRune> templateModifiers)
    {
        if (templateModifiers == null || templateModifiers.Count == 0)
            return inputModifiers.Count == 0; // 如果模板不需要修飾符，輸入也不應該有
            
        if (inputModifiers.Count != templateModifiers.Count)
            return false;
            
        // 檢查每個修飾符是否匹配
        foreach (var templateModifier in templateModifiers)
        {
            bool found = false;
            foreach (var inputModifier in inputModifiers)
            {
                if (inputModifier == templateModifier)
                {
                    found = true;
                    break;
                }
            }
            if (!found) return false;
        }
        
        return true;
    }
    
    public RuneSequence GetMatchingSequence(List<SpellRune> runes)
    {
        foreach (var sequence in validSequences)
        {
            if (IsValidSequence(runes))
                return sequence;
        }
        return null;
    }
    
    /*
    public float CalculateSpellProperty(List<SpellRune> runes, ModifierType propertyType)
    {
        float baseValue = 1f;
        float totalModifier = 1f;
        
        // 找到匹配的序列
        var sequence = GetMatchingSequence(runes);
        if (sequence != null)
        {
            switch (propertyType)
            {
                case ModifierType.Power:
                    baseValue = sequence.baseDamage;
                    break;
                case ModifierType.Duration:
                    baseValue = sequence.baseDuration;
                    break;
                case ModifierType.Range:
                    baseValue = sequence.baseRange;
                    break;
                case ModifierType.Speed:
                    baseValue = sequence.baseSpeed;
                    break;
                case ModifierType.Size:
                    baseValue = sequence.baseSize;
                    break;
            }
        }
        
        // 應用所有修飾符的效果
        foreach (var rune in runes)
        {
            if (rune.runeType == RuneType.Modifier)
            {
                totalModifier *= rune.GetModifierValue(propertyType);
            }
        }
        
        return baseValue * totalModifier;
    }
    */
} 