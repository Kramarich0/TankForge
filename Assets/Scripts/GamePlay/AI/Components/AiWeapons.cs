using UnityEngine;

public class AIWeapons
{
    readonly TankAI owner;
    public AIWeapons(TankAI owner) { this.owner = owner; }

    public void ShootAt(Transform t)
    {
        if (owner.bulletPool == null || owner.gunEnd == null) return;

        Vector3 predicted = new AICombat(owner).PredictTargetPosition(t);
        Vector3 aim = predicted - owner.gunEnd.position;
        Vector3 horizontal = new(aim.x, 0f, aim.z);
        float distance = horizontal.magnitude;
        float dy = aim.y;

        Vector3 initVelocity;

        if (owner.bulletUseGravity)
        {
            float v = owner.projectileSpeed;
            float g = Mathf.Abs(Physics.gravity.y);
            float d = distance;
            float v4 = v * v * v * v;
            float under = v4 - g * (g * d * d + 2f * dy * v * v);
            if (under >= 0f && d > 0.001f)
            {
                float sq = Mathf.Sqrt(under);
                float lowAngle = Mathf.Atan2(v * v - sq, g * d);
                float highAngle = Mathf.Atan2(v * v + sq, g * d);
                float chosen = Mathf.Min(lowAngle, highAngle);

                Vector3 flatDir = horizontal.normalized;
                Vector3 launchDir = Quaternion.AngleAxis(chosen * Mathf.Rad2Deg, Vector3.Cross(flatDir, Vector3.up)) * flatDir;
                initVelocity = launchDir * v;
            }
            else
            {
                initVelocity = aim.normalized * owner.projectileSpeed;
            }
        }
        else
        {
            initVelocity = aim.normalized * owner.projectileSpeed;
        }

        string shooterDisplay = (owner.teamComp != null && !string.IsNullOrEmpty(owner.teamComp.displayName))
            ? owner.teamComp.displayName
            : owner.gameObject.name;

        Collider[] shooterColliders = owner.GetComponentsInParent<Collider>();

        owner.bulletPool.SpawnBullet(
            owner.gunEnd.position,
            initVelocity,
            owner.teamComp != null ? owner.teamComp.team : TeamEnum.Neutral,
            shooterDisplay,
            owner.bulletDamage,
            shooterColliders
        );

        owner.shootSource?.Play();
    }
}
