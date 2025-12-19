using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
public class DragonController : MonoBehaviour
{
    public enum BossState { Idle, Chase, Attack, Recover, Enraged, Dead, JumpAttack, Intro } // added Intro
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
    public float attackCooldown = 10f;
    private float lastAttackTime;
    private bool isScream = false;

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

    [Header("Intro Roar")]
    public string roarTrigger = "Scream";            // Animator trigger to play roar
    public string roarStateName = "Scream";          // Exact state name of the roar clip
    public bool showHealthBarOnRoar = true;        // Force show HP UI when roaring
    private bool hasRoared = false;

    [Header("Flame Attack")]
    public DragonFlameHitbox flameHitbox;            // Assign the flame hitbox in Inspector

    [Header("Heavy Attack Lunge")]
    [Tooltip("Forward distance the dragon moves during the heavy attack animation.")]
    public float heavyLungeDistance = 8f;
    [Tooltip("Time (seconds) to cover the heavy lunge distance.")]
    public float heavyLungeDuration = 0.45f;
    private Coroutine heavyLungeRoutine;

    [Header("UI (optional)")]
    [SerializeField] private EnemyHealthBar enemyHealthBar; // assign in Inspector or auto-find

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
        if (!enemyHealthBar) enemyHealthBar = GetComponentInChildren<EnemyHealthBar>(true);
    }

    void Update()
    {
        if (!agent.isOnNavMesh)
            Debug.LogError("Dragon is NOT on the NavMesh!");

        switch (currentState)
        {
            case BossState.Idle:       HandleIdle(); break;
            case BossState.Chase:      HandleChase(); break;
            case BossState.Attack:     HandleAttack(); break;
            case BossState.Recover:    HandleRecover(); break;
            case BossState.Enraged:    HandleEnraged(); break;
            case BossState.Intro:      HandleIntroRoar(); break; // new
            case BossState.Dead:       break;
        }

        // Check enrage condition
        if (!isEnraged && currentHealth <= maxHealth * enrageThreshold)
        {
            EnterEnrageMode();
        }
    }

    // ------------------- STATE HANDLERS -------------------

    void HandleIntroRoar()
    {
        FacePlayer();

        // Wait until the roar animation finishes
        var st = anim.GetCurrentAnimatorStateInfo(0);
        bool inRoar = st.IsName(roarStateName); // tag optional
        if (inRoar && st.normalizedTime < 0.98f)
            return;

        // End of roar -> mark as done and resume normal logic
        hasRoared = true;
        if (agent && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        float dist = Vector3.Distance(transform.position, player.position);
        ChangeState(dist <= attackRange ? BossState.Attack : BossState.Chase);
    }

    private void StartIntroRoar()
    {
        hasRoared = false; // ensure not flagged yet
        if (agent && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
        anim.SetBool("isMoving", false);

        // Fire roar animation immediately
        anim.ResetTrigger(roarTrigger);
        anim.SetTrigger(roarTrigger);

        // Force-show health bar on roar
        if (showHealthBarOnRoar && enemyHealthBar)
        {
            enemyHealthBar.ForceShow(true);
        }

        ChangeState(BossState.Intro);
    }

    void HandleIdle()
    {
        anim.SetBool("isMoving", false);
        float dist = Vector3.Distance(transform.position, player.position);
        if (!hasRoared && dist < chaseRange)
        {
            StartIntroRoar();
            return;
        }
        if (dist < chaseRange)
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

        // First-time roar gate
        if (!hasRoared && dist <= chaseRange)
        {
            StartIntroRoar();
            return;
        }

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
            agent.updatePosition = false;
            agent.updateRotation = false;
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
        float timeSinceAttack = Time.time - lastAttackTime;
        bool cooldownDone = timeSinceAttack >= attackCooldown;

        // Always keep facing and stop moving during cooldown
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
        anim.SetBool("isMoving", false);
        FacePlayer();

        // If cooldown is done and player is in attack range, go back to attacking
        if (cooldownDone && dist <= attackRange)
        {
            ChangeState(BossState.Attack);
            return;
        }

        // If cooldown is done and player is out of range (+ hysteresis), resume chase
        if (cooldownDone && dist > attackRange + attackHysteresis)
        {
            anim.SetBool("isMoving", true);
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
            ChangeState(BossState.Chase);
            return;
        }

        // If still in cooldown, just hold and wait
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

        // Re‑enable agent position/rotation updates
        if (agent != null && agent.isOnNavMesh)
        {
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.Warp(transform.position); // sync position after attack
            agent.isStopped = false;
            agent.ResetPath();
        }

        // Don't change state here - let HandleRecover() enforce cooldown
        // Just mark that the attack animation finished
    }

    // ------------------- FLAME ATTACK ANIMATION EVENTS -------------------
    
    // Call this at the frame where the dragon starts breathing fire
    public void AE_FlameStart()
    {
        if (flameHitbox != null)
        {
            flameHitbox.ActivateFlame();
        }
    }

    // Call this if you want to manually stop the flame early (optional)
    public void AE_FlameEnd()
    {
        if (flameHitbox != null)
        {
            flameHitbox.DeactivateFlame();
        }
    }

    // ------------------- RESPAWN RESET -------------------

    // Reset all runtime state so the dragon behaves correctly after player respawn
    public void ResetBossOnRespawn()
    {
        // Health/state
        currentHealth = maxHealth;
        isEnraged = false;
        isPerformingAttack = false;
        isChasing = false;
        hasRoared = false;
        isScream = false;
        ChangeState(BossState.Idle);

        // Cooldown so dragon can act immediately after reset
        lastAttackTime = Time.time - attackCooldown;

        // Agent reset
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (agent)
        {
            agent.enabled = true;
            if (agent.isOnNavMesh)
            {
                agent.updatePosition = true;
                agent.updateRotation = true;
                agent.isStopped = false;
                agent.ResetPath();
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
            anim.ResetTrigger(roarTrigger);
            anim.SetBool("isMoving", false);
        }

        // Stop any running lunge coroutine
        if (heavyLungeRoutine != null)
        {
            StopCoroutine(heavyLungeRoutine);
            heavyLungeRoutine = null;
        }

        // Ensure flame is off
        if (flameHitbox != null)
        {
            flameHitbox.DeactivateFlame();
        }

        // Hide health bar until engaged again
        if (!enemyHealthBar) enemyHealthBar = GetComponentInChildren<EnemyHealthBar>(true);
        if (enemyHealthBar) enemyHealthBar.ForceShow(false);
    }
}
