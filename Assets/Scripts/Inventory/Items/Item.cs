using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Item : ScriptableObject
{
    [Header("Item Details")]
    public int ID; // Unique identifier for the item
    public string description;
    public Sprite icon;
    [Range(1, 5)] public int rarity; // Rarity level of the item (1-5, where 1 is common and 5 is legendary)
    public ItemType itemType; // Item type defined by the ItemType enum

    [Header("Item Properties")]
    public int maxStackSize = 1; // Maximum number of items in a stack
    public bool isUsable = false; // 是否可用
}

/*
 * ItemType 枚举定义了不同类型的物品。
 * Consumable: 可消耗物品，如药水、食物等。
 * Equipment: 装备物品，如武器、防具等。
 * Material: 材料物品，用于制作或升级其他物品。
 * QuestItem: 任务物品，通常与游戏中的任务相关。
 * Miscellaneous: 杂项物品，其他不属于上述类别的物品。
 */

[CreateAssetMenu(menuName = "Items/Consumable")]
public class Consumable : Item
{
    public int healthRestored; // 恢复的生命值
    public int manaRestored; // 恢复的法力值

    public void Use()
    {
        // 使用物品的逻辑
        Debug.Log($"Using consumable item: {name}");
    }
}

[CreateAssetMenu(menuName = "Items/Equipment")]
public class Equipment : Item
{
    public int attackPower; // 攻击力
    public int defensePower; // 防御力

    public void Equip()
    {
        // 装备物品的逻辑
        Debug.Log($"Equipping item: {name}");
    }
}

[CreateAssetMenu(menuName = "Items/Material")]
public class Material : Item
{
    public string craftingRecipe; // 制作配方

    public void UseForCrafting()
    {
        // 使用物品进行制作的逻辑
        Debug.Log($"Using material item for crafting: {name}");
    }
}

[CreateAssetMenu(menuName = "Items/QuestItem")]
public class QuestItem : Item
{
    public string questDescription; // 任务描述

    public void UseForQuest()
    {
        // 使用物品进行任务的逻辑
        Debug.Log($"Using quest item: {name}");
    }
}

[CreateAssetMenu(menuName = "Items/Miscellaneous")]
public class Miscellaneous : Item
{
    public string additionalInfo; // 其他信息
}

#region Enums
public enum ItemType
{
    Consumable,
    Equipment,
    Material,
    QuestItem,
    Miscellaneous
}


#endregion