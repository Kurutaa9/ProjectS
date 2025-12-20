using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private CharacterStatsSO baseStats;

    private float currentHealth;
    private float currentStamina;
    [Header("Status Flags")]
    [Tooltip("When true, incoming damage is ignored (used for dodge i-frames).")]
    public bool isInvincible = false;
    public bool isDead = false;

    [Header("Stamina Regen Control")]
    [Tooltip("Seconds to wait after any stamina usage before regen starts again.")]
    public float staminaRegenDelay = 0.75f;
    private float regenBlockedUntil = 0f;

    [Header("Healing")]
    public float healAmount = 30f;
    // public int maxFlasks = 3;
    private int currentFlasks;

    [Header("Currency")]
    private int currentSouls = 0;

    [System.Serializable]
    public class SoundEffect
    {
        public AudioClip clip;
        public float delay;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [Header("Audio")]
    public AudioSource audioSource;
    public List<SoundEffect> healSounds = new List<SoundEffect>();
    public List<SoundEffect> damageSounds = new List<SoundEffect>();
    public List<SoundEffect> deathSounds = new List<SoundEffect>();

    public UnityEvent<float> OnHealthChanged;
    public UnityEvent<float> OnStaminaChanged;
    public UnityEvent<int> OnFlasksChanged;
    public UnityEvent<int> OnSoulsChanged;
    public UnityEvent OnDeath;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        currentHealth = baseStats.maxHealth;
        currentStamina = baseStats.maxStamina;
        currentFlasks = baseStats.maxFlasks;
        currentSouls = 0;

        OnHealthChanged.Invoke(currentHealth);
        OnStaminaChanged.Invoke(currentStamina);
        OnFlasksChanged.Invoke(currentFlasks);
        OnSoulsChanged.Invoke(currentSouls);
    }

    void Update()
    {
        // stamina regen with delay after last consumption
        if (currentStamina < baseStats.maxStamina && Time.time >= regenBlockedUntil)
        {
            float before = currentStamina;
            currentStamina = Mathf.Min(currentStamina + baseStats.staminaRegen * Time.deltaTime, baseStats.maxStamina);
            if (!Mathf.Approximately(currentStamina, before))
            {
                OnStaminaChanged.Invoke(currentStamina);
            }
        }
    }

    public void ConsumeStamina(float amount)
    {
        float before = currentStamina;
        currentStamina = Mathf.Max(currentStamina - amount, 0f);
        regenBlockedUntil = Time.time + staminaRegenDelay; // block regen for a short time after use
        if (!Mathf.Approximately(currentStamina, before))
        {
            OnStaminaChanged.Invoke(currentStamina);
        }
    }

    public void ResetRegenTimer()
    {
        regenBlockedUntil = Time.time + staminaRegenDelay;
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, baseStats.maxHealth);
        OnHealthChanged.Invoke(currentHealth);
        PlaySounds(healSounds);
    }

    public bool CanHeal()
    {
        return currentFlasks > 0 && currentHealth < baseStats.maxHealth;
    }

    public void ConsumeFlask()
    {
        if (currentFlasks > 0)
        {
            currentFlasks--;
            OnFlasksChanged.Invoke(currentFlasks);
        }
    }

    public void AddSouls(int amount)
    {
        currentSouls += amount;
        baseStats.currentSolsSO = currentSouls; //update the SO
        OnSoulsChanged.Invoke(currentSouls);
    }

    public int GetCurrentSouls()
    {
        return currentSouls;
    }

    public void TakeDamage(float amount)
    {
        // Ignore damage while invincible (e.g., during dodge i-frames) or if already dead
        if (isInvincible || isDead)
            return;

        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        OnHealthChanged.Invoke(currentHealth);

        // Stop all player SFX and pending delayed sounds
        if (audioSource != null) audioSource.Stop();
        StopAllCoroutines();

        if (currentHealth <= 0f)
        {
            isDead = true;
            PlaySounds(deathSounds);
            OnDeath.Invoke();
        }
        else
        {
            PlaySounds(damageSounds);
            PlayerController pc = GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.OnHit();
            }
        }
    }

    public void SetInvincible(bool value)
    {
        isInvincible = value;
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public float GetMaxHealth()
    {
        return baseStats.maxHealth;
    }

    public float GetCurrentStamina()
    {
        return currentStamina;
    }

    public float GetMaxStamina()
    {
        return baseStats.maxStamina;
    }

    public float GetBaseDamage()
    {
        return baseStats.baseDamage;
    }

    public float GetRollStaminaCost()
    {
        return baseStats.rollStaminaCost;
    }

    public bool CanPerformAction(float staminaCost)
    {
        return currentStamina >= staminaCost;
    }

    public int GetCurrentFlasks()
    {
        return currentFlasks;
    }

    public void ResetStats()
    {
        currentHealth = baseStats.maxHealth;
        currentStamina = baseStats.maxStamina;
        currentFlasks = baseStats.maxFlasks;
        isInvincible = false;
        isDead = false;
        
        OnHealthChanged.Invoke(currentHealth);
        OnStaminaChanged.Invoke(currentStamina);
        OnFlasksChanged.Invoke(currentFlasks);
    }

    private IEnumerator PlaySoundDelayed(AudioClip clip, float delay, float volume)
    {
        yield return new WaitForSeconds(delay);
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    private void PlaySounds(List<SoundEffect> sounds)
    {
        if (audioSource == null || sounds == null) return;
        foreach (var sfx in sounds)
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
