using UnityEngine;

public enum RuneType
{
    Element,    // 元素符
    Behavior,   // 行為符
    Modifier,   // 數值符
    Trait       // 特性符
}

public enum ElementType
{
    Fire,
    Water,
    Wind,
    Earth,
    Wood,
    Ice,
    Lightning,
    Poison,
    Dark,
    Holy,
    Chrono
}

public enum BehaviorType
{
    Projectile,     // 發射
    Explosion,      // 爆炸
    Create,         // 創造
    Sustain,        // 持續
    Summon,         // 召喚
    Transform       // 變形
}

public enum ModifierType
{
    Power,          // 強度
    Duration,       // 持續時間
    Range,          // 範圍
    Speed,          // 速度
    Size            // 大小
}

public enum TraitType
{
    Tension,        // 緊繃
    Lightweight,    // 輕量化
    Piercing,       // 穿透
    Homing,         // 追蹤
    Chain           // 連鎖
} 