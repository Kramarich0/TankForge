using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TicketsDisplay : MonoBehaviour
{
    [Header("UI Elements")]
    public Image friendlyFill;
    public Image enemyFill;
    public TextMeshProUGUI friendlyText;
    public TextMeshProUGUI enemyText;

    [Header("Animation")]
    public float smoothSpeed = 5f;

    private float currentFriendlyFill = 1f;
    private float currentEnemyFill = 1f;

    void OnEnable()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsLevelInitialized)
        {
            StartCoroutine(DelayedInit());
            return;
        }

        SetupUI();
    }

    IEnumerator DelayedInit()
    {
        yield return new WaitUntil(() =>
            GameManager.Instance != null &&
            GameManager.Instance.IsLevelInitialized
        );
        SetupUI();
    }

    void SetupUI()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[TicketsDisplay] GameManager is null after waiting!");
            return;
        }

        GameManager.Instance.OnTicketsChanged += UpdateDisplay;
        UpdateDisplay(
            GameManager.Instance.friendlyTickets,
            GameManager.Instance.enemyTickets
        );
        Debug.Log("[TicketsDisplay] Successfully subscribed to tickets events");
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTicketsChanged -= UpdateDisplay;
        }
    }

    public void UpdateDisplay(int friendly, int enemy)
    {
        int maxFriendly = GameManager.Instance.InitialFriendly;
        int maxEnemy = GameManager.Instance.InitialEnemy;

        if (maxFriendly <= 0) maxFriendly = 1;
        if (maxEnemy <= 0) maxEnemy = 1;

        currentFriendlyFill = Mathf.Clamp01((float)friendly / maxFriendly);
        currentEnemyFill = Mathf.Clamp01((float)enemy / maxEnemy);

        Debug.Log($"[Tickets] Максимум: F={maxFriendly}, E={maxEnemy} | Текущие: F={friendly}, E={enemy} | %: F={currentFriendlyFill:P0}, E={currentEnemyFill:P0}");

        if (friendlyText != null) friendlyText.text = $"{friendly}";
        if (enemyText != null) enemyText.text = $"{enemy}";
    }

    void Update()
    {
        float lerpSpeed = smoothSpeed * Time.unscaledDeltaTime;

        if (friendlyFill != null)
        {
            friendlyFill.fillAmount = Mathf.Lerp(friendlyFill.fillAmount, currentFriendlyFill, lerpSpeed);
        }

        if (enemyFill != null)
        {
            enemyFill.fillAmount = Mathf.Lerp(enemyFill.fillAmount, currentEnemyFill, lerpSpeed);
        }
    }
}