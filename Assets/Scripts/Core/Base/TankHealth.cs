
using System;
using UnityEngine;

[RequireComponent(typeof(TeamComponent))]
[RequireComponent(typeof(Rigidbody))]
public class TankHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 100f;
    [HideInInspector] public float currentHealth;
    public Action<float, float> OnHealthChanged;

    [Header("UI")]
    public GameObject healthDisplay;

    private TeamComponent teamComp;
    private string lastAttackerName = null;

    [Header("Death Prefab")]
    public GameObject deathPrefab;

    void Awake()
    {
        teamComp = GetComponent<TeamComponent>();
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

    }

    public void TakeDamage(int amount, string source = null)
    {
        if (currentHealth <= 0) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);

        lastAttackerName = source;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
о к        Debug.Log($"[TankHealth] {gameObject.name} TakeDamage {amount} from {source}. HP before: {currentHealth}");

        if (currentHealth <= 0f)
        {
            Die(killerName: lastAttackerName);
        }
    }

    void Die(string killerName = null)
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
                string victimName = !string.IsNullOrEmpty(teamComp.displayName) ? teamComp.displayName : gameObject.name;
                GameManager.Instance?.OnTankDestroyed(GetComponent<TeamComponent>(), ticketCost, killerName, victimName);
            }
            else
            {
                GameManager.Instance?.OnPlayerTankDestroyed();
            }
        }

        if (deathPrefab != null)
        {
            GameObject hull = Instantiate(deathPrefab, transform.position, transform.rotation);

            hull.transform.rotation = transform.rotation;

            if (TryGetComponent<Rigidbody>(out var rbOriginal) && hull.TryGetComponent<Rigidbody>(out var rbHull))
            {
                rbHull.linearVelocity = rbOriginal.linearVelocity;
                rbHull.angularVelocity = rbOriginal.angularVelocity;
            }
        }

        Destroy(gameObject);
    }
}
