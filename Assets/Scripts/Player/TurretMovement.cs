using UnityEngine;

/// <summary>
/// Плавное вращение турели по Y, чтобы смотреть туда, куда смотрит камера (с Raycast'ом).
/// - Оставляет X/Z без изменений (турель вращается только вокруг Y).
/// - Берёт точку прицеливания из cameraTransform.forward через Raycast (если есть попадание) или дальнюю точку.
/// </summary>
public class TurretController : MonoBehaviour
{
    public Transform cameraTransform; // камера/объект, чьим взглядом целимся
    public Transform turret;          // объект поворота турели (pivot)
    public float rotationSpeed = 60f; // deg/sec
    public float maxDistance = 2000f;
    public LayerMask aimMask = ~0; // что считается поверхностью для луча (по умолчанию всё)

    void LateUpdate()
    {
        if (cameraTransform == null || turret == null) return;

        // Получаем цель: либо попадание рейка, либо точка далеко по направлению взгляда камеры
        Vector3 targetPoint;
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, aimMask))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = cameraTransform.position + cameraTransform.forward * maxDistance;
        }

        // Направление к цели по горизонтали (проекция на плоскость Y=const)
        Vector3 dir = targetPoint - turret.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion desired = Quaternion.LookRotation(dir.normalized, Vector3.up);
        // Оставляем только Y-компонент
        float desiredY = desired.eulerAngles.y;
        float currentY = turret.rotation.eulerAngles.y;
        float newY = Mathf.MoveTowardsAngle(currentY, desiredY, rotationSpeed * Time.deltaTime);

        Vector3 e = turret.rotation.eulerAngles;
        e.y = newY;
        turret.rotation = Quaternion.Euler(e);
    }
}
