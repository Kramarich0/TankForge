using UnityEngine;
using UnityEngine.InputSystem;

public class SingleCameraSwitcher : MonoBehaviour
{
    public Transform camPivot3rd;
    public Transform camPivot1st;
    public Transform mainCameraTransform;
    public InputActionReference switchAction;

    private bool is3rd = true;

    private void OnEnable()
    {
        switchAction.action.Enable();
        switchAction.action.performed += OnSwitch;
    }

    private void OnDisable()
    {
        switchAction.action.performed -= OnSwitch;
        switchAction.action.Disable();
    }

    private void OnSwitch(InputAction.CallbackContext ctx)
    {
        is3rd = !is3rd;
    }

    private void LateUpdate()
    {
        Transform targetPivot = is3rd ? camPivot3rd : camPivot1st;
        mainCameraTransform.position = targetPivot.position;
        mainCameraTransform.rotation = targetPivot.rotation;
    }
}
