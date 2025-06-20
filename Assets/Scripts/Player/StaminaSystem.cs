using UnityEngine;
using UnityEngine.Events;

public class StaminaSystem : Singleton<StaminaSystem>
{
    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenRate = 10f;
    [SerializeField] private float staminaRegenDelay = 1f;
    
    [Header("Stamina Costs")]
    [SerializeField] private float sprintCost = 10f;
    [SerializeField] private float jumpCost = 5f;
    [SerializeField] private float blockCost = 15f;
    
    public UnityEvent<float> onStaminaChanged;
    public UnityEvent onStaminaDepleted;

    private float _currentStamina;
    private float currentStamina
    {
        get => _currentStamina;
        set
        {
            _currentStamina = value;
            onStaminaChanged?.Invoke(_currentStamina / maxStamina);
        }
    }
    private float lastStaminaUseTime;
    private bool isRegenerating = true;
    
    private void Start()
    {
        currentStamina = maxStamina;
    }

    private void Update()
    {
        if (isRegenerating)
        {
            RegenerateStamina();
        }
        else if (Time.time >= lastStaminaUseTime + staminaRegenDelay)
        {
            isRegenerating = true;
        }
    }
    
    public bool UseStamina(float amount)
    {
        if (currentStamina >= amount)
        {
            currentStamina -= amount;
            lastStaminaUseTime = Time.time;
            isRegenerating = false;
            
            if (currentStamina <= 0)
            {
                onStaminaDepleted?.Invoke();
            }
            
            return true;
        }
        return false;
    }
    
    private void RegenerateStamina()
    {
        if (currentStamina < maxStamina)
        {
            currentStamina = Mathf.Min(currentStamina + staminaRegenRate * Time.deltaTime, maxStamina);
        }
        else
        {
            isRegenerating = false;
        }
    }
    
    public float GetStaminaPercentage()
    {
        return currentStamina / maxStamina;
    }
    
    public bool CanUseStamina(float amount)
    {
        return currentStamina >= amount;
    }
    
    // Specific stamina cost methods
    public bool UseSprintStamina()
    {
        return UseStamina(sprintCost * Time.deltaTime);
    }
    
    public bool UseJumpStamina()
    {
        return UseStamina(jumpCost);
    }
    
    public bool UseBlockStamina()
    {
        return UseStamina(blockCost);
    }
} 