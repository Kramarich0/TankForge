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

    private Transform fillBar;
    private Transform fillBarBack; // копия
    private Transform background;

    void Start()
    {
        if (!targetHealth) return;

        // Фон
        background = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
        background.SetParent(transform, false);
        background.localScale = new Vector3(width, height, 1f);
        var bgRenderer = background.GetComponent<MeshRenderer>();
        if (bgRenderer != null)
        {
            var bgMat = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Standard"));
            bgMat.color = Color.black;
            bgMat.SetInt("_CullMode", 0); // двустороннее
            bgRenderer.material = bgMat;
        }
        DestroyImmediate(background.GetComponent<Collider>());

        // Основной fillBar
        fillBar = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
        fillBar.SetParent(transform, false);
        fillBar.localScale = new Vector3(width, height, 1f);
        fillBar.localPosition = new Vector3(-width / 2f, 0, -0.01f);
        var fillRenderer = fillBar.GetComponent<MeshRenderer>();
        if (fillRenderer != null)
        {
            var mat = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Standard"));
            mat.color = (targetTeam != null && targetTeam.team == Team.Friendly) ? Color.green : Color.red;
            mat.SetInt("_CullMode", 0); // двустороннее
            fillRenderer.material = mat;
        }
        DestroyImmediate(fillBar.GetComponent<Collider>());

        // Копия для задней стороны
        fillBarBack = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
        fillBarBack.SetParent(transform, false);
        fillBarBack.localScale = fillBar.localScale;
        fillBarBack.localPosition = fillBar.localPosition + new Vector3(0, 0, 0.001f); // чуть вперёд
        fillBarBack.localRotation = Quaternion.Euler(0, 180f, 0); // повернули задом

        var backMat = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Standard"));
        backMat.color = fillRenderer.material.color;
        backMat.SetInt("_CullMode", 0); // двустороннее
        var fillBackRenderer = fillBarBack.GetComponent<MeshRenderer>();
        if (fillBackRenderer != null)
        {
            fillBackRenderer.material = backMat;
        }
        DestroyImmediate(fillBarBack.GetComponent<Collider>());
    }

    void Update()
    {
        if (!targetHealth) return;

        float ratio = Mathf.Clamp01(targetHealth.currentHealth / targetHealth.maxHealth);

        if (fillBar != null)
        {
            fillBar.localScale = new Vector3(width * ratio, height, 1f);
            fillBar.localPosition = new Vector3(-width / 2f + (width * ratio) / 2f, 0, -0.01f);

            fillBarBack.localScale = fillBar.localScale;
            fillBarBack.localPosition = fillBar.localPosition + new Vector3(0, 0, 0.001f);

            // цвет
            Color newColor = (targetTeam != null && targetTeam.team == Team.Friendly) ? Color.green : Color.red;
            fillBar.GetComponent<MeshRenderer>().material.color = newColor;
            fillBarBack.GetComponent<MeshRenderer>().material.color = newColor;
        }
    }
}
