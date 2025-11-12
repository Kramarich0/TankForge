using UnityEngine;
using UnityEngine.Pool;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private int damage = 20;
    [SerializeField] private float lifeTime = 8f;
    [SerializeField] private bool useGravity = true;

    private Rigidbody rb;
    private Collider col;

    private TeamEnum shooterTeam;
    private string shooterName;
    private float lifetimeTimer;
    private ObjectPool<Bullet> pool;
    private readonly List<Collider> ignoredColliders = new();

    public event Action<Bullet> OnBulletExpired;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.useGravity = useGravity;
    }

    public void Initialize(Vector3 velocity, TeamEnum shooter, string shooterName = null, int damage = 20, Collider[] ignoreWith = null)
    {
        rb.linearVelocity = velocity;
        shooterTeam = shooter;
        this.shooterName = shooterName;
        this.damage = damage;
        rb.useGravity = useGravity;

        lifetimeTimer = lifeTime;
        Debug.Log($"[Bullet] Initialize vel={velocity.magnitude:F1} team={shooter} shooterName={shooterName} damage={damage} ignores={ignoreWith?.Length ?? 0}");

        // Игнорируем только коллайдеры стрелка
        if (ignoreWith != null)
        {
            foreach (var oc in ignoreWith)
            {
                if (oc == null) continue;
                Physics.IgnoreCollision(col, oc, true);
                ignoredColliders.Add(oc);
            }
        }
    }


    void FixedUpdate()
    {
        lifetimeTimer -= Time.fixedDeltaTime;
        if (lifetimeTimer <= 0f)
            ReturnToPool();
    }


    void OnCollisionEnter(Collision collision)
    {
        if (collision == null) return;
        Debug.Log($"[Bullet] Collide with {collision.collider.name} (parent={collision.collider.transform.root.name}) shooterTeam={shooterTeam}");

        TeamEnum? targetTeam = null;
        if (collision.collider.GetComponentInParent<ITeamProvider>() is ITeamProvider tp)
            targetTeam = tp.Team;
        else if (collision.collider.GetComponentInParent<TeamComponent>() is TeamComponent tc)
            targetTeam = tc.team;

        if (targetTeam.HasValue && targetTeam.Value == shooterTeam)
        {
            return;
        }

        var damageable = collision.collider.GetComponentInParent<IDamageable>();
        damageable?.TakeDamage(damage, shooterName);

        ReturnToPool();
    }

    public void CleanupBeforeSpawn()
    {
        if (ignoredColliders.Count > 0)
        {
            foreach (var oc in ignoredColliders)
                if (oc != null) Physics.IgnoreCollision(col, oc, false);
            ignoredColliders.Clear();
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        col.enabled = true;
        lifetimeTimer = 0f;
    }

    private void ReturnToPool()
    {
        if (ignoredColliders.Count > 0)
        {
            foreach (var oc in ignoredColliders)
                if (oc != null) Physics.IgnoreCollision(col, oc, false);
            ignoredColliders.Clear();
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        pool?.Release(this);
        OnBulletExpired?.Invoke(this);
        gameObject.SetActive(false);
    }

    public void SetPool(ObjectPool<Bullet> pool)
    {
        this.pool = pool;
    }
}