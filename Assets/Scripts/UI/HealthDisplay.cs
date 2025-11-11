using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image healthBar;

    private TankHealth playerHealth;

    void Start()
    {
        var playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            Debug.LogError("[HealthDisplay] Игрок с тегом 'Player' не найден!", this);
            enabled = false;
            return;
        }

        playerHealth = playerObject.GetComponent<TankHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("[HealthDisplay] На игроке нет компонента TankHealth!", this);
            enabled = false;
            return;
        }

        playerHealth.OnHealthChanged += UpdateDisplay;
        UpdateDisplay(playerHealth.currentHealth, playerHealth.maxHealth);
    }

    void UpdateDisplay(float current, float max)
    {
        float fill = max > 0.001f ? current / max : 0f;

        if (healthText != null)
            healthText.text = $"{Mathf.RoundToInt(current)} / {Mathf.RoundToInt(max)}";

        if (healthBar != null)
            healthBar.fillAmount = fill;
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= UpdateDisplay;
    }
}