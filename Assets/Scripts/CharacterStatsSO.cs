using UnityEngine;

[CreateAssetMenu(fileName = "New Character Stats", menuName = "Character/Stats")]
public class CharacterStatsSO : ScriptableObject
{

    // [Header("Upgrade")]
    public int healthLevel = 1;
    public int staminaLevel = 1;
    public int flaskLevel = 1;
    public int damageLevel = 1;

    // [Header("Health")]
    public float maxHealth => 100f + (healthLevel * 50f) - 50f;

    // [Header("Stamina")]
    public float maxStamina => 100f + (staminaLevel * 50f) - 50f;
    public float staminaRegen = 1.0f;
    public float rollStaminaCost = 20f;

    // [Header("Combat")]
    public float baseDamage => 10f + (damageLevel * 5.0f) - 5.0f;

    // [Header("Currency")]
    public int soulsReward = 100;

    // [Header("Healing")]
    public int maxFlasks => 3 + (flaskLevel * 1) - 1;

    

    // [Header("Currency")]
    public int currentSolsSO = 0;

}