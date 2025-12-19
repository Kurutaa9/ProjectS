using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class Projectile : MonoBehaviour
{
    [Header("Damage")]
    public float damage = 15f;
    public Transform owner; // the mage who fired this

    [Header("Lifetime")]
    public float maxLifetime = 10f;
    private float spawnTime;

    [Header("VFX (optional)")]
    public GameObject hitVFX;
    public float vfxDestroyDelay = 2f;

    private bool hasHit = false;

    private void Start()
    {
        spawnTime = Time.time;
        var col = GetComponent<SphereCollider>();
        if (col) col.isTrigger = true;
    }

    private void Update()
    {
        // Destroy after max lifetime
        if (Time.time - spawnTime >= maxLifetime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // Ignore owner
        if (owner != null && other.transform.root == owner) return;

        // Check if it's the player
        if (other.CompareTag("Player"))
        {
            var playerStats = other.GetComponent<PlayerStats>();
            if (playerStats)
            {
                playerStats.TakeDamage(damage);
                hasHit = true;
                OnHit();
                return;
            }
        }

        // Check if it hit something solid (wall, ground, etc.)
        if (other.GetComponent<Collider>())
        {
            hasHit = true;
            OnHit();
        }
    }

    private void OnHit()
    {
        if (hitVFX)
        {
            var vfx = Instantiate(hitVFX, transform.position, Quaternion.identity);
            Destroy(vfx, vfxDestroyDelay);
        }

        Destroy(gameObject);
    }
}