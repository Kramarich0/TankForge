
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    public int damage = 20;
    public float lifeTime = 8f;
    public TeamEnum shooterTeam = TeamEnum.Neutral;
    public string shooterName = null;

    Rigidbody rb;
    Collider col;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.useGravity = true;
    }

    public void Initialize(Vector3 velocity, TeamEnum shooter, string shooterName = null)
    {
        shooterTeam = shooter;
        this.shooterName = shooterName;
        rb.linearVelocity = velocity;
        rb.useGravity = true;
        Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null) return;

        TeamComponent hitTeam = collision.collider.GetComponentInParent<TeamComponent>();
        if (hitTeam != null && hitTeam.team == shooterTeam)
        {

            if (col != null && collision.collider != null)
            {
                Physics.IgnoreCollision(col, collision.collider, true);
            }

            return;
        }

        IDamageable dmg = collision.collider.GetComponentInParent<IDamageable>();
        if (dmg != null)
        {
            if (dmg is TankHealth th)
            {
                th.SetAttacker(!string.IsNullOrEmpty(shooterName) ? shooterName : shooterTeam.ToString());
            }
            dmg.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
