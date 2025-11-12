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
    public GameObject crosshairAimUI;
    public GameObject crosshairSniperUI;
    public GameObject sniperVignette;
    public TankShoot tankShoot;

    [Header("Sniper Settings (игровой зум)")]
    public float zoomStep = 2f;
    public float zoomSpeed = 8f;
    public float zoomFOVReduction = 15f;
    public float maxZoomOffset = 0.35f;
    public float positionOffset = -0.2f;

    [Header("Optional follow settings")]
    public float followSpeed = 10f;
    private Vector3 recoilPositionOffset = Vector3.zero;
    private Vector3 recoilPositionVelocity = Vector3.zero;
    private bool isSniperView = false;
    private float zoomTarget = 0f;
    private float zoomCurrent = 0f;
    private float normalFOV;
    private Vector3 stableOffset;
    private float recoilDecay = 5f;
    private float driveSwayTimer = 0f;
    private readonly float driveSwayAmountX = 0.08f;
    private readonly float driveSwayAmountY = 0.02f;
    private float accumulatedRecoilAngle = 0f;
    private float recoilAngleVelocity = 0f;

    void Start()
    {
        toggleViewAction?.action?.Enable();
        zoomAction?.action?.Enable();

        if (mainCamera != null) normalFOV = mainCamera.fieldOfView;
        crosshairAimUI?.SetActive(true);
        crosshairSniperUI?.SetActive(false);
        if (sniperCamera != null) sniperCamera.enabled = false;
        sniperVignette?.SetActive(false);

        if (sniperCamera != null && gunEnd != null)
            stableOffset = sniperCamera.transform.position - gunEnd.position;

        if (tankShoot != null)
            tankShoot.onShotFired += OnSniperShot;

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
        float scroll;
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
            float delta = scroll * 0.2f * zoomStep;
            zoomTarget = Mathf.Clamp01(zoomTarget + delta);
        }


        zoomCurrent = Mathf.MoveTowards(zoomCurrent, zoomTarget, Time.deltaTime * zoomSpeed);
    }

    void UpdateCamera()
    {
        if (!isSniperView || sniperCamera == null || gunEnd == null) return;

        accumulatedRecoilAngle = Mathf.SmoothDamp(accumulatedRecoilAngle, 0f, ref recoilAngleVelocity, 1f / Mathf.Max(recoilDecay, 0.1f));
        recoilPositionOffset = Vector3.SmoothDamp(
            recoilPositionOffset,
            Vector3.zero,
            ref recoilPositionVelocity,
            1f / Mathf.Max(recoilDecay, 0.1f)
        );

        // если нужна тряска в камере раскоменть
        // driveSwayTimer += Time.deltaTime;
        // Vector3 noiseVector = new(
        //  Mathf.PerlinNoise(driveSwayTimer * 3.7f, 0) * 2f - 1f,
        //  Mathf.PerlinNoise(0, driveSwayTimer * 2.9f) * 2f - 1f,
        //  0
        // );

        // если нужна тряска в камере раскоменть
        // Vector3 driveSway = new(
        //     noiseVector.x * driveSwayAmountX,
        //     noiseVector.y * driveSwayAmountY,
        //     0
        // );


        Vector3 zoomOffset = gunEnd.forward * (maxZoomOffset * zoomCurrent);
        // Vector3 basePos = gunEnd.position + stableOffset + gunEnd.forward * positionOffset + zoomOffset + recoilPositionOffset + driveSway; // если нужна тряска в камере раскоменть
        Vector3 basePos = gunEnd.position + stableOffset + gunEnd.forward * positionOffset + zoomOffset + recoilPositionOffset; // а ето заккоменть
        sniperCamera.transform.position = Vector3.Lerp(sniperCamera.transform.position, basePos, Time.deltaTime * followSpeed);

        Quaternion baseRot = Quaternion.LookRotation(gunEnd.forward, gunEnd.up);
        Quaternion recoilLocalRot = Quaternion.Euler(accumulatedRecoilAngle, 0f, 0f);
        Quaternion finalRot = baseRot * recoilLocalRot;

        sniperCamera.transform.rotation = Quaternion.Slerp(sniperCamera.transform.rotation, finalRot, Time.deltaTime * followSpeed);
        float minFOV = Mathf.Clamp(normalFOV - zoomFOVReduction, 15f, normalFOV - 2f);
        sniperCamera.fieldOfView = Mathf.Lerp(normalFOV, minFOV, zoomCurrent);
    }


    void ToggleSniperView()
    {
        if (GameUIManager.Instance != null && GameUIManager.Instance.IsPaused) return;
        isSniperView = !isSniperView;

        if (mainCamera != null) mainCamera.enabled = !isSniperView;
        if (sniperCamera != null) sniperCamera.enabled = isSniperView;

        if (crosshairAimUI != null) crosshairAimUI.SetActive(true);
        if (crosshairSniperUI != null) crosshairSniperUI.SetActive(false);

        if (crosshairAimUI != null && crosshairAimUI.TryGetComponent<CrosshairAim>(out var crossAim))
            crossAim.SetCamera(mainCamera);

        if (crosshairSniperUI != null && crosshairSniperUI.TryGetComponent<CrosshairAim>(out var crossSniper))
            crossSniper.SetCamera(sniperCamera);

        sniperVignette?.SetActive(isSniperView);

        if (isSniperView)
        {
            zoomTarget = Mathf.Clamp01(zoomTarget);
            zoomCurrent = Mathf.Clamp01(zoomCurrent);
        }
    }

    void OnDestroy()
    {
        if (tankShoot != null)
            tankShoot.onShotFired -= OnSniperShot;
        toggleViewAction?.action?.Disable();
        zoomAction?.action?.Disable();
    }

    void OnSniperShot()
    {
        if (!isSniperView || sniperCamera == null || gunEnd == null) return;

        float minFOV = Mathf.Clamp(normalFOV - zoomFOVReduction, 15f, normalFOV - 2f);
        float currentFOV = Mathf.Lerp(normalFOV, minFOV, zoomCurrent);
        float recoilScale = currentFOV / normalFOV;

        recoilPositionOffset += 0.18f * recoilScale * -gunEnd.forward;
        recoilPositionOffset += Random.Range(-0.05f, 0.05f) * recoilScale * gunEnd.right;
        accumulatedRecoilAngle -= Random.Range(5.5f, 6f) * recoilScale;

        recoilDecay = 2f;
    }

}
