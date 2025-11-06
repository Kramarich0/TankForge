using UnityEngine;
using UnityEngine.InputSystem;

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
        if (toggleViewAction?.action != null)
        {
            toggleViewAction.action.Enable();
        }
        
        if (crosshairUI != null)
        {
            crosshairUI.SetActive(false);
        }

        if (sniperVignette != null)
        {
            sniperVignette.SetActive(false);
        }
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError("GameObject с TankSniperView неактивен! Активируем...");
            gameObject.SetActive(true);
        }

        if (toggleViewAction?.action == null)
        {
            Debug.LogError("toggleViewAction == null в Update!");
            return;
        }

        if (!toggleViewAction.action.enabled)
        {
            Debug.LogWarning("InputAction отключена! Пытаемся включить...");
            toggleViewAction.action.Enable();
        }

        // Debug.Log("InputAction.enabled = " + toggleViewAction.action.enabled);
        // Debug.Log("isSniperView = " + isSniperView);

        if (toggleViewAction.action.WasPressedThisFrame())
        {
            // Debug.Log("Кнопка нажата!");
            ToggleSniperView();
        }

        if (isSniperView)
        {
            if (gunEnd != null)
            {
                sniperCamera.transform.position = gunEnd.position;
                sniperCamera.transform.rotation = gunEnd.rotation;
            }
            else
            {
                Debug.LogError("gunEnd == null при isSniperView = true!");
            }
        }
    }

    void ToggleSniperView()
    {
        // Debug.Log("ToggleSniperView вызван! isSniperView = " + isSniperView);

        isSniperView = !isSniperView;
        // Debug.Log("Теперь isSniperView = " + isSniperView);

        mainCamera.enabled = !isSniperView;
        sniperCamera.enabled = isSniperView;

        if (crosshairUI != null)
        {
            crosshairUI.SetActive(isSniperView);
        }

        if (sniperVignette != null)
        {
            sniperVignette.SetActive(isSniperView);
            // Debug.Log("Vignette.SetActive(" + isSniperView + ")")
        }

        // Debug.Log("mainCamera.enabled = " + mainCamera.enabled);
        // Debug.Log("sniperCamera.enabled = " + sniperCamera.enabled);
    }

    void OnDestroy()
    {
        // Debug.Log("TankSniperView уничтожён!");
        if (toggleViewAction?.action != null && toggleViewAction.action.enabled)
        {
            toggleViewAction.action.Disable();
            // Debug.Log("InputAction отключена в OnDestroy!");
        }
    }
}