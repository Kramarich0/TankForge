
using UnityEngine;

[RequireComponent(typeof(TeamComponent))]
public class TankHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 100f;
    [HideInInspector] public float currentHealth;

    [Header("UI")]
    public GameObject healthDisplay;

    private TeamComponent teamComp;

    void Awake()
    {
        teamComp = GetComponent<TeamComponent>();
        currentHealth = maxHealth;

    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        Debug.Log($"{gameObject.name} получил {damage} урона, осталось HP: {currentHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} уничтожен!");

        if (teamComp != null)
        {
            if (teamComp.team == Team.Enemy)
            {
                GameManager.Instance?.OnEnemyDestroyed(gameObject);
            }
            else if (teamComp.team == Team.Friendly)
            {
                if (CompareTag("Player"))
                {
                    GameManager.Instance?.OnPlayerTankDestroyed();
                }
                else { Debug.Log("Союзник погиб!"); }
            }
        }

        Destroy(gameObject);
    }
}
