using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider staminaBar;

    void Start()
    {
        if (playerStats == null) playerStats = FindObjectOfType<PlayerStats>();

        playerStats.OnHealthChanged.AddListener(UpdateHealthBar);
        playerStats.OnStaminaChanged.AddListener(UpdateStaminaBar);

        UpdateHealthBar(playerStats.GetCurrentHealth());
        UpdateStaminaBar(playerStats.GetCurrentStamina());
    }

    private void UpdateHealthBar(float currentHealth)
    {
        healthBar.value = currentHealth / playerStats.GetMaxHealth();
    }

    private void UpdateStaminaBar(float currentStamina)
    {
        staminaBar.value = currentStamina / playerStats.GetMaxStamina();
    }
}