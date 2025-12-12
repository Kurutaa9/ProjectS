using System.Collections;
using UnityEngine;

// Plays the player's death animation, then respawns at last checkpoint (or initial position if none).
public class PlayerDeathHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private PlayerController playerController;

    [Header("Animation")] 
    [Tooltip("Exact Animator state name to play on death")] public string deathStateName = "Death";
    [Tooltip("Animator layer index where the death state resides")] public int animatorLayer = 0;
    [Tooltip("Extra delay after death animation completes before respawn (seconds)")] public float respawnDelay = 0.25f;
    [Tooltip("Animator state to force after respawn to exit death")] public string idleStateName = "Idle";

    [Header("Respawn")]
    [Tooltip("Seconds of invincibility after respawn")] public float postRespawnIFrames = 1.0f;

    private bool isDying = false;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    // cached runtime components/state to disable/restore during death
    private CharacterController cachedCC;
    private bool ccWasEnabled = true;
    private MonoBehaviour cachedRootMotionComp;
    private bool rootMotionWasEnabled = false;
    private bool animatorApplyRootMotionWas = false;

    private void Awake()
    {
        if (!playerStats) playerStats = GetComponent<PlayerStats>();
        if (!playerController) playerController = GetComponent<PlayerController>();
        if (!playerAnimator) playerAnimator = GetComponentInChildren<Animator>();
        // cache CharacterController and any root motion controller on the player
        cachedCC = GetComponent<CharacterController>();
        // Try to find an existing script that applies root motion (common name in project)
        cachedRootMotionComp = GetComponent<RootMotionController>();
        if (cachedRootMotionComp == null)
            cachedRootMotionComp = GetComponentInChildren<RootMotionController>();

        if (playerAnimator != null)
            animatorApplyRootMotionWas = playerAnimator.applyRootMotion;

        // Capture the initial spawn point for fallback
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    private void OnEnable()
    {
        if (playerStats) playerStats.OnDeath.AddListener(OnPlayerDeath);
    }

    private void OnDisable()
    {
        if (playerStats) playerStats.OnDeath.RemoveListener(OnPlayerDeath);
    }

    private void OnPlayerDeath()
    {
        if (isDying) return;
        isDying = true;

        // Lock inputs and stop actions
        if (playerController)
        {
            playerController.inputsLocked = true;
            playerController.attackLocked = true;
            playerController.isSprinting = false;

            // Interrupt combo and restore base animator controller
            if (playerController.playerCombat != null)
            {
                playerController.playerCombat.InterruptCombo();
            }
            if (playerAnimator != null && playerController != null)
            {
                var baseCtrl = playerController.GetComponent<PlayerController>();
                // PlayerController caches base in baseAnimatorController; use it
                // (Animator is the same instance referenced by PlayerController)
                // Ensure override from combo is removed so "Death" state exists
            }
        }


        // Immediately disable CharacterController movement so physics/move calls stop
        if (cachedCC != null)
        {
            ccWasEnabled = cachedCC.enabled;
            cachedCC.enabled = false;
        }

        // Disable any root-motion applier component so the animator cannot move the parent
        if (cachedRootMotionComp != null)
        {
            rootMotionWasEnabled = cachedRootMotionComp.enabled;
            cachedRootMotionComp.enabled = false;
        }

        // Prevent animator root motion from being applied while dying (we're teleporting on respawn)
        if (playerAnimator != null)
        {
            animatorApplyRootMotionWas = playerAnimator.applyRootMotion;
            playerAnimator.applyRootMotion = false;
        }

        if (playerAnimator != null && playerController != null)
        {
            var pc = playerController;
            // set animator to base controller cached by PlayerController
            if (pc != null)
            {
                // safe restore
                var baseCtrl = pc.GetComponent<PlayerController>();
                if (baseCtrl != null)
                {
                    // use the cached baseAnimatorController
                    if (baseCtrl.anim != null)
                    {
                        var baseAC = baseCtrl.GetType()
                            .GetField("baseAnimatorController", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            ?.GetValue(baseCtrl) as RuntimeAnimatorController;
                        if (baseAC != null)
                            playerAnimator.runtimeAnimatorController = baseAC;
                    }
                }
            }
        }

        playerAnimator.Play(deathStateName, animatorLayer, 0f);

        StartCoroutine(DeathThenRespawnRoutine());
    }

    private IEnumerator DeathThenRespawnRoutine()
    {
        // Wait for death state to finish
        yield return null;
        if (playerAnimator && !string.IsNullOrEmpty(deathStateName))
        {
            int deathHash = Animator.StringToHash(deathStateName);
            float safety = 10f;
            float t = 0f;
            while (t < safety)
            {
                var st = playerAnimator.GetCurrentAnimatorStateInfo(animatorLayer);
                if (st.shortNameHash == deathHash && st.normalizedTime >= 0.99f)
                    break;
                t += Time.deltaTime;
                yield return null;
            }
        }

        if (respawnDelay > 0f) yield return new WaitForSeconds(respawnDelay);

        // Respawn at last checkpoint if available, else at initial
        Vector3 respawnPos = CheckpointManager.HasCheckpoint ? CheckpointManager.CheckpointPosition : initialPosition;
        Quaternion respawnRot = CheckpointManager.HasCheckpoint ? CheckpointManager.CheckpointRotation : initialRotation;

        // Teleport player (handle CharacterController safely to avoid snap-back)
        var cc = (playerController != null) ? playerController.controller : null;
        if (cc != null)
        {
            // ensure controller is disabled while teleporting (if not already)
            bool prev = cc.enabled;
            cc.enabled = false;
            transform.SetPositionAndRotation(respawnPos, respawnRot);
            cc.enabled = prev;
        }
        else
        {
            transform.SetPositionAndRotation(respawnPos, respawnRot);
        }

        // Restore stats then apply brief invincibility (order matters: heal before i-frames)
        if (playerStats)
        {
            RestoreFull(playerStats);
            playerStats.SetInvincible(true);
        }

        // Force reset to an idle/locomotion state so we don't remain in death
        if (playerAnimator && !string.IsNullOrEmpty(idleStateName))
        {
            foreach (var p in playerAnimator.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Trigger)
                    playerAnimator.ResetTrigger(p.name);
            }
            playerAnimator.Play(idleStateName, animatorLayer, 0f);
        }

        // Small delay to ensure position is settled, then re-enable inputs
        yield return null;
        if (playerController)
        {
            playerController.inputsLocked = false;
            playerController.attackLocked = false;
        }

        // Restore CharacterController and root motion component states
        if (cachedCC != null)
        {
            cachedCC.enabled = ccWasEnabled;
        }

        if (cachedRootMotionComp != null)
        {
            cachedRootMotionComp.enabled = rootMotionWasEnabled;
        }

        if (playerAnimator != null)
        {
            playerAnimator.applyRootMotion = animatorApplyRootMotionWas;
        }

        if (postRespawnIFrames > 0f && playerStats)
        {
            yield return new WaitForSeconds(postRespawnIFrames);
            playerStats.SetInvincible(false);
        }

        isDying = false;
    }

    private void RestoreFull(PlayerStats stats)
    {
        // Top up to max without exceeding it
        float missingHealth = Mathf.Max(0f, stats.GetMaxHealth() - stats.GetCurrentHealth());
        if (missingHealth > 0f)
        {
            stats.TakeDamage(-missingHealth);
        }

        float missingStamina = Mathf.Max(0f, stats.GetMaxStamina() - stats.GetCurrentStamina());
        if (missingStamina > 0f)
        {
            stats.ConsumeStamina(-missingStamina);
        }
    }
}
