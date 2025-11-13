using UnityEngine;
using UnityEngine.AI;
using BT;

[DisallowMultipleComponent]
public class BossBehaviorTree : MonoBehaviour
{
    [Header("Refs")]
    public string playerTag = "Player";
    public Transform target;
    public NavMeshAgent agent;
    public Animator anim;
    public BossCombat combat;

    [Header("Ranges (meters)")]
    public float detectionRange = 30f;
    public float attackRange = 3.0f;
    public float stopDistanceBuffer = 0.2f;

    [Header("Facing")]
    public float faceTargetSpeed = 10f;
    public Vector3 chestOffset = new Vector3(0, 1.2f, 0);

    [Header("Attack cadence")]
    public float minAttackCooldown = 0.4f;
    public float maxAttackCooldown = 0.9f;

    private float cooldownTimer = 0f;
    private BTNode root;

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!anim) anim = GetComponentInChildren<Animator>();
        if (!combat) combat = GetComponent<BossCombat>();
    }

    void Start()
    {
        if (!target)
        {
            var playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj) target = playerObj.transform;
        }

        if (agent)
        {
            agent.updateRotation = false; // manual rotating
            agent.stoppingDistance = Mathf.Max(attackRange - stopDistanceBuffer, 0.1f);
        }

        BuildTree();
    }

    void Update()
    {
        if (!target || root == null) return;

        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        // Update animator high-level flags
        if (anim)
        {
            bool inCombat = IsInDetectionRange();
            bool moving = agent && agent.velocity.sqrMagnitude > 0.02f && agent.isStopped == false;
            anim.SetBool("InCombat", inCombat);
            anim.SetBool("IsMoving", moving);
        }

        root.Evaluate();
    }

    private void BuildTree()
    {
        // Conditions
        var condHasTarget = new ConditionNode(() => target != null);
        var condInDetect = new ConditionNode(IsInDetectionRange);
        var condInAttack = new ConditionNode(IsInAttackRange);
        var condCanAttackNow = new ConditionNode(() => cooldownTimer <= 0f && combat && combat.IsAttacking == false);

        // Actions
        var actFace = new ActionNode(() => { FaceTarget(); return NodeState.Success; });
        var actChase = new ActionNode(ChaseAction);
        var actAttack = new ActionNode(AttackAction);
        var actIdle = new ActionNode(IdleAction);

        // Attack subtree: if in attack range and can attack, face then attack
        var attackSeq = new Sequence(condInAttack, condCanAttackNow, actFace, actAttack);

        // Engage subtree: if in detection range, try attack else chase
        var engageSel = new Selector(
            attackSeq,
            new Sequence(condInDetect, actChase)
        );

        root = new Sequence(condHasTarget, new Selector(engageSel, actIdle));
    }

    private bool IsInDetectionRange()
    {
        if (!target) return false;
        return Vector3.Distance(transform.position, target.position) <= detectionRange;
    }

    private bool IsInAttackRange()
    {
        if (!target) return false;
        return Vector3.Distance(transform.position, target.position) <= attackRange;
    }

    private void FaceTarget()
    {
        if (!target) return;
        Vector3 to = (target.position + chestOffset) - (transform.position + chestOffset);
        to.y = 0f;
        if (to.sqrMagnitude < 0.0001f) return;
        Quaternion look = Quaternion.LookRotation(to.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, faceTargetSpeed * Time.deltaTime);
    }

    private NodeState ChaseAction()
    {
        if (!agent || !target) return NodeState.Failure;
        agent.isStopped = false;
        agent.SetDestination(target.position);
        FaceTarget();
        // Chase until in attack range
        return IsInAttackRange() ? NodeState.Success : NodeState.Running;
    }

    private NodeState AttackAction()
    {
        if (!combat) return NodeState.Failure;

        // If already attacking, wait; when finishes, start cooldown
        if (combat.IsAttacking)
        {
            // keep facing during attack, optional
            FaceTarget();
            return NodeState.Running;
        }

        // Start an attack and mark cooldown to be set after completion
        combat.StartRandomAttack();
        StartCoroutine(WaitAttackAndCooldown());
        return NodeState.Running;
    }

    private System.Collections.IEnumerator WaitAttackAndCooldown()
    {
        // Wait until attack ends
        while (combat && combat.IsAttacking)
            yield return null;

        cooldownTimer = Random.Range(minAttackCooldown, maxAttackCooldown);
    }

    private NodeState IdleAction()
    {
        if (agent)
        {
            agent.isStopped = true;
        }
        return NodeState.Running; // idle keeps running
    }
}
