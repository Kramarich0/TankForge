using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 5f;
    public int damage = 20;

    private bool hasHit = false;

    void Start()
    {
        gameObject.SetActive(true);
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * speed;
        }
        Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;      
        hasHit = true;

        IDamageable target = collision.collider.GetComponent<IDamageable>();
        if (target == null)
            target = collision.collider.GetComponentInParent<IDamageable>();

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

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;
        Destroy(gameObject);
    }

    void Explode()
    {
    }
}
