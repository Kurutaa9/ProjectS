using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MageAnimEvents : MonoBehaviour
{
    [Header("References")]
    public Transform mageTransform;
    public Transform player;

    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint; // spawn point (e.g., hand or staff tip)
    public float projectileSpeed = 20f;
    public float projectileDamage = 15f;

    [Header("Spread Settings (for multi-projectile attacks)")]
    public bool useSpreadFire = false;
    [Range(1, 5)]
    public int projectileCount = 3;
    public float spreadAngle = 30f; // degrees between projectiles

    void Awake()
    {
        if (!mageTransform) mageTransform = transform;
        if (!projectileSpawnPoint) projectileSpawnPoint = transform; // fallback to root
    }

    // Called by animation event at the moment projectile should fire
    public void FireProjectile()
    {
        if (!projectilePrefab || !player) return;

        if (useSpreadFire)
        {
            FireSpreadProjectiles();
        }
        else
        {
            FireSingleProjectile();
        }
    }

    private void FireSingleProjectile()
    {
        var proj = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.identity);
        var rb = proj.GetComponent<Rigidbody>();
        var projScript = proj.GetComponent<Projectile>(); // see below for Projectile script

        if (rb)
        {
            Vector3 dir = (player.position - projectileSpawnPoint.position).normalized;
            rb.velocity = dir * projectileSpeed;
        }

        if (projScript)
        {
            projScript.damage = projectileDamage;
            projScript.owner = mageTransform;
        }
    }

    private void FireSpreadProjectiles()
    {
        Vector3 baseDir = (player.position - projectileSpawnPoint.position).normalized;
        float angleStep = spreadAngle / (projectileCount - 1);
        float startAngle = -spreadAngle / 2f;

        for (int i = 0; i < projectileCount; i++)
        {
            float angle = startAngle + (i * angleStep);
            Quaternion rot = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 dir = rot * baseDir;

            var proj = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.identity);
            var rb = proj.GetComponent<Rigidbody>();
            var projScript = proj.GetComponent<Projectile>();

            if (rb)
            {
                rb.velocity = dir * projectileSpeed;
            }

            if (projScript)
            {
                projScript.damage = projectileDamage;
                projScript.owner = mageTransform;
            }
        }
    }

    // Optional: called when a spell cast starts (e.g., to play VFX)
    public void OnSpellStart()
    {
        // Play charging VFX, sound, etc.
        Debug.Log("Spell casting started");
    }

    // Optional: called when a spell cast ends
    public void OnSpellEnd()
    {
        // Stop charging VFX, etc.
        Debug.Log("Spell casting ended");
    }
}