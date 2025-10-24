using UnityEngine;
using UnityEngine.AI;

public class MonsterController : MonoBehaviour
{
    public enum State { Idle, Chase, Attack, Recover, Dead }
    private State currentState;

    [Header("References")]
    public Transform player;
    private NavMeshAgent agent;
    private Animator anim;
    private EnemyStatController enemyStats;

    [Header("Settings")]
    public float maxHealth;
    private float currentHealth;
    public float chaseRange = 15f;
    public float attackRange;
    public float attackCooldown;
    private float lastAttackTime;
    private float playerDetectionInterval = 0.5f;
    private float lastDetectionTime;

    [Header("Attack Facing")]
    public float attackTurnSpeed = 8f;          // how fast to rotate toward player while preparing / performing attack
    public float maxAttackStartAngle = 60f;     // must face player within this angle before starting attack
    public bool allowAttackWhileTurning = false;

    private bool isAttacking = false;           // true only while the attack animation is playing

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        currentHealth = maxHealth;
        ChangeState(State.Idle);
        lastDetectionTime = Time.time - playerDetectionInterval;
    }

    void Update()
    {
        if (Time.time - lastDetectionTime >= playerDetectionInterval)
        {
            DetectPlayer();
            lastDetectionTime = Time.time;
        }

        switch (currentState)
        {
            case State.Idle:    HandleIdle(); break;
            case State.Chase:   HandleChase(); break;
            case State.Attack:  HandleAttack(); break;
            case State.Recover: HandleRecover(); break;
            case State.Dead:    break;
        }
    }

    //detect player
    private void DetectPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, chaseRange);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                player = hit.transform;
                break;
            }
        }
    }

    // ------------------- STATE HANDLERS -------------------

    void HandleIdle()
    {
        anim.SetBool("isMoving", false);
        if (player != null && Vector3.Distance(transform.position, player.position) < chaseRange)
            ChangeState(State.Chase);
    }

    void HandleChase()
    {
        if (player == null) return;
        anim.SetBool("isMoving", true);
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }

        if (Vector3.Distance(transform.position, player.position) <= attackRange)
            ChangeState(State.Attack);
    }

    void HandleAttack()
    {
        if (player == null) { ChangeState(State.Idle); return; }

        // Always face player while in Attack state
        FacePlayerYawOnly(attackTurnSpeed);

        float dist = Vector3.Distance(transform.position, player.position);
        if (!isAttacking)
        {
            // If player moved out of range, resume chase
            if (dist > attackRange * 1.1f)
            {
                ChangeState(State.Chase);
                return;
            }

            // Check angle before initiating attack
            float angle = AngleToPlayerOnPlane();
            if (angle > maxAttackStartAngle && !allowAttackWhileTurning)
            {
                // Keep rotating this frame; do not start attack yet
                anim.SetBool("isMoving", false);
                if (agent.isOnNavMesh) agent.isStopped = true;
                return;
            }

            // Ready to trigger attack?
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }
                anim.SetBool("isMoving", false);
                anim.ResetTrigger("Attack"); // safety
                anim.SetTrigger("Attack");
                isAttacking = true; // will be cleared by animation event
                // Do NOT switch to Recover yet; stay in Attack until animation says it's done
            }
        }
        else
        {
            // During attack: optionally allow early cancel if player far / behind
            if (dist > attackRange * 1.5f)
            {
                // Abort after current animation ends (you could add a hard cancel if needed)
            }
        }
    }

    void HandleRecover()
    {
        // Move again while waiting for cooldown
        if (player == null) { ChangeState(State.Idle); return; }

        float dist = Vector3.Distance(transform.position, player.position);

        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        anim.SetBool("isMoving", true);

        if (dist <= attackRange && Time.time - lastAttackTime >= attackCooldown)
        {
            ChangeState(State.Attack);
        }
        else if (dist > attackRange * 1.2f)
        {
            ChangeState(State.Chase);
        }
    }

    // ------------------- HELPERS -------------------

    void FacePlayerYawOnly(float turnSpeed)
    {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f) return;
        Quaternion targetRot = Quaternion.LookRotation(toPlayer);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
    }

    float AngleToPlayerOnPlane()
    {
        Vector3 fwd = transform.forward; fwd.y = 0;
        Vector3 toPlayer = player.position - transform.position; toPlayer.y = 0;
        if (toPlayer.sqrMagnitude < 0.0001f) return 0f;
        return Vector3.Angle(fwd, toPlayer);
    }

    // Animation Event (place at the end of the attack animation)
    public void AE_AttackEnd()
    {
        isAttacking = false;
        lastAttackTime = Time.time;
        ChangeState(State.Recover);
    }

    // (Optional) Animation Event at the actual hit frame
    public void AE_AttackImpact()
    {
        // Apply damage / spawn hitbox here
    }

    // ------------------- STATE CHANGES -------------------

    void ChangeState(State newState)
    {
        currentState = newState;
    }

    // ------------------- DAMAGE SYSTEM -------------------

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0 && currentState != State.Dead)
        {
            Die();
        }
    }

    void Die()
    {
        ChangeState(State.Dead);
        if (agent.isOnNavMesh) agent.isStopped = true;
        anim.SetTrigger("Die");
    }
}
