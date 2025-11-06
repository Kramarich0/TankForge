using UnityEngine;
using UnityEngine.InputSystem;

public class GunElevation : MonoBehaviour
{
    public InputActionReference lookAction; 

    public float sensitivity = 0.05f;       
    public float maxDegreesPerSecond = 10f; 
    public float minAngle = -10f;          
    public float maxAngle = 20f;           

    private float currentPitch = 0f;        
    private float targetPitch = 0f;         

    void OnEnable()
    {
        if (lookAction == null || lookAction.action == null) return;
        lookAction.action.Enable();

        float cur = transform.localEulerAngles.x;
        if (cur > 180f) cur -= 360f;
        currentPitch = cur;
        targetPitch = currentPitch;
    }

    void OnDisable()
    {
        if (lookAction == null || lookAction.action == null) return;
        lookAction.action.Disable();
    }

    void Update()
    {
        if (lookAction == null || lookAction.action == null) return;

        float mouseY = lookAction.action.ReadValue<Vector2>().y;

        targetPitch -= mouseY * sensitivity;
        targetPitch = Mathf.Clamp(targetPitch, minAngle, maxAngle);

        currentPitch = Mathf.MoveTowardsAngle(currentPitch, targetPitch, maxDegreesPerSecond * Time.deltaTime);

        Vector3 euler = transform.localEulerAngles;
        euler.x = currentPitch;
        transform.localEulerAngles = euler;
    }
}