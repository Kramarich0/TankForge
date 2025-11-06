using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthDisplay : MonoBehaviour
{
    public TextMeshProUGUI healthText; 
    public Image healthBar;            

    private float currentHealth = 0f;
    private float maxHealth = 100f;

    void Awake()
    {
        if (healthText == null) Debug.LogError("Health Text не назначен!", this);
        if (healthBar == null) Debug.LogError("Health Bar не назначен!", this);
    }

    public void SetMaxHealth(float max)
    {
        maxHealth = Mathf.Max(0.0001f, max);
        SetHealth(maxHealth);
    }

    public void SetHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0f, maxHealth);
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (healthText != null)
            healthText.text = $"{Mathf.RoundToInt(currentHealth)} / {Mathf.RoundToInt(maxHealth)} HP";

        if (healthBar != null)
            healthBar.fillAmount = currentHealth / maxHealth;
    }
}
