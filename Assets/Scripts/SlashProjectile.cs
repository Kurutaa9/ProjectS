using UnityEngine;

public class SlashProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 15f;
    public float damage = 20f;
    public float lifetime = 5f;

    private bool hasHit = false;
    private Vector3 moveDirection;
    private bool useWorldDirection = false;

    public void SetDirection(Vector3 dir)
    {
        moveDirection = dir.normalized;
        useWorldDirection = true;
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (useWorldDirection)
        {
            transform.position += moveDirection * speed * Time.deltaTime;
        }
        else
        {
            // Move forward relative to rotation (default behavior)
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return; // Optional: if you want it to only hit one thing. 
                            // For a slash, maybe it hits everything in its path? 
                            // Let's allow it to hit multiple things but only damage the player once.

        if (other.CompareTag("Player"))
        {
            var stats = other.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.TakeDamage(damage);
                hasHit = true; // If we want to destroy on impact, set this.
                
                // If we want the slash to go THROUGH the player, don't destroy.
                // If we want it to stop, destroy.
                // Usually slashes go through.
            }
        }
    }
}
