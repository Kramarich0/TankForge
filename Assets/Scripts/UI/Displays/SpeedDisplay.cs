using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class SpeedDisplay : MonoBehaviour
{
    private TextMeshProUGUI speedText;

    void Start()
    {
        speedText = GetComponent<TextMeshProUGUI>();
        if (speedText == null)
        {
            Debug.LogError("No TextMeshProUGUI component found on this object!", this);
        }
    }

    public void SetSpeed(int speedKmh)
    {
        if (speedText != null)
            speedText.text = Mathf.RoundToInt(speedKmh) + " км/ч";
    }

}