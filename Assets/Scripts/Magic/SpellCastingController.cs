using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using System;

public class SpellCastingController : MonoBehaviour
{
    [Header("Casting Settings")]
    [SerializeField] private float maxCastingTime = 10f;
    [SerializeField] private KeyCode castKey = KeyCode.R;
    [SerializeField] private SpellTemplate[] availableSpells;
    
    [Header("Rune Input")]
    [SerializeField] private SpellRune[] availableRunes;
    
    public UnityEvent<List<SpellRune>> onRuneSequenceChanged;
    public UnityEvent<SpellTemplate> onSpellCast;
    public UnityEvent onCastingStarted;
    public UnityEvent onCastingCancelled;
    
    private List<SpellRune> currentRuneSequence = new List<SpellRune>();
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
    
    private void AddRuneToSequence(SpellRune rune)
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
        
        // Find a matching spell template
        SpellTemplate matchingSpell = null;
        foreach (var spell in availableSpells)
        {
            if (spell.IsValidSequence(currentRuneSequence))
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
    
    private void CastSpell(SpellTemplate spell)
    {
        Debug.Log($"Casting spell: {spell.spellName}");
        // Get the matching sequence
        var sequence = spell.GetMatchingSequence(currentRuneSequence);
        if (sequence == null || sequence.spellEffectPrefab == null)
        {
            Debug.LogWarning($"No valid sequence or effect prefab found for spell {spell.spellName}");
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
        GameObject spellEffect = Instantiate(sequence.spellEffectPrefab, spawnPosition, spawnRotation);
        spellEffect.GetComponent<SpellEffect>().caster = gameObject; // Set the caster reference
        
        /*
        // Set spell properties on the effect
        SpellEffect spellEffectComponent = spellEffect.GetComponent<SpellEffect>();
        if (spellEffectComponent != null)
        {
            spellEffectComponent.Initialize(damage, duration, range, speed, size);
        }
        else
        {
            Debug.LogError($"SpellEffect component not found on prefab: {sequence.spellEffectPrefab.name}");
        }
        */

        // Start recovery time using the spell's recovery time
        if (playerController != null)
        {
            playerController.StartRecovery(sequence.recoveryTime);
        }
        
        onSpellCast?.Invoke(spell);
    }
    
    public List<SpellRune> GetCurrentRuneSequence()
    {
        return new List<SpellRune>(currentRuneSequence);
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