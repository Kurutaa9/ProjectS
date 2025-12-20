using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalAppear : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign the EnemyStatController of the 3rd Boss here.")]
    public EnemyStatController bossStats;

    [Tooltip("The Portal GameObject to enable when the boss dies.")]
    public GameObject portalObject;

    void Start()
    {
        // Ensure the portal is hidden at the start
        if (portalObject != null)
        {
            portalObject.SetActive(false);
        }

        // Subscribe to the boss death event
        if (bossStats != null)
        {
            bossStats.OnDeath.AddListener(OnBossDefeated);
        }
        else
        {
            Debug.LogWarning("PortalAppear: No Boss Stats assigned!");
        }
    }

    void OnBossDefeated()
    {
        if (portalObject != null)
        {
            portalObject.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        // Clean up listener to avoid memory leaks
        if (bossStats != null)
        {
            bossStats.OnDeath.RemoveListener(OnBossDefeated);
        }
    }
}
