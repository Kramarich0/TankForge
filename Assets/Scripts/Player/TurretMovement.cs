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
        if (cameraTransform == null || turret == null) return;

        Vector3 dir = cameraTransform.forward;
        dir = Vector3.ProjectOnPlane(dir, Vector3.up);

        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized * (invertTurretForward ? -1f : 1f));
        float angleDiff = Mathf.DeltaAngle(turret.rotation.eulerAngles.y, targetRot.eulerAngles.y);

        if (Mathf.Abs(angleDiff) <= snapAngle)
        {
            turret.rotation = Quaternion.Euler(turret.rotation.eulerAngles.x, targetRot.eulerAngles.y, turret.rotation.eulerAngles.z);
        }
        else
        {
            float step = rotationSpeed * Time.deltaTime;
            turret.rotation = Quaternion.RotateTowards(turret.rotation, Quaternion.Euler(0f, targetRot.eulerAngles.y, 0f), step);
        }
    }
}
