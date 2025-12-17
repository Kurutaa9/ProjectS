using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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

    public bool isHit = false;
    private bool isDead = false;

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
            stats.OnTakeDamage.AddListener(PlayGetHit);
            stats.OnDeath.AddListener(OnDeath);
        }
    }

    private void OnDisable()
    {
        if (stats)
        {
            stats.OnTakeDamage.RemoveListener(PlayGetHit);
            stats.OnDeath.RemoveListener(OnDeath);
        }
    }

    private void OnDeath()
    {
        isDead = true;
        StopAllCoroutines(); // Stop any recovery routines so we don't wake up after death
    }

    private void PlayGetHit()
    {
        // Don't play hit animation if already dead or currently being hit
        if (isDead || isHit) return;

        StartCoroutine(HitRoutine());
    }

    private IEnumerator HitRoutine()
    {
        isHit = true;

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
