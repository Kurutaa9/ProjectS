using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Ensure a Collider exists on the weapon (typically set as Trigger)
[RequireComponent(typeof(Collider))]
public class Weapon : MonoBehaviour
{
    public float damage;
    private readonly List<GameObject> hitVictims = new List<GameObject>();
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
        hitVictims.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!canDamage) return;

        // Prevent self-hits: ignore colliders that belong to the same root as the owner
        if (ownerRoot != null && other.transform.root == ownerRoot) return;

        // Already hit this object in this attack window
        if (hitVictims.Contains(other.gameObject)) return;

        // Note: don't rely solely on collider tags; many setups tag the root only.
        // Instead, detect valid target components on the hit object or its parents.
        if (ownerTeam == Team.Player)
        {
            var enemy = other.GetComponent<EnemyStatController>() ?? other.GetComponentInParent<EnemyStatController>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                hitVictims.Add(other.gameObject);
                return;
            }
        }
        else if (ownerTeam == Team.Enemy)
        {
            var player = other.GetComponent<PlayerStats>() ?? other.GetComponentInParent<PlayerStats>();
            if (player != null)
            {
                player.TakeDamage(damage);
                hitVictims.Add(other.gameObject);
                return;
            }
        }
    }

}
