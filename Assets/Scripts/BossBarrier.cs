using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BossBarrier : MonoBehaviour
{
    [Tooltip("Tag used to identify the player.")]
    public string playerTag = "Player";

    [Tooltip("Optional collider used to block passage. If null, the component's Collider will be used.")]
    public Collider blockingCollider;

    [Tooltip("Assign the MAW (boss) EnemyStatController so the barrier can react to its death. If null, no boss-death behaviour will be applied.")]
    public EnemyStatController bossStats;

    [Tooltip("If true the barrier GameObject will be destroyed when the boss dies. If false the collider will be disabled so players can pass.")]
    public bool destroyBarrierOnBossDeath = true;

    // Tracks whether the player has entered at least once
    private bool playerHasEntered = false;
    // Barrier locked after the first enter->exit cycle
    private bool barrierLocked = false;

    private void Reset()
    {
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    private void OnValidate()
    {
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    private void Awake()
    {
        if (blockingCollider == null)
        {
            blockingCollider = GetComponent<Collider>();
        }

        // Ensure the collider used for detection starts as a trigger
        if (blockingCollider != null)
        {
            blockingCollider.isTrigger = true;
        }
    }

    private void OnEnable()
    {
        // If bossStats is assigned, hook its death event.
        // Do NOT check current health here because EnemyStatController.Start
        // may initialize health after this OnEnable runs.
        if (bossStats != null)
        {
            bossStats.OnDeath?.AddListener(OnBossDeath);
        }
    }

    // Run after all Awake/OnEnable/Start ordering on other objects so health is initialized
    private void Start()
    {
        if (bossStats != null)
        {
            // Now it's safe to check current health (EnemyStatController.Start should have run)
            if (bossStats.GetCurrentHealth() <= 0f)
            {
                OnBossDeath();
            }
        }
    }

    private void OnDisable()
    {
        if (bossStats != null)
        {
            bossStats.OnDeath.RemoveListener(OnBossDeath);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (barrierLocked) return;
        if (!other.CompareTag(playerTag)) return;

        // mark that the player has entered at least once
        playerHasEntered = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (barrierLocked) return;
        if (!other.CompareTag(playerTag)) return;

        // Only lock barrier after a prior enter
        if (playerHasEntered)
        {
            ActivateBarrier();
        }
    }

    private void ActivateBarrier()
    {
        barrierLocked = true;

        if (blockingCollider != null)
        {
            // Turn off trigger so collider becomes a physical block
            blockingCollider.isTrigger = false;
            blockingCollider.enabled = true;
        }
        else
        {
            Debug.LogWarning($"{name}: No collider available to activate as barrier.");
        }
    }

    private void OnBossDeath()
    {
        // When the assigned boss dies allow passage (or remove the barrier object).
        if (destroyBarrierOnBossDeath)
        {
            Destroy(gameObject);
        }
        else
        {
            barrierLocked = false;
            if (blockingCollider != null)
            {
                // Disable collider so players can pass through
                blockingCollider.enabled = false;
            }
            else
            {
                Debug.LogWarning($"{name}: No collider available to disable after boss death.");
            }
        }
    }
}
