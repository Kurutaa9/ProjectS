using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private EnemyStatController enemyStats;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, 0f); // Position above enemy

    void Start()
    {
        enemyStats.OnHealthChanged.AddListener(UpdateHealthBar);
        enemyStats.OnDamageStateChanged.AddListener(ToggleVisibility);
        enemyStats.OnDeath.AddListener(OnEnemyDeath);

        UpdateHealthBar(enemyStats.GetCurrentHealth());
        ToggleVisibility(enemyStats.HasTakenDamage());
    }

    void LateUpdate()
    {
        //healtbar above the enemy
        transform.position = enemyStats.gameObject.transform.position + offset;
        //healthbar facing the camera
        transform.rotation = Camera.main.transform.rotation;
    }

    private void UpdateHealthBar(float currentHealth)
    {
        healthBar.value = currentHealth / enemyStats.GetMaxHealth();
    }

    private void ToggleVisibility(bool hasTakenDamage)
    {
        canvas.enabled = hasTakenDamage;
    }

    private void OnEnemyDeath()
    {
        Destroy(gameObject);
    }
}