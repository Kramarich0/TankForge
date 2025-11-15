using System.Collections;
using UnityEngine;

[RequireComponent(typeof(TeamComponent))]
public class AITankHealth : MonoBehaviour, IDamageable
{
    private static WaitForSeconds _waitForSeconds0_1 = new(0.1f);

    [Header("Health")]
    [HideInInspector] public float maxHealth;
    [HideInInspector] public float currentHealth;
    public System.Action<float, float> OnHealthChanged;
    private bool isDead = false;

    [Header("Death")]
    public GameObject deathPrefab;

    private TeamComponent teamComp;
    private string lastAttackerName;

    void Start()
    {
        teamComp = GetComponent<TeamComponent>();
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int amount, string source = null)
    {
        if (isDead || currentHealth <= 0) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);
        lastAttackerName = source;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (gameObject == null || !gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"[AITankHealth] Попытка умереть уничтоженного объекта: {name}");
            return;
        }

        StartCoroutine(SafeDeathSequence());
    }

    private IEnumerator SafeDeathSequence()
    {
        if (gameObject == null || !gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"[AITankHealth] Объект уже уничтожен: {name}");
            yield break;
        }

        int ticketCost = GetTicketCost();
        string victimName = teamComp.displayName ?? gameObject.name;
        TeamComponent teamComponentRef = teamComp;

        if (teamComponentRef != null && teamComponentRef.gameObject != null && teamComponentRef.gameObject.activeInHierarchy)
        {
            GameManager.Instance.OnTankDestroyed(teamComponentRef, ticketCost, lastAttackerName, victimName);
        }
        else
        {
            Debug.LogWarning($"[AITankHealth] TeamComponent невалиден для {name}");
        }


        yield return new WaitForFixedUpdate();
        DisableAllComponentsImmediately();

        if (deathPrefab != null)
        {
            CreateCorpseSafely();
        }

        yield return _waitForSeconds0_1;

        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    private void DisableAllComponentsImmediately()
    {
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        var colliders = GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }

        var wheelColliders = GetComponentsInChildren<WheelCollider>();
        foreach (var wheel in wheelColliders)
        {
            wheel.enabled = false;
        }

        if (TryGetComponent<TankAI>(out var ai))
            ai.enabled = false;
        if (TryGetComponent<TankCollisionDamage>(out var tcd))
            tcd.enabled = false;

        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = false;
        }
    }

    private void CreateCorpseSafely()
    {
        try
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.1f;
            GameObject hull = Instantiate(deathPrefab, spawnPos, transform.rotation);

            if (hull.TryGetComponent<Rigidbody>(out var rbHull))
            {
                Vector3 randomForce = new Vector3(
                    Random.Range(-2f, 2f),
                    Random.Range(1f, 3f),
                    Random.Range(-2f, 2f)
                );
                rbHull.AddForce(randomForce, ForceMode.Impulse);

                Vector3 randomTorque = new Vector3(
                    Random.Range(-5f, 5f),
                    Random.Range(-10f, 10f),
                    Random.Range(-5f, 5f)
                );
                rbHull.AddTorque(randomTorque, ForceMode.Impulse);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка при создании трупа: {e.Message}");
        }
    }

    private int GetTicketCost()
    {
        if (TryGetComponent<TankAI>(out var ai))
        {
            return ai.CurrentTankClass switch
            {
                TankClass.Light => 100,
                TankClass.Medium => 200,
                TankClass.Heavy => 300,
                _ => 150
            };
        }
        return 200;
    }
}