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

    [Header("Rune Attack Settings")]
    public GameObject runePrefab;
    public float runeDamage = 20f;
    public float runeRadius = 3f;
    public float runeDelay = 1.5f;

    [Header("Sandwich Attack Settings")]
    public GameObject wallPrefab;
    public float wallSpawnDistance = 5f;
    public float wallSpeed = 8f;
    public float wallDamage = 25f;
    public float wallStartDelay = 1.0f; // Delay before walls start moving
    public Vector3 wallRotationOffset = Vector3.zero; // Add rotation offset

    [Header("Slash Attack Settings")]
    public GameObject slashPrefab;
    public float slashSpeed = 15f;
    public float slashDamage = 20f;
    public Vector3 slashRotationOffset = Vector3.zero; // To adjust if the prefab is rotated wrong

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

    public void SpawnRuneAtPlayer()
    {
        if (!runePrefab || !player) return;

        Vector3 spawnPos = player.position;
        
        // Raycast to find ground level so rune sits flat
        // Start raycast from slightly above player to hit ground below
        if (Physics.Raycast(player.position + Vector3.up * 1f, Vector3.down, out RaycastHit hit, 5f))
        {
            spawnPos = hit.point + Vector3.up * 0.05f; // Slight offset to avoid z-fighting
        }
        else
        {
            // Fallback if raycast fails (e.g. player jumping high): just use player Y or ground level
            spawnPos.y = transform.position.y + 0.05f; 
        }

        GameObject rune = Instantiate(runePrefab, spawnPos, Quaternion.identity);
        var runeScript = rune.GetComponent<GroundRune>();
        if (runeScript)
        {
            runeScript.Initialize(runeDamage, runeRadius, runeDelay);
        }
    }

    public void SpawnSandwichWalls()
    {
        if (!wallPrefab || !player) return;

        // Calculate direction from Boss to Player to determine "Left" and "Right"
        Vector3 dirToPlayer = (player.position - mageTransform.position).normalized;
        dirToPlayer.y = 0; // Keep it horizontal
        
        if (dirToPlayer.sqrMagnitude < 0.001f) dirToPlayer = mageTransform.forward;

        // Right vector relative to the boss-player line
        // Cross(Up, Forward) = Right
        Vector3 right = Vector3.Cross(Vector3.up, dirToPlayer).normalized; 

        // Spawn Left Wall (Left of the player from boss perspective)
        // Position: Player - Right * Distance
        Vector3 leftSpawnPos = player.position - right * wallSpawnDistance;
        // Rotation: Look at Right (move towards player)
        Quaternion leftRot = Quaternion.LookRotation(right);

        // Spawn Right Wall (Right of the player from boss perspective)
        // Position: Player + Right * Distance
        Vector3 rightSpawnPos = player.position + right * wallSpawnDistance;
        // Rotation: Look at -Right (move towards player)
        Quaternion rightRot = Quaternion.LookRotation(-right);

        SpawnWall(leftSpawnPos, leftRot);
        SpawnWall(rightSpawnPos, rightRot);
    }

    private void SpawnWall(Vector3 pos, Quaternion rot)
    {
        // Adjust height to match player or ground
        pos.y = player.position.y + 1f; // Lift slightly so it's not in the floor

        GameObject wall = Instantiate(wallPrefab, pos, rot);
        MagicWall mw = wall.GetComponent<MagicWall>();
        if (mw)
        {
            mw.speed = wallSpeed;
            mw.damage = wallDamage;
            mw.startDelay = wallStartDelay;
        }
    }

    public void FireSlashProjectile()
    {
        if (!slashPrefab || !player) return;

        // 1. Calculate direction to player (ignoring Y for horizontal slash, or include Y for aimed)
        // Usually slashes travel horizontally along the ground or at chest height.
        Vector3 dir = (player.position - projectileSpawnPoint.position).normalized;
        
        // 2. Rotation: Look at player
        Quaternion rot = Quaternion.LookRotation(dir);
        
        // 3. Apply offset (e.g. if prefab is flat on ground, might need X=90)
        rot *= Quaternion.Euler(slashRotationOffset);

        // 4. Spawn
        GameObject slash = Instantiate(slashPrefab, projectileSpawnPoint.position, rot);
        
        // 5. Setup script
        SlashProjectile sp = slash.GetComponent<SlashProjectile>();
        if (sp)
        {
            sp.speed = slashSpeed;
            sp.damage = slashDamage;
            // Force movement direction towards player, ignoring the rotation offset
            sp.SetDirection(dir);
        }
    }
}