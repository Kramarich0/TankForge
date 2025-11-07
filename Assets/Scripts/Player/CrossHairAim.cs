using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Прицел (UI) — корректно позиционирует crosshair на Canvas любого типа.
/// Использует ScreenPointToLocalPointInRectangle -> rectTransform.anchoredPosition.
/// </summary>
public class CrosshairAim : MonoBehaviour
{
    public Transform gunEnd;
    public Camera mainCamera;
    public LayerMask groundMask = ~0;
    public float maxDistance = 1000f;
    [Range(0f, 50f)] public float smoothSpeed = 20f;
    public bool preferCameraRay = true;

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
        // Инициализация текущей позиции (anchored)
        currentAnchoredPos = rectTransform.anchoredPosition;
    }

    void LateUpdate()
    {
        if (mainCamera == null || crosshairImage == null || rectTransform == null || canvas == null) return;

        Vector3 worldHitPoint = Vector3.zero;
        bool hitSomething = false;

        if (preferCameraRay)
        {
            Ray camRay = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
            if (Physics.Raycast(camRay, out RaycastHit camHit, maxDistance, groundMask))
            {
                worldHitPoint = camHit.point;
                hitSomething = true;
            }
        }

        if (!hitSomething && gunEnd != null)
        {
            Ray ray = new Ray(gunEnd.position, gunEnd.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, groundMask))
            {
                worldHitPoint = hit.point;
                hitSomething = true;
            }
        }

        if (!hitSomething)
        {
            worldHitPoint = (preferCameraRay && mainCamera != null)
                ? (mainCamera.transform.position + mainCamera.transform.forward * maxDistance)
                : (gunEnd != null ? (gunEnd.position + gunEnd.forward * maxDistance) : mainCamera.transform.position + mainCamera.transform.forward * maxDistance);
        }

        Vector3 screenPoint = mainCamera.WorldToScreenPoint(worldHitPoint);

        if (screenPoint.z > 0)
        {
            Vector2 localPoint;
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            // преобразуем screenPoint -> локальные координаты канваса
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera, out localPoint))
            {
                Vector2 targetAnchored = localPoint;
                currentAnchoredPos = Vector2.Lerp(currentAnchoredPos, targetAnchored, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
                rectTransform.anchoredPosition = currentAnchoredPos;
            }
        }
    }
}
