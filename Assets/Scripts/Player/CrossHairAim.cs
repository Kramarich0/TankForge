using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Корректно размещает экранный UI-прицел (Image) над точкой попадания луча от gunEnd.
/// Работает с Canvas в режиме ScreenSpace - Camera или Overlay; использует RectTransformUtility.
/// </summary>
public class CrosshairAim : MonoBehaviour
{
    public Transform gunEnd;
    public Camera mainCamera;
    public Canvas canvas; // обязательно назначить (канвас, где находится UI)
    public LayerMask groundMask;
    public float maxDistance = 1000f;

    private RectTransform rectTransform;
    private RectTransform canvasRect;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null) Debug.LogError("CrosshairAimImproved: требуется RectTransform на элементе UI");
        if (mainCamera == null) mainCamera = Camera.main;
        if (canvas == null) Debug.LogError("CrosshairAimImproved: назначьте Canvas (Screen Space - Camera или Overlay)");
        else canvasRect = canvas.GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        if (gunEnd == null || mainCamera == null || rectTransform == null || canvasRect == null) return;

        Ray ray = new Ray(gunEnd.position, gunEnd.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, groundMask))
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(mainCamera, hit.point);

            // Конвертируем screenPoint в локальные координаты canvas
            Vector2 localPoint;
            bool ok = RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out localPoint);
            if (ok)
            {
                rectTransform.localPosition = localPoint;
            }
        }
        else
        {
            // если не попали — можно спрятать прицел или поставить в центр
            // rectTransform.localPosition = Vector2.zero;
        }
    }
}
