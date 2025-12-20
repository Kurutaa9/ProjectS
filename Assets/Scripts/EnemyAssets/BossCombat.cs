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
        
        [System.Serializable]
        public class SoundEffect
        {
            public AudioClip clip;
            public float delay;
            [Range(0f, 1f)] public float volume = 1f;
        }

        [Header("Audio")]
        public AudioClip attackSound;
        [Tooltip("Delay in seconds before playing the sound")]
        public float soundDelay = 0f;
        public List<SoundEffect> soundEffects = new List<SoundEffect>();
    }

    public Animator anim;
    public List<Weapon> weapons = new List<Weapon>();
    public AudioSource audioSource;

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
        if (!audioSource) audioSource = GetComponent<AudioSource>();

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

        // Play Sound
        PlayAttackSounds(data);


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

    private IEnumerator PlaySoundDelayed(AudioClip clip, float delay, float volume = 1f)
    {
        yield return new WaitForSeconds(delay);
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    private void PlayAttackSounds(AttackData data)
    {
        if (audioSource == null) return;

        // 1. Play sounds from AttackSO (if assigned)
        if (data.attackSO != null)
        {
            // New list of sounds
            if (data.attackSO.soundEffects != null)
            {
                foreach (var sfx in data.attackSO.soundEffects)
                {
                    if (sfx.clip != null)
                    {
                        if (sfx.delay > 0)
                            StartCoroutine(PlaySoundDelayed(sfx.clip, sfx.delay, sfx.volume));
                        else
                            audioSource.PlayOneShot(sfx.clip, sfx.volume);
                    }
                }
            }
        }

        // 2. Play sounds from AttackData (BossCombat inspector)
        // Legacy single sound
        if (data.attackSound != null)
        {
            if (data.soundDelay > 0)
                StartCoroutine(PlaySoundDelayed(data.attackSound, data.soundDelay));
            else
                audioSource.PlayOneShot(data.attackSound);
        }
        // New list of sounds
        if (data.soundEffects != null)
        {
            foreach (var sfx in data.soundEffects)
            {
                if (sfx.clip != null)
                {
                    if (sfx.delay > 0)
                        StartCoroutine(PlaySoundDelayed(sfx.clip, sfx.delay, sfx.volume));
                    else
                        audioSource.PlayOneShot(sfx.clip, sfx.volume);
                }
            }
        }
    }
}
