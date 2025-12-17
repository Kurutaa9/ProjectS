using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemyStatController : MonoBehaviour
{
    [SerializeField] private CharacterStatsSO baseStats;

    private float currentHealth;
    private bool hasTakenDamage;
    public UnityEvent<bool> OnDamageStateChanged;
    public UnityEvent<float> OnHealthChanged;

    public UnityEvent OnTakeDamage;
    public UnityEvent OnDeath;

    void Start()
    {
        currentHealth = baseStats.maxHealth;
        hasTakenDamage = false;
        OnHealthChanged.Invoke(currentHealth);
        OnDamageStateChanged.Invoke(hasTakenDamage);
    }

    void Update()
    {

    }

    public void TakeDamage(float amount)
    {
        //Debug.Log($"enemy take {amount} damage");
        if (!hasTakenDamage)
        {
            hasTakenDamage = true;
            OnDamageStateChanged.Invoke(hasTakenDamage);
            //Debug.Log($"invoked {hasTakenDamage}");
        }

        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        OnHealthChanged.Invoke(currentHealth);

        if (currentHealth > 0f)
        {
            OnTakeDamage.Invoke();
        }
        else
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

    public float GetBaseDamage()
    {
        return baseStats.baseDamage;
    }

    public bool HasTakenDamage()
    {
        return hasTakenDamage;
    }
}
