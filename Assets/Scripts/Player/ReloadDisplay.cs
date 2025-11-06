using UnityEngine;
using TMPro;

public class ReloadDisplay : MonoBehaviour
{
    private TextMeshProUGUI reloadText;
    private float currentReloadTime = 6f;
    private float maxReloadTime = 6f;
    private bool isReloading = false;

    void Start()
    {
        reloadText = GetComponent<TextMeshProUGUI>();
        if (reloadText == null)
        {
            Debug.LogError("No TextMeshProUGUI component found on this object!", this);
        }
    }

    public void SetReload(float remainingTime, float maxTime)
    {
        currentReloadTime = remainingTime;
        maxReloadTime = maxTime;
        isReloading = remainingTime > 0f;

        if (reloadText != null && isReloading)
        {
            reloadText.text = "<color=red>Перезарядка: " + Mathf.CeilToInt(currentReloadTime).ToString() + "с</color>";
            gameObject.SetActive(true);
        }
        else
        {
            reloadText.text = "<color=green>Готово</color>";
            gameObject.SetActive(true); 
        }
    }

    public void Hide()
    {
        isReloading = false;
        if (reloadText != null)
        {
            gameObject.SetActive(false);
        }
    }
}