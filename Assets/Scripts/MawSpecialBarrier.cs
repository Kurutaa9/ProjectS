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

    [Header("Music Settings")]
    [SerializeField] private AudioClip bossMusic;
    [SerializeField] private AudioClip explorationMusic;
    [Tooltip("Assign a specific collider to act as the music trigger. This collider will NOT be forced to isTrigger=false.")]
    [SerializeField] private Collider musicTriggerCollider;

    private bool hasSubscribed = false;
    private PlayerStats playerStats;

    void Awake()
    {
        // Ensure any colliders on this object are NOT triggers as requested
        // EXCEPTION: The music trigger collider
        var cols = GetComponentsInChildren<Collider>(true);
        foreach (var c in cols)
        {
            if (c != null && c != musicTriggerCollider) c.isTrigger = false;
        }

        // Try to auto-resolve boss stats if not assigned
        if (bossStats == null)
        {
            // Prefer a GameObject tagged "Enemy" (since we don't have a "Boss" tag)
            var bossObj = GameObject.FindGameObjectWithTag("Enemy");
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

    void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerStats = player.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.OnDeath.AddListener(OnPlayerDeath);
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
        if (playerStats != null) playerStats.OnDeath.RemoveListener(OnPlayerDeath);
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (GlobalAudioManager.Instance != null && bossMusic != null)
            {
                GlobalAudioManager.Instance.PlayMusic(bossMusic);
            }
        }
    }

    private void OnPlayerDeath()
    {
        if (GlobalAudioManager.Instance != null && explorationMusic != null)
        {
            GlobalAudioManager.Instance.PlayMusic(explorationMusic);
        }
    }

    private void OnBossDeath()
    {
        if (GlobalAudioManager.Instance != null && explorationMusic != null)
        {
            GlobalAudioManager.Instance.PlayMusic(explorationMusic);
        }
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
