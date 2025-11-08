// Bullet.cs
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    public int damage = 20;
    public float lifeTime = 8f;
    public Team shooterTeam = Team.Neutral;

    Rigidbody rb;
    Collider col;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        // по умолчанию гравитация выключена — будет включена в Initialize если нужно
        rb.useGravity = false;
    }

    public void Initialize(Vector3 velocity, bool useGravity, Team shooter)
    {
        shooterTeam = shooter;
        rb.linearVelocity = velocity;
        rb.useGravity = useGravity;
        Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null) return;

        // Проверка команды по цели
        TeamComponent hitTeam = collision.collider.GetComponentInParent<TeamComponent>();
        if (hitTeam != null && hitTeam.team == shooterTeam)
        {
            // Попали по своему — постарайся игнорировать дальнейшие столкновения с этим коллайдером
            if (col != null && collision.collider != null)
            {
                Physics.IgnoreCollision(col, collision.collider, true);
            }
            // не уничтожаем пулю сразу — она продолжит путь и может попасть по врагу
            return;
        }

        // Попытка нанести урон через IDamageable
        IDamageable dmg = collision.collider.GetComponentInParent<IDamageable>();
        if (dmg != null)
        {
            dmg.TakeDamage(damage);
        }

        // Можно добавить эффекты взрыва тут
        Destroy(gameObject);
    }
}
