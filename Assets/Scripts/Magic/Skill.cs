using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using System;

[CreateAssetMenu(fileName = "New Skill", menuName = "Magic/Skill")]
public class Skill : ScriptableObject
{
    public string Id; // 唯一ID
    public string Name; // 技能名稱（可自動生成或玩家命名）
    public GameObject prefab; // 技能的實體預製件（可選）
    public List<ElementType> elements; 
    public List<BehaviorType> behaviors;
    public List<ModifierType> acceptModifiers;  

    public SkillEffect effect; // 技能的邏輯與實際效果
    public SkillCost cost; // 魔力、冷卻、詠唱時間等
    public SkillMeta meta; // 額外資訊：等級、創造者、圖示等
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
