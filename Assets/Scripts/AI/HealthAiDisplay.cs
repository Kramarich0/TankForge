// HealthAiDisplay.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Canvas))]
public class HealthAiDisplay : MonoBehaviour
{
    public TankHealth target;
    public TeamComponent targetTeam;

    public TextMeshProUGUI healthText; // TextMeshPro UI (в Canvas)
    public Image healthBar; // Image (fill)
    public float verticalOffset = 2.2f;
    public Color friendlyColor = Color.green;
    public Color enemyColor = Color.red;

    Camera cam;

    void Start()
    {
        cam = Camera.main;
        if (target != null)
        {
            SetMaxHealth(target.maxHealth);
            UpdateDisplay();
        }
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = target.transform.position + Vector3.up * verticalOffset;
        if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);

        UpdateDisplay();
    }

    public void SetMaxHealth(float max)
    {
        // just ensure text shows correct value
        if (healthBar != null) { } // nothing to setup here except fillAmount usage
    }

    public void UpdateDisplay()
    {
        if (target == null) return;

        float cur = target.currentHealth;
        float max = target.maxHealth;

        if (healthText != null)
            healthText.text = $"{Mathf.RoundToInt(cur)} / {Mathf.RoundToInt(max)} HP";

        if (healthBar != null)
            healthBar.fillAmount = Mathf.Clamp01(max > 0f ? cur / max : 0f);

        if (targetTeam != null && healthBar != null)
            healthBar.color = (targetTeam.team == Team.Friendly) ? friendlyColor : enemyColor;
    }
}
