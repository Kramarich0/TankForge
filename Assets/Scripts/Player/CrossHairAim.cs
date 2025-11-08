using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class CrosshairAim : MonoBehaviour
{
    public Transform gunEnd;
    public Camera mainCamera;
    public Camera activeCamera;
    public LayerMask groundMask = ~0;
    public float maxDistance = 1000f;
    public float smoothSpeed = 15f;

    Image crosshairImage;
    RectTransform rectTransform;
    Vector2 currentAnchoredPos;
    Canvas canvas;

    void Start()
    {
        crosshairImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        if (activeCamera == null) activeCamera = mainCamera;
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null) Debug.LogError("Crosshair: Canvas не найден!");
        currentAnchoredPos = rectTransform.anchoredPosition;
    }

    void LateUpdate()
    {
        if (mainCamera == null || gunEnd == null) return;

        TankShoot shootComp = gunEnd.GetComponentInParent<TankShoot>();
        if (shootComp == null) return;

        float speed = shootComp.bulletSpeed;

        Vector3 hitPoint = PredictProjectileHitPoint(gunEnd.position, gunEnd.forward, speed, Physics.gravity);

        Vector3 screenPoint = activeCamera.WorldToScreenPoint(hitPoint);
        if (screenPoint.z <= 0f) return;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPoint,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : activeCamera,
            out Vector2 localPoint))
        {
            Vector2 targetAnchored = localPoint;
            float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
            currentAnchoredPos = Vector2.Lerp(currentAnchoredPos, targetAnchored, t);
            rectTransform.anchoredPosition = currentAnchoredPos;
        }
    }

    private Vector3 PredictProjectileHitPoint(Vector3 start, Vector3 direction, float speed, Vector3 gravity)
    {
        Vector3 velocity = direction.normalized * Mathf.Max(0.0001f, speed);
        Vector3 pos = start;
        float dt = Time.fixedDeltaTime;

        for (float t = 0f; t < 10f; t += dt)
        {
            velocity += gravity * dt;
            Vector3 nextPos = pos + velocity * dt;

            if (Physics.Linecast(pos, nextPos, out RaycastHit hit, groundMask))
                return hit.point;

            pos = nextPos;
            if ((pos - start).sqrMagnitude > maxDistance * maxDistance) break;
        }

        return pos;
    }
}
