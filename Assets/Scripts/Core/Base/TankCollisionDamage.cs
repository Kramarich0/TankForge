using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TeamComponent))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(IDamageable))]
public class TankCollisionDamage : MonoBehaviour
{
    [Header("Collision damage settings")]
    public float minCollisionSpeed = 3f;
    [Tooltip("Multiplier to tune damage scale. Start with 0.01 - 0.1 and tune.")]
    public float damageMultiplier = 0.02f;
    public bool debugLogs = false;

    private readonly HashSet<int> processedCollisionIds = new();

    Rigidbody rb;
    internal TeamComponent teamComp;
    IDamageable selfDamageable;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        teamComp = GetComponent<TeamComponent>();
        selfDamageable = GetComponent<IDamageable>();

        if (rb == null) Debug.LogError("[TankCollisionDamage] Rigidbody missing!");
        if (teamComp == null) Debug.LogError("[TankCollisionDamage] TeamComponent missing!");
        if (selfDamageable == null) Debug.LogError("[TankCollisionDamage] TankHealth missing!");
    }

    private void OnCollisionEnter(Collision collision)
    {

        if (collision == null) return;
        if (collision.rigidbody == null) { if (debugLogs) Debug.Log("[TC] collision.rigidbody == null"); return; }


        int otherId = collision.collider.GetInstanceID();
        if (processedCollisionIds.Contains(otherId)) return;
        processedCollisionIds.Add(otherId);

        StartCoroutine(RemoveProcessedAfterFixedUpdate(otherId));


        IDamageable otherDamageable = collision.collider.GetComponentInParent<IDamageable>();
        if (otherDamageable == null) { if (debugLogs) Debug.Log("[TC] other has no IDamageable"); return; }

        TeamComponent otherTeam = collision.collider.GetComponentInParent<TeamComponent>();
        if (otherTeam != null && teamComp != null && otherTeam.team == teamComp.team)
        {
            if (debugLogs) Debug.Log("[TC] collision with teammate - ignored");
            return;
        }

        Rigidbody otherRb = collision.rigidbody;
        if (otherRb == null) { if (debugLogs) Debug.Log("[TC] other has no Rigidbody"); return; }

        float impactSpeed = collision.relativeVelocity.magnitude;

        if (impactSpeed < minCollisionSpeed)
        {
            if (debugLogs) Debug.Log($"[TC] impactSpeed {impactSpeed:F2} < min {minCollisionSpeed}");
            return;
        }

        float massThis = rb.mass;
        float massOther = otherRb.mass;

        int damageToOther = Mathf.Max(1, Mathf.RoundToInt(0.5f * massThis * impactSpeed * impactSpeed * damageMultiplier));

        int damageToThis = Mathf.Max(1, Mathf.RoundToInt(0.5f * massOther * impactSpeed * impactSpeed * damageMultiplier));

        if (debugLogs)
        {
            Debug.Log($"[TC] Collide: {name} (m={massThis}) <-> {collision.collider.name} (m={massOther}) | v={impactSpeed:F2} => dmgToOther={damageToOther}, dmgToThis={damageToThis}");
        }

        otherDamageable.TakeDamage(damageToOther, source: gameObject.name);
        selfDamageable.TakeDamage(damageToThis, source: collision.gameObject.name);
    }

    private IEnumerator RemoveProcessedAfterFixedUpdate(int id)
    {
        yield return new WaitForFixedUpdate();
        processedCollisionIds.Remove(id);
    }
}
