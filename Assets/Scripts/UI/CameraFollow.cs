using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public InputActionReference lookAction; 
    public float sensitivity = 0.04f;
    public Vector3 offset = new(0,5,-10);
    public float rotationSpeed = 3f;   
    public float smoothSpeed = 5f;

    private float yaw = 0f;

    void OnEnable() => lookAction.action.Enable();
    void OnDisable() => lookAction.action.Disable();

    void LateUpdate()
    {
        if (target == null || lookAction == null || lookAction.action == null) return;

        Vector2 look = lookAction.action.ReadValue<Vector2>();

        yaw += look.x * sensitivity * rotationSpeed; 

        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        Vector3 desiredPos = target.position + rot * offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * offset.y);
    }
}