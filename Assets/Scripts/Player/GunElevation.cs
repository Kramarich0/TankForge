using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Управление углом возвышения орудия по входу lookAction (обычно та же ось, что и камера).
/// Плавно интерполируется к targetPitch с помощью SmoothDampAngle (без проблем с 0-360).
/// </summary>
public class GunElevation : MonoBehaviour
{
    public InputActionReference lookAction; // привязать Player/Look -> <Mouse>/delta
    public float sensitivity = 0.08f; // градусы/пиксель
    public float smoothSpeed = 12f; // скорость сглаживания угла (больше - быстрее)
    public float minAngle = -10f; // вниз
    public float maxAngle = 20f;  // вверх

    private float currentPitch;
    private float targetPitch;
    private float pitchVelocity;

    void OnEnable()
    {
        lookAction?.action?.Enable();

        float cur = transform.localEulerAngles.x;
        if (cur > 180f) cur -= 360f;
        currentPitch = cur;
        targetPitch = currentPitch;
    }

    void OnDisable() => lookAction?.action?.Disable();

    void Update()
    {
        if (lookAction?.action == null) return;
        Vector2 delta = lookAction.action.ReadValue<Vector2>();

        // Изменяем целевой угол. Минус — чтобы вертикальный ввод совпадал с привычным поведением.
        targetPitch -= delta.y * sensitivity;
        targetPitch = Mathf.Clamp(targetPitch, minAngle, maxAngle);

        // Плавное приближение (угловая версия).
        currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref pitchVelocity, 1f / smoothSpeed);

        // Применяем локальную ротацию по X, сохраняем остальные оси
        Vector3 e = transform.localEulerAngles;
        e.x = currentPitch;
        transform.localEulerAngles = e;
    }
}
