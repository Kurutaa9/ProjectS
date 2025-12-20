using System.Collections;
using UnityEngine;

public class BossArenaController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Boss Enemy. If empty, will try to find object with tag 'Enemy'.")]
    [SerializeField] private EnemyStatController bossStats;
    
    [Tooltip("The object that blocks the exit/entry once the fight starts. This will be ENABLED when player enters, and DISABLED when boss/player dies.")]
    [SerializeField] private GameObject arenaBarrier;

    [Header("Music Settings")]
    [SerializeField] private AudioClip bossMusic;
    [SerializeField] private AudioClip explorationMusic;
    [SerializeField] private float musicFadeTime = 1.0f;

    [Header("Settings")]
    [Tooltip("Delay after boss death before the barrier disappears.")]
    [SerializeField] private float victoryDelay = 2.0f;

    private bool isFightActive = false;
    private bool isBossDefeated = false;
    private PlayerStats playerStats;

    private void Awake()
    {
        // Ensure the collider on this object is a trigger so the player can walk through it
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogWarning("BossArenaController: No Collider found on this object! Please add a Box Collider and set IsTrigger to true.");
        }
    }

    private void Start()
    {
        // 1. Setup Boss Reference
        if (bossStats == null)
        {
            GameObject bossObj = GameObject.FindGameObjectWithTag("Enemy");
            if (bossObj != null)
            {
                bossStats = bossObj.GetComponent<EnemyStatController>();
            }
        }

        if (bossStats != null)
        {
            bossStats.OnDeath.AddListener(OnBossDeath);
        }
        else
        {
            Debug.LogWarning("BossArenaController: No Boss Stats found!");
        }

        // 2. Setup Player Reference
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerStats = player.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.OnDeath.AddListener(OnPlayerDeath);
            }
        }

        // 3. Ensure Barrier is initially disabled (open)
        if (arenaBarrier != null)
        {
            if (arenaBarrier == gameObject)
            {
                Debug.LogError("BossArenaController: The 'Arena Barrier' cannot be the same object as the Controller! The Controller must remain active to handle the fight logic. Please create a separate child object for the visual barrier.");
            }
            else
            {
                arenaBarrier.SetActive(false);
            }
        }
    }

    private void OnDestroy()
    {
        // Cleanup listeners
        if (bossStats != null) bossStats.OnDeath.RemoveListener(OnBossDeath);
        if (playerStats != null) playerStats.OnDeath.RemoveListener(OnPlayerDeath);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only trigger if player enters, fight hasn't started yet, and boss is still alive
        if (!isFightActive && !isBossDefeated && other.CompareTag("Player"))
        {
            StartBossFight();
        }
    }

    private void StartBossFight()
    {
        isFightActive = true;

        // 1. Trap the player
        if (arenaBarrier != null)
        {
            arenaBarrier.SetActive(true);
        }

        // 2. Play Boss Music
        if (GlobalAudioManager.Instance != null && bossMusic != null)
        {
            GlobalAudioManager.Instance.PlayMusic(bossMusic, musicFadeTime);
        }
    }

    private void OnBossDeath()
    {
        if (!isFightActive) return; // Already handled or not active
        
        isBossDefeated = true;
        StartCoroutine(VictoryRoutine());
    }

    private IEnumerator VictoryRoutine()
    {
        yield return new WaitForSeconds(victoryDelay);

        // 1. Open the barrier
        if (arenaBarrier != null)
        {
            arenaBarrier.SetActive(false);
        }

        // 2. Return to exploration music
        if (GlobalAudioManager.Instance != null && explorationMusic != null)
        {
            GlobalAudioManager.Instance.PlayMusic(explorationMusic, musicFadeTime);
        }

        isFightActive = false;
        
        // Disable this trigger so it doesn't fire again
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    private void OnPlayerDeath()
    {
        if (!isFightActive) return;

        // Reset the arena immediately so player can return
        ResetArena();
    }

    public void ResetArena()
    {
        isFightActive = false;

        // 1. Open the barrier
        if (arenaBarrier != null)
        {
            arenaBarrier.SetActive(false);
        }

        // 2. Return to exploration music
        if (GlobalAudioManager.Instance != null && explorationMusic != null)
        {
            GlobalAudioManager.Instance.PlayMusic(explorationMusic, musicFadeTime);
        }
    }
}
