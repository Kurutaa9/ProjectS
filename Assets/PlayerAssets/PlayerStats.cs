using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private CharacterStatsSO baseStats;

    private float currentHealth;
    private float currentStamina;

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
        //stamina regen
        if (currentStamina < baseStats.maxStamina)
        {
            currentStamina = Mathf.Min(currentStamina + baseStats.staminaRegen * Time.deltaTime, baseStats.maxStamina);
            OnStaminaChanged.Invoke(currentStamina);
        }
    }

    public void ConsumeStamina(float amount)
    {
        currentStamina = Mathf.Max(currentStamina - amount, 0f);
        OnStaminaChanged.Invoke(currentStamina);
    }

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        OnHealthChanged.Invoke(currentHealth);
        if (currentHealth <= 0f)
        {
            OnDeath.Invoke();
        }
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
}
