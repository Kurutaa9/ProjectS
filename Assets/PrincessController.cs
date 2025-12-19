using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class PrincessController : MonoBehaviour
{
    public enum BossState { Idle, Chase, Attack, Recover, Enraged, Dead, Retreat }
    private BossState currentState;

    [Header("References")]
    public Transform player;
    private NavMeshAgent agent;
    private Animator anim;

    [Header("Boss Settings")]
    public float maxHealth = 1000f;
    private float currentHealth;
    public float chaseRange = 30f;       // detection range
    public float attackRange = 20f;      // preferred range to attack from (ranged)
    public float attackCooldown = 3f;    // 3 second idle between attacks
    private float lastAttackTime;

    [Header("Retreat Settings")]
    public float retreatRange = 8f;      // if player closer than this, retreat
    public float safeDistance = 15f;     // retreat until this far from player
    public float retreatSpeed = 5f;      // movement speed while retreating

    [Header("Rotation")]
    public float attackTurnSpeed = 8f; // rotation speed while attacking

    [Header("Enrage Settings")]
    public float enrageThreshold = 0.5f; // 50% HP
    private bool isEnraged = false;

    [Tooltip("Extra distance required to resume chase after being in attack range")]
    public float attackHysteresis = 0.5f;



    [HideInInspector] public bool isChasing; // public flag for UI

    public class AttackOption
    {
        public string name;      // for debugging
        public string trigger;   // Animator trigger to fire
    }

    // Fill this in the Inspector with as many attacks as you want
    public AttackOption[] meleeAttacks = new AttackOption[]
    {
        new AttackOption { name = "Attack 2",  trigger = "AttackLight" },
        new AttackOption { name = "360",  trigger = "AttackHeavy" },
        new AttackOption { name = "Combo", trigger = "AttackSpecial" },
    };

    private bool isPerformingAttack = false;
    private bool isInitialized = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        currentHealth = maxHealth;
        ChangeState(BossState.Idle);
        
        StartCoroutine(InitializeNavMeshAgent());
    }

    private IEnumerator InitializeNavMeshAgent()
    {
        // Wait a frame for NavMesh to be ready
        yield return null;

        if (agent == null)
        {
            Debug.LogError("Princess has no NavMeshAgent!");
            yield break;
        }

        // Disable then re-enable to force refresh
        agent.enabled = false;
        yield return null;
        agent.enabled = true;
        yield return null;

        // If still not on NavMesh, try to warp
        if (!agent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                agent.enabled = false;
                yield return null;
                agent.enabled = true;
                yield return null;
                
                if (!agent.isOnNavMesh)
                {
                    agent.Warp(hit.position);
                }
                Debug.Log($"Princess warped to NavMesh at {hit.position}");
            }
            else
            {
                Debug.LogError("Princess could not find nearby NavMesh surface within 10 units!");
            }
        }

        if (agent.isOnNavMesh)
        {
            agent.stoppingDistance = Mathf.Max(0f, attackRange);
            isInitialized = true;
            Debug.Log("Princess NavMeshAgent initialized successfully!");
        }
    }

    void Update()
    {
        // Wait for initialization
        if (!isInitialized)
            return;

        // Safety check
        if (agent == null || !agent.isOnNavMesh)
            return;

        switch (currentState)
        {
            case BossState.Idle:
                HandleIdle();
                break;

            case BossState.Chase:
                HandleChase();
                break;

            case BossState.Attack:
                HandleAttack();
                break;
            
            case BossState.Recover:
                HandleRecover();
                break;

            case BossState.Enraged:
                HandleEnraged();
                break;

            case BossState.Retreat:
                HandleRetreat();
                break;

            case BossState.Dead:
                break;
        }

        // Check enrage condition
        if (!isEnraged && currentHealth <= maxHealth * enrageThreshold)
        {
            EnterEnrageMode();
        }
    }

    // ------------------- STATE HANDLERS -------------------

    void HandleIdle()
    {
        anim.SetBool("isMoving", false);
        anim.SetBool("isRetreating", false);
        if (Vector3.Distance(transform.position, player.position) < chaseRange)
        {
            ChangeState(BossState.Chase);
        }
    }

    void HandleChase()
    {
        isChasing = true;
        float dist = Vector3.Distance(transform.position, player.position);

        Debug.Log($"Chase: dist={dist:F1}, attackRange={attackRange}, retreatRange={retreatRange}, speed={agent.speed}, stopped={agent.isStopped}, hasPath={agent.hasPath}, pathPending={agent.pathPending}");

        // If player is too close, retreat first
        if (dist < retreatRange)
        {
            ChangeState(BossState.Retreat);
            return;
        }

        // If within attack range -> attack
        if (dist <= attackRange)
        {
            anim.SetBool("isMoving", false);
            anim.SetBool("isRetreating", false);
            ChangeState(BossState.Attack);
            return;
        }

        // Otherwise, move closer to get in attack range
        anim.SetBool("isMoving", true);
        anim.SetBool("isRetreating", false);
        if (agent != null && agent.isOnNavMesh)
        {
            agent.updateRotation = true; // Re-enable rotation for normal movement
            agent.isStopped = false;
            bool pathSet = agent.SetDestination(player.position);
            Debug.Log($"SetDestination result: {pathSet}, playerPos={player.position}");
        }
    }

    void HandleAttack()
    {
        // Stop moving while attacking
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.enabled = false; // you’re already doing this
        }

        FacePlayer();
        anim.SetBool("isMoving", false);
        anim.SetBool("isRetreating", false);

        // If already mid‑attack animation, just let it play
        if (isPerformingAttack)
            return;

        // Only start a melee attack when in range
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > attackRange)
        {
            // out of range → go back to chase
            ChangeState(BossState.Chase);
            return;
        }

        // Pick a random attack and fire its trigger
        AttackOption opt = PickRandomMeleeAttack();
        if (opt == null)
        {
            // fallback to your original single attack
            anim.SetTrigger("AttackLight");
        }
        else
        {
            ResetMeleeAttackTriggers();
            anim.SetTrigger(opt.trigger);
        }

        isPerformingAttack = true;
        // Stay in Attack state until animation finishes (AE_AttackEnd will handle transition)
    }

    void HandleRecover()
    {
        float dist = Vector3.Distance(transform.position, player.position);
        agent.enabled = true;

        // If player got too close during recovery, retreat immediately
        if (dist < retreatRange)
        {
            ChangeState(BossState.Retreat);
            return;
        }

        // Hold position and keep facing player during cooldown
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
        anim.SetBool("isMoving", false);
        FacePlayer();

        // Wait for cooldown
        if (Time.time - lastAttackTime < attackCooldown)
        {
            return; // still waiting
        }

        // Cooldown done - decide next action
        if (dist <= attackRange)
        {
            // In range, attack again
            ChangeState(BossState.Attack);
        }
        else
        {
            // Out of range, chase to get closer
            ChangeState(BossState.Chase);
        }
    }

    void HandleRetreat()
    {
        float dist = Vector3.Distance(transform.position, player.position);

        // If we've retreated far enough, stop and attack
        if (dist >= safeDistance)
        {
            anim.SetBool("isRetreating", false);
            if (agent != null && agent.isOnNavMesh)
            {
                agent.updateRotation = true;
                agent.velocity = Vector3.zero; // Stop movement
                agent.ResetPath();
                agent.isStopped = false; // Allow agent to be used again
            }
            ChangeState(BossState.Chase);
            return;
        }

        // Otherwise, keep retreating (move away from player)
        anim.SetBool("isRetreating", true);
        if (agent != null && agent.isOnNavMesh)
        {
            agent.updateRotation = false; // Let us control facing
            agent.isStopped = false;
            agent.speed = retreatSpeed;

            // Calculate retreat direction (opposite of player direction)
            Vector3 retreatDir = (transform.position - player.position).normalized;
            
            // Move directly away from player using agent.Move
            Vector3 moveAmount = retreatDir * retreatSpeed * Time.deltaTime;
            agent.Move(moveAmount);
        }

        // Face player while retreating
        FacePlayer();
    }

    void HandleEnraged()
    {
        // Enraged state = faster movement + shorter cooldown
        agent.speed = 6f;
        attackCooldown = 1f;

        if (Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            FacePlayer(); // keep turning toward player in enraged melee
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                anim.SetTrigger("EnrageAttack"); // Use stronger attack
                lastAttackTime = Time.time;
            }
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            anim.SetBool("isMoving", true);
        }
    }

    // ------------------- STATE CHANGES -------------------

    void ChangeState(BossState newState)
    {
        currentState = newState;
    }

    void EnterEnrageMode()
    {
        isEnraged = true;
        ChangeState(BossState.Enraged);
        anim.SetTrigger("Enrage");
    }

    // ------------------- DAMAGE SYSTEM -------------------

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0 && currentState != BossState.Dead)
        {
            Die();
        }
    }

    void Die()
    {
        ChangeState(BossState.Dead);
        agent.isStopped = true;
        anim.SetTrigger("Die");
        // Disable boss logic here
    }

    // Smooth horizontal facing toward player
    private void FacePlayer()
    {
        if (player == null) return;
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, attackTurnSpeed * Time.deltaTime);
    }

    private AttackOption PickRandomMeleeAttack()
    {
        if (meleeAttacks == null || meleeAttacks.Length == 0) return null;
        // pick a random index uniformly
        int idx = Random.Range(0, meleeAttacks.Length);
        // simple safety: ensure trigger is not empty
        for (int i = 0; i < meleeAttacks.Length; i++)
        {
            var opt = meleeAttacks[idx];
            if (opt != null && !string.IsNullOrEmpty(opt.trigger))
                return opt;
            idx = (idx + 1) % meleeAttacks.Length;
        }
        return null;
    }

    private void ResetMeleeAttackTriggers()
    {
        if (anim == null || meleeAttacks == null) return;
        foreach (var opt in meleeAttacks)
        {
            if (opt != null && !string.IsNullOrEmpty(opt.trigger))
                anim.ResetTrigger(opt.trigger);
        }
    }
    
    // Animation Event: call on the last frame of each ranged attack clip
    public void AE_AttackEnd()
    {
        isPerformingAttack = false;

        // Re‑enable agent so boss can move again
        if (agent != null)
        {
            agent.enabled = true;
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true; // stay still during recovery
                agent.ResetPath();
            }
        }

        float dist = player ? Vector3.Distance(transform.position, player.position) : Mathf.Infinity;

        // If player is too close, retreat immediately
        if (dist < retreatRange)
        {
            ChangeState(BossState.Retreat);
            return;
        }

        // Otherwise go to Recover (3-second idle before next attack)
        lastAttackTime = Time.time;
        ChangeState(BossState.Recover);
    }

    // Reset all runtime state so the boss can behave after player respawn
    public void ResetBossOnRespawn()
    {
        // Health/state
        currentHealth = maxHealth;
        isEnraged = false;
        isPerformingAttack = false;
        isInitialized = true; // keep initialized after respawn
    
        isChasing = false;
        currentState = BossState.Idle;

        // Cooldown so boss can act immediately
        lastAttackTime = Time.time - attackCooldown;

        // Agent
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (agent)
        {
            agent.enabled = true;
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
        }

        // Animator cleanup
        if (!anim) anim = GetComponent<Animator>();
        if (anim)
        {
            anim.ResetTrigger("AttackLight");
            anim.ResetTrigger("AttackHeavy");
            anim.ResetTrigger("AttackSpecial");
            anim.ResetTrigger("Enrage");
            anim.ResetTrigger("EnrageAttack");
            anim.ResetTrigger("Die");
            anim.SetBool("isMoving", false);
            anim.SetBool("isRetreating", false);
        }

        // Reset agent speed (in case it was changed during retreat)
        if (agent) 
        {
            agent.speed = 3.5f; // default speed
            agent.updateRotation = true;
        }

        // Hide health bar until engaged again
        var hb = GetComponentInChildren<EnemyHealthBar>(true);
        if (hb) hb.ForceShow(false);
    }
}
