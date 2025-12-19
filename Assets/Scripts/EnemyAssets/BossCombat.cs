using System.Collections;
using System.Collections.Generic;
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
        public float selectionWeight = 1f;
        [Tooltip("If assigned, the AttackSO damage overrides damagePerHit for this attack.")]
        public AttackSO attackSO;
    }

    public Animator anim;
    public List<Weapon> weapons = new List<Weapon>();

    [Header("Animator Sync")]
    [Tooltip("Keep IsAttacking true until the Animator exits this tag (helps when an attack is a chain of multiple clips/states)")]
    public bool endOnAnimatorTagExit = true;
    [Tooltip("Animator state tag used by all boss attack states")] public string attackTag = "Attack";

    [Header("Attacks")] 
    public List<AttackData> attacks = new List<AttackData>();

    [Header("Debug")] public bool logAttacks = false;

    public bool IsAttacking { get; private set; }
    public float LastRecovery { get; private set; }

    void Awake()
    {
        if (!anim) anim = GetComponentInChildren<Animator>();
        if (weapons == null || weapons.Count == 0)
        {
            Weapon w = GetComponentInChildren<Weapon>();
            if (w) weapons.Add(w);
        }

        // Default initialization if list is empty (for backward compatibility/testing)
        if (attacks.Count == 0)
        {
            attacks.Add(new AttackData { name = "Short Combo", animatorTrigger = "AttackShort", recovery = 0.5f, damagePerHit = 10f, selectionWeight = 0.5f });
            attacks.Add(new AttackData { name = "Special",     animatorTrigger = "AttackSpecial", recovery = 0.9f, damagePerHit = 20f, selectionWeight = 0.15f });
        }
    }

    public void StartRandomAttack()
    {
        if (IsAttacking) return;
        AttackData data = PickAttack();
        if (data == null) return;
        if (logAttacks) Debug.Log($"Boss starting attack: {data.name}");
        StartCoroutine(AttackRoutine(data));
    }

    public void StopAttack()
    {
        StopAllCoroutines();
        IsAttacking = false;

        if (weapons != null)
        {
            foreach (var w in weapons)
            {
                if (w)
                {
                    w.canDamage = false;
                    w.EndAttack();
                }
            }
        }

        if (anim)
        {
            // Reset triggers so they don't fire after the hit animation
            foreach (var attack in attacks)
            {
                if (!string.IsNullOrEmpty(attack.animatorTrigger))
                    anim.ResetTrigger(attack.animatorTrigger);
            }
        }
    }

    private AttackData PickAttack()
    {
        if (attacks == null || attacks.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var a in attacks) totalWeight += Mathf.Max(0f, a.selectionWeight);

        if (totalWeight <= 0.0001f) return attacks[0]; // fallback

        float r = Random.value * totalWeight;
        float current = 0f;
        foreach (var a in attacks)
        {
            current += Mathf.Max(0f, a.selectionWeight);
            if (r < current) return a;
        }
        return attacks[attacks.Count - 1];
    }

    private IEnumerator AttackRoutine(AttackData data)
    {
        IsAttacking = true;
        LastRecovery = data.recovery;
        if (anim && !string.IsNullOrEmpty(data.animatorTrigger))
        {
            foreach (var attack in attacks)
            {
                if (!string.IsNullOrEmpty(attack.animatorTrigger))
                    anim.ResetTrigger(attack.animatorTrigger);
            }
            anim.SetTrigger(data.animatorTrigger);
        }

        // Determine damage per hit
        float resolvedDamage = data.damagePerHit;
        if (data.attackSO != null) resolvedDamage = data.attackSO.damage;

        if (weapons != null)
        {
            foreach (var w in weapons)
            {
                if (w) w.damage = resolvedDamage;
            }
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
