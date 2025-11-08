using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 5f;
    public int damage = 20;

    private bool hasHit = false;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }


    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;

        IDamageable target = collision.collider.GetComponent<IDamageable>();
        target ??= collision.collider.GetComponentInParent<IDamageable>();

        if (target != null)
        {
            Debug.Log($"[Bullet] Попадание по {collision.collider.name} (parent {collision.collider.transform.root.name}). Наношу {damage} урона.");
            target.TakeDamage(damage);
        }
        else
        {
            Debug.Log($"[Bullet] Попадание по {collision.collider.name} — IDamageable не найден.");
        }

        Explode();

        if (TryGetComponent<Collider>(out var col)) col.enabled = false;
        if (TryGetComponent<Rigidbody>(out var rb)) rb.linearVelocity = Vector3.zero;
        Destroy(gameObject);
    }

    void Explode()
    {
    }
}
