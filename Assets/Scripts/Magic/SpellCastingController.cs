using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;
using System;

public class SpellCastingController : MonoBehaviour
{
    [Header("Casting Settings")]
    [SerializeField] private float maxCastingTime = 10f;
    [SerializeField] private KeyCode castKey = KeyCode.R;
    [SerializeField] private Skill[] availableSpells;
    
    [Header("Rune Input")]
    [SerializeField] private Rune[] availableRunes;
    
    public UnityEvent<List<Rune>> onRuneSequenceChanged;
    public UnityEvent<Skill> onSpellCast;
    public UnityEvent onCastingStarted;
    public UnityEvent onCastingCancelled;
    public UnityEvent<CastingMode> onCastingModeStarted;
    public UnityEvent onCastingModeEnded;
    
    private List<Rune> currentRuneSequence = new List<Rune>();
    private List<ElementType> currentElements = new List<ElementType>();
    private List<BehaviorType> currentBehaviors = new List<BehaviorType>();
    private List<ModifierRune> currentModifierRunes = new List<ModifierRune>(); // 存儲修飾符符文
    private float castingStartTime;
    private bool isCasting;
    private bool isReadyToCast; // 符文輸入完成，準備施放
    private bool isHoldingCastKey; // 是否按住施法鍵
    private Skill pendingSpell; // 待施放的技能
    private float castKeyPressTime; // 按下施法鍵的時間
    private float holdThreshold = 0.2f; // 長按的時間閾值（秒）
    private bool isInHoldMode = false; // 是否已經進入長按模式
    private CameraController cameraController;
    private PlayerController playerController;
    
    [Header("Debug Info")]
    [SerializeField] private bool showDebugInfo = true;
    
    private void Start()
    {
        cameraController = FindObjectOfType<CameraController>();
        playerController = GetComponent<PlayerController>();
    }
    
    private void Update()
    {
        if (isCasting)
        {
            // Check for casting timeout
            if (Time.time - castingStartTime > maxCastingTime)
            {
                CancelCasting();
                return;
            }
            
            // Check for rune input
            foreach (var rune in availableRunes)
            {
                if (Input.GetKeyDown(rune.inputKey))
                {
                    Debug.Log($"Rune {rune.name} pressed");
                    AddRuneToSequence(rune);
                }
            }
            
            // Check for cast key press/release
            if (Input.GetKeyDown(castKey))
            {
                if (currentRuneSequence.Count > 0)
                {
                    // Record the time when cast key was pressed
                    castKeyPressTime = Time.time;
                    // Try to find matching spell and prepare for casting
                    PrepareSpellCasting();
                }
                else
                {
                    // No runes entered, cancel casting
                    CancelCasting();
                }
            }
            else if (Input.GetKey(castKey) && isHoldingCastKey && pendingSpell != null)
            {
                // Check if we've crossed the hold threshold
                float holdDuration = Time.time - castKeyPressTime;
                if (holdDuration >= holdThreshold)
                {
                    // We've held long enough, start the hold mode (only once)
                    if (!IsInHoldMode())
                    {
                        StartHoldMode(pendingSpell.castingMode);
                    }
                }
            }
            else if (Input.GetKeyUp(castKey) && isHoldingCastKey)
            {
                if (pendingSpell != null)
                {
                    // Determine if it was a quick tap or a hold
                    float holdDuration = Time.time - castKeyPressTime;
                    bool wasLongPress = holdDuration >= holdThreshold;
                    
                    // Execute spell based on hold duration
                    ExecuteSpell(wasLongPress);
                }
            }
        }
        else
        {
            // Start casting
            if (Input.GetKeyDown(castKey) && !playerController.IsRecovering())
            {
                StartCasting();
            }
        }
    }
    
    private void StartCasting()
    {
        Debug.Log("Starting casting");
        isCasting = true;
        castingStartTime = Time.time;
        isReadyToCast = false;
        isHoldingCastKey = false;
        isInHoldMode = false;
        pendingSpell = null;
        currentRuneSequence.Clear();
        onCastingStarted?.Invoke();
        
        // Start camera transition
        if (cameraController != null)
        {
            cameraController.StartSpellCasting();
        }
    }
    
    private void CancelCasting()
    {
        Debug.Log("Cancelling casting");
        isCasting = false;
        isReadyToCast = false;
        isHoldingCastKey = false;
        isInHoldMode = false;
        pendingSpell = null;
        currentRuneSequence.Clear();
        onCastingCancelled?.Invoke();
        
        // End any active casting mode
        if (cameraController != null)
        {
            cameraController.EndSpellCasting();
        }
        
        if (playerController != null)
        {
            playerController.DisableDirectionalCasting();
        }
        
        onCastingModeEnded?.Invoke();
    }
    
    private void AddRuneToSequence(Rune rune)
    {
        /*+
        // Check if the rune is compatible with the current sequence
        if (currentRuneSequence.Count > 0)
        {
            bool isCompatible = false;
            foreach (var existingRune in currentRuneSequence)
            {
                if (rune.IsCompatibleWith(existingRune))
                {
                    isCompatible = true;
                    break;
                }
            }
            
            if (!isCompatible)
            {
                Debug.Log($"Rune {rune.runeName} is not compatible with the current sequence!");
                return;
            }
        }
        */
        
        currentRuneSequence.Add(rune);
        onRuneSequenceChanged?.Invoke(currentRuneSequence);
    }
    
    private void PrepareSpellCasting()
    {
        if (currentRuneSequence.Count == 0)
        {
            CancelCasting();
            return;
        }

        // 驗證並拆分詠唱符文內容
        if (!AnalyzeRunes())
        {
            Debug.Log("Invalid rune sequence!");
            CancelCasting();
            return;
        }
        
        // Find a matching spell template
        Skill matchingSpell = null;
        foreach (var spell in availableSpells)
        {
            if (spell.elements.SequenceEqual(currentElements) && spell.behaviors.SequenceEqual(currentBehaviors))
            {
                matchingSpell = spell;
                break;
            }
        }
        
        if (matchingSpell != null)
        {
            // Check modifier compatibility (using optimized version)
            var incompatibleModifiers = matchingSpell.GetIncompatibleModifiers(currentModifierRunes);
            if (incompatibleModifiers.Count > 0)
            {
                Debug.LogWarning($"Spell '{matchingSpell.Name}' does not support the following modifiers: {string.Join(", ", incompatibleModifiers)}");
                Debug.Log($"Supported modifiers: {string.Join(", ", matchingSpell.acceptModifiers)}");
                // 可以選擇是否要取消詠唱，或者忽略不兼容的修飾符
                // 這裡我們選擇繼續，但會在施法時忽略不兼容的修飾符
            }
            
            pendingSpell = matchingSpell;
            isReadyToCast = true;
            isHoldingCastKey = true;
            
            // Start casting mode based on spell's casting mode
            StartCastingMode(matchingSpell.castingMode);
        }
        else
        {
            Debug.Log("Invalid rune sequence!");
            CancelCasting();
        }
    }
    
    private void StartCastingMode(CastingMode castingMode)
    {
        Debug.Log($"Starting casting mode: {castingMode}");
        onCastingModeStarted?.Invoke(castingMode);
        
        // Note: At this point we only prepare the casting mode
        // The actual behavior will be determined when ExecuteSpell is called
        // based on whether it was a quick tap or long hold
        
        switch (castingMode)
        {
            case CastingMode.Self:
                // Prepare for self casting (behavior decided on execution)
                break;
            case CastingMode.Directional:
                // Prepare for directional casting (behavior decided on execution)
                break;
            case CastingMode.TargetPoint:
                // TODO: Prepare for target point casting
                break;
            case CastingMode.LockOnEnemy:
                // TODO: Prepare for enemy targeting
                break;
            case CastingMode.LockOnFriendly:
                // TODO: Prepare for friendly targeting
                break;
        }
    }
    
    private void StartDirectionalCasting()
    {
        if (cameraController != null)
        {
            // Move camera to first-person position
            cameraController.StartDirectionalCasting();
        }
        
        // Enable mouse look for player rotation
        if (playerController != null)
        {
            playerController.EnableDirectionalCasting();
        }
    }
    
    private bool IsInHoldMode()
    {
        return isInHoldMode;
    }
    
    private void StartHoldMode(CastingMode castingMode)
    {
        isInHoldMode = true;
        Debug.Log($"Starting hold mode for: {castingMode}");
        
        switch (castingMode)
        {
            case CastingMode.Self:
                // TODO: 開始蓄力效果 - 可能顯示蓄力UI、粒子效果等
                Debug.Log("Starting Self hold mode - Charging effect");
                break;
                
            case CastingMode.Directional:
                // 開始第一人稱瞄準模式
                Debug.Log("Starting Directional hold mode - Entering FPP aiming");
                StartDirectionalCasting();
                break;
                
            case CastingMode.TargetPoint:
                // TODO: 開始瞄準模式
                Debug.Log("Starting TargetPoint hold mode - Entering targeting mode");
                break;
                
            case CastingMode.LockOnEnemy:
                // TODO: 開始持續鎖定模式
                Debug.Log("Starting LockOnEnemy hold mode - Sustained targeting");
                break;
                
            case CastingMode.LockOnFriendly:
                // TODO: 開始持續鎖定友方模式
                Debug.Log("Starting LockOnFriendly hold mode - Sustained friendly targeting");
                break;
        }
    }
    
    private void ExecuteSpell(bool wasLongPress)
    {
        if (pendingSpell == null)
        {
            CancelCasting();
            return;
        }
        
        Debug.Log($"Executing spell: {pendingSpell.Name}, Long press: {wasLongPress}");
        
        // Handle different casting behaviors based on casting mode and press type
        switch (pendingSpell.castingMode)
        {
            case CastingMode.Self:
                if (wasLongPress)
                {
                    // TODO: 長按自身施法 - 蓄力效果
                    Debug.Log("Long press Self casting - Charged effect");
                    CastSpell(pendingSpell);
                }
                else
                {
                    // TODO: 點按自身施法 - 快速施放
                    Debug.Log("Quick tap Self casting - Instant effect");
                    CastSpell(pendingSpell);
                }
                break;
                
            case CastingMode.Directional:
                if (wasLongPress)
                {
                    // 長按方向性施法 - 第一人稱瞄準模式已經在按住期間啟動了
                    Debug.Log("Long press Directional casting - Aimed shot (already in FPP)");
                    CastSpell(pendingSpell);
                }
                else
                {
                    // TODO: 點按方向性施法 - 快速朝前方施放
                    Debug.Log("Quick tap Directional casting - Quick shot");
                    CastSpell(pendingSpell);
                }
                break;
                
            case CastingMode.TargetPoint:
                if (wasLongPress)
                {
                    // TODO: 長按指定點施法 - 進入瞄準模式
                    Debug.Log("Long press TargetPoint casting - Aimed targeting");
                    CastSpell(pendingSpell);
                }
                else
                {
                    // TODO: 點按指定點施法 - 快速指定
                    Debug.Log("Quick tap TargetPoint casting - Quick targeting");
                    CastSpell(pendingSpell);
                }
                break;
                
            case CastingMode.LockOnEnemy:
                if (wasLongPress)
                {
                    // TODO: 長按鎖定敵人 - 持續鎖定模式
                    Debug.Log("Long press LockOnEnemy - Sustained lock");
                    CastSpell(pendingSpell);
                }
                else
                {
                    // TODO: 點按鎖定敵人 - 快速鎖定
                    Debug.Log("Quick tap LockOnEnemy - Quick lock");
                    CastSpell(pendingSpell);
                }
                break;
                
            case CastingMode.LockOnFriendly:
                if (wasLongPress)
                {
                    // TODO: 長按鎖定友方 - 持續鎖定模式
                    Debug.Log("Long press LockOnFriendly - Sustained lock");
                    CastSpell(pendingSpell);
                }
                else
                {
                    // TODO: 點按鎖定友方 - 快速鎖定
                    Debug.Log("Quick tap LockOnFriendly - Quick lock");
                    CastSpell(pendingSpell);
                }
                break;
        }
        
        // Reset casting state
        isCasting = false;
        isReadyToCast = false;
        isHoldingCastKey = false;
        isInHoldMode = false;
        pendingSpell = null;
        currentRuneSequence.Clear();
        
        // End casting mode
        if (cameraController != null)
        {
            cameraController.EndSpellCasting();
        }
        
        if (playerController != null)
        {
            playerController.DisableDirectionalCasting();
        }
        
        onCastingModeEnded?.Invoke();
    }
    
    private bool AnalyzeRunes()
    {
        if (!currentRuneSequence[0] is ElementRune || currentRuneSequence.Count < 2)
        {
            Debug.LogWarning("Invalid rune sequence: must start with an ElementRune and have at least 2 runes.");
            return false;
        }

        currentElements.Clear();
        currentBehaviors.Clear();
        currentModifierRunes.Clear();

        int dirtyFlag = 0;
        foreach (Rune rune in currentRuneSequence)
        {
            if (rune is ElementRune elementRune)
            {
                if (dirtyFlag > 1)
                {
                    Debug.LogWarning("Element runes must be placed before behavior and modifier runes.");
                    return false;
                }
                else
                {
                    currentElements.Add(elementRune.elementType);
                    dirtyFlag = 1; // Set dirty flag for elements
                }
            }
            else if (rune is BehaviorRune behaviorRune)
            {
                if (dirtyFlag > 2)
                {
                    Debug.LogWarning("Behavior runes must be placed before modifier runes.");
                    return false;
                }
                else
                {
                    currentBehaviors.Add(behaviorRune.behaviorType);
                    dirtyFlag = 2; // Set dirty flag for behaviors
                }
            }
            else if (rune is ModifierRune modifierRune)
            {
                if (dirtyFlag > 3)
                {
                    Debug.LogWarning("Modifier runes must be placed last.");
                    return false;
                }
                else
                {
                    currentModifierRunes.Add(modifierRune); // 只存儲修飾符符文
                    dirtyFlag = 3; // Set dirty flag for modifiers
                }
            }
        }

        if (currentElements.Count == 0 || currentBehaviors.Count == 0)
        {
            Debug.LogWarning("Rune sequence must contain at least one element and one behavior rune.");
            return false;
        }

        // Debug modifier information
        if (currentModifierRunes.Count > 0)
        {
            Debug.Log($"=== Applied Modifiers ===");
            foreach (var modifierRune in currentModifierRunes)
            {
                Debug.Log($"{modifierRune.modifierType}: {modifierRune.modifierValue:+0.0%} ({(1f + modifierRune.modifierValue):F2}x multiplier)");
            }
        }
        else
        {
            Debug.Log("No modifiers applied to this spell.");
        }

        return true;
    }
    
    private void CastSpell(Skill spell)
    {
        Debug.Log($"Casting spell: {spell.Name}");
        if (spell.prefab == null)
        {
            Debug.LogWarning($"No prefab found for spell {spell.Name}");
            return;
        }
        
        // Calculate all spell properties at once
        SpellProperties properties = spell.CalculateSpellProperties(currentModifierRunes);
        
        // Debug modifier effects
        Debug.Log($"=== Spell Properties ===");
        Debug.Log($"Power: {spell.basePower} -> {properties.power} (modifier: {(properties.power/spell.basePower):F2}x)");
        Debug.Log($"Duration: {spell.baseDuration} -> {properties.duration} (modifier: {(properties.duration/spell.baseDuration):F2}x)");
        Debug.Log($"Range: {spell.baseRange} -> {properties.range} (modifier: {(properties.range/spell.baseRange):F2}x)");
        Debug.Log($"Speed: {spell.baseSpeed} -> {properties.speed} (modifier: {(properties.speed/spell.baseSpeed):F2}x)");
        Debug.Log($"Size: {spell.baseSize} -> {properties.size} (modifier: {(properties.size/spell.baseSize):F2}x)");
        Debug.Log($"Allowed modifiers: {string.Join(", ", spell.acceptModifiers)}");
        
        // Calculate spawn position and rotation
        Vector3 spawnPosition = transform.position + transform.forward * 2f; // Spawn 2 units in front of the caster
        Quaternion spawnRotation = transform.rotation;
        
        // Instantiate spell effect
        GameObject spellEffect = Instantiate(spell.prefab, spawnPosition, spawnRotation);
        spellEffect.GetComponent<SpellEffect>().caster = gameObject; // Set the caster reference
        
        // Set spell properties on the effect
        SpellEffect spellEffectComponent = spellEffect.GetComponent<SpellEffect>();
        if (spellEffectComponent != null)
        {
            spellEffectComponent.Initialize(properties.power, properties.duration, properties.range, properties.speed, properties.size);
        }
        else
        {
            Debug.LogError($"SpellEffect component not found on prefab: {spell.prefab.name}");
        }

        // Start recovery time using the spell's recovery time
        
        onSpellCast?.Invoke(spell);
    }
    
    public List<Rune> GetCurrentRuneSequence()
    {
        return new List<Rune>(currentRuneSequence);
    }
    
    public bool IsCasting()
    {
        return isCasting;
    }
    
    public bool IsReadyToCast()
    {
        return isReadyToCast;
    }
    
    public bool IsHoldingCastKey()
    {
        return isHoldingCastKey;
    }
    
    public CastingMode GetCurrentCastingMode()
    {
        return pendingSpell?.castingMode ?? CastingMode.None;
    }
    
    public float GetCastingProgress()
    {
        if (!isCasting)
            return 0f;
            
        return Mathf.Clamp01((Time.time - castingStartTime) / maxCastingTime);
    }
} 