using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("Basic Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackRecoveryTime = 0.5f;  // 攻擊後的僵直時間
    
    [Header("Combat Settings")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRadius = 1.5f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private CombatSystem.AttackDirection preferredAttackDirection = CombatSystem.AttackDirection.Horizontal;
    
    private NavMeshAgent agent;
    private Transform player;
    private Animator animator;
    private bool isAttacking = false;
    private bool isRecovering = false;
    private float lastAttackTime;
    private float recoveryEndTime;
    private float currentHealth;
    private bool isDead;
    
    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        currentHealth = maxHealth;
    }
    
    private void Update()
    {
        if (isDead) return;
        if (isRecovering)
        {
            if (Time.time >= recoveryEndTime)
            {
                isRecovering = false;
            }
            return;
        }
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= detectionRange)
        {
            if (distanceToPlayer <= attackRange && !isAttacking && Time.time >= lastAttackTime + attackCooldown)
            {
                StartAttack();
            }
            else if (!isAttacking)
            {
                ChasePlayer();
            }
        }
        else
        {
            StopChasing();
        }
    }
    
    private void StartAttack()
    {
        isAttacking = true;
        agent.isStopped = true;
        
        // 播放攻擊動畫
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        
        // 開始攻擊判定
        StartCoroutine(PerformAttack());
    }
    
    private IEnumerator PerformAttack()
    {
        // 等待動畫播放到攻擊判定點
        yield return new WaitForSeconds(0.5f);
        
        // 檢查攻擊範圍內的玩家
        Collider[] hitColliders = Physics.OverlapSphere(transform.position + transform.forward * attackRadius, attackRadius, playerLayer);
        foreach (var hitCollider in hitColliders)
        {
            PlayerHealth playerHealth = hitCollider.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage, preferredAttackDirection);
            }
        }
        
        // 結束攻擊
        isAttacking = false;
        lastAttackTime = Time.time;
        
        // 開始僵直
        StartRecovery();
    }
    
    private void StartRecovery()
    {
        isRecovering = true;
        recoveryEndTime = Time.time + attackRecoveryTime;
    }
    
    private void ChasePlayer()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
        
        // 更新動畫
        if (animator != null)
        {
            animator.SetBool("IsMoving", true);
        }
    }
    
    private void StopChasing()
    {
        agent.isStopped = true;
        
        // 更新動畫
        if (animator != null)
        {
            animator.SetBool("IsMoving", false);
        }
    }
    
    public void TakeDamage(float damage, CombatSystem.AttackDirection attackDirection)
    {
        if (isDead) return;
        
        // Check if attack direction matches enemy's preferred direction
        float damageMultiplier = (attackDirection == preferredAttackDirection) ? 1.5f : 1f;
        
        currentHealth -= damage * damageMultiplier;
        Debug.Log("Enemy took damage: " + damage * damageMultiplier);

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        isDead = true;
        agent.enabled = false;
        
        // Disable collider and other components
        Collider enemyCollider = GetComponent<Collider>();
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }
        
        // TODO: Play death animation
        // TODO: Drop loot
        // TODO: Add death effects
        
        // Destroy after delay
        Destroy(gameObject, 3f);
    }
    
    private void OnDrawGizmosSelected()
    {
        // 繪製檢測範圍
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // 繪製攻擊範圍
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // 繪製攻擊判定範圍
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position + transform.forward * attackRadius, attackRadius);
    }
} 