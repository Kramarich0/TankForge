using UnityEngine;

public class TankHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 100f;
    [HideInInspector] public float currentHealth;

    [Header("UI")]
    public HealthDisplay healthDisplay;       
    public HealthAiDisplay aiHealthDisplay;     

    void Awake()
    {
        currentHealth = maxHealth;

        if (healthDisplay != null)
        {
            healthDisplay.SetMaxHealth(maxHealth);
            healthDisplay.SetHealth(currentHealth);
            Debug.Log($"[TankHealth] {gameObject.name} привязан HealthDisplay: {healthDisplay.gameObject.name}");
        }

        if (aiHealthDisplay != null)
        {
            aiHealthDisplay.SetMaxHealth(maxHealth);
            aiHealthDisplay.SetHealth(currentHealth);
            Debug.Log($"[TankHealth] {gameObject.name} привязан HealthAiDisplay: {aiHealthDisplay.gameObject.name}");
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        Debug.Log($"{gameObject.name} получил {damage} урона, осталось HP: {currentHealth}");

        if (healthDisplay != null)
            healthDisplay.SetHealth(currentHealth);

        if (aiHealthDisplay != null)
            aiHealthDisplay.SetHealth(currentHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} уничтожен!");
        Destroy(gameObject);
    }
}
