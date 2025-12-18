using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider staminaBar;
    [SerializeField] private TextMeshProUGUI soulsText;
    [SerializeField] private TextMeshProUGUI flaskText;

    void Start()
    {
        if (playerStats == null) playerStats = FindObjectOfType<PlayerStats>();

        playerStats.OnHealthChanged.AddListener(UpdateHealthBar);
        playerStats.OnStaminaChanged.AddListener(UpdateStaminaBar);
        playerStats.OnSoulsChanged.AddListener(UpdateSoulsText);
        playerStats.OnFlasksChanged.AddListener(UpdateFlaskText);

        UpdateHealthBar(playerStats.GetCurrentHealth());
        UpdateStaminaBar(playerStats.GetCurrentStamina());
        UpdateSoulsText(playerStats.GetCurrentSouls());
        UpdateFlaskText(playerStats.GetCurrentFlasks());
    }

    private void UpdateHealthBar(float currentHealth)
    {
        healthBar.value = currentHealth / playerStats.GetMaxHealth();
    }

    private void UpdateStaminaBar(float currentStamina)
    {
        staminaBar.value = currentStamina / playerStats.GetMaxStamina();
    }

    private void UpdateSoulsText(int souls)
    {
        if (soulsText) soulsText.text = "Sols: " + souls.ToString();
    }

    private void UpdateFlaskText(int flasks)
    {
        if (flaskText) flaskText.text = "Flasks: " + flasks.ToString();
    }
}