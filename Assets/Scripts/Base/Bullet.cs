using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    public int damage = 20;
    public float lifeTime = 8f;
    public Team shooterTeam = Team.Neutral;

    [Header("Physics")]
    public bool useGravity = true;
    public float gravityMultiplier = 1f; // усиливаем гравитацию при необходимости

    private Rigidbody rb;
    private Collider col;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.useGravity = false; // включим в Initialize
    }

    public void Initialize(Vector3 initialVelocity, Team shooter)
    {
        shooterTeam = shooter;
        rb.linearVelocity = initialVelocity;
        rb.useGravity = true; // включаем сразу, чтобы синхронизировать с CrosshairAim
        Destroy(gameObject, lifeTime);
    }
    
    void FixedUpdate()
    {
        // Добавляем гравитацию вручную для стабильного эффекта
        if (useGravity)
        {
            rb.AddForce(Physics.gravity * (gravityMultiplier - 1f) * rb.mass, ForceMode.Acceleration);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null) return;

        TeamComponent hitTeam = collision.collider.GetComponentInParent<TeamComponent>();
        if (hitTeam != null && hitTeam.team == shooterTeam)
        {
            if (col != null && collision.collider != null)
                Physics.IgnoreCollision(col, collision.collider, true);
            return;
        }

        if (collision.collider.TryGetComponent<IDamageable>(out var dmg))
        {
            dmg.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
