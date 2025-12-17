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

    public UnityEvent<float> OnHealthChanged;
    public UnityEvent<float> OnStaminaChanged;
    public UnityEvent OnDeath;

    void Start()
    {
        currentHealth = baseStats.maxHealth;
        currentStamina = baseStats.maxStamina;
        OnHealthChanged.Invoke(currentHealth);
        OnStaminaChanged.Invoke(currentStamina);
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

    public void ResetStats()
    {
        currentHealth = baseStats.maxHealth;
        currentStamina = baseStats.maxStamina;
        isInvincible = false;
        
        OnHealthChanged.Invoke(currentHealth);
        OnStaminaChanged.Invoke(currentStamina);
    }
}
