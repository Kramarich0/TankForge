using UnityEngine;

public class TreeSpawner : MonoBehaviour
{
    public GameObject treePrefab; // твой Prefab с Collider/Rigidbody
    public int count = 10;       // сколько деревьев
    public Vector3 areaSize = new(50, 0, 50); // размер поля

    void Start()
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = transform.position + new Vector3(
                Random.Range(-areaSize.x / 2, areaSize.x / 2),
                0,
                Random.Range(-areaSize.z / 2, areaSize.z / 2)
            );

            // Находим поверхность (Terrain или плоскость)
            RaycastHit hit;
            if (Physics.Raycast(pos + Vector3.up * 50, Vector3.down, out hit, 100f))
            {
                GameObject tree = Instantiate(treePrefab, hit.point, Quaternion.Euler(0, Random.Range(0, 360), 0));
                tree.transform.parent = transform; // чтобы сцена была аккуратной
            }
        }
    }
}
