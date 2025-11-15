using UnityEngine;
using UnityEngine.AI;

public class AINavigation
{
    readonly TankAI owner;
    public AINavigation(TankAI owner) { this.owner = owner; }

    public void AlignBodyToVelocity()
    {
        if (owner.body != null && owner.agent != null)
        {
            Vector3 vel = owner.agent.velocity;
            if (vel.sqrMagnitude > 0.01f)
            {
                Vector3 forwardDir = vel.normalized * (owner.invertBodyForward ? -1f : 1f);
                Quaternion target = Quaternion.LookRotation(forwardDir);
                owner.body.rotation = Quaternion.RotateTowards(owner.body.rotation, target, owner.rotationSpeed * Time.deltaTime);
            }
        }
    }

    public void AlignBodyToVector(Vector3 vec)
    {
        if (owner.body != null)
        {
            Vector3 forwardDir = vec * (owner.invertBodyForward ? -1f : 1f);
            Quaternion target = Quaternion.LookRotation(forwardDir);
            owner.body.rotation = Quaternion.RotateTowards(owner.body.rotation, target, owner.rotationSpeed * Time.deltaTime);
        }
    }

    public void MoveTo(Vector3 position)
    {
        if (owner.agent != null && owner.navAvailable && owner.agent.isOnNavMesh)
        {
            owner.agent.isStopped = false;
            owner.agent.SetDestination(position);
            AlignBodyToVelocity();
        }
    }

    public void PatrolRandom()
    {
        if (owner.agent != null && owner.navAvailable && owner.agent.isOnNavMesh && !owner.agent.hasPath)
        {
            Vector3 rand = owner.transform.position + Random.insideUnitSphere * 8f;
            if (NavMesh.SamplePosition(rand, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                owner.agent.SetDestination(hit.position);
            AlignBodyToVelocity();
        }
    }

    public void DrawGizmos()
    {
        if (!owner.debugGizmos) return;
        Gizmos.color = Color.green;
        if (owner.agent != null)
            Gizmos.DrawLine(owner.transform.position, owner.transform.position + owner.agent.velocity);
    }
}
