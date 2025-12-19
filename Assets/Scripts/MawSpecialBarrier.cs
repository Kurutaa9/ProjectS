using System.Collections;
using UnityEngine;

public class MawSpecialBarrier : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign the boss' EnemyStatController so the barrier can react to its death. If left empty, the script will attempt to find one on a GameObject tagged 'Boss' or in parent objects.")]
    [SerializeField] private EnemyStatController bossStats;

    [Header("Behaviour")]
    [Tooltip("If true the barrier GameObject will be Destroyed after the delay. Otherwise it will be deactivated.")]
    [SerializeField] private bool destroyOnDeath = false;
    [Tooltip("Delay (seconds) before the barrier disappears after boss death.")]
    [SerializeField] private float disappearDelay = 0f;

    private bool hasSubscribed = false;

    void Awake()
    {
        // Ensure any colliders on this object are NOT triggers as requested
        var cols = GetComponentsInChildren<Collider>(true);
        foreach (var c in cols)
        {
            if (c != null) c.isTrigger = false;
        }

        // Try to auto-resolve boss stats if not assigned
        if (bossStats == null)
        {
            // Prefer a GameObject tagged "Boss"
            var bossObj = GameObject.FindGameObjectWithTag("Boss");
            if (bossObj != null)
            {
                bossStats = bossObj.GetComponent<EnemyStatController>();
            }

            // Fallback: look in parents
            if (bossStats == null)
            {
                bossStats = GetComponentInParent<EnemyStatController>();
            }
        }
    }

    void OnEnable()
    {
        TrySubscribe();
    }

    void OnDisable()
    {
        TryUnsubscribe();
    }

    private void TrySubscribe()
    {
        if (bossStats != null && !hasSubscribed)
        {
            bossStats.OnDeath.AddListener(OnBossDeath);
            hasSubscribed = true;
        }
    }

    private void TryUnsubscribe()
    {
        if (bossStats != null && hasSubscribed)
        {
            bossStats.OnDeath.RemoveListener(OnBossDeath);
            hasSubscribed = false;
        }
    }

    private void OnBossDeath()
    {
        // Start coroutine so we can respect optional delay
        StartCoroutine(DisappearRoutine());
    }

    private IEnumerator DisappearRoutine()
    {
        if (disappearDelay > 0f)
            yield return new WaitForSeconds(disappearDelay);

        // Ensure colliders are not left as triggers and disable damaging components if any
        var cols = GetComponentsInChildren<Collider>(true);
        foreach (var c in cols)
        {
            if (c != null) c.isTrigger = false;
            c.enabled = false;
        }

        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            r.enabled = false;
        }

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
