using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Управление углом возвышения орудия по направлению камеры.
/// Пушка плавно интерполируется к цели, ограничена min/max углами.
/// </summary>
public class GunElevation : MonoBehaviour
{
    public Transform cameraTransform;      // Камера игрока
    public float smoothSpeed = 8f;         // скорость сглаживания
    public float minAngle = -10f;
    public float maxAngle = 20f;

    private float currentPitch;
    private float targetPitch;
    private float pitchVelocity;

    void Start()
    {
        float cur = transform.localEulerAngles.x;
        if (cur > 180f) cur -= 360f;
        currentPitch = cur;
        targetPitch = currentPitch;
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Получаем вертикальный угол камеры в локальных координатах
        float camPitch = cameraTransform.localEulerAngles.x;
        if (camPitch > 180f) camPitch -= 360f;

        // Ограничиваем целевой угол ствола относительно камеры
        targetPitch = Mathf.Clamp(camPitch, minAngle, maxAngle);

        // Плавное приближение
        currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref pitchVelocity, 1f / smoothSpeed);

        // Применяем локальную ротацию по X, остальные оси остаются прежними
        Vector3 e = transform.localEulerAngles;
        e.x = currentPitch;
        transform.localEulerAngles = e;
    }
}
