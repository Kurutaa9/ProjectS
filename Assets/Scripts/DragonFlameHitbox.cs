using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DragonFlameHitbox : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damage = 30f;
    public Transform ownerRoot; // dragon root to avoid self-hit
    
    [Header("Particle Effect")]
    public ParticleSystem flameParticles;
    
    [Header("Hitbox Timing")]
    [Tooltip("Delay before hitbox becomes active after emission starts")]
    public float activationDelay = 0.2f;
    [Tooltip("How long the hitbox stays active")]
    public float activeDuration = 2f;
    
    private bool isActive = false;
    private HashSet<int> hitTargets = new HashSet<int>();
    private Collider hitboxCollider;

    void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        hitboxCollider.isTrigger = true;
        hitboxCollider.enabled = false; // start disabled
        
        if (ownerRoot == null)
            ownerRoot = transform.root;
            
        if (flameParticles != null)
            flameParticles.Stop();
    }

    public void ActivateFlame()
    {
        StartCoroutine(FlameSequence());
    }

    private IEnumerator FlameSequence()
    {
        // Clear previous hits
        hitTargets.Clear();
        
        // Start particle emission
        if (flameParticles != null)
        {
            flameParticles.Play();
        }
        
        // Wait for activation delay
        yield return new WaitForSeconds(activationDelay);
        
        // Enable hitbox
        isActive = true;
        if (hitboxCollider != null)
            hitboxCollider.enabled = true;
        
        // Keep hitbox active for duration
        yield return new WaitForSeconds(activeDuration);
        
        // Deactivate
        DeactivateFlame();
    }

    public void DeactivateFlame()
    {
        isActive = false;
        
        if (hitboxCollider != null)
            hitboxCollider.enabled = false;
            
        if (flameParticles != null)
            flameParticles.Stop();
            
        hitTargets.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;
        
        // Prevent self-hits
        if (ownerRoot != null && other.transform.root == ownerRoot) return;
        
        // Try to damage player
        var player = other.GetComponent<PlayerStats>() ?? other.GetComponentInParent<PlayerStats>();
        if (player != null)
        {
            int key = player.GetInstanceID();
            if (hitTargets.Contains(key)) return;
            
            player.TakeDamage(damage);
            hitTargets.Add(key);
        }
    }

    // Optional: continuous damage while staying in flames
    private void OnTriggerStay(Collider other)
    {
        // Uncomment if you want damage-over-time instead of single hit
        // if (!isActive) return;
        // ... damage logic with timer
    }
}
