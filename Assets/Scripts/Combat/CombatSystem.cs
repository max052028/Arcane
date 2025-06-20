using UnityEngine;
using System;

public class CombatSystem : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private LayerMask enemyLayer;
    
    [Header("Block Settings")]
    [SerializeField] private float blockAngle = 45f;
    [SerializeField] private float blockStaminaCost = 15f;
    
    public enum AttackDirection
    {
        Horizontal,
        Vertical
    }
    
    private AttackDirection currentAttackDirection = AttackDirection.Horizontal;
    private float lastAttackTime;
    private StaminaSystem staminaSystem;
    
    public event Action<AttackDirection> onAttackDirectionChanged;
    public event Action onAttackPerformed;
    public event Action onBlockPerformed;
    
    private void Start()
    {
        staminaSystem = GetComponent<StaminaSystem>();
        if (staminaSystem == null)
        {
            Debug.LogWarning("StaminaSystem not found on the same GameObject!");
        }
    }
    
    private void Update()
    {
        HandleAttackInput();
        HandleBlockInput();
    }
    
    private void HandleAttackInput()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= lastAttackTime + attackCooldown)
        {
            PerformAttack();
        }
        
        // Switch attack direction with Q key
        if (Input.GetKeyDown(KeyCode.Q))
        {
            currentAttackDirection = currentAttackDirection == AttackDirection.Horizontal 
                ? AttackDirection.Vertical 
                : AttackDirection.Horizontal;
            onAttackDirectionChanged?.Invoke(currentAttackDirection);
        }
    }
    
    private void HandleBlockInput()
    {
        if (Input.GetMouseButton(1)) // Right mouse button for blocking
        {
            if (staminaSystem != null && staminaSystem.CanUseStamina(blockStaminaCost))
            {
                staminaSystem.UseBlockStamina();
                onBlockPerformed?.Invoke();
            }
        }
    }
    
    private void PerformAttack()
    {
        lastAttackTime = Time.time;
        
        // Perform raycast to detect enemies
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, 0.5f, transform.forward, attackRange, enemyLayer);
        
        foreach (RaycastHit hit in hits)
        {
            // Check if the enemy has a component that can receive damage
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // Calculate damage based on attack direction and other factors
                float damage = CalculateDamage();
                damageable.TakeDamage(damage, currentAttackDirection);
            }
        }
        
        onAttackPerformed?.Invoke();
    }
    
    private float CalculateDamage()
    {
        // Base damage calculation
        float baseDamage = 10f;
        
        // Add modifiers based on attack direction
        switch (currentAttackDirection)
        {
            case AttackDirection.Horizontal:
                baseDamage *= 1.2f; // Horizontal attacks deal more damage
                break;
            case AttackDirection.Vertical:
                baseDamage *= 0.8f; // Vertical attacks deal less damage but have other benefits
                break;
        }
        
        return baseDamage;
    }
    
    public bool IsBlocking()
    {
        return Input.GetMouseButton(1);
    }
    
    public AttackDirection GetCurrentAttackDirection()
    {
        return currentAttackDirection;
    }
}

// Interface for objects that can take damage
public interface IDamageable
{
    void TakeDamage(float damage, CombatSystem.AttackDirection attackDirection);
} 