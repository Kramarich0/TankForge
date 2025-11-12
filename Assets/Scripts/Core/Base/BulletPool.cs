using UnityEngine;
using UnityEngine.Pool;

public class BulletPool : MonoBehaviour
{
    [SerializeField] private Bullet prefab;
    [SerializeField] private int initialSize = 20;

    private ObjectPool<Bullet> pool;

    void Awake()
    {
        pool = new ObjectPool<Bullet>(
            createFunc: () =>
            {
                var bullet = Instantiate(prefab, transform);
                bullet.gameObject.SetActive(false);
                bullet.SetPool(pool);
                return bullet;
            },

            actionOnGet: bullet =>
            {
                bullet.CleanupBeforeSpawn();
            },

            actionOnRelease: bullet =>
            {
                bullet.gameObject.SetActive(false);
            },

            actionOnDestroy: bullet => Destroy(bullet.gameObject),
            collectionCheck: true,
            defaultCapacity: initialSize,
            maxSize: 500
        );
    }

    public Bullet SpawnBullet(Vector3 position, Vector3 velocity, TeamEnum team, string shooterName, int damage, Collider[] ignoreColliders = null)
    {
        var bullet = pool.Get();
        bullet.transform.SetPositionAndRotation(position, Quaternion.LookRotation(velocity.normalized));

        bullet.Initialize(velocity, team, shooterName, damage, ignoreColliders);

        bullet.gameObject.SetActive(true);
        Debug.Log($"[Pool] Spawn at {position} vel={velocity.magnitude:F1} team={team} damage={damage}");

        return bullet;
    }

}
