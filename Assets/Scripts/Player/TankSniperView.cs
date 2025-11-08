using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TankSniperView : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference toggleViewAction;
    public InputActionReference zoomAction;

    [Header("References")]
    public Transform gunEnd;
    public Camera mainCamera;
    public Camera sniperCamera;
    public GameObject crosshairUI;
    public GameObject sniperVignette; // GameObject вместо Image, чтобы просто включать/выключать

    [Header("Sniper Settings")]
    [Range(5f, 40f)] public float sniperFOV = 15f;
    public float positionOffset = -0.2f;
    public float zoomStep = 2f;
    public float minZoom = 10f;
    public float maxZoom = 40f;

    private bool isSniperView = false;
    private float zoomTarget;
    private float zoomCurrent;
    private float normalFOV;
    private Vector3 stableOffset;

    void Start()
    {
        toggleViewAction?.action?.Enable();
        zoomAction?.action?.Enable();

        if (mainCamera != null)
            normalFOV = mainCamera.fieldOfView;

        if (crosshairUI != null)
            crosshairUI.SetActive(false);

        if (sniperCamera != null)
        {
            sniperCamera.enabled = false;
            zoomTarget = sniperFOV;
            zoomCurrent = sniperFOV;
            stableOffset = sniperCamera.transform.position - gunEnd.position;
        }

        if (sniperVignette != null)
            sniperVignette.SetActive(false); // изначально выключена
    }

    void Update()
    {
        if (toggleViewAction != null && toggleViewAction.action.WasPressedThisFrame())
            ToggleSniperView();

        if (isSniperView)
            HandleZoom();

        UpdateCamera();
    }

    void HandleZoom()
    {
        float scroll = zoomAction?.action.ReadValue<Vector2>().y ?? Input.GetAxis("Mouse ScrollWheel") * 5f;
        if (Mathf.Abs(scroll) > 0.01f)
            zoomTarget = Mathf.Clamp(zoomTarget - scroll * zoomStep, minZoom, maxZoom);

        zoomCurrent = zoomTarget;
    }

    void UpdateCamera()
    {
        if (sniperCamera != null && gunEnd != null && isSniperView)
        {
            sniperCamera.transform.SetPositionAndRotation(gunEnd.position + stableOffset + gunEnd.forward * positionOffset, gunEnd.rotation);
            sniperCamera.fieldOfView = zoomCurrent;
        }

        if (mainCamera != null)
            mainCamera.fieldOfView = normalFOV;
    }

    void ToggleSniperView()
    {
        isSniperView = !isSniperView;

        if (mainCamera != null) mainCamera.enabled = !isSniperView;
        if (sniperCamera != null) sniperCamera.enabled = isSniperView;
        if (crosshairUI != null) crosshairUI.SetActive(isSniperView);
        if (sniperVignette != null) sniperVignette.SetActive(isSniperView); // включаем только в снайперском режиме
    }

    void OnDestroy()
    {
        toggleViewAction?.action?.Disable();
        zoomAction?.action?.Disable();
    }
}
