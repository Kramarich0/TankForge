using UnityEngine;
using TMPro;

public class SpeedDisplay : MonoBehaviour
{
    private TextMeshProUGUI speedText;
    private float currentSpeed = 0f;

    void Start()
    {
        speedText = GetComponent<TextMeshProUGUI>();
        if (speedText == null)
        {
            Debug.LogError("No TextMeshProUGUI component found on this object!", this);
        }
    }

    public void SetSpeed(float speed)
    {
        currentSpeed = speed;
    }

    void Update()
    {
        if (speedText != null)
        {
            float speedKmh = Mathf.Abs(currentSpeed) *10.0f;
            speedText.text = Mathf.RoundToInt(speedKmh) + " км/ч";
        }
    }
}