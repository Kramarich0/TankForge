using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Стабильная камера-следователь.
/// - Использует LateUpdate (камера должна следовать после всех Update).
/// - Крутится по yaw/pitch, с clamp по pitch.
/// - Позиция сглаживается через SmoothDamp, поворот через Slerp.
/// - Ожидает, что lookAction отдаёт delta (например <Mouse>/delta в New Input System).
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // обычно пустой объект на танке (target.position = центр корпуса)

    [Header("Input")]
    public InputActionReference lookAction; // привязать Player/Look -> <Mouse>/delta

    [Header("Orbit")]
    public float distance = 10f;
    public float height = 4.5f;
    public Vector2 pitchClamp = new Vector2(-20f, 60f); // min, max
    public float sensitivity = 0.12f; // degrees per pixel
    public float rotationLerpSpeed = 12f; // ускорение поворота
    public float positionSmoothTime = 0.12f; // сглаживание позиции

    private float yaw;   // поворот вокруг Y
    private float pitch; // вертикальный угол
    private Vector3 currentVelocity;

    void OnEnable() => lookAction?.action?.Enable();
    void OnDisable() => lookAction?.action?.Disable();

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("CameraFollowImproved: target не задан!");
            enabled = false;
            return;
        }

        // Инициализация углов по текущему положению камеры относительно цели
        Vector3 dir = (transform.position - target.position);
        Quaternion init = Quaternion.LookRotation(dir);
        Vector3 e = init.eulerAngles;
        yaw = e.y;
        pitch = e.x;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector2 look = Vector2.zero;
        if (lookAction?.action != null)
        {
            look = lookAction.action.ReadValue<Vector2>(); // ожидаем delta (px)
        }

        // Обновляем углы — sensitivity интерпретируем как градусы/пиксель
        yaw += look.x * sensitivity;
        pitch -= look.y * sensitivity; // обычно инвертируем Y для "нативного" поведения мыши
        pitch = Mathf.Clamp(pitch, pitchClamp.x, pitchClamp.y);

        // Вычисляем желаемую позицию в мировых координатах: вращаем вектор назад от цели
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPos = target.position + rot * new Vector3(0f, 0f, distance * -1f) + Vector3.up * height;

        // Плавно двигаем камеру
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref currentVelocity, positionSmoothTime);

        // Плавный поворот к цели (чтобы не дергалось при резких движениях)
        Quaternion lookRot = Quaternion.LookRotation((target.position + Vector3.up * (height * 0.3f)) - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationLerpSpeed * Time.deltaTime);
    }
}
