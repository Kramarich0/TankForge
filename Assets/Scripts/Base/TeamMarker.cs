using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TeamMarker : MonoBehaviour
{
    [Header("Target / Pivot")]
    [Tooltip("Кастомный пустой объект (pivot) — маркер будет ровно на нём")]
    public Transform pivot;

    [Header("Appearance")]
    public TeamComponent teamComp;
    public float size = 0.7f;
    public Vector3 localOffset = Vector3.zero;
    public bool faceCamera = false;

    Mesh mesh;
    MeshRenderer mr;

    void OnEnable()
    {
        mr = GetComponent<MeshRenderer>();

        var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                     Shader.Find("Unlit/Color") ??
                     Shader.Find("Standard");

        if (mr.sharedMaterial == null)
            mr.material = new Material(shader);
        else
            mr.material = new Material(mr.sharedMaterial);


        mr.sharedMaterial.doubleSidedGI = true;
        mr.sharedMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);

        CreateMeshIfNeeded();
        GetComponent<MeshFilter>().sharedMesh = mesh;


        if (teamComp == null) teamComp = GetComponentInParent<TeamComponent>();


        if (pivot != null && pivot.IsChildOf(transform))
        {

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

            Vector3 dirToCam = Camera.main.transform.position - transform.position;
            if (dirToCam != Vector3.zero)
            {

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
            mr.sharedMaterial.color = (teamComp.team == TeamEnum.Friendly) ? Color.green : Color.red;
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
                new Vector3(0f, 1f, 0f),
                new Vector3(-0.5f, 0f, 0f),
                new Vector3(0.5f, 0f, 0f),
                new Vector3(0f, 1f, 0f),
                new Vector3(-0.5f, 0f, 0f),
                new Vector3(0.5f, 0f, 0f)
            },
            triangles = new[]
            {
                0, 2, 1,
                3, 4, 5
            }
        };

        mesh.RecalculateNormals();
    }


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
