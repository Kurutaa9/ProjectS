using System.Collections;
using UnityEngine;

public class BossCombat : MonoBehaviour
{
    [System.Serializable]
    public class AttackData
    {
        public string name;
        [Tooltip("Animator trigger name to fire for this attack")] public string animatorTrigger = "";
        [Tooltip("How many hits in this attack")] public int hits = 1;
        [Tooltip("Seconds before first hit (windup)")] public float windup = 0.35f;
        [Tooltip("Seconds between successive hits")] public float timeBetweenHits = 0.4f;
        [Tooltip("How long each hit window is active (enables weapon.canDamage)")] public float hitWindow = 0.2f;
        [Tooltip("Recovery time after last hit")] public float recovery = 0.6f;
        [Tooltip("Optional damage applied per hit if using Weapon component")] public float damagePerHit = 10f;
        [Range(0f, 1f)] public float selectionWeight = 1f;
    }

    public Animator anim;
    public Weapon weapon; // optional; if present, toggles canDamage and sets damage

    [Header("Animator Sync")]
    [Tooltip("Keep IsAttacking true until the Animator exits this tag (helps when an attack is a chain of multiple clips/states)")]
    public bool endOnAnimatorTagExit = true;
    [Tooltip("Animator state tag used by all boss attack states")] public string attackTag = "Attack";

    [Header("Attacks")] 
    public AttackData shortCombo = new AttackData { name = "Short Combo", animatorTrigger = "AttackShort", hits = 2, windup = 0.3f, timeBetweenHits = 0.35f, hitWindow = 0.18f, recovery = 0.5f, damagePerHit = 10f, selectionWeight = 0.5f };
    public AttackData longCombo  = new AttackData { name = "Long Combo",  animatorTrigger = "AttackLong",  hits = 5, windup = 0.35f, timeBetweenHits = 0.33f, hitWindow = 0.18f, recovery = 0.7f, damagePerHit = 8f,  selectionWeight = 0.35f };
    public AttackData special    = new AttackData { name = "Special",     animatorTrigger = "AttackSpecial", hits = 1, windup = 0.6f, timeBetweenHits = 0.4f, hitWindow = 0.25f, recovery = 0.9f, damagePerHit = 20f, selectionWeight = 0.15f };

    [Header("Attack Scriptables (optional damage override)")]
    [Tooltip("If assigned, the AttackSO damage overrides damagePerHit for the corresponding attack.")]
    public AttackSO shortAttackSO;
    public AttackSO longAttackSO;
    public AttackSO specialAttackSO;

    [Header("Debug")] public bool logAttacks = false;

    [Header("Damage Windows")]
    [Tooltip("If true, rely on animation events on the Boss' attack clips to toggle weapon.canDamage. If false, this script opens timed hit windows.")]
    public bool useAnimationEventsForDamageWindows = true;

    public bool IsAttacking { get; private set; }

    void Awake()
    {
        if (!anim) anim = GetComponentInChildren<Animator>();
        if (!weapon) weapon = GetComponentInChildren<Weapon>();
    }

    public void StartRandomAttack()
    {
        if (IsAttacking) return;
        AttackData data = PickAttack();
        if (logAttacks) Debug.Log($"Boss starting attack: {data.name}");
        StartCoroutine(AttackRoutine(data));
    }

    private AttackData PickAttack()
    {
        float w1 = Mathf.Max(0f, shortCombo.selectionWeight);
        float w2 = Mathf.Max(0f, longCombo.selectionWeight);
        float w3 = Mathf.Max(0f, special.selectionWeight);
        float total = w1 + w2 + w3;
        if (total <= 0.0001f) return shortCombo; // fallback
        float r = Random.value * total;
        if (r < w1) return shortCombo;
        r -= w1;
        if (r < w2) return longCombo;
        return special;
    }

    private IEnumerator AttackRoutine(AttackData data)
    {
        IsAttacking = true;
        if (anim && !string.IsNullOrEmpty(data.animatorTrigger))
        {
            anim.ResetTrigger("AttackShort");
            anim.ResetTrigger("AttackLong");
            anim.ResetTrigger("AttackSpecial");
            anim.SetTrigger(data.animatorTrigger);
        }

        // Initial windup
        if (data.windup > 0f) yield return new WaitForSeconds(data.windup);

        // Determine damage per hit. If an AttackSO is assigned for the selected attack,
        // prefer its damage value; otherwise use data.damagePerHit.
        float resolvedDamage = data.damagePerHit;
        if (weapon)
        {
            if (ReferenceEquals(data, shortCombo) && shortAttackSO != null) resolvedDamage = shortAttackSO.damage;
            else if (ReferenceEquals(data, longCombo) && longAttackSO != null) resolvedDamage = longAttackSO.damage;
            else if (ReferenceEquals(data, special) && specialAttackSO != null) resolvedDamage = specialAttackSO.damage;
        }

        if (weapon)
        {
            // Set damage once; animation events or code windows will toggle canDamage.
            weapon.damage = resolvedDamage;
        }

        if (useAnimationEventsForDamageWindows)
        {
            // Let the animation events drive the timing. We just wait until the
            // expected duration passes (approximate using hits/timing) before recovery.
            float totalDuration = 0f;
            // Rough estimate to avoid ending instantly even if no tag sync
            totalDuration += Mathf.Max(0f, data.hits - 1) * Mathf.Max(0f, data.timeBetweenHits);
            totalDuration += Mathf.Max(0f, data.hitWindow);
            if (totalDuration > 0f) yield return new WaitForSeconds(totalDuration);
        }
        else
        {
            for (int i = 0; i < Mathf.Max(1, data.hits); i++)
            {
                // Enable damage window
                if (weapon)
                {
                    // Clear per-swing victim list so each hit can register
                    weapon.StartAttack();
                    weapon.canDamage = true;
                }
                yield return new WaitForSeconds(Mathf.Max(0.01f, data.hitWindow));
                if (weapon) weapon.canDamage = false;

                // time to next hit (skip after last)
                if (i < data.hits - 1 && data.timeBetweenHits > 0f)
                    yield return new WaitForSeconds(data.timeBetweenHits);
            }
        }

        // Recovery
        if (data.recovery > 0f) yield return new WaitForSeconds(data.recovery);

        // Optional: wait until the Animator fully exits the attack tag. This
        // ensures that if the attack is implemented as multiple clips/states
        // (all tagged with the same attack tag), we won't end the attack early
        // and let movement resume before the last clip finishes.
        if (endOnAnimatorTagExit && anim)
        {
            // Wait at least one frame to let transitions start
            yield return null;
            // Keep waiting while the current state on layer 0 is tagged as attack
            while (anim.GetCurrentAnimatorStateInfo(0).IsTag(attackTag))
            {
                yield return null;
            }
        }

        IsAttacking = false;
    }
}
