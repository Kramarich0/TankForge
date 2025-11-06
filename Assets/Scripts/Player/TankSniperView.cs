using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Переключение прицела (sniper). Убираем любые попытки включать GameObject в Update.
/// Toggle по InputAction.WasPressedThisFrame()\
/// sniperCamera следует позиционировать каждый кадр на gunEnd, или можно parent it.
/// </summary>
public class TankSniperView : MonoBehaviour
{
    public InputActionReference toggleViewAction;
    public Transform gunEnd;
    public Camera mainCamera;
    public Camera sniperCamera;
    public GameObject crosshairUI;
    public GameObject sniperVignette;
    private bool isSniperView = false;

    void Start()
    {
        toggleViewAction?.action?.Enable();
        SetSniperState(false);
    }

    void Update()
    {
        if (toggleViewAction?.action == null) return;

        if (toggleViewAction.action.WasPressedThisFrame())
        {
            SetSniperState(!isSniperView);
        }

        if (isSniperView && gunEnd != null && sniperCamera != null)
        {
            // простое позиционирование камеры в точке оружия
            sniperCamera.transform.SetPositionAndRotation(gunEnd.position, gunEnd.rotation);
        }
    }

    void SetSniperState(bool on)
    {
        isSniperView = on;
        if (mainCamera != null) mainCamera.enabled = !on;
        if (sniperCamera != null) sniperCamera.enabled = on;
        if (crosshairUI != null) crosshairUI.SetActive(on);
        if (sniperVignette != null) sniperVignette.SetActive(on);
    }

    void OnDestroy()
    {
        toggleViewAction?.action?.Disable();
    }
}
