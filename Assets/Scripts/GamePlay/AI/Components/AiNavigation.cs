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
        if (owner.agent == null)
        {
            if (owner.debugLogs) Debug.LogWarning("[AI] MoveTo called but agent is null.");
            return;
        }

        if (!owner.navAvailable || !owner.agent.isOnNavMesh)
        {
            if (owner.debugLogs) Debug.Log("[AI] Nav unavailable — skipping MoveTo.");
            return;
        }

        owner.agent.isStopped = false;
        owner.agent.updatePosition = true;

        bool needSet = true;
        if (owner.agent.hasPath && !owner.agent.pathPending)
        {
            Vector3 curDest = owner.agent.path.corners.Length > 0 ? owner.agent.path.corners[owner.agent.path.corners.Length - 1] : owner.agent.destination;
            if (Vector3.Distance(curDest, position) < 0.5f)
                needSet = false;
        }

        if (needSet)
        {
            owner.agent.SetDestination(position);
        }

        if (owner.agent.hasPath && !owner.agent.pathPending)
        {
            float vel = owner.agent.velocity.magnitude;
            if (vel < 0.05f)
            {
                // try forcing re-path once
                if (owner.debugLogs) Debug.Log("[AI] Agent velocity near zero while hasPath — forcing re-path.");
                owner.agent.ResetPath();
                owner.agent.SetDestination(position);
                owner.agent.isStopped = false;
            }
        }

        AlignBodyToVelocity();
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
