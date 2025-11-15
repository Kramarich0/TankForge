using UnityEngine;

public class AIPerception
{
    readonly TankAI owner;
    public AIPerception(TankAI owner) { this.owner = owner; }

    public void FindNearestEnemy()
    {
        TeamComponent[] all = Object.FindObjectsByType<TeamComponent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        float bestDist = float.MaxValue;
        Transform best = null;
        foreach (var tc in all)
        {
            if (tc == null || tc == owner.teamComp) continue;
            if (tc.team == owner.teamComp.team) continue;
            float d = Vector3.SqrMagnitude(tc.transform.position - owner.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = tc.transform;
            }
        }
        owner.currentTarget = best;
        if (owner.currentTarget != null) owner.targetAgent = owner.currentTarget.GetComponent<UnityEngine.AI.NavMeshAgent>();
    }

    public void FindNearestCapturePoint()
    {
        Collider[] captureColliders = Physics.OverlapSphere(owner.transform.position, owner.capturePointDetectionRadius, owner.capturePointsLayer);
        float bestDist = float.MaxValue;
        CapturePoint best = null;

        foreach (var col in captureColliders)
        {
            CapturePoint cp = col.GetComponent<CapturePoint>();
            if (cp == null) continue;

            if (cp.GetControllingTeam() == owner.teamComp.team) continue;

            float d = Vector3.SqrMagnitude(cp.transform.position - owner.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = cp;
            }
        }

        owner.currentCapturePointTarget = best;
        if (owner.debugLogs && best != null) UnityEngine.Debug.Log($"[AI] Found capture point: {best.gameObject.name} at distance {Mathf.Sqrt(bestDist):F1}");
    }

    public bool HasLineOfSight(Transform t)
    {
        if (t == null || owner.gunEnd == null) return false;

        Vector3 from = owner.gunEnd.position + owner.gunEnd.forward * 0.15f;
        Vector3 to = t.position + Vector3.up * 1.2f;
        Vector3 dir = to - from;
        float maxDist = Mathf.Min(owner.ShootRange, dir.magnitude);

        RaycastHit[] hits = Physics.RaycastAll(from, dir.normalized, maxDist);
        if (hits == null || hits.Length == 0)
        {
            return true;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            var hitTransform = hit.collider.transform;

            if (hit.collider.isTrigger) continue;

            if (hitTransform.IsChildOf(owner.transform)) continue;

            if (hitTransform == t || hitTransform.IsChildOf(t))
                return true;

            if (hit.collider.TryGetComponent<TeamComponent>(out var team))
            {
                return team.team != owner.teamComp.team;
            }

            return false;
        }

        return true;
    }


    public void DrawGizmos()
    {
        if (!owner.debugGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(owner.transform.position, owner.DetectionRadius);

        if (owner.currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(owner.transform.position, owner.currentTarget.position);
        }

        if (owner.currentCapturePointTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(owner.transform.position, owner.currentCapturePointTarget.transform.position);
        }
    }
}
