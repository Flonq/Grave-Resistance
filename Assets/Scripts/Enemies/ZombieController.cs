using UnityEngine;
using UnityEngine.AI;

public class ZombieController : MonoBehaviour
{
    [Header("Zombie Stats")]
    public float health = 100f;
    public float walkSpeed = 2f;
    public float chaseSpeed = 4f;
    public float attackDamage = 20f;
    public float attackRange = 2f;
    public float detectionRange = 10f;
    
    [Header("Patrol Settings")]
    public float patrolRange = 5f;
    public float waitTime = 2f;
    
    // Components
    private NavMeshAgent agent;
    private Transform player;
    private Animator animator;
    
    // State Machine
    public enum ZombieState { Idle, Patrol, Chase, Attack }
    public ZombieState currentState = ZombieState.Idle;
    
    // Patrol variables
    private Vector3 startPosition;
    private Vector3 patrolTarget;
    private float waitTimer;
    
    // Combat variables
    private float lastAttackTime;
    private float attackCooldown = 1.5f;
    
    void Start()
    {
        // Get components
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        
        // Initialize
        startPosition = transform.position;
        agent.speed = walkSpeed;
        
        // Start in idle state
        ChangeState(ZombieState.Idle);
    }
    
    void Update()
    {
        if (health <= 0) return;
        
        // State machine
        switch (currentState)
        {
            case ZombieState.Idle:
                IdleState();
                break;
            case ZombieState.Patrol:
                PatrolState();
                break;
            case ZombieState.Chase:
                ChaseState();
                break;
            case ZombieState.Attack:
                AttackState();
                break;
        }
        
        // Check for player detection
        CheckPlayerDetection();
    }
    
    void IdleState()
    {
        waitTimer += Time.deltaTime;
        
        if (waitTimer >= waitTime)
        {
            ChangeState(ZombieState.Patrol);
        }
    }
    
    void PatrolState()
    {
        
        // If reached target, go back to Idle
        if (!agent.hasPath || agent.remainingDistance < 1.5f)
        {
            ChangeState(ZombieState.Idle);
        }
    }
    
    void ChaseState()
    {
        if (player == null) return;
        
        // Chase player
        agent.SetDestination(player.position);
        
        // Check if close enough to attack
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackRange)
        {
            ChangeState(ZombieState.Attack);
        }
        
        // If player too far, go back to patrol
        if (distanceToPlayer > detectionRange * 1.5f)
        {
            ChangeState(ZombieState.Patrol);
        }
    }
    
    void AttackState()
    {
        if (player == null) return;
        
        // Stop moving and face player
        agent.SetDestination(transform.position);
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(directionToPlayer);
        
        // Attack cooldown
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            PerformAttack();
            lastAttackTime = Time.time;
        }
        
        // Check if player moved away
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > attackRange * 1.2f)
        {
            ChangeState(ZombieState.Chase);
        }
    }
    
    void CheckPlayerDetection()
    {
        if (player == null || currentState == ZombieState.Attack) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Simple distance-based detection
        if (distanceToPlayer <= detectionRange)
        {
            ChangeState(ZombieState.Chase);
        }
    }
    
    void GetRandomPatrolPoint()
    {
        // Patrol range'i her seferinde bulunduğu yerden hesapla
        Vector3 randomDirection = Random.insideUnitSphere * patrolRange;
        randomDirection += transform.position; // Her seferinde mevcut pozisyon
        randomDirection.y = transform.position.y;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRange, 1))
        {
            patrolTarget = hit.position;
            agent.SetDestination(patrolTarget);
        }
    }
    
    void PerformAttack()
    {
        
        // Damage the player
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null) 
        {
            playerHealth.TakeDamage(attackDamage);
        }
    }
    
    void ChangeState(ZombieState newState)
    {
        currentState = newState;
        
        switch (newState)
        {
            case ZombieState.Idle:
                agent.SetDestination(transform.position);
                waitTimer = 0f; // Timer'ı sıfırla
                break;
            case ZombieState.Patrol:
                agent.speed = walkSpeed;
                GetRandomPatrolPoint(); // Yeni patrol noktası al
                break;
            case ZombieState.Chase:
                agent.speed = chaseSpeed;
                break;
            case ZombieState.Attack:
                agent.SetDestination(transform.position);
                break;
        }
    }
    
    public void TakeDamage(float damage)
    {
        health -= damage;
        
        if (health <= 0)
        {
            Die();
        }
    }
    
    void Die()
    {
        Debug.Log("=== ZOMBIE DIE() CALLED ===");
        
        // Notify GameManager for kill count
        if (GameManager.Instance != null)
        {
            Debug.Log("GameManager found, calling AddKill()");
            GameManager.Instance.AddKill();
        }
        else
        {
            Debug.LogError("GameManager.Instance is NULL!");
        }
        
        // Disable components
        agent.enabled = false;
        this.enabled = false;
        
        // TODO: Death animation, loot drop, etc.
    }
    
    // Debug visualization
    void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Attack range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Patrol range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(startPosition, patrolRange);
    }
} 