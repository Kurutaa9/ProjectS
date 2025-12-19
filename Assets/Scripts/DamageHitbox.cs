using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DamageHitbox : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damage = 10f;
    public Transform ownerRoot; // To avoid self-damage
    public Weapon.Team ownerTeam = Weapon.Team.Player; // To know who to target

    private HashSet<int> hitTargets = new HashSet<int>();

    private void OnTriggerEnter(Collider other)
    {
        // Prevent self-hits
        if (ownerRoot != null && other.transform.root == ownerRoot) return;

        // Check for Enemy
        if (ownerTeam == Weapon.Team.Player)
        {
            var enemy = other.GetComponent<EnemyStatController>() ?? other.GetComponentInParent<EnemyStatController>();
            if (enemy != null)
            {
                int id = enemy.GetInstanceID();
                if (hitTargets.Contains(id)) return;

                enemy.TakeDamage(damage);
                hitTargets.Add(id);
                // Optional: Spawn hit VFX here
            }
        }
        // Check for Player
        else if (ownerTeam == Weapon.Team.Enemy)
        {
            var player = other.GetComponent<PlayerStats>() ?? other.GetComponentInParent<PlayerStats>();
            if (player != null)
            {
                int id = player.GetInstanceID();
                if (hitTargets.Contains(id)) return;

                player.TakeDamage(damage);
                hitTargets.Add(id);
            }
        }
    }

    public void Setup(float newDamage, Transform newOwnerRoot, Weapon.Team newOwnerTeam)
    {
        damage = newDamage;
        ownerRoot = newOwnerRoot;
        ownerTeam = newOwnerTeam;
        hitTargets.Clear();
    }
}
