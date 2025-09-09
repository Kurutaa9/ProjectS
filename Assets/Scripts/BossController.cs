using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

public class BossController : MonoBehaviour
{
    public enum BossState { Idle, Chase, Attack, Recover, Enraged, Dead }
    private BossState currentState;

    [Header("References")]
    public Transform player;
    private NavMeshAgent agent;
    private Animator anim;

    [Header("UI References")]
    public GameObject healthBarUI; // Drag your health bar UI here
    public Slider healthSlider; // If using a slider for health bar
    public TextMeshProUGUI bossName;

    [Header("Boss Settings")]
    public float maxHealth = 1000f;
    private float currentHealth;
    public float chaseRange = 15f;
    public float attackRange = 3f;
    public float attackCooldown = 2f;
    private float lastAttackTime;

    [Header("Enrage Settings")]
    public float enrageThreshold = 0.5f; // 50% HP
    private bool isEnraged = false;

    [Header("Jump Attack Settings")]
    public float jumpAttackMinRange = 5f;
    public float jumpAttackMaxRange = 9f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        anim.applyRootMotion = false; // off by default
        currentHealth = maxHealth;
        ChangeState(BossState.Idle);
    }

    void Update()
    {
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
            // Show health UI when entering chase zone
            if (healthBarUI != null)
            {
                bossName.text = "Maw";
                healthBarUI.SetActive(true);
            }
            ChangeState(BossState.Chase);
        }
    }

    void HandleChase()
    {
        anim.SetBool("isMoving", true);
        agent.isStopped = false;
        agent.SetDestination(player.position);

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= attackRange || (dist >= jumpAttackMinRange && dist <= jumpAttackMaxRange))
        {
            ChangeState(BossState.Attack);
        }
    }

    void HandleAttack()
    {
        agent.isStopped = true;
        anim.SetBool("isMoving", false);

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist >= jumpAttackMinRange && dist <= jumpAttackMaxRange)
            {
                anim.SetTrigger("JumpAttack"); // Jump: root motion via animation events
            }
            else
            {
                anim.SetTrigger("Attack"); // Normal attack
            }
            lastAttackTime = Time.time;
        }

        ChangeState(BossState.Recover);
    }

    void HandleRecover()
    {
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            ChangeState(BossState.Chase);
        }
    }

    void HandleEnraged()
    {
        // Enraged state = faster movement + shorter cooldown
        agent.speed = 6f;
        attackCooldown = 1f;

        if (Vector3.Distance(transform.position, player.position) <= attackRange)
        {
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

        // Update health bar UI
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth / maxHealth;
        }

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
        // Hide health UI when boss dies
        if (healthBarUI != null)
        {
            healthBarUI.SetActive(false);
        }
    }

    // Animation Events (call these from the Jump Attack clip)
    public void AE_EnableRootMotion()
    {
        anim.applyRootMotion = true;
        if (agent != null) agent.enabled = false; // prevent conflicts while leaping
    }

    public void AE_DisableRootMotion()
    {
        anim.applyRootMotion = false;
        if (agent != null) agent.enabled = true; // restore NavMeshAgent after leap
    }
}
