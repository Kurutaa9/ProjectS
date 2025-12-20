using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Ensure a Collider exists on the weapon (typically set as Trigger)
[RequireComponent(typeof(Collider))]
public class Weapon : MonoBehaviour
{
    public float damage;
    public float stunChanceMultiplier = 1.0f;
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

    [Header("Visual Effects")]
    [SerializeField] private TrailRenderer normalTrail;
    [SerializeField] private TrailRenderer heavyTrail;
    [SerializeField] private Transform vfxSpawnPoint;

    [System.Serializable]
    public class WeaponVFXConfig
    {
        public GameObject vfxPrefab;
        [Tooltip("Optional prefab with a collider/damage script to spawn alongside the VFX.")]
        public GameObject hitBoxPrefab;
        [Tooltip("Delay in seconds (relative to VFX spawn) before the hitbox spawns.")]
        public float hitBoxDelay = 0f;
        [Tooltip("Duration in seconds for the hitbox to remain active.")]
        public float hitBoxDuration = 0.5f;
        [Tooltip("Multiplier applied to the weapon's damage for this hitbox (e.g. 0.5 for half damage, 2.0 for double).")]
        public float damageMultiplier = 1.0f;
        public Vector3 positionOffset;
        public Vector3 rotationOffset;
        public Vector3 scale = Vector3.one;
        [Tooltip("Delay in seconds before the VFX actually spawns.")]
        public float startDelay = 0f;
        [Tooltip("Duration in seconds before the VFX is destroyed. If 0, uses the ParticleSystem's duration.")]
        public float lifeTime = 0f;
        [Tooltip("If greater than 0, stops the particle emission after this many seconds.")]
        public float stopEmissionAfter = 0f;
        [Tooltip("Normalized time (0.0 to 1.0) of the attack animation when the VFX should trigger.")]
        [Range(0f, 1f)]
        public float triggerPoint = 0.9f;
    }

    [Header("VFX Configurations")]
    public WeaponVFXConfig heavyAttackVFX;
    public WeaponVFXConfig lightAttackVFX;

    private PlayerStats playerStats;
    private EnemyStatController enemyStats;

    void Awake()
    {
        if (ownerRoot == null)
        {
            ownerRoot = transform.root;
        }

        // Cache stats components
        if (ownerRoot != null)
        {
            playerStats = ownerRoot.GetComponent<PlayerStats>();
            enemyStats = ownerRoot.GetComponent<EnemyStatController>();
        }

        if (autoAssignTeamFromRootTag && ownerRoot != null)
        {
            if (ownerRoot.CompareTag("Enemy")) 
            {
                ownerTeam = Team.Enemy;
            }
            else if (ownerRoot.CompareTag("Player")) 
            {
                ownerTeam = Team.Player;
            }
            else
            {
                // Fallback: Check for known components if tag is missing
                if (ownerRoot.GetComponent<EnemyStatController>() != null || ownerRoot.GetComponent<BossController>() != null || ownerRoot.GetComponent<DragonController>() != null)
                {
                    ownerTeam = Team.Enemy;
                }
                else if (ownerRoot.GetComponent<PlayerStats>() != null)
                {
                    ownerTeam = Team.Player;
                }
            }
        }

        // Ensure trails are off at start
        if (normalTrail) normalTrail.emitting = false;
        if (heavyTrail) heavyTrail.emitting = false;
    }

    public void StartAttack()
    {
        hitVictimIds.Clear();
    }

    public void EnableTrail(bool isHeavy)
    {
        if (isHeavy)
        {
            if (heavyTrail) heavyTrail.emitting = true;
            if (normalTrail) normalTrail.emitting = false;
        }
        else
        {
            if (normalTrail) normalTrail.emitting = true;
            if (heavyTrail) heavyTrail.emitting = false;
        }
    }

    public void DisableTrails()
    {
        if (normalTrail) normalTrail.emitting = false;
        if (heavyTrail) heavyTrail.emitting = false;
    }

    public void PlayAttackVFX(bool isHeavy)
    {
        WeaponVFXConfig config = isHeavy ? heavyAttackVFX : lightAttackVFX;
        
        if (config != null && config.vfxPrefab != null)
        {
            StartCoroutine(SpawnVFXRoutine(config));
        }
    }

    // Keep old method for compatibility if needed, but redirect it
    public void PlayHeavyParticle()
    {
        PlayAttackVFX(true);
    }

    private IEnumerator SpawnVFXRoutine(WeaponVFXConfig config)
    {
        if (config.startDelay > 0)
        {
            yield return new WaitForSeconds(config.startDelay);
        }

        // Determine base position and rotation
        Vector3 basePos = (vfxSpawnPoint != null) ? vfxSpawnPoint.position : transform.position;
        Quaternion baseRot = (vfxSpawnPoint != null) ? vfxSpawnPoint.rotation : transform.rotation;

        // Apply offsets
        // Position offset is applied relative to the rotation
        Vector3 finalPos = basePos + (baseRot * config.positionOffset);
        // Rotation offset is applied on top of the base rotation
        Quaternion finalRot = baseRot * Quaternion.Euler(config.rotationOffset);

        // Instantiate the VFX prefab
        GameObject vfx = Instantiate(config.vfxPrefab, finalPos, finalRot);
        
        // Apply scale
        vfx.transform.localScale = config.scale;

        // Instantiate the HitBox prefab if assigned
        if (config.hitBoxPrefab != null)
        {
            if (config.hitBoxDelay > 0)
            {
                StartCoroutine(SpawnHitBoxDelayed(config, finalPos, finalRot));
            }
            else
            {
                CreateHitBox(config, finalPos, finalRot);
            }
        }

        ParticleSystem ps = vfx.GetComponent<ParticleSystem>();

        // Handle early emission stop if requested
        if (config.stopEmissionAfter > 0 && ps != null)
        {
            StartCoroutine(StopEmissionRoutine(ps, config.stopEmissionAfter));
        }

        // Determine destruction time
        float destroyTime = 2.0f; // Default fallback

        if (config.lifeTime > 0)
        {
            destroyTime = config.lifeTime;
        }
        else
        {
            // Attempt to calculate from ParticleSystem
            if (ps != null)
            {
                destroyTime = ps.main.duration + ps.main.startLifetime.constantMax;
            }
        }

        Destroy(vfx, destroyTime);
    }

    private IEnumerator SpawnHitBoxDelayed(WeaponVFXConfig config, Vector3 pos, Quaternion rot)
    {
        yield return new WaitForSeconds(config.hitBoxDelay);
        CreateHitBox(config, pos, rot);
    }

    private void CreateHitBox(WeaponVFXConfig config, Vector3 pos, Quaternion rot)
    {
        GameObject hitBox = Instantiate(config.hitBoxPrefab, pos, rot);
        hitBox.transform.localScale = config.scale;
        
        // Try to configure the hitbox with damage info if it has a compatible component
        var damageDealer = hitBox.GetComponent<DamageHitbox>();
        if (damageDealer != null)
        {
            damageDealer.Setup(this.damage * config.damageMultiplier, this.ownerRoot, this.ownerTeam);
        }
        else
        {
            // Fallback for other types if needed, or legacy support
            var dragonHitbox = hitBox.GetComponent<DragonFlameHitbox>();
            if (dragonHitbox != null)
            {
                dragonHitbox.damage = this.damage * config.damageMultiplier;
                dragonHitbox.ownerRoot = this.ownerRoot;
                dragonHitbox.ActivateFlame(); // Assuming we want to activate it immediately
            }
        }
        
        // If the hitbox doesn't destroy itself, destroy it after the specified duration
        Destroy(hitBox, config.hitBoxDuration > 0 ? config.hitBoxDuration : 0.5f);
    }

    private IEnumerator StopEmissionRoutine(ParticleSystem ps, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (ps != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    public void EndAttack()
    {
        hitVictimIds.Clear();
        DisableTrails();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!canDamage) return;

        // Prevent self-hits: ignore colliders that belong to the same root as the owner
        if (ownerRoot != null && other.transform.root == ownerRoot) return;

        // Calculate damage with stats and variation
        float finalDamage = this.damage;
        float variation = Random.Range(0.85f, 1.15f); // +/- 15% variation

        if (ownerTeam == Team.Player && playerStats != null)
        {
            // Player: Base Damage + Weapon Damage
            finalDamage = (playerStats.GetBaseDamage() + this.damage) * variation;
        }
        else if (ownerTeam == Team.Enemy && enemyStats != null)
        {
            // Enemy: Base Damage + Weapon Damage (Additive, consistent with Player)
            finalDamage = (enemyStats.GetBaseDamage() + this.damage) * variation;
        }
        else
        {
            // Fallback
            finalDamage = this.damage * variation;
        }

        // Note: don't rely solely on collider tags; many setups tag the root only.
        // Instead, detect valid target components on the hit object or its parents.
        if (ownerTeam == Team.Player)
        {
            var enemy = other.GetComponent<EnemyStatController>() ?? other.GetComponentInParent<EnemyStatController>();
            if (enemy != null)
            {
                int key = enemy.GetInstanceID();
                if (hitVictimIds.Contains(key)) return;
                enemy.TakeDamage(finalDamage, stunChanceMultiplier);
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
                player.TakeDamage(finalDamage);
                hitVictimIds.Add(key);
                return;
            }
        }
    }

}
