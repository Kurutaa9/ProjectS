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

    // NEW: allow external systems (e.g., dragon roar) to force-show the bar
    private bool forcedVisible = false;

    public void ForceShow(bool value = true)
    {
        forcedVisible = value;
        if (canvas)
            canvas.enabled = forcedVisible || enemyStats.HasTakenDamage() || (bossController && bossController.isChasing);
    }

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

        if (canvas)
        {
            // forcedVisible overrides the usual conditions
            bool chaseVisible = bossController && bossController.isChasing;
            canvas.enabled = forcedVisible || enemyStats.HasTakenDamage() || chaseVisible;
        }
    }

    private void UpdateHealthBar(float currentHealth)
    {
        targetHealthValue = currentHealth / enemyStats.GetMaxHealth();
    }

    private void ToggleVisibility(bool hasTakenDamage)
    {
        if (canvas)
        {
            bool chaseVisible = bossController && bossController.isChasing;
            canvas.enabled = forcedVisible || hasTakenDamage || chaseVisible;
        }
    }

    private void OnEnemyDeath()
    {
        // Check if the enemy has a respawn manager and is respawnable
        var respawnManager = enemyStats.GetComponent<EnemyRespawnManager>();
        if (respawnManager != null && respawnManager.respawnType == EnemyRespawnManager.RespawnType.Respawnable)
        {
            // Do not destroy. The canvas will be hidden by ToggleVisibility when stats are reset.
            if (canvas) canvas.enabled = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}