using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Ensure a Collider exists on the weapon (typically set as Trigger)
[RequireComponent(typeof(Collider))]
public class Weapon : MonoBehaviour
{
    public float damage;
    // Track victims by a stable key (component/root instanceID) to avoid multi-collider double hits
    private readonly HashSet<int> hitVictimIds = new HashSet<int>();
    public bool canDamage = false;

    public enum Team { Player, Enemy }
    [Header("Ownership / Targeting")]
    public Team ownerTeam = Team.Player;
    [Tooltip("Root transform of the character that owns this weapon. Used to avoid self-hits.")]
    public Transform ownerRoot;

    [Tooltip("When enabled, will auto-set ownerTeam based on ownerRoot tag ('Player'/'Enemy') in Awake.")]
    public bool autoAssignTeamFromRootTag = true;

    [SerializeField]
    private float attackStaminaCost;


    void Awake()
    {
        if (ownerRoot == null)
        {
            ownerRoot = transform.root;
        }

        if (autoAssignTeamFromRootTag && ownerRoot != null)
        {
            if (ownerRoot.CompareTag("Enemy")) ownerTeam = Team.Enemy;
            else if (ownerRoot.CompareTag("Player")) ownerTeam = Team.Player;
        }
    }

    public void StartAttack()
    {
        hitVictimIds.Clear();
    }

    public void EndAttack()
    {
        hitVictimIds.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!canDamage) return;

        // Prevent self-hits: ignore colliders that belong to the same root as the owner
        if (ownerRoot != null && other.transform.root == ownerRoot) return;

        // Note: don't rely solely on collider tags; many setups tag the root only.
        // Instead, detect valid target components on the hit object or its parents.
        if (ownerTeam == Team.Player)
        {
            var enemy = other.GetComponent<EnemyStatController>() ?? other.GetComponentInParent<EnemyStatController>();
            if (enemy != null)
            {
                int key = enemy.GetInstanceID();
                if (hitVictimIds.Contains(key)) return;
                enemy.TakeDamage(damage);
                hitVictimIds.Add(key);
                return;
            }
        }
        else if (ownerTeam == Team.Enemy)
        {
            var player = other.GetComponent<PlayerStats>() ?? other.GetComponentInParent<PlayerStats>();
            if (player != null)
            {
                int key = player.GetInstanceID();
                if (hitVictimIds.Contains(key)) return;
                player.TakeDamage(damage);
                hitVictimIds.Add(key);
                return;
            }
        }
    }

}
