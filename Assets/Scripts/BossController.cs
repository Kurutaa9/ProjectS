using UnityEngine;
using UnityEngine.AI;

public class BossController : MonoBehaviour
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
    public float chaseRange = 15f;
    public float attackRange = 3f;
    public float attackCooldown = 2f;
    private float lastAttackTime;

    [Header("Jump Attack Settings")]
    public float jumpDuration = 1.0f;       // how long the jump lasts
    public float jumpHeight = 3f;           // max height of the arc
    public AnimationCurve jumpCurve;        // controls jump arc
    public float jumpTriggerRange = 15.0f;
    private bool isJumping = false;
    private Vector3 jumpStart;
    private Vector3 jumpTarget;
    private float jumpTimer = 0f;

    [Header("Rotation")]
    public float attackTurnSpeed = 8f; // rotation speed while attacking

    [Header("Enrage Settings")]
    public float enrageThreshold = 0.5f; // 50% HP
    private bool isEnraged = false;

    [Tooltip("Extra distance required to resume chase after being in attack range")]
    public float attackHysteresis = 0.5f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        currentHealth = maxHealth;
        ChangeState(BossState.Idle);

        if (agent != null)
        {
            agent.stoppingDistance = Mathf.Max(0f, attackRange); // stop at attack range
        }
    }

    void Update()
    {
        if (!agent.isOnNavMesh)
        Debug.LogError("Boss is NOT on the NavMesh!");

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
            
            case BossState.JumpAttack:
                HandleJumpAttack();
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


        anim.SetBool("isMoving", true);
        float dist = Vector3.Distance(transform.position, player.position);

        // If within attack range, stop and switch to Attack
        if (dist <= attackRange)
        {
            anim.SetBool("isMoving", false);
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            ChangeState(BossState.Attack);
            return;
        }

        // Otherwise, chase
        anim.SetBool("isMoving", true);
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }

        if (Vector3.Distance(transform.position, player.position) > jumpTriggerRange) // if player is far, leap
        {
            StartJumpAttack();
        }
    }

    void HandleAttack()
    {
        agent.isStopped = true;
        FacePlayer(); // ensure boss faces player while attacking
        anim.SetBool("isMoving", false);

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            anim.SetTrigger("Attack"); 
            lastAttackTime = Time.time;
            ChangeState(BossState.Recover);
        }
    }

    void StartJumpAttack()
    {
        isJumping = true;
        agent.enabled = false; // disable NavMeshAgent control
        jumpStart = transform.position;
        // jumpTarget = player.position; // snapshot target
        jumpTimer = 0f;

         // Add an offset in front of the player
        Vector3 forwardOffset = player.forward * 2f; // 2 units in front of player
        jumpTarget = player.position + forwardOffset; // snapshot target in front

        anim.SetTrigger("JumpAttack"); // play jump animation
        ChangeState(BossState.JumpAttack);
    }

    void HandleJumpAttack()
    {
        if (!isJumping) return;

        FacePlayer(); // keep rotating toward player during jump

        jumpTimer += Time.deltaTime;
        float t = jumpTimer / jumpDuration;

        if (t >= 1f)
        {
            // End jump
            transform.position = jumpTarget;
            isJumping = false;
            agent.enabled = true;
            lastAttackTime = Time.time;
            ChangeState(BossState.Recover);
        }
        else
        {
            // Horizontal lerp
            Vector3 horiz = Vector3.Lerp(jumpStart, jumpTarget, t);

            // Vertical arc using curve
            float height = jumpCurve.Evaluate(t) * jumpHeight;
            transform.position = new Vector3(horiz.x, jumpStart.y + height, horiz.z);
            // float height = 4 * jumpHeight * t * (1 - t);
            // transform.position = new Vector3(horiz.x, jumpStart.y + height, horiz.z);

        }
    }



    void HandleRecover()
    {
        float dist = Vector3.Distance(transform.position, player.position);

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
}
