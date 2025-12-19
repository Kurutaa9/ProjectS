using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class animEvents : MonoBehaviour
{
    public List<Weapon> weapons = new List<Weapon>();

    [Header("Slash Trail VFX (Particle)")]
    public ParticleSystem slashTrail;
    public bool stopOnEnd = true;
    public bool clearOnRestart = true;

    [Header("Slash Trail Renderer")]
    public TrailRenderer slashTrailRenderer;
    [Tooltip("Clear trail before each new swing.")]
    public bool trailClearOnRestart = true;
    [Tooltip("Use TrailRenderer.emitting (preferred) vs enable/disable component.")]
    public bool trailUseEmittingToggle = true;
    [Tooltip("Optional delay (seconds) before stopping emission after hit window ends.")]
    public float trailStopDelay = 0f;

    void Awake()
    {
        if (weapons == null || weapons.Count == 0)
        {
            Weapon w = GetComponentInParent<Weapon>();
            if (!w) w = GetComponentInChildren<Weapon>();
            if (w) weapons.Add(w);
        }
        // Optional auto-find if child named "SlashTrail"
        if (!slashTrail)
        {
            Transform t = transform.Find("SlashTrail");
            if (t) slashTrail = t.GetComponent<ParticleSystem>();
        }
        if (!slashTrailRenderer)
        {
            // Try find first child TrailRenderer (e.g. on axe tip)
            slashTrailRenderer = GetComponentInChildren<TrailRenderer>();
        }
        if (slashTrailRenderer)
        {
            // Ensure starts off
            if (trailUseEmittingToggle) slashTrailRenderer.emitting = false;
            else slashTrailRenderer.enabled = false;
        }
    }

    // Called by Animation Event at start of hit window
    public void weaponCanDamageTrue()
    {
        weaponCanDamageTrue(0);
    }

    public void weaponCanDamageTrue(int index)
    {
        if (weapons != null && index >= 0 && index < weapons.Count && weapons[index] != null)
        {
            weapons[index].StartAttack();
            weapons[index].canDamage = true;
        }

        // Particle trail
        if (slashTrail)
        {
            if (clearOnRestart) slashTrail.Clear(true);
            slashTrail.Play(true);
        }

        // TrailRenderer
        if (slashTrailRenderer)
        {
            if (trailClearOnRestart) slashTrailRenderer.Clear();
            if (trailUseEmittingToggle) slashTrailRenderer.emitting = true;
            else slashTrailRenderer.enabled = true;
        }
    }

    // Called by Animation Event at end of hit window
    public void weaponCanDamageFalse()
    {
        weaponCanDamageFalse(0);
    }

    public void weaponCanDamageFalse(int index)
    {
        if (weapons != null && index >= 0 && index < weapons.Count && weapons[index] != null)
        {
            weapons[index].canDamage = false;
            weapons[index].EndAttack();
        }

        // Particle trail
        if (slashTrail && stopOnEnd)
        {
            slashTrail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        // TrailRenderer (allow optional delay so it tapers)
        if (slashTrailRenderer)
        {
            if (trailStopDelay <= 0f)
            {
                StopTrailRendererNow();
            }
            else
            {
                StartCoroutine(StopTrailDelayed(trailStopDelay));
            }
        }
    }

    private IEnumerator StopTrailDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        StopTrailRendererNow();
    }

    private void StopTrailRendererNow()
    {
        if (!slashTrailRenderer) return;
        if (trailUseEmittingToggle) slashTrailRenderer.emitting = false;
        else slashTrailRenderer.enabled = false;
    }

    // Optional separate animation events if you prefer (add to clips):
    public void AE_TrailStart()
    {
        if (slashTrailRenderer)
        {
            if (trailClearOnRestart) slashTrailRenderer.Clear();
            if (trailUseEmittingToggle) slashTrailRenderer.emitting = true;
            else slashTrailRenderer.enabled = true;
        }
    }
    public void AE_TrailStop()
    {
        StopTrailRendererNow();
    }
}
