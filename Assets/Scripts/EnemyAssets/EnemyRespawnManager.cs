using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyRespawnManager : MonoBehaviour
{
    public enum RespawnType
    {
        Respawnable,
        Unrespawnable
    }

    [Header("Settings")]
    public RespawnType respawnType = RespawnType.Respawnable;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool isDead = false;

    private EnemyStatController stats;
    private Animator anim;
    private UnityEngine.AI.NavMeshAgent agent;

    // Global list to track all enemies
    public static List<EnemyRespawnManager> allEnemies = new List<EnemyRespawnManager>();

    private void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        
        stats = GetComponent<EnemyStatController>();
        anim = GetComponentInChildren<Animator>();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    }

    private void OnEnable()
    {
        if (!allEnemies.Contains(this))
        {
            allEnemies.Add(this);
        }
    }

    private void OnDisable()
    {
        // We do NOT remove from the list on Disable, because we disable the object to "kill" it.
        // We only remove on Destroy.
    }

    private void OnDestroy()
    {
        if (allEnemies.Contains(this))
        {
            allEnemies.Remove(this);
        }
    }

    public void SetDead(bool dead)
    {
        isDead = dead;
    }

    public void ResetEnemy()
    {
        // If Unrespawnable and dead, do not respawn
        if (respawnType == RespawnType.Unrespawnable && isDead)
        {
            return;
        }

        // Otherwise (Respawnable OR (Unrespawnable but alive)), reset
        
        // 1. Re-enable object
        gameObject.SetActive(true);

        // 2. Reset Position
        if (agent != null)
        {
            // Ensure agent is enabled so Warp works
            agent.enabled = true;

            // Find nearest point on NavMesh to startPosition to ensure valid placement
            UnityEngine.AI.NavMeshHit hit;
            // Search within 2.0f radius. Adjust if needed.
            if (UnityEngine.AI.NavMesh.SamplePosition(startPosition, out hit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                agent.Warp(startPosition);
            }

            agent.transform.rotation = startRotation;
            
            // Reset agent state
            // Important: ResetPath and stop agent to clear any stale state
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            
            agent.updatePosition = true;
            agent.updateRotation = true;
        }
        else
        {
            transform.position = startPosition;
            transform.rotation = startRotation;
        }

        // 3. Reset Stats
        if (stats != null)
        {
            stats.ResetStats();
        }

        // 4. Reset Animator
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        // 5. Re-enable all MonoBehaviours (AI, HitHandler, etc.) that were disabled by DeathHandler
        MonoBehaviour[] scripts = GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var script in scripts)
        {
            script.enabled = true;
        }

        // Reset HitHandler specifically
        var hitHandler = GetComponent<EnemyHitHandler>();
        if (hitHandler) hitHandler.ResetHitHandler();

        // Reset DeathHandler specifically
        var deathHandler = GetComponent<EnemyDeathHandler>();
        if (deathHandler) deathHandler.ResetDeathHandler();

        // Reset Weapons (ensure damage is disabled)
        var weapons = GetComponentsInChildren<Weapon>(true);
        foreach (var w in weapons)
        {
            w.canDamage = false;
            w.EndAttack(); // Clear any hit lists if your weapon script has this
        }

        // NEW: Reset Boss logic/state if this is a boss
        var boss = GetComponent<BossController>();
        if (boss) boss.ResetBossOnRespawn();

        var dragon = GetComponent<DragonController>();
        if (dragon) dragon.ResetBossOnRespawn();

        // Reset Princess boss
        var princess = GetComponent<PrincessController>();
        if (princess) princess.ResetBossOnRespawn();

        // 6. Reset Flags
        isDead = false;
    }

    // Static method to be called by Player/GameManager
    public static void RespawnAllEnemies()
    {
        foreach (var enemy in allEnemies)
        {
            if (enemy != null)
            {
                enemy.ResetEnemy();
            }
        }
    }
}
