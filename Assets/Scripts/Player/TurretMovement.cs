using UnityEngine;

public class TurretController : MonoBehaviour
{
    public Transform cameraTransform; // цель/камера
    public Transform turret;          // сам объект башни (pivot должен быть в основании)
    public float rotationSpeed = 80f; // градусов в секунду
    public float snapAngle = 1f;
    public bool invertTurretForward = false;

    void LateUpdate()
    {
        if (cameraTransform == null || turret == null || turret.parent == null) return;

        Transform parent = turret.parent;

        // направление от башни к точке на камере в мировых координатах
        Vector3 worldDir = cameraTransform.position - turret.position;
        if (worldDir.sqrMagnitude < 0.0001f) return;

        // переводим направление в локальные координаты родителя (чтобы учитывать наклон корпуса)
        Vector3 localDir = parent.InverseTransformDirection(worldDir);

        // проецируем на локальную "горизонтальную" плоскость родителя (убираем вертикальную составляющую)
        localDir.y = 0f;
        if (localDir.sqrMagnitude < 0.0001f) return;

        localDir.Normalize();
        if (invertTurretForward) localDir = -localDir;

        // вычисляем целевой угол в локальных координатах (y)
        float targetAngle = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;

        // читаем текущий локальный угол Y башни
        float currentAngle = turret.localEulerAngles.y;
        // Mathf.DeltaAngle корректно работает при переходе через 360
        float angleDiff = Mathf.DeltaAngle(currentAngle, targetAngle);

        if (Mathf.Abs(angleDiff) <= snapAngle)
        {
            turret.localEulerAngles = new Vector3(0f, targetAngle, 0f);
        }
        else
        {
            // плавный поворот с ограничением скорости
            float step = rotationSpeed * Time.deltaTime;
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, step);
            turret.localEulerAngles = new Vector3(0f, newAngle, 0f);
        }
    }
}
