using UnityEngine;
using UnityEngine.AI;

public class AIStateHandler
{
    readonly TankAI owner;
    readonly AIPerception perception;
    readonly AINavigation navigation;
    readonly AICombat combat;
    readonly AIWeapons weapons;

    public AIStateHandler(TankAI owner, AIPerception perception, AINavigation navigation, AICombat combat, AIWeapons weapons)
    {
        this.owner = owner;
        this.perception = perception;
        this.navigation = navigation;
        this.combat = combat;
        this.weapons = weapons;
    }

    public void UpdateState()
    {
        owner.scanTimer -= Time.deltaTime;
        if (owner.scanTimer <= 0f)
        {
            owner.scanTimer = owner.scanInterval;
            perception.FindNearestEnemy();
            perception.FindNearestCapturePoint();
        }

        Transform target = DetermineTarget(out TankAI.AIState nextState);
        owner.currentState = nextState;

        if (nextState == TankAI.AIState.Fighting && target != null)
        {
            combat.AimAt(target);

            if (Time.time >= owner.nextFireTime)
            {
                bool los = perception.HasLineOfSight(target);
                Vector3 aimDir = (target.position - owner.gunEnd.position).normalized;
                float angle = Vector3.Angle(owner.gunEnd.forward, aimDir);

                float shootAngleThreshold = owner.enableStrafeWhileShooting ? 25f : 20f;

                if (owner.debugLogs) Debug.Log($"[AI] LOS={los}, angle={angle:F1}, threshold={shootAngleThreshold}");

                if (los && angle < shootAngleThreshold)
                {
                    weapons.ShootAt(target);
                    owner.nextFireTime = Time.time + 1f / Mathf.Max(0.0001f, owner.fireRate);
                }
            }


            if (owner.enableStrafeWhileShooting)
            {
                Vector3 strafePoint = GetStrafePoint(target, owner.strafeRadius, owner.strafeSpeed, owner.strafePhase);

                navigation.MoveTo(strafePoint);

                if (owner.agent != null)
                    owner.agent.stoppingDistance = Mathf.Max(0.3f, owner.shootRange * 0.05f);
            }
            else
            {
                navigation.MoveTo(target.position);

                if (owner.agent != null)
                {
                    float dist = Vector3.Distance(owner.transform.position, target.position);
                    owner.agent.stoppingDistance = Mathf.Clamp(owner.shootRange * 0.12f, 0.3f, owner.shootRange * 0.4f);

                    if (dist < owner.shootRange * 0.45f)
                        owner.agent.speed = owner.moveSpeed * 0.45f;
                    else
                        owner.agent.speed = owner.moveSpeed;
                }
            }
        }
        else if (target != null)
        {
            navigation.MoveTo(target.position);

            if (owner.agent != null)
                owner.agent.stoppingDistance = 0f;
        }
        else
        {
            navigation.PatrolRandom();
        }
    }

    Transform DetermineTarget(out TankAI.AIState nextState)
    {
        if (owner.currentCapturePointTarget != null)
        {
            float distToCapturePoint = Vector3.Distance(owner.transform.position, owner.currentCapturePointTarget.transform.position);
            float distToEnemy = owner.currentTarget != null ? Vector3.Distance(owner.transform.position, owner.currentTarget.position) : float.MaxValue;

            if (distToCapturePoint < distToEnemy || distToEnemy > owner.shootRange * 1.5f)
            {
                nextState = TankAI.AIState.Moving;
                return owner.currentCapturePointTarget.transform;
            }
            else if (distToEnemy < owner.shootRange)
            {
                nextState = TankAI.AIState.Fighting;
                return owner.currentTarget;
            }
        }

        if (owner.player != null)
        {
            nextState = TankAI.AIState.Fighting;
            return owner.player;
        }

        if (owner.currentTarget != null)
        {
            float distToEnemy = Vector3.Distance(owner.transform.position, owner.currentTarget.position);
            nextState = (distToEnemy <= owner.shootRange) ? TankAI.AIState.Fighting : TankAI.AIState.Moving;
            return owner.currentTarget;
        }

        nextState = TankAI.AIState.Patrolling;
        return null;
    }


    public void OnDrawGizmos()
    {
        if (!owner.debugGizmos) return;
        perception.DrawGizmos();
        navigation.DrawGizmos();
    }

    Vector3 GetStrafePoint(Transform target, float radius, float speed, float phaseOffset)
    {
        if (target == null) return owner.transform.position;

        float phase = (Time.time * Mathf.Max(0.001f, speed) + phaseOffset) % 1f;
        float angle = phase * Mathf.PI * 2f;

        Vector3 toBot = owner.transform.position - target.position;
        Vector3 flat = Vector3.ProjectOnPlane(toBot, Vector3.up);
        if (flat.sqrMagnitude < 0.001f)
            flat = -owner.transform.forward;

        flat.Normalize();
        Vector3 perp = Vector3.Cross(flat, Vector3.up).normalized;

        Vector3 offset = (perp * Mathf.Cos(angle) + flat * Mathf.Sin(angle)) * radius;
        return target.position + offset;
    }
}
