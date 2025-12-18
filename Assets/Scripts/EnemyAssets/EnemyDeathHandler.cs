using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Minimal death handler:
// - On OnDeath, immediately plays the configured death state on the Animator (no triggers/guards)
// - Waits until the death animation finishes (normalizedTime >= 0.99)
// - Destroys this GameObject after an optional delay
public class EnemyDeathHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyStatController stats;
    [SerializeField] private Animator targetAnimator;

    [Header("Animation")]
    [Tooltip("Exact Animator state name to play on death")] public string deathStateName = "Death";
    [Tooltip("Animator layer index where the death state resides")] public int animatorLayer = 0;
    [Tooltip("Delay after the death animation completes before destroying the object")] public float destroyDelay = 0f;

    private bool isDying = false;

    private void Awake()
    {
        if (!stats) stats = GetComponent<EnemyStatController>();
        if (!targetAnimator) targetAnimator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        if (stats) stats.OnDeath.AddListener(HandleDeath);
    }

    private void OnDisable()
    {
        if (stats) stats.OnDeath.RemoveListener(HandleDeath);
    }

    public void ResetDeathHandler()
    {
        isDying = false;
        StopAllCoroutines();
    }

    private void HandleDeath()
    {
        if (isDying) return;
        isDying = true;

        if (targetAnimator && !string.IsNullOrEmpty(deathStateName))
        {
            // Clear any queued triggers before forcing the death state
            foreach (var p in targetAnimator.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Trigger)
                    targetAnimator.ResetTrigger(p.name);
            }
            // Instantly play the death animation from the beginning
            targetAnimator.Play(deathStateName, animatorLayer, 0f);
        }
        // Stop all movement/attacks and disable other behaviours so nothing can interrupt death
        StopAllOtherSystems();
        StartCoroutine(WaitAndDestroy());
    }

    private IEnumerator WaitAndDestroy()
    {
        // Give one frame for the Animator to enter the death state
        yield return null;

        if (targetAnimator && !string.IsNullOrEmpty(deathStateName))
        {
            int deathHash = Animator.StringToHash(deathStateName);
            float safety = 10f; // seconds safety cap
            float t = 0f;
            while (t < safety)
            {
                var st = targetAnimator.GetCurrentAnimatorStateInfo(animatorLayer);
                // Wait until we're in the death state and it has played almost fully
                if (st.shortNameHash == deathHash && st.normalizedTime >= 0.99f)
                    break;

                t += Time.deltaTime;
                yield return null;
            }
        }

        if (destroyDelay > 0f)
            yield return new WaitForSeconds(destroyDelay);

        // Check for RespawnManager
        var respawnManager = GetComponent<EnemyRespawnManager>();
        if (respawnManager != null)
        {
            respawnManager.SetDead(true);
            gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void StopAllOtherSystems()
    {
        // Stop NavMesh movement if present
        var agent = GetComponent<NavMeshAgent>();
        if (agent)
        {
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.enabled = false;
        }

        // Disable weapon damage immediately
        var weapons = GetComponentsInChildren<Weapon>(true);
        foreach (var w in weapons)
        {
            w.canDamage = false;
            w.enabled = false;
        }

        // Disable all other scripts under this enemy except this handler and the stats
        var behaviours = GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var b in behaviours)
        {
            if (b == this) continue;
            if (stats != null && b == stats) continue;
            b.enabled = false;
        }
    }
}
