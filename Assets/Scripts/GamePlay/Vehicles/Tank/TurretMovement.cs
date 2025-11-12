using UnityEngine;

public class TurretController : MonoBehaviour
{
    public Transform cameraTransform;
    public Transform turret;
    public float rotationSpeed = 80f;
    public float snapAngle = 1f;
    public bool invertTurretForward = false;

    void LateUpdate()
    {
        if (cameraTransform == null || turret == null || turret.parent == null) return;

        Transform parent = turret.parent;


        Vector3 worldDir = cameraTransform.position - turret.position;
        if (worldDir.sqrMagnitude < 0.0001f) return;


        Vector3 localDir = parent.InverseTransformDirection(worldDir);


        localDir.y = 0f;
        if (localDir.sqrMagnitude < 0.0001f) return;

        localDir.Normalize();
        if (invertTurretForward) localDir = -localDir;


        float targetAngle = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;


        float currentAngle = turret.localEulerAngles.y;

        float angleDiff = Mathf.DeltaAngle(currentAngle, targetAngle);

        if (Mathf.Abs(angleDiff) <= snapAngle)
        {
            turret.localEulerAngles = new Vector3(0f, targetAngle, 0f);
        }
        else
        {

            float step = rotationSpeed * Time.deltaTime;
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, step);
            turret.localEulerAngles = new Vector3(0f, newAngle, 0f);
        }
    }
}
