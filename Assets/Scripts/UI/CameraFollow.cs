using UnityEngine;
using UnityEngine.InputSystem;

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
    public float baseSensitivity = 0.12f;            // базовая чувствительность
    public float zoomSensitivityMultiplier = 0.6f;   // при приближении чувствительность умножается на это

    [Header("Smoothing")]
    [Tooltip("Малые значения = более резкая камера")]
    public float rotationSmoothTime = 0.04f;
    public float positionSmoothTime = 0.06f;
    [Tooltip("Если ввод активен (мышь двигается больше этого), камера становится более отзывчивой)")]
    public float inputResponsivenessThreshold = 0.02f;

    [Header("Zoom / Distance")]
    public float minDistance = 3f;
    public float maxDistance = 14f;
    public float zoomSpeed = 8f;            // влияет на то, как быстро меняется targetDistance при скролле
    public float distanceSmoothTime = 0.12f; // сглаживание плавного перехода расстояния (SmoothDamp)
    private float targetDistance;
    private float currentDistance;
    private float distanceVelocity = 0f;

    [Header("Collision")]
    [Tooltip("Радиус камеры для SphereCast - предотвращает врезание в стену")]
    public float cameraCollisionRadius = 0.3f;
    public float collisionOffset = 0.25f; // отступ от коллизии

    // Внутренние
    private float yaw = 0f;
    private float pitch = 10f;
    private float yawVel = 0f;
    private float pitchVel = 0f;
    private Vector3 positionVelocity = Vector3.zero;

    void Start()
    {
        if (lookAction?.action != null) lookAction.action.Enable();
        if (zoomAction?.action != null) zoomAction.action.Enable();

        currentDistance = -offset.z;
        targetDistance = currentDistance;

        if (target != null)
        {
            // Инициализируем yaw/pitch относительно текущей позиции камеры для плавного старта
            Vector3 dir = (transform.position - target.position).normalized;
            yaw = Quaternion.LookRotation(Vector3.ProjectOnPlane(dir, Vector3.up)).eulerAngles.y;
            float rawPitch = Quaternion.LookRotation(dir).eulerAngles.x;
            if (rawPitch > 180f) rawPitch -= 360f;
            pitch = Mathf.Clamp(rawPitch, minPitch, maxPitch);
        }
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

        // delta from input (instant)
        float deltaYaw = look.x * baseSensitivity;
        float deltaPitch = -look.y * baseSensitivity;

        // Приближение/отдаление — получаем scroll.y (может быть 0, ±120 и т.д. в зависимости от устройства)
        float scrollY = 0f;
        if (zoomAction?.action != null)
        {
            Vector2 s = zoomAction.action.ReadValue<Vector2>();
            scrollY = s.y;
        }
        else
        {
            scrollY = Input.GetAxis("Mouse ScrollWheel");
            // Note: old Input возвращает мелкие значения (~0.1) — в зависимости от устройства различается
        }

        // Нормализуем или масштабируем scroll при больших значениях (например 120)
        // Если scrollY очень большой (мышь даёт 120/ -120), уменьшим до более удобного диапазона
        if (Mathf.Abs(scrollY) > 10f) scrollY *= 0.01f;

        // --- ROTATION: адаптивное сглаживание ---
        float rotSmooth = rotationSmoothTime;
        if (inputMag > inputResponsivenessThreshold) rotSmooth *= 0.28f; // при движении — очень отзывчивая
        // уменьшение чувствительности при зуме (чтобы при сильном приближении не "скакала" камера)
        float zoomFactor = Mathf.InverseLerp(maxDistance, minDistance, currentDistance); // 0..1 (1 = при близком)
        float sensitivityScale = Mathf.Lerp(1f, zoomSensitivityMultiplier, zoomFactor);
        deltaYaw *= sensitivityScale;
        deltaPitch *= sensitivityScale;

        // Плавное добавление углов — используем SmoothDampAngle для стабильности
        yaw = Mathf.SmoothDampAngle(yaw, yaw + deltaYaw, ref yawVel, rotSmooth);
        pitch = Mathf.SmoothDampAngle(pitch, Mathf.Clamp(pitch + deltaPitch, minPitch, maxPitch), ref pitchVel, rotSmooth);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // --- ZOOM: мгновенно меняем targetDistance, плавно currentDistance (SmoothDamp) ---
        // scrollY положительное обычно означает прокрутка вверх -> хотим приближать камеру (уменьшать distance)
        if (Mathf.Abs(scrollY) > 0.0001f)
        {
            // более предсказуемое поведение: при скролле меняем targetDistance сразу
            float delta = scrollY * zoomSpeed;
            // инвертировать, если у тебя колёсико в обратную сторону
            targetDistance = Mathf.Clamp(targetDistance - delta, minDistance, maxDistance);
        }

        // Плавно сглаживаем расстояние (без резких "прыжков")
        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, distanceSmoothTime, Mathf.Infinity, Time.deltaTime);

        // --- Положение камеры (ориентированный оффсет) ---
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredOffset = rot * new Vector3(0f, offset.y, -currentDistance);
        Vector3 desiredPos = target.position + desiredOffset;

        // --- COLLISION: SphereCast — предотвращает заезд в стены (мягко поднимает камеру) ---
        Vector3 rayOrigin = target.position + Vector3.up * 0.8f;
        Vector3 dir = (desiredPos - rayOrigin);
        float dist = dir.magnitude;
        if (dist > 0.001f)
        {
            dir.Normalize();
            RaycastHit hit;
            if (Physics.SphereCast(rayOrigin, cameraCollisionRadius, dir, out hit, dist))
            {
                // Сдвинуть камеру чуть вперед от точки столкновения — чтобы не "прилипать" к стене
                desiredPos = hit.point + hit.normal * collisionOffset;
            }
        }

        // --- POSITION smoothing: адаптивный (быстрая реакция при вводе) ---
        float posSmooth = positionSmoothTime;
        if (inputMag > inputResponsivenessThreshold) posSmooth *= 0.28f;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref positionVelocity, posSmooth);

        // --- ROTATION target: смотреть на цель (слегка выше центра) ---
        Vector3 lookTarget = target.position + Vector3.up * (offset.y * 0.5f);
        Quaternion lookRot = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
        // Slerp для мягкой финальной ориентации (высокая скорость, но без плеча "плыв")
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 1f - Mathf.Exp(-18f * Time.deltaTime));
    }
}
