using UnityEngine;
using UnityEngine.UI;

public class CrosshairAim : MonoBehaviour
{
    public Transform gunEnd;
    public Camera mainCamera;
    public LayerMask groundMask = ~0; // по умолчанию всё
    public float maxDistance = 1000f;
    [Range(0f, 50f)] public float smoothSpeed = 20f;
    public bool preferCameraRay = true; // если true — пробуем сначала центр камеры, иначе — луч от ствола

    private Image crosshairImage;
    private RectTransform rectTransform;
    private Vector3 currentScreenPos;

    void Start()
    {
        crosshairImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        if (mainCamera == null) mainCamera = Camera.main;
        if (crosshairImage == null || rectTransform == null) Debug.LogError("Crosshair: UI Image или RectTransform не найдены!");
        currentScreenPos = rectTransform.position;
    }

    void LateUpdate()
    {
        if (mainCamera == null || crosshairImage == null || rectTransform == null) return;

        Vector3 worldHitPoint = Vector3.zero;
        bool hitSomething = false;

        // Первый вариант: пробуем луч от камеры через центр экрана (обычно даёт корректный прицел)
        if (preferCameraRay)
        {
            Ray camRay = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
            if (Physics.Raycast(camRay, out RaycastHit camHit, maxDistance, groundMask))
            {
                worldHitPoint = camHit.point;
                hitSomething = true;
            }
        }

        // Если не попали от камеры, или предпочитаем - используем луч от ствола
        if (!hitSomething && gunEnd != null)
        {
            Ray ray = new Ray(gunEnd.position, gunEnd.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, groundMask))
            {
                worldHitPoint = hit.point;
                hitSomething = true;
            }
        }

        // Если всё ещё ничего — проецируем точку далеко по направлению камеры
        if (!hitSomething)
        {
            worldHitPoint = (preferCameraRay && mainCamera != null)
                ? (mainCamera.transform.position + mainCamera.transform.forward * maxDistance)
                : (gunEnd != null ? (gunEnd.position + gunEnd.forward * maxDistance) : Vector3.zero);
        }

        // Конвертируем в экранные координаты корректно
        Vector3 screenPoint = mainCamera.WorldToScreenPoint(worldHitPoint);

        if (screenPoint.z > 0)
        {
            Vector3 targetPos = screenPoint;
            currentScreenPos = Vector3.Lerp(currentScreenPos, targetPos, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
            rectTransform.position = currentScreenPos;
        }
    }
}
