using UnityEngine;

public class AICombat
{
    readonly TankAI owner;
    public AICombat(TankAI owner) { this.owner = owner; }

    public void AimAt(Transform t)
    {
        if (owner.turret != null)
        {
            Vector3 dir = t.position - owner.turret.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
                owner.turret.rotation = Quaternion.RotateTowards(owner.turret.rotation, targetRot, owner.rotationSpeed * Time.deltaTime);
            }
        }

        if (owner.gun != null && owner.gunEnd != null)
        {
            Vector3 predicted = PredictTargetPosition(t);
            Vector3 adjustedTarget = new(predicted.x, owner.gun.position.y, predicted.z);
            Vector3 localDir = owner.turret.InverseTransformDirection(adjustedTarget - owner.gun.position);

            float pitch;
            if (owner.gunUsesLocalXForPitch)
                pitch = Mathf.Atan2(localDir.y, localDir.z) * Mathf.Rad2Deg;
            else
                pitch = Mathf.Atan2(localDir.y, localDir.x) * Mathf.Rad2Deg;

            pitch = Mathf.Clamp(pitch, owner.minGunAngle, owner.maxGunAngle);
            Vector3 currentEuler = owner.gun.localEulerAngles;
            float newPitch = Mathf.MoveTowardsAngle(currentEuler.x, pitch, owner.rotationSpeed * Time.deltaTime);
            owner.gun.localEulerAngles = new Vector3(newPitch, currentEuler.y, currentEuler.z);
        }
    }

    public Vector3 PredictTargetPosition(Transform t)
    {
        Vector3 targetPos = t.position;
        Vector3 targetVel = Vector3.zero;
        if (owner.targetAgent != null) targetVel = owner.targetAgent.velocity;
        else
        {
            if (t.TryGetComponent<Rigidbody>(out var rb)) targetVel = rb.linearVelocity;
        }

        Vector3 dir = targetPos - owner.gunEnd.position;
        float dist = dir.magnitude;
        float time = dist / Mathf.Max(0.001f, owner.projectileSpeed);

        for (int i = 0; i < 3; i++)
        {
            Vector3 predicted = targetPos + targetVel * time;
            Vector3 toPred = predicted - owner.gunEnd.position;
            float d = toPred.magnitude;
            time = d / Mathf.Max(0.001f, owner.projectileSpeed);
        }

        return targetPos + targetVel * time;
    }
}
