using UnityEngine;
using UnityEngine.InputSystem;

public class GunElevation : MonoBehaviour
{
    public InputActionReference lookAction;
    public float sensitivity = 0.6f;       // более удобная ось
    public float smoothTime = 0.06f;       // сглаживание
    public float minAngle = -10f;
    public float maxAngle = 20f;
    public bool invertY = false;

    private float currentPitch = 0f;
    private float targetPitch = 0f;
    private float velocity = 0f;

    void OnEnable()
    {
        if (lookAction?.action != null) lookAction.action.Enable();
        float cur = transform.localEulerAngles.x;
        if (cur > 180f) cur -= 360f;
        currentPitch = cur;
        targetPitch = currentPitch;
    }

    void OnDisable()
    {
        if (lookAction?.action != null) lookAction.action.Disable();
    }

    void Update()
    {
        if (lookAction == null || lookAction.action == null) return;

        Vector2 look = lookAction.action.ReadValue<Vector2>();
        float mouseY = look.y;
        if (invertY) mouseY = -mouseY;

        targetPitch -= mouseY * sensitivity;
        targetPitch = Mathf.Clamp(targetPitch, minAngle, maxAngle);

        currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref velocity, smoothTime);

        Vector3 euler = transform.localEulerAngles;
        euler.x = currentPitch;
        transform.localEulerAngles = euler;
    }
}
