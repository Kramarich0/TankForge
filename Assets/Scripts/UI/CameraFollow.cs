using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Улучшенная камера: targetYaw/targetPitch, адаптивное сглаживание, плавная коррекция расстояния при коллизии (SphereCast).
/// Основное отличие: при коллизии мы не мгновенно переносим камеру в hit.point, а корректируем целевую дистанцию и сглаживаем к ней.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target / Input")]
    public Transform target;
    public InputActionReference lookAction;  // Vector2
    public InputActionReference zoomAction;  // Vector2 (mouse scroll -> y)

    [Header("Orbit")]
    public Vector3 offset = new Vector3(0f, 4.0f, -8.0f);
    public float minPitch = -20f;
    public float maxPitch = 50f;
    public float baseSensitivity = 0.12f;
    public float zoomSensitivityMultiplier = 0.6f;

    [Header("Smoothing")]
    public float rotationSmoothTime = 0.04f;
    public float positionSmoothTime = 0.06f;
    public float inputResponsivenessThreshold = 0.02f;

    [Header("Zoom / Distance")]
    public float minDistance = 3f;
    public float maxDistance = 14f;
    public float zoomSpeed = 8f;
    public float distanceSmoothTime = 0.12f;
    private float targetDistance;
    private float currentDistance;
    private float distanceVelocity = 0f;

    [Header("Collision")]
    public float cameraCollisionRadius = 0.3f;
    public float collisionOffset = 0.25f; // отступ от коллизии

    // internal
    private float yaw = 0f;
    private float pitch = 10f;
    private float yawVel = 0f;
    private float pitchVel = 0f;
    private Vector3 positionVelocity = Vector3.zero;

    // target angles for smoother pattern
    private float targetYaw;
    private float targetPitch;

    void Start()
    {
        if (lookAction?.action != null) lookAction.action.Enable();
        if (zoomAction?.action != null) zoomAction.action.Enable();

        currentDistance = -offset.z;
        targetDistance = currentDistance;

        if (target != null)
        {
            Vector3 dir = (transform.position - target.position).normalized;
            yaw = Quaternion.LookRotation(Vector3.ProjectOnPlane(dir, Vector3.up)).eulerAngles.y;
            float rawPitch = Quaternion.LookRotation(dir).eulerAngles.x;
            if (rawPitch > 180f) rawPitch -= 360f;
            pitch = Mathf.Clamp(rawPitch, minPitch, maxPitch);
        }

        targetYaw = yaw;
        targetPitch = pitch;
    }

    void OnDisable()
    {
        if (lookAction?.action != null) lookAction.action.Disable();
        if (zoomAction?.action != null) zoomAction.action.Disable();
    }

    void LateUpdate()
    {
        if (target == null) return;

        // --- INPUT ---
        Vector2 look = Vector2.zero;
        if (lookAction?.action != null) look = lookAction.action.ReadValue<Vector2>();
        float inputMag = look.magnitude;

        float deltaYaw = look.x * baseSensitivity;
        float deltaPitch = -look.y * baseSensitivity;

        // zoom input
        float scrollY = 0f;
        if (zoomAction?.action != null)
        {
            Vector2 s = zoomAction.action.ReadValue<Vector2>();
            scrollY = s.y;
        }
        else
        {
            scrollY = Input.GetAxis("Mouse ScrollWheel");
        }

        if (Mathf.Abs(scrollY) > 10f) scrollY *= 0.01f;

        // adapt sensitivity by zoom
        float rotSmooth = rotationSmoothTime;
        if (inputMag > inputResponsivenessThreshold) rotSmooth *= 0.28f;
        float zoomFactor = Mathf.InverseLerp(maxDistance, minDistance, currentDistance);
        float sensitivityScale = Mathf.Lerp(1f, zoomSensitivityMultiplier, zoomFactor);
        deltaYaw *= sensitivityScale;
        deltaPitch *= sensitivityScale;

        // NEW PATTERN: накапливаем в targetAngles и сглаживаем к ним
        targetYaw += deltaYaw;
        targetPitch = Mathf.Clamp(targetPitch + deltaPitch, minPitch, maxPitch);

        yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVel, rotSmooth);
        pitch = Mathf.SmoothDampAngle(pitch, targetPitch, ref pitchVel, rotSmooth);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // ZOOM: меняем targetDistance сразу при скролле
        if (Mathf.Abs(scrollY) > 0.0001f)
        {
            float delta = scrollY * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance - delta, minDistance, maxDistance);
        }

        // COLLISION: рассчитываем желаемую дистанцию с учётом препятствий
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredOffset = rot * new Vector3(0f, offset.y, -targetDistance); // note: targetDistance
        Vector3 desiredPos = target.position + desiredOffset;

        Vector3 rayOrigin = target.position + Vector3.up * 0.8f;
        Vector3 dir = (desiredPos - rayOrigin);
        float dist = dir.magnitude;

        float desiredDistanceAfterCollision = targetDistance; // базовое значение — то, к чему мы хотим вернуться
        if (dist > 0.001f)
        {
            dir.Normalize();
            if (Physics.SphereCast(rayOrigin, cameraCollisionRadius, dir, out RaycastHit hit, dist))
            {
                // если есть препятствие — уменьшаем максимальную дистанцию (камера ближе к игроку)
                float hitDistance = hit.distance;
                float safe = Mathf.Max(minDistance, hitDistance - collisionOffset);
                desiredDistanceAfterCollision = Mathf.Min(desiredDistanceAfterCollision, safe);
            }
        }

        // Плавно сглаживаем currentDistance к desiredDistanceAfterCollision
        currentDistance = Mathf.SmoothDamp(currentDistance, Mathf.Clamp(desiredDistanceAfterCollision, minDistance, maxDistance),
            ref distanceVelocity, distanceSmoothTime, Mathf.Infinity, Time.deltaTime);

        // Позиция и ориентация
        Vector3 finalOffset = rot * new Vector3(0f, offset.y, -currentDistance);
        Vector3 finalPos = target.position + finalOffset;

        // Position smoothing (адаптивный)
        float posSmooth = positionSmoothTime;
        if (inputMag > inputResponsivenessThreshold) posSmooth *= 0.28f;
        transform.position = Vector3.SmoothDamp(transform.position, finalPos, ref positionVelocity, posSmooth);

        // Смотреть на цель (чуть выше центра)
        Vector3 lookTarget = target.position + Vector3.up * (offset.y * 0.5f);
        Quaternion lookRot = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 1f - Mathf.Exp(-18f * Time.deltaTime));
    }
}
