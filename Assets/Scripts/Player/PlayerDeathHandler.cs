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

    private bool isDying = false;
    private Vector3 startPosition;
    private Quaternion startRotation;

    private void Start()
    {
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

        isDying = false;
    }
}
