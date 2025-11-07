using UnityEngine;

public class TurretController : MonoBehaviour
{
    public Transform cameraTransform;  
    public Transform turret;           

    public float rotationSpeed = 30f;  
    public float snapAngle = 1f;       
    public bool invertTurretForward = false;

    void LateUpdate()
    {
        if (cameraTransform == null || turret == null) return;

        // Вектор к цели, проецируем на горизонтальную плоскость
        Vector3 dir = cameraTransform.position - turret.position;
        dir = Vector3.ProjectOnPlane(dir, Vector3.up); 
        if (dir.sqrMagnitude < 0.0001f) return;

        // Целевая rotation только по Y
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized * (invertTurretForward ? -1f : 1f));
        
        // Сохраняем текущие X и Z, меняем только Y
        Vector3 euler = turret.rotation.eulerAngles;
        float angleDiff = Mathf.DeltaAngle(euler.y, targetRot.eulerAngles.y);

        if (Mathf.Abs(angleDiff) <= snapAngle)
        {
            euler.y = targetRot.eulerAngles.y;
        }
        else
        {
            float step = rotationSpeed * Time.deltaTime;
            euler.y = Mathf.MoveTowardsAngle(euler.y, targetRot.eulerAngles.y, step);
        }

        turret.rotation = Quaternion.Euler(euler);
    }
}
