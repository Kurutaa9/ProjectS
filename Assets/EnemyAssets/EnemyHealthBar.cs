using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private EnemyStatController enemyStats;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, 0f);

    [SerializeField] private BossController bossController; // optional reference

    [Header("Health Bar Animation")]
    [SerializeField] private float lerpSpeed = 5f;
    private float targetHealthValue;
    private float currentDisplayedHealth;

    void Start()
    {
        if (!bossController) bossController = GetComponentInParent<BossController>();

        enemyStats.OnHealthChanged.AddListener(UpdateHealthBar);
        enemyStats.OnDamageStateChanged.AddListener(ToggleVisibility);
        enemyStats.OnDeath.AddListener(OnEnemyDeath);

        UpdateHealthBar(enemyStats.GetCurrentHealth());
        ToggleVisibility(enemyStats.HasTakenDamage());
        
        // Initialize displayed health to current (no lerp on first frame)
        currentDisplayedHealth = enemyStats.GetCurrentHealth() / enemyStats.GetMaxHealth();
        healthBar.value = currentDisplayedHealth;
    }

    void Update()
    {
        // Smoothly lerp health bar to target value
        if (Mathf.Abs(currentDisplayedHealth - targetHealthValue) > 0.001f)
        {
            currentDisplayedHealth = Mathf.Lerp(currentDisplayedHealth, targetHealthValue, Time.deltaTime * lerpSpeed);
            healthBar.value = currentDisplayedHealth;
        }
    }

    void LateUpdate()
    {
        transform.position = enemyStats.gameObject.transform.position + offset;
        transform.rotation = Camera.main.transform.rotation;

        // keep visibility updated if chase state changes
        if (bossController)
        {
            canvas.enabled = enemyStats.HasTakenDamage() || bossController.isChasing;
        }
    }

    private void UpdateHealthBar(float currentHealth)
    {
        targetHealthValue = currentHealth / enemyStats.GetMaxHealth();
    }

    private void ToggleVisibility(bool hasTakenDamage)
    {
        // broaden condition to include chase
        if (bossController)
            canvas.enabled = hasTakenDamage || bossController.isChasing;
        else
            canvas.enabled = hasTakenDamage;
    }

    private void OnEnemyDeath()
    {
        Destroy(gameObject);
    }
}