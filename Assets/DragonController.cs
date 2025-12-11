using UnityEngine;
using UnityEngine.AI;

public class DragonController : MonoBehaviour
{
    public enum BossState { Idle, Chase, Attack, Recover, Enraged, Dead, JumpAttack }
    private BossState currentState;

    [Header("References")]
    public Transform player;
    private NavMeshAgent agent;
    private Animator anim;

    [Header("Boss Settings")]
    public float maxHealth = 1000f;
    private float currentHealth;
    public float chaseRange = 50f;
    public float attackRange = 1000f;
    public float attackCooldown = 3f;
    private float lastAttackTime;

    [Header("Rotation")]
    public float attackTurnSpeed = 8f; // rotation speed while attacking

    [Header("Enrage Settings")]
    public float enrageThreshold = 0.5f; // 50% HP
    private bool isEnraged = false;

    [Tooltip("Extra distance required to resume chase after being in attack range")]
    public float attackHysteresis = 0.5f;

    [Header("Jump Attack Landing VFX")]
    public ParticleSystem earthquakePrefab;      // assign particle prefab
    public float earthquakeDestroyDelay = 5f;    // auto-destroy seconds (optional)
    public bool spawnAtJumpTarget = true;        // use jumpTarget instead of current position

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

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        currentHealth = maxHealth;
        ChangeState(BossState.Idle);
        if (agent != null)
        {
            agent.stoppingDistance = attackRange; // stop at attack range
        }
    }

    void Update()
    {
        if (!agent.isOnNavMesh)
        Debug.LogError("Dragon is NOT on the NavMesh!");

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
        if (Vector3.Distance(transform.position, player.position) < chaseRange)
        {
            ChangeState(BossState.Chase);
        }
    }

    void HandleChase()
    {
        Debug.Log($"Chase: speed={agent.speed}, stopped={agent.isStopped}, onNav={agent.isOnNavMesh}, hasPath={agent.hasPath}, remaining={agent.remainingDistance}");
        isChasing = true;

        anim.SetBool("isMoving", true);
        float dist = Vector3.Distance(transform.position, player.position);

        // If within melee range -> attack
        if (dist <= attackRange)
        {
            anim.SetBool("isMoving", false);
            ChangeState(BossState.Attack);
            return;
        }

        // Otherwise, chase
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
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
        lastAttackTime = Time.time;  // Set cooldown NOW when attack starts, not at end
        // no cooldown: Recover can be kept simple or removed
        ChangeState(BossState.Recover);
    }

    void HandleRecover()
    {
        float dist = Vector3.Distance(transform.position, player.position);
        agent.enabled = true;
        // Hold position and keep facing while in attack range
        if (dist <= attackRange)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
            anim.SetBool("isMoving", false);
            FacePlayer(); // keep oriented during recovery

            if (Time.time - lastAttackTime >= attackCooldown)
            {
                ChangeState(BossState.Attack);
            }
            return;
        }

        // If player moved out of attack range + hysteresis, resume chase
        if (dist > attackRange + attackHysteresis)
        {
            anim.SetBool("isMoving", true);
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }

            if (Time.time - lastAttackTime >= attackCooldown)
            {
                ChangeState(BossState.Chase);
            }
        }
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
    
    // Animation Event: call on the last frame of each melee attack clip
    public void AE_AttackEnd()
    {
        isPerformingAttack = false;

        // Re‑enable agent so boss can move again
        if (agent != null)
        {
            agent.enabled = true;
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.ResetPath();
            }
        }

        float dist = player ? Vector3.Distance(transform.position, player.position) : Mathf.Infinity;
        // if still in range, go back to Attack (which will pick another random attack),
        // otherwise chase again
        ChangeState(dist <= attackRange ? BossState.Attack : BossState.Chase);
    }
}
