using UnityEngine;

[CreateAssetMenu(fileName = "New Character Stats", menuName = "Character/Stats")]
public class CharacterStatsSO : ScriptableObject
{
    [Header("Health")]
    public float maxHealth = 100f;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaRegen = 1.0f;
    public float rollStaminaCost = 20f;

    [Header("Combat")]
    public float baseDamage = 10f;

    [Header("Currency")]
    public int soulsReward = 100;
}