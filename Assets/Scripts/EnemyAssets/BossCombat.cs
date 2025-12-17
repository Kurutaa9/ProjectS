using System.Collections;
using UnityEngine;

public class BossCombat : MonoBehaviour
{
    [System.Serializable]
    public class AttackData
    {
        public string name;
        [Tooltip("Animator trigger name to fire for this attack")] public string animatorTrigger = "";
        [Tooltip("Recovery time after last hit")] public float recovery = 0.6f;
        [Tooltip("Optional damage applied per hit if using Weapon component")] public float damagePerHit = 10f;
        [Range(0f, 1f)] public float selectionWeight = 1f;
    }

    public Animator anim;
    public Weapon weapon;

    [Header("Animator Sync")]
    [Tooltip("Keep IsAttacking true until the Animator exits this tag (helps when an attack is a chain of multiple clips/states)")]
    public bool endOnAnimatorTagExit = true;
    [Tooltip("Animator state tag used by all boss attack states")] public string attackTag = "Attack";

    [Header("Attacks")] 
    public AttackData shortCombo = new AttackData { name = "Short Combo", animatorTrigger = "AttackShort", recovery = 0.5f, damagePerHit = 10f, selectionWeight = 0.5f };
    public AttackData special    = new AttackData { name = "Special",     animatorTrigger = "AttackSpecial", recovery = 0.9f, damagePerHit = 20f, selectionWeight = 0.15f };

    [Header("Attack Scriptables (optional damage override)")]
    [Tooltip("If assigned, the AttackSO damage overrides damagePerHit for the corresponding attack.")]
    public AttackSO shortAttackSO;
    public AttackSO specialAttackSO;

    [Header("Debug")] public bool logAttacks = false;

    public bool IsAttacking { get; private set; }
    public float LastRecovery { get; private set; }

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

    public void StopAttack()
    {
        StopAllCoroutines();
        IsAttacking = false;

        if (weapon)
        {
            weapon.canDamage = false;
            weapon.EndAttack();
        }

        if (anim)
        {
            // Reset triggers so they don't fire after the hit animation
            anim.ResetTrigger("AttackShort");
            anim.ResetTrigger("AttackSpecial");
        }
    }

    private AttackData PickAttack()
    {
        float w1 = Mathf.Max(0f, shortCombo.selectionWeight);
        float w3 = Mathf.Max(0f, special.selectionWeight);
        float total = w1 + w3;
        if (total <= 0.0001f) return shortCombo; // fallback
        float r = Random.value * total;
        if (r < w1) return shortCombo;
        return special;
    }

    private IEnumerator AttackRoutine(AttackData data)
    {
        IsAttacking = true;
        LastRecovery = data.recovery;
        if (anim && !string.IsNullOrEmpty(data.animatorTrigger))
        {
            anim.ResetTrigger("AttackShort");
            anim.ResetTrigger("AttackSpecial");
            anim.SetTrigger(data.animatorTrigger);
        }

        // Determine damage per hit
        float resolvedDamage = data.damagePerHit;
        if (weapon)
        {
            if (ReferenceEquals(data, shortCombo) && shortAttackSO != null) resolvedDamage = shortAttackSO.damage;
            else if (ReferenceEquals(data, special) && specialAttackSO != null) resolvedDamage = specialAttackSO.damage;
            weapon.damage = resolvedDamage;
        }


        if (endOnAnimatorTagExit && anim)
        {
            // Wait for transition to start
            // yield return null;
            // yield return null;

            // Wait until we enter the attack tag (with timeout)
            float timeout = 1.0f;
            while (timeout > 0f && !anim.GetCurrentAnimatorStateInfo(0).IsTag(attackTag))
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            // Wait while in the attack tag
            while (anim.GetCurrentAnimatorStateInfo(0).IsTag(attackTag))
            {
                yield return null;
            }
        }

        IsAttacking = false;
    }
}
