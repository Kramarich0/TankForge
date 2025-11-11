using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HealthMarkerSimple : MonoBehaviour
{
    [Header("Target")]
    public TankHealth targetHealth;
    public TeamComponent targetTeam;

    [Header("Size")]
    public float width = 1f;
    public float height = 0.15f;

    private MeshRenderer fillRenderer;
    private MeshRenderer fillBackRenderer;
    private Camera mainCamera;

    void Start()
    {
        if (!targetHealth) return;
        FindMainCamera();
        CreateMarker();
    }

    void Update()
    {
        if (!targetHealth) return;
        FindMainCamera();

        if (mainCamera != null)
        {
            transform.LookAt(mainCamera.transform);
            transform.Rotate(0, 180f, 0);
        }

        float ratio = Mathf.Clamp01(targetHealth.currentHealth / targetHealth.maxHealth);
        var fillScale = new Vector3(width * ratio, height, 1f);
        var fillPosition = new Vector3(-width / 2f + (width * ratio) / 2f, 0, -0.01f);

        transform.Find("FillBar").localScale = fillScale;
        transform.Find("FillBar").localPosition = fillPosition;
        transform.Find("FillBarBack").localScale = fillScale;
        transform.Find("FillBarBack").localPosition = fillPosition + new Vector3(0, 0, 0.001f);

        Color newColor = (targetTeam != null && targetTeam.team == TeamEnum.Friendly)
            ? Color.green
            : Color.red;

        if (fillRenderer != null) fillRenderer.material.color = newColor;
        if (fillBackRenderer != null) fillBackRenderer.material.color = newColor;
    }

    void CreateMarker()
    {
        var background = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
        background.name = "Background";
        background.SetParent(transform, false);
        background.localScale = new Vector3(width, height, 1f);
        var bgMat = new Material(Shader.Find("Unlit/Color")) { color = Color.black };
        background.GetComponent<MeshRenderer>().material = bgMat;
        DestroyImmediate(background.GetComponent<Collider>());

        var fillBar = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
        fillBar.name = "FillBar";
        fillBar.SetParent(transform, false);
        fillBar.localPosition = new Vector3(-width / 2f, 0, -0.01f);
        fillRenderer = fillBar.GetComponent<MeshRenderer>();
        var fillMat = new Material(Shader.Find("Unlit/Color"))
        {
            color = (targetTeam != null && targetTeam.team == TeamEnum.Friendly)
                ? Color.green
                : Color.red
        };
        fillRenderer.material = fillMat;
        DestroyImmediate(fillBar.GetComponent<Collider>());

        var fillBarBack = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
        fillBarBack.name = "FillBarBack";
        fillBarBack.SetParent(transform, false);
        fillBackRenderer = fillBarBack.GetComponent<MeshRenderer>();
        fillBarBack.GetComponent<MeshFilter>().mesh = fillBar.GetComponent<MeshFilter>().mesh;
        fillBarBack.localScale = Vector3.one;
        fillBarBack.localRotation = Quaternion.Euler(0, 180f, 0);
        fillBackRenderer.material = new Material(fillMat) { color = fillMat.color };
        DestroyImmediate(fillBarBack.GetComponent<Collider>());
    }

    void FindMainCamera()
    {
        if (Camera.main != null)
        {
            mainCamera = Camera.main;
            return;
        }

        var brain = FindFirstObjectByType<Unity.Cinemachine.CinemachineBrain>(); mainCamera = brain != null ? brain.OutputCamera : null;
    }
}