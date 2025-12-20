using UnityEngine;

public class MagicWall : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 5f;
    public float damage = 20f;
    public float lifetime = 5f;
    public float startDelay = 0.5f; // Delay before moving

    private bool hasDealtDamage = false;
    private bool isMoving = false;

    void Start()
    {
        Destroy(gameObject, lifetime + startDelay); // Extend lifetime by delay
        StartCoroutine(StartMovementRoutine());
    }

    System.Collections.IEnumerator StartMovementRoutine()
    {
        yield return new WaitForSeconds(startDelay);
        isMoving = true;
    }

    void Update()
    {
        if (!isMoving) return;

        // Move forward relative to own rotation
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasDealtDamage)
        {
            // Assuming Player has a PlayerStats component or similar
            var stats = other.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.TakeDamage(damage);
                hasDealtDamage = true;
            }
            
            // Optional: Destroy wall on impact?
            // For a "sandwich" effect, usually they pass through or stop.
            // Let's let them pass through to complete the visual of crossing.
        }
    }
}
