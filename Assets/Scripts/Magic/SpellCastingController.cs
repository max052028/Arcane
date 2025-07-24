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
    
    private List<Rune> currentRuneSequence = new List<Rune>();
    private List<ElementType> currentElements = new List<ElementType>();
    private List<BehaviorType> currentBehaviors = new List<BehaviorType>();
    private List<ModifierType> currentModifiers = new List<ModifierType>();
    private float castingStartTime;
    private bool isCasting;
    private CameraController cameraController;
    private PlayerController playerController;
    
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
            
            // Check for cast completion
            if (Input.GetKeyDown(castKey))
            {
                CompleteCasting();
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
        currentRuneSequence.Clear();
        onCastingCancelled?.Invoke();
        
        // End camera transition
        if (cameraController != null)
        {
            cameraController.EndSpellCasting();
        }
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
    
    private void CompleteCasting()
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
        // 顯示當前符文序列
        Debug.Log($"Casting spell with elements: {string.Join(", ", currentElements)} and behaviors: {string.Join(", ", currentBehaviors)}");
        
        // Find a matching spell template
        Skill matchingSpell = null;
        foreach (var spell in availableSpells)
        {
            // 顯示技能符文序列
            Debug.Log($"Checking spell: {spell.Name} with elements: {string.Join(", ", spell.elements)} and behaviors: {string.Join(", ", spell.behaviors)}");
            // 可以跟我解釋一下為什麼下面這行會不相等嗎?我印出來的結果看起來是一樣的
            // 謝謝
            if (spell.elements.SequenceEqual(currentElements) && spell.behaviors.SequenceEqual(currentBehaviors))
            {
                matchingSpell = spell;
                break;
            }
        }
        
        if (matchingSpell != null)
        {
            CastSpell(matchingSpell);
        }
        else
        {
            Debug.Log("Invalid rune sequence!");
        }
        
        isCasting = false;
        currentRuneSequence.Clear();
        
        // End camera transition
        if (cameraController != null)
        {
            cameraController.EndSpellCasting();
        }
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
        currentModifiers.Clear();

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
                    currentModifiers.Add(modifierRune.modifierType);
                    dirtyFlag = 3; // Set dirty flag for modifiers
                }
            }
        }

        if (currentElements.Count == 0 || currentBehaviors.Count == 0)
        {
            Debug.LogWarning("Rune sequence must contain at least one element and one behavior rune.");
            return false;
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
        

        /*
        // Calculate spell properties
        float damage = spell.CalculateSpellProperty(currentRuneSequence, ModifierType.Power);
        float duration = spell.CalculateSpellProperty(currentRuneSequence, ModifierType.Duration);
        float range = spell.CalculateSpellProperty(currentRuneSequence, ModifierType.Range);
        float speed = spell.CalculateSpellProperty(currentRuneSequence, ModifierType.Speed);
        float size = spell.CalculateSpellProperty(currentRuneSequence, ModifierType.Size);
        */
        
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
            spellEffectComponent.Initialize(10, 5, 100, 20, 1); // Example values, replace with actual calculations
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
    
    public float GetCastingProgress()
    {
        if (!isCasting)
            return 0f;
            
        return Mathf.Clamp01((Time.time - castingStartTime) / maxCastingTime);
    }
} 