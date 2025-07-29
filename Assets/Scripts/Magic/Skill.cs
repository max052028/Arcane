using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using System;

[System.Serializable]
public struct SpellProperties
{
    public float power;
    public float duration;
    public float range;
    public float speed;
    public float size;
}

[CreateAssetMenu(fileName = "New Skill", menuName = "Magic/Skill")]
public class Skill : ScriptableObject
{
    public string Id; // 唯一ID
    public string Name; // 技能名稱（可自動生成或玩家命名）
    public CastingMode castingMode; // 詠唱模式：自我、方向性、指定點、鎖定敵人或友方
    public GameObject prefab; // 技能的實體預製件（可選）
    public List<ElementType> elements; 
    public List<BehaviorType> behaviors;
    public List<ModifierType> acceptModifiers;  

    public SkillEffect effect; // 技能的邏輯與實際效果
    public SkillCost cost; // 魔力、冷卻、詠唱時間等
    public SkillMeta meta; // 額外資訊：等級、創造者、圖示等

    [Header("Base Properties")]
    public float basePower = 10f;
    public float baseDuration = 5f;
    public float baseRange = 100f;
    public float baseSpeed = 20f;
    public float baseSize = 1f;

    /// <summary>
    /// 計算套用修飾符後的技能屬性值
    /// </summary>
    /// <param name="runeSequence">當前的符文序列</param>
    /// <param name="property">要計算的屬性類型</param>
    /// <returns>計算後的屬性值</returns>
    public float CalculateSpellProperty(List<Rune> runeSequence, ModifierType property)
    {
        // 檢查此技能是否允許該修飾符
        if (!acceptModifiers.Contains(property))
        {
            // 如果不支援該修飾符，返回基礎值
            return GetBaseValue(property);
        }

        float baseValue = GetBaseValue(property);
        float finalValue = baseValue;

        // 遍歷符文序列中的修飾符
        foreach (var rune in runeSequence)
        {
            if (rune is ModifierRune modifierRune && modifierRune.modifierType == property)
            {
                // 套用修飾符效果
                finalValue *= (1f + modifierRune.modifierValue);
            }
        }

        return finalValue;
    }

    /// <summary>
    /// 計算套用修飾符後的所有技能屬性值（優化版本）
    /// </summary>
    /// <param name="modifierRunes">修飾符符文列表</param>
    /// <returns>包含所有屬性的結構</returns>
    public SpellProperties CalculateSpellProperties(List<ModifierRune> modifierRunes)
    {
        var properties = new SpellProperties
        {
            power = basePower,
            duration = baseDuration,
            range = baseRange,
            speed = baseSpeed,
            size = baseSize
        };

        // 一次性計算所有修飾符效果
        foreach (var modifierRune in modifierRunes)
        {
            // 檢查此技能是否支援該修飾符
            if (!acceptModifiers.Contains(modifierRune.modifierType))
                continue;

            float multiplier = 1f + modifierRune.modifierValue;
            
            switch (modifierRune.modifierType)
            {
                case ModifierType.Power:
                    properties.power *= multiplier;
                    break;
                case ModifierType.Duration:
                    properties.duration *= multiplier;
                    break;
                case ModifierType.Range:
                    properties.range *= multiplier;
                    break;
                case ModifierType.Speed:
                    properties.speed *= multiplier;
                    break;
                case ModifierType.Size:
                    properties.size *= multiplier;
                    break;
            }
        }

        return properties;
    }

    /// <summary>
    /// 獲取指定屬性的基礎值
    /// </summary>
    private float GetBaseValue(ModifierType property)
    {
        switch (property)
        {
            case ModifierType.Power:
                return basePower;
            case ModifierType.Duration:
                return baseDuration;
            case ModifierType.Range:
                return baseRange;
            case ModifierType.Speed:
                return baseSpeed;
            case ModifierType.Size:
                return baseSize;
            default:
                return 1f;
        }
    }

    /// <summary>
    /// 檢查修飾符是否與此技能兼容
    /// </summary>
    /// <param name="modifierType">要檢查的修飾符類型</param>
    /// <returns>如果兼容返回 true，否則返回 false</returns>
    public bool IsModifierCompatible(ModifierType modifierType)
    {
        return acceptModifiers.Contains(modifierType);
    }

    /// <summary>
    /// 獲取所有不兼容的修飾符
    /// </summary>
    /// <param name="runeSequence">當前的符文序列</param>
    /// <returns>不兼容的修飾符列表</returns>
    public List<ModifierType> GetIncompatibleModifiers(List<Rune> runeSequence)
    {
        var incompatibleModifiers = new List<ModifierType>();
        
        foreach (var rune in runeSequence)
        {
            if (rune is ModifierRune modifierRune)
            {
                if (!IsModifierCompatible(modifierRune.modifierType))
                {
                    incompatibleModifiers.Add(modifierRune.modifierType);
                }
            }
        }
        
        return incompatibleModifiers;
    }

    /// <summary>
    /// 獲取所有不兼容的修飾符（優化版本）
    /// </summary>
    /// <param name="modifierRunes">修飾符符文列表</param>
    /// <returns>不兼容的修飾符列表</returns>
    public List<ModifierType> GetIncompatibleModifiers(List<ModifierRune> modifierRunes)
    {
        var incompatibleModifiers = new List<ModifierType>();
        
        foreach (var modifierRune in modifierRunes)
        {
            if (!IsModifierCompatible(modifierRune.modifierType))
            {
                incompatibleModifiers.Add(modifierRune.modifierType);
            }
        }
        
        return incompatibleModifiers;
    }
} 

public class SkillEffect
{
    public ElementType element; // 最終判定的元素屬性
    public BehaviorType behavior; // 發射、創造、附加等
    public List<ModifierType> modifiers; // 多重、加速、增強等

    // public TargetingMode targeting; // 鎖定、指定點、範圍等
    // public AreaShape shape; // 點、扇形、線、環狀等
    public float power; // 傷害或效果強度
    public float duration; // 效果持續時間
}

public class SkillCost
{
    public int manaCost;
    public float castTime;
    public float cooldown;
    public float gailureChance; // 由詠唱長度等推算
}

public class SkillMeta
{
    public string createdBy; // 玩家或NPC名稱
    public Sprite icon;
    public int level;
    public string description;
    public DateTime createdAt;
}

public enum CastingMode
{
    None,
    Self,
    Directional,
    TargetPoint,
    LockOnEnemy,
    LockOnFriendly
}