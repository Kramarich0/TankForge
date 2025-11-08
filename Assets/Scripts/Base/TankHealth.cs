// TankHealth.cs
using UnityEngine;

public class TankHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 100f;
    [HideInInspector] public float currentHealth;

    [Header("UI")]
    public GameObject healthDisplay; // optional legacy (оставил на случай)
    public HealthAiDisplay aiHealthDisplay; // prefab instance reference (world-space canvas)

    void Awake()
    {
        currentHealth = maxHealth;

        if (aiHealthDisplay != null)
        {
            // Если это префаб, можно инстанцировать, но ожидаем, что ты назначишь prefab в инспекторе и он уже инстанцирован
            aiHealthDisplay.target = this;
            aiHealthDisplay.targetTeam = GetComponent<TeamComponent>();
            aiHealthDisplay.UpdateDisplay();
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        Debug.Log($"{gameObject.name} получил {damage} урона, осталось HP: {currentHealth}");

        if (aiHealthDisplay != null)
        {
            aiHealthDisplay.UpdateDisplay();
        }

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
