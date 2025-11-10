using TMPro;
using UnityEngine;

public class TankCountDisplay : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI TanksCountText;

    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[TankCounterUI] GameManager.Instance не найден!");
            enabled = false;
            return;
        }

        GameManager.Instance.OnTankCountChanged += UpdateTankCounters;

        UpdateTankCounters(0, 0);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTankCountChanged -= UpdateTankCounters;
        }
    }

    private void UpdateTankCounters(int friendly, int enemy)
    {
        if (TanksCountText == null) return;

        TanksCountText.text = $"{friendly}:{enemy}";
    }

}
