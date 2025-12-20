using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
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
    public float attackRange = 5f;
    public float attackCooldown = 1f;
    private float lastAttackTime;

    [Header("Jump Attack Settings")]
    public float jumpDuration = 2.0f;       // how long the jump lasts
    public float jumpHeight = 5f;           // max height of the arc
    public AnimationCurve jumpCurve;        // controls jump arc
    public float jumpAttackRange = 10f;
    public SoundEffectConfig[] jumpAttackSounds; // SFX for jump attack
    private bool isJumping = false;
    private Vector3 jumpStart;
    private Vector3 jumpTarget;
    private float jumpTimer = 0f;

    [Header("Rotation")]
    public float attackTurnSpeed = 8f; // rotation speed while attacking

    [Header("Enrage Settings")]
    public float enrageThreshold = 0.5f; // 50% HP
    private bool isEnraged = false;

    [Header("Death Settings")]
    public GameObject deathMessagePrefab; // Prefab to spawn on death

    [Tooltip("Extra distance required to resume chase after being in attack range")]
    public float attackHysteresis = 0.5f;

    [Header("Jump Attack Landing VFX")]
    public ParticleSystem earthquakePrefab;      // assign particle prefab
    public float earthquakeDestroyDelay = 5f;    // auto-destroy seconds (optional)
    public bool spawnAtJumpTarget = true;        // use jumpTarget instead of current position

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

        // Hook into EnemyStatController if it exists
        var stats = GetComponent<EnemyStatController>();
        if (stats != null)
        {
            stats.OnDeath.AddListener(Die);
            
            // Fix: If stats.currentHealth is 0, it likely hasn't initialized yet. Use maxHealth instead.
            if (stats.currentHealth > 0)
            {
                currentHealth = stats.currentHealth;
            }
            else
            {
                // Fallback to stats.GetMaxHealth(), or local maxHealth if that's also 0
                float statMax = stats.GetMaxHealth();
                currentHealth = statMax > 0 ? statMax : maxHealth;
            }
            
            // Sync local maxHealth to match stats
            if (stats.GetMaxHealth() > 0) maxHealth = stats.GetMaxHealth();
        }
        else
        {
            currentHealth = maxHealth;
        }

        ChangeState(BossState.Idle);
        if (agent != null)
        {
            agent.stoppingDistance = Mathf.Max(0f, attackRange); // stop at attack range
        }
    }

    void Update()
    {
        //if (!agent.isOnNavMesh)
        //Debug.LogError("Boss is NOT on the NavMesh!");

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
        isChasing = true;

        anim.SetBool("isMoving", true);
        float dist = Vector3.Distance(transform.position, player.position);

        // Prefer jump if in band
        if (dist >= jumpAttackRange && dist <= chaseRange)
        {
            anim.SetBool("isMoving", false);
            StartJumpAttack();
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
            PlayAttackSounds(opt.soundEffects);
        }

        isPerformingAttack = true;
        // no cooldown: Recover can be kept simple or removed
        ChangeState(BossState.Recover);
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
        PlayAttackSounds(jumpAttackSounds); // Play jump SFX

        ChangeState(BossState.JumpAttack);
    }

    // Animation Event: place at landing frame of Jump Attack clip
    public void AE_JumpLandImpact()
    {
        if (!earthquakePrefab) return;
        Vector3 spawnPos = spawnAtJumpTarget ? jumpTarget : transform.position;
        var ps = Instantiate(earthquakePrefab, spawnPos, Quaternion.identity);
        ps.Play();
        if (earthquakeDestroyDelay > 0f)
            Destroy(ps.gameObject, earthquakeDestroyDelay);
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
        if (currentState == BossState.Dead) return;

        ChangeState(BossState.Dead);
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        if (anim != null) anim.SetTrigger("Die");
        
        if (deathMessagePrefab != null)
        {
            Instantiate(deathMessagePrefab, transform.position, Quaternion.identity);
        }
        // Disable boss logic here
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

    // Reset all runtime state so the boss can behave after player respawn
    public void ResetBossOnRespawn()
    {
        this.enabled = true;

        // Health/state
        currentHealth = maxHealth;
        isEnraged = false;
        isPerformingAttack = false;
        isJumping = false;
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
            anim.ResetTrigger("JumpAttack");
            anim.SetBool("isMoving", false);
        }

        // Hide health bar until engaged again
        var hb = GetComponentInChildren<EnemyHealthBar>(true);
        if (hb) hb.ForceShow(false);
    }
}
