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
    public LayerMask obstacleMask = 1;   // Default layer (set in Inspector)

    [Header("Rotation")]
    public float attackTurnSpeed = 8f; // rotation speed while attacking

    [Header("Enrage Settings")]
    public float enrageThreshold = 0.5f; // 50% HP
    private bool isEnraged = false;

    [Header("Death Settings")]
    public GameObject deathMessagePrefab; // Prefab to spawn on death

    [Tooltip("Extra distance required to resume chase after being in attack range")]
    public float attackHysteresis = 0.5f;



    [HideInInspector] public bool isChasing; // public flag for UI

    [System.Serializable]
    public class SoundEffectConfig
    {
        public AudioClip clip;
        public float delay; // Delay from the start of the attack
        public float duration; // Duration to play (0 = full length)
        [Range(0, 1)] public float volume = 1f;
    }

    [System.Serializable]
    public class AttackOption
    {
        public string name;      // for debugging
        public string trigger;   // Animator trigger to fire
        public SoundEffectConfig[] soundEffects;
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
    private AudioSource audioSource;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // NEW: Hook into EnemyStatController if it exists
        // This ensures Die() runs even if the weapon hits the StatController instead of this script
        var stats = GetComponent<EnemyStatController>();
        if (stats != null)
        {
            stats.OnDeath.AddListener(Die);
            currentHealth = stats.currentHealth; // Sync health
        }
        else
        {
            currentHealth = maxHealth;
        }

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
            PlayAttackSounds(opt.soundEffects);
        }

        isPerformingAttack = true;
        // Stay in Attack state until animation finishes (AE_AttackEnd will handle transition)
    }

    void PlayAttackSounds(SoundEffectConfig[] sounds)
    {
        if (sounds == null || audioSource == null) return;

        foreach (var sfx in sounds)
        {
            if (sfx.clip != null)
            {
                StartCoroutine(PlaySoundDelayed(sfx));
            }
        }
    }

    IEnumerator PlaySoundDelayed(SoundEffectConfig sfx)
    {
        if (sfx.delay > 0)
            yield return new WaitForSeconds(sfx.delay);
        
        if (sfx.clip == null) yield break;

        if (sfx.duration > 0)
        {
            // Create a temporary AudioSource to allow stopping after duration
            GameObject tempGO = new GameObject("TempSFX_" + sfx.clip.name);
            tempGO.transform.position = transform.position;
            tempGO.transform.SetParent(transform); // Move with boss
            
            AudioSource tempSource = tempGO.AddComponent<AudioSource>();
            // Copy basic settings from main audio source if available
            if (audioSource != null)
            {
                tempSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
                tempSource.spatialBlend = audioSource.spatialBlend;
                tempSource.rolloffMode = audioSource.rolloffMode;
                tempSource.minDistance = audioSource.minDistance;
                tempSource.maxDistance = audioSource.maxDistance;
            }
            else
            {
                tempSource.spatialBlend = 1f; // Default to 3D
            }

            tempSource.clip = sfx.clip;
            tempSource.volume = sfx.volume;
            tempSource.Play();

            Destroy(tempGO, sfx.duration);
        }
        else
        {
            // Play fully
            if (audioSource != null)
                audioSource.PlayOneShot(sfx.clip, sfx.volume);
        }
    }

    void HandleRecover()
    {
        float dist = Vector3.Distance(transform.position, player.position);
        agent.enabled = true;

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

        // If player got too close during recovery, retreat now
        if (dist < retreatRange)
        {
            ChangeState(BossState.Retreat);
            return;
        }

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
            anim.SetBool("isStrafingLeft", false);
            anim.SetBool("isStrafingRight", false);

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

        FacePlayer(); // Always face player

        // Check for wall behind
        Vector3 retreatDir = (transform.position - player.position).normalized;
        // Raycast backwards from slightly up to avoid ground
        bool wallBehind = Physics.Raycast(transform.position + Vector3.up, retreatDir, 2f, obstacleMask);

        if (wallBehind)
        {
            // Wall detected behind! Try to strafe.
            //anim.SetBool("isRetreating", false);

            // Check Left and Right relative to boss
            bool blockedLeft = Physics.Raycast(transform.position + Vector3.up, -transform.right, 2f, obstacleMask);
            bool blockedRight = Physics.Raycast(transform.position + Vector3.up, transform.right, 2f, obstacleMask);

            Vector3 moveDir = Vector3.zero;

            // Prefer left, then right
            if (!blockedLeft)
            {
                anim.SetBool("isStrafingLeft", true);
                anim.SetBool("isStrafingRight", false);
                moveDir = -transform.right;
            }
            else if (!blockedRight)
            {
                anim.SetBool("isStrafingLeft", false);
                anim.SetBool("isStrafingRight", true);
                moveDir = transform.right;
            }
            else
            {
                // Cornered (blocked behind, left, and right) -> Force Attack
                anim.SetBool("isStrafingLeft", false);
                anim.SetBool("isStrafingRight", false);
                ChangeState(BossState.Attack);
                return;
            }

            // Execute Strafe Movement
            if (agent != null && agent.isOnNavMesh)
            {
                agent.updateRotation = false;
                agent.isStopped = false;
                agent.speed = retreatSpeed;
                agent.Move(moveDir * retreatSpeed * Time.deltaTime);
            }
        }
        else
        {
            // No wall behind -> Normal Retreat
            anim.SetBool("isRetreating", true);
            anim.SetBool("isStrafingLeft", false);
            anim.SetBool("isStrafingRight", false);

            if (agent != null && agent.isOnNavMesh)
            {
                agent.updateRotation = false; // Let us control facing
                agent.isStopped = false;
                agent.speed = retreatSpeed;
                
                // Move directly away from player using agent.Move
                Vector3 moveAmount = retreatDir * retreatSpeed * Time.deltaTime;
                agent.Move(moveAmount);
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
        if (currentState == BossState.Chase)
        {
            var hb = GetComponentInChildren<EnemyHealthBar>(true);
            if (hb) hb.ForceShow(true);
        }
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
        // Prevent running twice
        if (currentState == BossState.Dead) return;

        if (deathMessagePrefab != null)
        {
            // Instantiate at boss position to be safe (works for both UI and World Space)
            Instantiate(deathMessagePrefab, transform.position, Quaternion.identity);
        }
        
        ChangeState(BossState.Dead);
        
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
        
        if (anim != null)
        {
            anim.SetTrigger("Die");
        }
        
        // Optional: Disable this script so Update() stops running
        this.enabled = false;
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

        // Always go to Recover (3-second idle before next attack)
        lastAttackTime = Time.time;
        ChangeState(BossState.Recover);
    }

    // Reset all runtime state so the boss can behave after player respawn
    public void ResetBossOnRespawn()
    {
        this.enabled = true; // Ensure script is re-enabled on respawn
        
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
            anim.SetBool("isStrafingLeft", false);
            anim.SetBool("isStrafingRight", false);
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