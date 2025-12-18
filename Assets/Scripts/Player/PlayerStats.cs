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

    [Header("Stamina Regen Control")]
    [Tooltip("Seconds to wait after any stamina usage before regen starts again.")]
    public float staminaRegenDelay = 0.75f;
    private float regenBlockedUntil = 0f;

    [Header("Healing")]
    public float healAmount = 30f;
    public int maxFlasks = 3;
    private int currentFlasks;

    [Header("Currency")]
    private int currentSouls = 0;

    public UnityEvent<float> OnHealthChanged;
    public UnityEvent<float> OnStaminaChanged;
    public UnityEvent<int> OnFlasksChanged;
    public UnityEvent<int> OnSoulsChanged;
    public UnityEvent OnDeath;

    void Start()
    {
        currentHealth = baseStats.maxHealth;
        currentStamina = baseStats.maxStamina;
        currentFlasks = maxFlasks;
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

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, baseStats.maxHealth);
        OnHealthChanged.Invoke(currentHealth);
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
        OnSoulsChanged.Invoke(currentSouls);
    }

    public int GetCurrentSouls()
    {
        return currentSouls;
    }

    public void TakeDamage(float amount)
    {
        // Ignore damage while invincible (e.g., during dodge i-frames)
        if (isInvincible)
            return;

        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        OnHealthChanged.Invoke(currentHealth);
        if (currentHealth <= 0f)
        {
            OnDeath.Invoke();
        }
        else
        {
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
        currentFlasks = maxFlasks;
        isInvincible = false;
        
        OnHealthChanged.Invoke(currentHealth);
        OnStaminaChanged.Invoke(currentStamina);
        OnFlasksChanged.Invoke(currentFlasks);
    }
}
