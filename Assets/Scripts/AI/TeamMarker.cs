using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TeamMarker : MonoBehaviour
{
    [Header("Target / Pivot")]
    [Tooltip("Кастомный пустой объект (pivot) — маркер будет ровно на нём")]
    public Transform pivot;

    [Header("Appearance")]
    public TeamComponent teamComp;       // можно перетащить вручную, скрипт попытается подтянуть сам
    public float size = 0.7f;           // масштаб маркера
    public Vector3 localOffset = Vector3.zero; // смещение относительно pivot (если нужно)
    public bool faceCamera = true;      // поворачивать ли маркер к камере (по умолчанию да)

    // internal
    Mesh mesh;
    MeshRenderer mr;

    void OnEnable()
    {
        mr = GetComponent<MeshRenderer>();

        // выбираем рабочий шейдер (URP/legacy/Standard)
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                     Shader.Find("Unlit/Color") ??
                     Shader.Find("Standard");

        // создаём материал-инстанс, если его нет (или заменяем, чтобы не перетирать общий материал)
        if (mr.sharedMaterial == null)
            mr.material = new Material(shader);
        else
            mr.material = new Material(mr.sharedMaterial);

        // делаем двухсторонний (без отсечения) чтобы треугольник был виден с любой стороны
        mr.sharedMaterial.doubleSidedGI = true;
        mr.sharedMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);

        CreateMeshIfNeeded();
        GetComponent<MeshFilter>().sharedMesh = mesh;

        // если teamComp не назначен, попробуем найти в родителях
        if (teamComp == null) teamComp = GetComponentInParent<TeamComponent>();

        // Защита: если маркер по ошибке родитель pivot'а, делаем его sibling'ом (чтобы маркер не был родителем танка)
        if (pivot != null && pivot.IsChildOf(transform))
        {
            // переставляем маркер в тот же уровень, где pivot (чтобы не быть его родителем)
            transform.SetParent(pivot.parent, true);
        }

        UpdateColor();
    }

    void Update()
    {
        if (pivot == null) return;
        if (teamComp == null) teamComp = GetComponentInParent<TeamComponent>();

        transform.position = pivot.position + (pivot.rotation * localOffset);

        if (faceCamera && Camera.main != null)
        {
            // Считаем вектор от маркера к камере
            Vector3 dirToCam = Camera.main.transform.position - transform.position;
            if (dirToCam != Vector3.zero)
            {
                // создаём rotation так, чтобы фронт маркера (+Z) смотрел на камеру
                transform.rotation = Quaternion.LookRotation(dirToCam, Vector3.up);
            }
        }
        else
        {
            transform.rotation = Quaternion.identity;
        }

        transform.localScale = Vector3.one * Mathf.Max(0.0001f, size);
        UpdateColor();
    }

    void UpdateColor()
    {
        if (mr == null) mr = GetComponent<MeshRenderer>();
        if (mr == null) return;

        if (teamComp == null)
        {
            mr.sharedMaterial.color = Color.white;
        }
        else
        {
            mr.sharedMaterial.color = (teamComp.team == Team.Friendly) ? Color.green : Color.red;
        }
    }

    private void CreateMeshIfNeeded()
    {
        if (mesh != null) return;

        mesh = new Mesh
        {
            name = "MarkerTriangle",
            vertices = new[]
            {
                new Vector3(0f, 1f, 0f),    // верх
                new Vector3(-0.5f, 0f, 0f), // низ левый
                new Vector3(0.5f, 0f, 0f),  // низ правый
                new Vector3(0f, 1f, 0f),    // верх обратной стороны
                new Vector3(-0.5f, 0f, 0f), // низ левый обратной стороны
                new Vector3(0.5f, 0f, 0f)   // низ правый обратной стороны
            },
            triangles = new[]
            {
                0, 2, 1, // первая сторона
                3, 4, 5  // обратная сторона
            }
        };

        mesh.RecalculateNormals();
    }

    // Debug: показывает линию от pivot до маркера в сцене
    void OnDrawGizmosSelected()
    {
        if (pivot != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pivot.position, transform.position);
            Gizmos.DrawSphere(pivot.position, 0.05f);
        }
    }
}
