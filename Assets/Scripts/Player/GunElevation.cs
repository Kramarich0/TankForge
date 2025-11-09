using UnityEngine;
using UnityEngine.InputSystem;





public class GunElevation : MonoBehaviour
{
    public Transform cameraTransform;      
    public float smoothSpeed = 8f;         
    public float minAngle = -10f;
    public float maxAngle = 20f;

    private float currentPitch;
    private float targetPitch;
    private float pitchVelocity;

    void Start()
    {
        float cur = transform.localEulerAngles.x;
        if (cur > 180f) cur -= 360f;
        currentPitch = cur;
        targetPitch = currentPitch;
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        
        float camPitch = cameraTransform.localEulerAngles.x;
        if (camPitch > 180f) camPitch -= 360f;

        
        targetPitch = Mathf.Clamp(camPitch, minAngle, maxAngle);

        
        currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref pitchVelocity, 1f / smoothSpeed);

        
        Vector3 e = transform.localEulerAngles;
        e.x = currentPitch;
        transform.localEulerAngles = e;
    }
}
