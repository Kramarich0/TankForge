using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Прицел (UI) — показывает точку потенциального попадания снаряда.
/// Работает для любого Canvas, плавно двигая crosshair.
/// </summary>
public class CrosshairAim : MonoBehaviour
{
    public Transform gunEnd;             // Дуло пушки
    public Camera mainCamera;            // Камера для расчёта UI
    public LayerMask groundMask = ~0;    // Слои, по которым "падает" снаряд
    public float maxDistance = 1000f;    // Максимальная дальность трассировки
    public float projectileSpeed = 100f; // Скорость снаряда (для аппроксимации)
    public float gravity = 9.81f;        // Гравитация
    [Range(0f, 50f)] public float smoothSpeed = 20f;

    private Image crosshairImage;
    private RectTransform rectTransform;
    private Vector2 currentAnchoredPos;
    private Canvas canvas;

    void Start()
    {
        crosshairImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        if (mainCamera == null) mainCamera = Camera.main;
        if (crosshairImage == null || rectTransform == null) Debug.LogError("Crosshair: UI Image или RectTransform не найдены!");
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null) Debug.LogError("Crosshair: Canvas не найден в родительских объектах!");
        currentAnchoredPos = rectTransform.anchoredPosition;
    }

    void LateUpdate()
    {
        if (mainCamera == null || crosshairImage == null || rectTransform == null || canvas == null || gunEnd == null) return;

        // Рассчитываем предполагаемую точку падения снаряда
        Vector3 hitPoint = PredictProjectileHitPoint(gunEnd.position, gunEnd.forward, projectileSpeed, gravity);

        // Преобразуем мировую точку в экранные координаты
        Vector3 screenPoint = mainCamera.WorldToScreenPoint(hitPoint);

        if (screenPoint.z > 0) // Проверяем, что точка перед камерой
        {
            Vector2 localPoint;
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPoint, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera, out localPoint))
            {
                Vector2 targetAnchored = localPoint;
                // Плавное движение прицела
                currentAnchoredPos = Vector2.Lerp(currentAnchoredPos, targetAnchored, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
                rectTransform.anchoredPosition = currentAnchoredPos;
            }
        }
    }

    // Простая аппроксимация траектории снаряда для прицела
    private Vector3 PredictProjectileHitPoint(Vector3 start, Vector3 direction, float speed, float gravity)
    {
        Vector3 velocity = direction.normalized * speed;
        Vector3 position = start;

        float timeStep = 0.02f; // шаг симуляции
        for (float t = 0; t < maxDistance / speed; t += timeStep)
        {
            Vector3 nextPos = position + velocity * timeStep;
            velocity.y -= gravity * timeStep;

            if (Physics.Linecast(position, nextPos, out RaycastHit hit, groundMask))
                return hit.point;

            position = nextPos;
        }

        return position; // Если не встретили препятствия
    }
}
