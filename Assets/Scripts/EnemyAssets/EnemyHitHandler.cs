using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class EnemyHitHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyStatController stats;
    [SerializeField] private Animator anim;
    [SerializeField] private NavMeshAgent agent;

    [Header("Settings")]
    [Tooltip("Name of the animation state to play when hit")]
    public string getHitStateName = "GetHit";
    [Tooltip("Layer index for the GetHit animation")]
    public int animatorLayer = 0;
    [Tooltip("How long to stun the enemy if animation length cannot be found")]
    public float defaultStunDuration = 0.5f;
    [Tooltip("Probability (0-1) that the enemy will be stunned when hit.")]
    [Range(0f, 1f)]
    public float stunProbability = 1.0f;

    public bool isHit = false;
    private bool isDead = false;

    public UnityEvent OnStunned;

    private void Awake()
    {
        // Auto-assign references if missing
        if (!stats) stats = GetComponent<EnemyStatController>();
        if (!anim) anim = GetComponentInChildren<Animator>();
        if (!agent) agent = GetComponent<NavMeshAgent>();
    }

    private void OnEnable()
    {
        if (stats)
        {
            stats.OnTakeDamageWithStun.AddListener(PlayGetHitWithStun);
            stats.OnDeath.AddListener(OnDeath);
        }
    }

    private void OnDisable()
    {
        if (stats)
        {
            stats.OnTakeDamageWithStun.RemoveListener(PlayGetHitWithStun);
            stats.OnDeath.RemoveListener(OnDeath);
        }
    }

    private void OnDeath()
    {
        isDead = true;
        StopAllCoroutines(); // Stop any recovery routines so we don't wake up after death
    }

    // Called by EnemyRespawnManager to reset state
    public void ResetHitHandler()
    {
        isDead = false;
        isHit = false;
        StopAllCoroutines();
    }

    private void PlayGetHitWithStun(float stunMultiplier)
    {
        // Don't play hit animation if already dead or currently being hit
        if (isDead || isHit) return;

        // Check probability with multiplier
        // Effective probability = base probability * multiplier
        // e.g. 0.5 * 1.5 = 0.75 chance
        float effectiveProbability = Mathf.Clamp01(stunProbability * stunMultiplier);

        if (Random.value > effectiveProbability) return;

        StartCoroutine(HitRoutine());
    }

    private IEnumerator HitRoutine()
    {
        isHit = true;
        OnStunned.Invoke();

        // Disable weapons immediately
        var weapons = GetComponentsInChildren<Weapon>(true);
        foreach (var w in weapons)
        {
            w.canDamage = false;
            w.EndAttack();
        }

        // Stop Movement
        if (agent && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            if (agent.isOnNavMesh) agent.ResetPath();
        }

        // Play Animation
        if (anim)
        {
            anim.Play(getHitStateName, animatorLayer, 0f);
            
            // Wait a frame for the state to switch
            yield return null;

            // Wait for animation to finish
            float duration = defaultStunDuration;
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(animatorLayer);
            
            if (info.IsName(getHitStateName))
            {
                duration = info.length;
            }

            yield return new WaitForSeconds(duration);
        }
        else
        {
            // Fallback if no animator, just wait default time
            yield return new WaitForSeconds(defaultStunDuration);
        }

        // Resume Movement
        // Only resume if we haven't died in the meantime
        if (!isDead && agent && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        isHit = false;
    }
}
