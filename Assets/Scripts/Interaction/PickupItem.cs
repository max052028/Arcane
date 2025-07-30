using UnityEngine;

/// <summary>
/// 可拾取物品
/// </summary>
public class PickupItem : InteractableObject
{
    [Header("Pickup Settings")]
    [SerializeField] private string itemName = "Item";
    [SerializeField] private int quantity = 1;
    [SerializeField] private bool destroyOnPickup = true;
    [SerializeField] private GameObject pickupEffect;
    [SerializeField] private AudioClip pickupSound;
    
    private void Start()
    {
        // Set interaction settings
        SetInteractionName(itemName);
        SetInteractionPrompt($"按 F 拾取 {itemName}");
    }
    
    public override void Interact(GameObject player)
    {
        if (!CanInteract)
            return;
            
        // Try to add item to player inventory
        var inventory = player.GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            bool success = inventory.AddItem(itemName, quantity);
            if (success)
            {
                OnPickupSuccess(player);
            }
            else
            {
                OnPickupFailed(player);
            }
        }
        else
        {
            Debug.LogWarning("Player has no inventory component!");
            OnPickupSuccess(player); // Fallback
        }
        
        base.Interact(player);
    }
    
    private void OnPickupSuccess(GameObject player)
    {
        Debug.Log($"Picked up {quantity}x {itemName}");
        
        // Play pickup effect
        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, transform.rotation);
        }
        
        // Play pickup sound
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }
        
        // Destroy or disable object
        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
        else
        {
            SetCanInteract(false);
            gameObject.SetActive(false);
        }
    }
    
    private void OnPickupFailed(GameObject player)
    {
        Debug.Log($"Failed to pick up {itemName} - inventory full?");
        // Could show UI message here
    }
}

/// <summary>
/// 簡單的玩家庫存系統（示例）
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    [SerializeField] private int maxSlots = 20;
    
    private System.Collections.Generic.Dictionary<string, int> items = 
        new System.Collections.Generic.Dictionary<string, int>();
    
    /// <summary>
    /// 添加物品到庫存
    /// </summary>
    public bool AddItem(string itemName, int quantity)
    {
        if (items.ContainsKey(itemName))
        {
            items[itemName] += quantity;
        }
        else
        {
            if (items.Count >= maxSlots)
            {
                return false; // Inventory full
            }
            items[itemName] = quantity;
        }
        
        Debug.Log($"Added {quantity}x {itemName} to inventory");
        return true;
    }
    
    /// <summary>
    /// 獲取物品數量
    /// </summary>
    public int GetItemCount(string itemName)
    {
        return items.ContainsKey(itemName) ? items[itemName] : 0;
    }
    
    /// <summary>
    /// 移除物品
    /// </summary>
    public bool RemoveItem(string itemName, int quantity)
    {
        if (!items.ContainsKey(itemName) || items[itemName] < quantity)
        {
            return false;
        }
        
        items[itemName] -= quantity;
        if (items[itemName] <= 0)
        {
            items.Remove(itemName);
        }
        
        return true;
    }
}
