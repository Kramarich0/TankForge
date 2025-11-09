using UnityEngine;
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
    public GameObject crosshairAimUI;      // CrossHairAim для 3-го лица
    public GameObject crosshairSniperUI;   // CrossHairSniper для снайперского
    public GameObject sniperVignette;

    [Header("Sniper Settings (игровой зум)")]
    public float zoomStep = 2f;            // чувствительность колеса
    public float zoomSpeed = 8f;           // плавность интерполяции зума
    public float zoomFOVReduction = 15f;   // насколько уменьшится FOV при полном зуме
    public float maxZoomOffset = 0.35f;    // смещение камеры вперед при полном зуме
    public float positionOffset = -0.2f;   // базовое смещение камеры от gunEnd

    [Header("Optional follow settings")]
    public float followSpeed = 10f;        // скорость подстройки поворота камеры к стволу

    bool isSniperView = false;
    float zoomTarget = 0f; // 0..1
    float zoomCurrent = 0f;
    float normalFOV;
    Vector3 stableOffset;

    void Start()
    {
        toggleViewAction?.action?.Enable();
        zoomAction?.action?.Enable();

        if (mainCamera != null) normalFOV = mainCamera.fieldOfView;
        if (crosshairAimUI != null) crosshairAimUI.SetActive(true);
        if (crosshairSniperUI != null) crosshairSniperUI.SetActive(false);
        if (sniperCamera != null) sniperCamera.enabled = false;
        if (sniperVignette != null) sniperVignette.SetActive(false);

        // инициализация смещения (работаем в мировых координатах)
        if (sniperCamera != null && gunEnd != null)
            stableOffset = sniperCamera.transform.position - gunEnd.position;

        // начальное значение zoomCurrent/zoomTarget = 0 (без зума)
        zoomTarget = 0f;
        zoomCurrent = 0f;
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
        float scroll = 0f;
        if (zoomAction != null && zoomAction.action != null)
        {
            Vector2 v = zoomAction.action.ReadValue<Vector2>();
            scroll = v.y;
        }
        else
        {
            scroll = Input.GetAxis("Mouse ScrollWheel");
        }

        if (Mathf.Abs(scroll) > 0.001f)
        {
            // делаем шаг помягче: 0..1 нормализованный target
            float delta = scroll * 0.2f * zoomStep;
            zoomTarget = Mathf.Clamp01(zoomTarget + delta);
        }

        // более плавная интерполяция текущего значения к целевому
        zoomCurrent = Mathf.MoveTowards(zoomCurrent, zoomTarget, Time.deltaTime * zoomSpeed);
    }

    void UpdateCamera()
    {
        // Обычная камера — держим базовый FOV
        if (mainCamera != null && !isSniperView)
            mainCamera.fieldOfView = normalFOV;

        if (!isSniperView || sniperCamera == null || gunEnd == null) return;

        // position: базовая позиция + небольшое смещение вперёд в зависимости от zoomCurrent
        Vector3 zoomOffset = gunEnd.forward * (maxZoomOffset * zoomCurrent);
        Vector3 targetPos = gunEnd.position + stableOffset + gunEnd.forward * positionOffset + zoomOffset;
        sniperCamera.transform.position = Vector3.Lerp(sniperCamera.transform.position, targetPos, Time.deltaTime * followSpeed);

        // rotation: плавно следуем направлению ствола, но не жестко привязываемся (плавность followSpeed)
        Quaternion targetRot = Quaternion.LookRotation(gunEnd.forward, gunEnd.up);
        sniperCamera.transform.rotation = Quaternion.Slerp(sniperCamera.transform.rotation, targetRot, Time.deltaTime * followSpeed);

        // FOV: плавно между нормальным FOV и уменьшенным (normalFOV - zoomFOVReduction)
        float minFOV = Mathf.Clamp(normalFOV - zoomFOVReduction, 10f, normalFOV - 2f); // защита от слишком малого FOV
        sniperCamera.fieldOfView = Mathf.Lerp(normalFOV, minFOV, zoomCurrent);
    }

    void ToggleSniperView()
    {
        isSniperView = !isSniperView;

        if (mainCamera != null) mainCamera.enabled = !isSniperView;
        if (sniperCamera != null) sniperCamera.enabled = isSniperView;

        if (crosshairAimUI != null) crosshairAimUI.SetActive(!isSniperView);
        if (crosshairSniperUI != null) crosshairSniperUI.SetActive(isSniperView);

        if (crosshairAimUI != null && crosshairAimUI.TryGetComponent<CrosshairAim>(out var crossAim))
            crossAim.SetCamera(mainCamera);

        if (crosshairSniperUI != null && crosshairSniperUI.TryGetComponent<CrosshairAim>(out var crossSniper))
            crossSniper.SetCamera(sniperCamera);

        if (sniperVignette != null) sniperVignette.SetActive(isSniperView);

        // при включении снайперки сбрасываем target/current, чтобы зум начинался предсказуемо
        if (isSniperView)
        {
            zoomTarget = Mathf.Clamp01(zoomTarget);
            zoomCurrent = Mathf.Clamp01(zoomCurrent);
        }
    }

    void OnDestroy()
    {
        toggleViewAction?.action?.Disable();
        zoomAction?.action?.Disable();
    }
}
