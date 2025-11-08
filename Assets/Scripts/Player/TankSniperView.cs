using UnityEngine;
using UnityEngine.InputSystem;

public class TankSniperView : MonoBehaviour
{
    public InputActionReference toggleViewAction;
    public InputActionReference zoomAction; // optional, bind to scroll or custom
    public Transform gunEnd;
    public Camera mainCamera;
    public Camera sniperCamera;
    public GameObject crosshairUI;
    public GameObject sniperVignette;

    [Range(0.1f, 60f)] public float sniperFOV = 12f;
    public float transitionSpeed = 10f;
    public float sniperDistanceOffset = 0f; // если нужно сместить камеру чуть назад/вперед при перехвате

    private bool isSniperView = false;
    private float normalFOV;
    private float currentZoom = 0f;
    private float targetZoom = 0f;
    public float zoomSpeed = 5f;
    public float minZoom = -10f;
    public float maxZoom = 10f;

    void Start()
    {
        if (toggleViewAction?.action != null) toggleViewAction.action.Enable();
        if (zoomAction?.action != null) zoomAction.action.Enable();

        if (crosshairUI != null) crosshairUI.SetActive(false);
        if (sniperVignette != null) sniperVignette.SetActive(false);

        if (mainCamera != null) normalFOV = mainCamera.fieldOfView;
        else normalFOV = Camera.main ? Camera.main.fieldOfView : 60f;
    }

    void Update()
    {
        if (toggleViewAction?.action == null)
        {
            // попытка включить
            if (toggleViewAction?.action != null) toggleViewAction.action.Enable();
            return;
        }

        if (toggleViewAction.action.WasPressedThisFrame())
        {
            ToggleSniperView();
        }

        // Zoom input
        if (zoomAction?.action != null)
        {
            Vector2 z = zoomAction.action.ReadValue<Vector2>();
            targetZoom += z.y * 0.5f;
        }
        else
        {
            // fallback: мышь колесико
            float wheel = Input.GetAxis("Mouse ScrollWheel");
            targetZoom += wheel * 3f;
        }
        targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        currentZoom = Mathf.Lerp(currentZoom, targetZoom, Time.deltaTime * zoomSpeed);

        if (isSniperView)
        {
            if (gunEnd != null && sniperCamera != null)
            {
                Vector3 desiredPos = gunEnd.position + gunEnd.forward * sniperDistanceOffset;
                //sniperCamera.transform.position = Vector3.Lerp(sniperCamera.transform.position, desiredPos, 1f - Mathf.Exp(-transitionSpeed * Time.deltaTime));
                sniperCamera.transform.rotation = Quaternion.Slerp(sniperCamera.transform.rotation, gunEnd.rotation, 1f - Mathf.Exp(-transitionSpeed * Time.deltaTime));

                sniperCamera.fieldOfView = Mathf.Lerp(sniperCamera.fieldOfView, Mathf.Max(1f, sniperFOV + currentZoom), Time.deltaTime * transitionSpeed);
            }
        }
        else
        {
            if (mainCamera != null)
            {
                mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, Mathf.Max(1f, normalFOV + currentZoom), Time.deltaTime * (transitionSpeed * 0.6f));
            }
        }
    }

    void ToggleSniperView()
    {
        isSniperView = !isSniperView;

        if (mainCamera != null) mainCamera.enabled = !isSniperView;
        if (sniperCamera != null) sniperCamera.enabled = isSniperView;

        if (crosshairUI != null) crosshairUI.SetActive(isSniperView);
        if (sniperVignette != null) sniperVignette.SetActive(isSniperView);
    }

    void OnDestroy()
    {
        if (toggleViewAction?.action != null && toggleViewAction.action.enabled)
            toggleViewAction.action.Disable();
        if (zoomAction?.action != null && zoomAction.action.enabled)
            zoomAction.action.Disable();
    }
}
