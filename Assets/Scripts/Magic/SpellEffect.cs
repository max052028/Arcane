using UnityEngine;
using System.Collections;

public class SpellEffect : MonoBehaviour
{
    [Header("Effect Settings")]
    public GameObject caster;
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private GameObject impactEffect;
    
    private float damage;
    private float duration;
    private float range;
    private float speed;
    private float size;
    
    private Vector3 startPosition;
    private bool hasHit;
    
    public void Initialize(float damage, float duration, float range, float speed, float size)
    {
        this.damage = damage;
        this.duration = duration;
        this.range = range;
        this.speed = speed;
        this.size = size;
        
        // Apply size modifier
        transform.localScale *= size;
        
        // Apply speed modifier
        moveSpeed *= speed;
        
        startPosition = transform.position;
        
        // Check renderer
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            if (!renderer.enabled)
            {
                renderer.enabled = true;
            }
        }
        
        // Start effect lifetime
        StartCoroutine(EffectLifetime());
    }
    
    private void Update()
    {
        if (hasHit) return;
        
        // Move forward
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        
        // Check if we've exceeded range
        if (Vector3.Distance(startPosition, transform.position) > range)
        {
            Destroy(gameObject);
            return;
        }
        
        // Check for collisions
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, 0.5f, transform.forward, 0.1f, targetLayers);
        foreach (RaycastHit hit in hits)
        {
            HandleHit(hit);
        }
    }
    
    private void HandleHit(RaycastHit hit)
    {
        hasHit = true;
        
        // Try to damage the hit object
        IDamageable damageable = hit.collider.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage, CombatSystem.AttackDirection.Horizontal);
        }
        
        // Spawn impact effect
        if (impactEffect != null)
        {
            Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
        }
        
        // Destroy the spell effect
        Destroy(gameObject);
    }
    
    private IEnumerator EffectLifetime()
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
} 