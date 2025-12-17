using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BossManager : MonoBehaviour
{
    public string playerTag = "Player";
    public Transform target; // Player target
    public NavMeshAgent agent;
    public Animator anim;
    public BossCombat combat;
    [SerializeField] private EnemyStatController stats;
    [SerializeField] private EnemyHitHandler hitHandler;

    [Header("Distances (meters)")]
    public float detectionRange = 30f; // start engaging when within this range
    public float attackRange = 3.0f;   // start attacking when within this range
    public float stopDistanceBuffer = 0.2f; // small buffer around attackRange

    [Header("Behaviour")]
    public float faceTargetSpeed = 10f;
    public Vector3 chestOffset = new Vector3(0, 1.2f, 0);

    [Header("Attack cadence")]
    public float minAttackCooldown = 1.0f;
    public float maxAttackCooldown = 2.0f;

    private float cooldownTimer = 0f;
    private bool prevAttacking = false;

    private enum BossState { Idle, Chasing, InRange }
    private BossState state = BossState.Idle;

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!anim) anim = GetComponentInChildren<Animator>();
        if (!combat) combat = GetComponent<BossCombat>();
        if (!stats) stats = GetComponent<EnemyStatController>();
        if (!hitHandler) hitHandler = GetComponent<EnemyHitHandler>();
    }

    void OnEnable()
    {
        if (stats) stats.OnTakeDamage.AddListener(OnDamageReceived);
    }

    void OnDisable()
    {
        if (stats) stats.OnTakeDamage.RemoveListener(OnDamageReceived);
    }

    private void OnDamageReceived()
    {
        if (combat) combat.StopAttack();
        cooldownTimer = 0f;
        prevAttacking = false;
    }

    void Start()
    {
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj) target = playerObj.transform;
        }

        if (agent)
        {
            agent.updateRotation = false; // we'll rotate manually to face the player
            agent.stoppingDistance = Mathf.Max(attackRange - stopDistanceBuffer, 0.1f);
        }
    }

    void Update()
    {
        if (!target) return;
        if (anim && anim.GetCurrentAnimatorStateInfo(0).IsName("GetHit")) return;
        if (hitHandler != null && hitHandler.isHit) return;


        bool isAttackingNow = combat != null && combat.IsAttacking;
        if (prevAttacking && !isAttackingNow)
        {
            cooldownTimer = combat.LastRecovery;
        }
        prevAttacking = isAttackingNow;

        float dist = Vector3.Distance(transform.position, target.position);

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        // While attacking, prevent NavMeshAgent from snapping the transform back to
        // its stopping position. Re-enable after the attack and resync the agent.
        if (agent)
        {
            if (isAttackingNow)
            {
                agent.velocity = Vector3.zero;
                if (agent.updatePosition) agent.updatePosition = false;
            }
            else
            {
                if (!agent.updatePosition)
                {
                    agent.updatePosition = true;
                    if (agent.isOnNavMesh)
                    {
                        agent.Warp(transform.position);
                    }
                }
            }
        }

        // State selection: while attacking, remain InRange and keep agent stopped
        if (isAttackingNow)
        {
            SetState(BossState.InRange);
        }
        else if (dist > detectionRange)
        {
            SetState(BossState.Idle);
        }
        else if (dist > attackRange)
        {
            SetState(BossState.Chasing);
        }
        else
        {
            SetState(BossState.InRange);
        }

        switch (state)
        {
            case BossState.Idle:
                if (agent)
                {
                    agent.isStopped = true;
                }
                break;

            case BossState.Chasing:
                if (agent)
                {
                    if (!isAttackingNow)
                    {
                        agent.isStopped = false;
                        if (agent.isOnNavMesh)
                        {
                            agent.SetDestination(target.position);
                        }
                    }
                }
                FaceTarget();
                break;

            case BossState.InRange:
                if (agent)
                {
                    agent.isStopped = true; // stop and fight
                }
                FaceTarget();
                HandleAttacking();
                break;
        }
    }

    private void HandleAttacking()
    {
        if (!combat) return;

        // Don't start new attack if currently attacking or in cooldown
        if (combat.IsAttacking || cooldownTimer > 0f) return;

        // Start a random attack from the three profiles
        combat.StartRandomAttack();
    }

    private void FaceTarget()
    {
        Vector3 to = (target.position + chestOffset) - (transform.position + chestOffset);
        to.y = 0f;
        if (to.sqrMagnitude < 0.0001f) return;
        Quaternion look = Quaternion.LookRotation(to.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, faceTargetSpeed * Time.deltaTime);
    }

    private void SetState(BossState s)
    {
        state = s;
        if (anim)
        {
            anim.SetBool("IsMoving", state == BossState.Chasing);
            anim.SetBool("InCombat", state != BossState.Idle);
        }
    }
}
