using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float invincibilityDuration = 0.5f;
    
    public UnityEvent<float> onHealthChanged;
    public UnityEvent onPlayerDeath;
    
    private float _currentHealth;
    private float currentHealth
    {
        get => _currentHealth;
        set
        {
            _currentHealth = value;
            onHealthChanged?.Invoke(_currentHealth / maxHealth);
        }
    }
    private float lastDamageTime;
    private bool isInvincible;
    
    private void Start()
    {
        currentHealth = maxHealth;
        onHealthChanged?.Invoke(currentHealth / maxHealth);
    }
    
    public void TakeDamage(float damage, CombatSystem.AttackDirection attackDirection)
    {
        if (isInvincible) return;
        
        currentHealth -= damage;
        lastDamageTime = Time.time;
        isInvincible = true;
        Debug.Log("Player took damage: " + damage);
        
        onHealthChanged?.Invoke(currentHealth / maxHealth);
        
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Reset invincibility after duration
            Invoke(nameof(ResetInvincibility), invincibilityDuration);
        }
    }
    
    private void ResetInvincibility()
    {
        isInvincible = false;
    }
    
    private void Die()
    {
        onPlayerDeath?.Invoke();
        
        // Disable player controls
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // TODO: Play death animation
        // TODO: Show game over screen
        // TODO: Add death effects
        
        Debug.Log("Player died!");
    }
    
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
} 