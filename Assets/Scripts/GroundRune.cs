using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GroundRune : MonoBehaviour
{
    [Header("Settings")]
    public float delay = 1.5f; // Time player has to dodge
    public float damage = 20f;
    public float radius = 3f;

    [Header("Visuals")]
    public GameObject explosionVFX; // Impact/Explosion particle prefab
    public GameObject warningVisual; // Warning particle prefab or object

    public void Initialize(float dmg, float rad, float del)
    {
        damage = dmg;
        radius = rad;
        delay = del;
    }

    void Start()
    {
        StartCoroutine(ExplodeRoutine());
    }

    IEnumerator ExplodeRoutine()
    {
        // 1. Warning Phase
        if (warningVisual)
        {
            warningVisual.SetActive(true);
            var ps = warningVisual.GetComponent<ParticleSystem>();
            if (ps) ps.Play();
        }
        
        yield return new WaitForSeconds(delay);

        // 2. Explosion Phase
        if (warningVisual)
        {
            var ps = warningVisual.GetComponent<ParticleSystem>();
            if (ps) 
            {
                ps.Stop(); // Stop emitting
                // Don't disable immediately if it has trailing particles, 
                // but usually we want the warning to disappear.
                warningVisual.SetActive(false); 
            }
            else
            {
                warningVisual.SetActive(false);
            }
        }

        if (explosionVFX) 
        {
            GameObject vfx = Instantiate(explosionVFX, transform.position, Quaternion.identity);
            var ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.loop = false;
                Destroy(vfx, main.duration - 0.2F);
            }
            else
            {
                Destroy(vfx);
            }
        }

        // 3. Damage Logic
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                var stats = hit.GetComponent<PlayerStats>();
                if (stats) stats.TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}