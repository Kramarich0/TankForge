
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
        bool isPlayer = gameObject.CompareTag("Player");

        if (teamComp != null)
        {
            int ticketCost = 200;
            if (TryGetComponent(out TankAI ai))
            {
                ticketCost = ai.tankClass switch
                {
                    TankAI.TankClass.Light => 100,
                    TankAI.TankClass.Medium => 200,
                    TankAI.TankClass.Heavy => 300,
                    _ => 150
                };
            }

            if (!isPlayer)
            {
                GameManager.Instance?.OnTankDestroyed(teamComp.team, ticketCost);
            }
            else
            {
                GameManager.Instance?.OnPlayerTankDestroyed();
            }
        }

        Destroy(gameObject);
    }
}
