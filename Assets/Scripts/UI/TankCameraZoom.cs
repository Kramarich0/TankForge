using UnityEngine;
using Unity.Cinemachine;

public class CameraZoomController : MonoBehaviour
{
    [Header("References")]
    public GameObject vCamObject; // просто перетаскиваем GameObject виртуальной камеры

    [Header("Zoom Settings")]
    public bool useFOV = true; // true = зум через FOV, false = через FollowOffset
    public float minFOV = 15f;
    public float maxFOV = 60f;
    public float fovSpeed = 20f;

    public Vector3 followOffsetMin = new Vector3(0,5,-6);
    public Vector3 followOffsetMax = new Vector3(0,10,-12);
    public float followOffsetSpeed = 5f;

    [Header("Smoothing")]
    public float smoothTime = 0.08f;

    private CinemachineVirtualCamera vCam;
    private CinemachineTransposer transposer;
    private float targetFOV;
    private float fovVelocity = 0f;
    private Vector3 targetOffset;

    void Start()
    {
        if (vCamObject == null)
        {
            Debug.LogError("CameraZoomController: vCamObject не назначен!");
            enabled = false;
            return;
        }

        vCam = vCamObject.GetComponent<CinemachineVirtualCamera>();
        if (vCam == null)
        {
            Debug.LogError("На GameObject нет компонента CinemachineVirtualCamera!");
            enabled = false;
            return;
        }

        targetFOV = vCam.m_Lens.FieldOfView;
        transposer = vCam.GetCinemachineComponent<CinemachineTransposer>();
        targetOffset = transposer != null ? transposer.m_FollowOffset : Vector3.zero;

        if (!useFOV && transposer == null)
        {
            Debug.LogWarning("Transposer не найден, переключаем на FOV зум.");
            useFOV = true;
        }
    }

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.001f)
        {
            if (useFOV)
            {
                targetFOV -= scroll * fovSpeed;
                targetFOV = Mathf.Clamp(targetFOV, minFOV, maxFOV);
            }
            else if (transposer != null)
            {
                float t = Mathf.InverseLerp(followOffsetMin.z, followOffsetMax.z, targetOffset.z);
                t += -scroll * 0.1f;
                t = Mathf.Clamp01(t);
                targetOffset = Vector3.Lerp(followOffsetMin, followOffsetMax, t);
            }
        }

        if (useFOV)
        {
            vCam.m_Lens.FieldOfView = Mathf.SmoothDamp(vCam.m_Lens.FieldOfView, targetFOV, ref fovVelocity, smoothTime);
        }
        else if (transposer != null)
        {
            transposer.m_FollowOffset = Vector3.Lerp(transposer.m_FollowOffset, targetOffset, Time.deltaTime * followOffsetSpeed);
        }
    }
}
