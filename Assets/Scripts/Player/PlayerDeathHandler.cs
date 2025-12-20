using System.Collections;
using UnityEngine;

public class PlayerDeathHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private PlayerController playerController;

    [Header("Settings")]
    public string deathStateName = "Death";
    public float respawnDelay = 3.0f;

    [Header("Audio")]
    public AudioSource audioSource;
    public System.Collections.Generic.List<PlayerStats.SoundEffect> respawnSounds = new System.Collections.Generic.List<PlayerStats.SoundEffect>();

    private bool isDying = false;
    private Vector3 startPosition;
    private Quaternion startRotation;

    private void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        // Cache initial position
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    private void OnEnable()
    {
        if (playerStats != null) playerStats.OnDeath.AddListener(OnPlayerDeath);
    }

    private void OnDisable()
    {
        if (playerStats != null) playerStats.OnDeath.RemoveListener(OnPlayerDeath);
    }

    private void OnPlayerDeath()
    {
        if (isDying) return;
        isDying = true;

        if (playerController != null)
        {
            playerController.HandlePlayerDeath();
        }

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        yield return null; 

        float animationDuration = 0f;

        if (playerAnimator != null)
        {
            playerAnimator.Play(deathStateName, 0, 0f);
            
            yield return null; 

            AnimatorStateInfo info = playerAnimator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(deathStateName))
            {
                animationDuration = info.length;
            }
            else
            {
                animationDuration = 2f;
            }
        }

        if (animationDuration > 0)
        {
            yield return new WaitForSeconds(animationDuration);
        }
        yield return new WaitForSeconds(respawnDelay);


        // --- Respawn Logic ---
        // Disable CharacterController to prevent physics glitches during teleport
        if (playerController != null && playerController.controller != null)
            playerController.controller.enabled = false;

        // 5. Check Checkpoint
        if (CheckpointManager.HasCheckpoint)
        {
            transform.position = CheckpointManager.CheckpointPosition;
            transform.rotation = CheckpointManager.CheckpointRotation;
        }
        else
        {
            transform.position = startPosition;
            transform.rotation = startRotation;
        }

        if (playerController != null && playerController.controller != null)
            playerController.controller.enabled = true;

        // 6. Reset Stats
        if (playerStats != null)
        {
            playerStats.ResetStats();
        }

        // 7. Unlock Controls
        if (playerController != null)
        {
            playerController.RespawnPlayer();
        }

        PlaySounds(respawnSounds);
        isDying = false;
    }

    private IEnumerator PlaySoundDelayed(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void PlaySounds(System.Collections.Generic.List<PlayerStats.SoundEffect> sounds)
    {
        if (audioSource == null || sounds == null) return;
        foreach (var sfx in sounds)
        {
            if (sfx.clip != null)
            {
                if (sfx.delay > 0)
                    StartCoroutine(PlaySoundDelayed(sfx.clip, sfx.delay));
                else
                    audioSource.PlayOneShot(sfx.clip);
            }
        }
    }
}
