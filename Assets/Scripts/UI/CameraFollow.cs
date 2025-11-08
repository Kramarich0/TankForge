using UnityEngine;
using UnityEngine.InputSystem;

[HelpURL("https://docs.unity3d.com/Manual/Cameras.html")]
public class CameraFollow : MonoBehaviour
{
    [Header("Target / Input")]
    public Transform target;
    public InputActionReference lookAction;   // Vector2
    public InputActionReference zoomAction;   // Vector2 (y) or Mouse Scroll

    [Header("Orbit")]
    [Tooltip("Local offset: x (right), y (up), z (back). Distance is -z by default.")]
    public Vector3 offset = new Vector3(0f, 4f, -8f);
    public float minPitch = -20f;
    public float maxPitch = 50f;
    [Tooltip("Base rotation sensitivity (degrees per second). Multiplicative with deltaTime.")]
    public float baseSensitivity = 120f;
    [Tooltip("Multiplier to reduce sensitivity when zoomed in.")]
    [Range(0.1f, 1f)] public float zoomSensitivityMultiplier = 0.6f;

    [Header("Smoothing")]
    public float rotationSmoothTime = 0.06f;
    public float positionSmoothTime = 0.08f;
    [Tooltip("When input magnitude exceeds this, camera becomes more responsive.")]
    public float inputResponsivenessThreshold = 0.02f;

    [Header("Zoom / Distance")]
    public float minDistance = 3f;
    public float maxDistance = 14f;
    public float zoomSpeed = 8f;
    public float distanceSmoothTime = 0.12f;

    [Header("Collision")]
    public float cameraCollisionRadius = 0.3f;
    public float collisionOffset = 0.25f;
    [Tooltip("Layers considered as obstruction for camera (terrain, walls...). Exclude player layer.")]
    public LayerMask obstructionMask = ~0;

    [Header("Physics / Update mode")]
    [Tooltip("If true and target has Rigidbody — camera may follow in FixedUpdate for best stability.")]
    public bool followInFixedUpdateIfRigidbody = true;
    [Tooltip("If true — prefer rb.position inside LateUpdate when using interpolation.")]
    public bool preferRbPositionInLateUpdate = true;

    [Header("Stability")]
    [Tooltip("Ignore tiny position changes (noise).")]
    public float positionEpsilon = 0.001f;
    [Tooltip("Ignore tiny angle changes (noise).")]
    public float angleEpsilon = 0.01f;

    // internal state
    float yaw = 0f;
    float pitch = 10f;
    float yawVel = 0f, pitchVel = 0f;
    Vector3 positionVelocity = Vector3.zero;
    float currentDistance;
    float targetDistance;
    float distanceVelocity = 0f;

    Rigidbody targetRb;
    bool followInFixed = false;

    void Start()
    {
        if (lookAction?.action != null) lookAction.action.Enable();
        if (zoomAction?.action != null) zoomAction.action.Enable();

        currentDistance = -offset.z;
        targetDistance = currentDistance;

        if (target != null)
        {
            targetRb = target.GetComponent<Rigidbody>();

            // initialize yaw/pitch from current camera-to-target direction
            Vector3 dir = (transform.position - target.position).normalized;
            Vector3 flat = Vector3.ProjectOnPlane(dir, Vector3.up);
            if (flat.sqrMagnitude > 0.001f)
                yaw = Quaternion.LookRotation(flat).eulerAngles.y;
            float rawPitch = Quaternion.LookRotation(dir).eulerAngles.x;
            if (rawPitch > 180f) rawPitch -= 360f;
            pitch = Mathf.Clamp(rawPitch, minPitch, maxPitch);
        }

        followInFixed = followInFixedUpdateIfRigidbody && (targetRb != null);
        if (followInFixed && targetRb != null && targetRb.interpolation == RigidbodyInterpolation.None)
        {
            Debug.Log("[CameraFollow] Рекомендую включить rb.interpolation = Interpolate для более плавной камеры.");
        }
    }

    void OnDisable()
    {
        if (lookAction?.action != null) lookAction.action.Disable();
        if (zoomAction?.action != null) zoomAction.action.Disable();
    }

    void FixedUpdate()
    {
        if (followInFixed) Follow(Time.fixedDeltaTime, true);
    }

    void LateUpdate()
    {
        if (!followInFixed) Follow(Time.deltaTime, false);
    }

    void Follow(float dt, bool calledFromFixed)
    {
        if (target == null) return;

        // --- Read input
        Vector2 look = Vector2.zero;
        if (lookAction?.action != null) look = lookAction.action.ReadValue<Vector2>();
        else
        {
            // fallback: mouse delta (not ideal for all platforms)
            look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        }
        float inputMag = look.magnitude;

        // Make input frame-rate independent: sensitivity is degrees / second
        float deltaYaw = look.x * baseSensitivity * dt;
        float deltaPitch = -look.y * baseSensitivity * dt;

        // zoom input
        float scrollY = 0f;
        if (zoomAction?.action != null)
        {
            Vector2 s = zoomAction.action.ReadValue<Vector2>();
            scrollY = s.y;
        }
        else
        {
            scrollY = Input.GetAxis("Mouse ScrollWheel");
        }

        // scale sensitivity when zoomed in
        float zoomFactor = Mathf.InverseLerp(maxDistance, minDistance, currentDistance);
        float sensitivityScale = Mathf.Lerp(1f, zoomSensitivityMultiplier, zoomFactor);
        deltaYaw *= sensitivityScale;
        deltaPitch *= sensitivityScale;

        // responsiveness: when user actively moving camera, reduce smoothing for responsiveness
        float rotSmooth = rotationSmoothTime;
        if (inputMag > inputResponsivenessThreshold) rotSmooth *= 0.28f;

        // accumulate target angles
        float targetYaw = yaw + deltaYaw * (360f / (2 * Mathf.PI)); // not necessary but keeps units consistent — left as linear
        targetYaw = Mathf.Repeat(targetYaw, 360f);
        float targetPitch = Mathf.Clamp(pitch + deltaPitch * (360f / (2 * Mathf.PI)), minPitch, maxPitch);

        // smoothing (use dt-aware SmoothDampAngle overload)
        yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVel, Mathf.Max(0.0001f, rotSmooth), Mathf.Infinity, dt);
        pitch = Mathf.SmoothDampAngle(pitch, targetPitch, ref pitchVel, Mathf.Max(0.0001f, rotSmooth), Mathf.Infinity, dt);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // zoom change
        if (Mathf.Abs(scrollY) > 0.0001f)
        {
            float delta = scrollY * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance - delta, minDistance, maxDistance);
        }

        // target position from Rigidbody or Transform
        Vector3 targetPos;
        if (!calledFromFixed && targetRb != null && preferRbPositionInLateUpdate)
            targetPos = targetRb.position;
        else
            targetPos = (targetRb != null ? targetRb.position : target.position);

        // compute rotation and offsets
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredOffset = rot * new Vector3(0f, offset.y, -targetDistance);
        Vector3 desiredPos = targetPos + desiredOffset;

        // collision check: spherecast from near target to desired position
        float desiredDistanceAfterCollision = targetDistance;
        Vector3 rayOrigin = targetPos + Vector3.up * 0.8f;
        Vector3 toDesired = desiredPos - rayOrigin;
        float dist = toDesired.magnitude;
        if (dist > 0.001f)
        {
            Vector3 dir = toDesired / dist;
            if (Physics.SphereCast(rayOrigin, cameraCollisionRadius, dir, out RaycastHit hit, dist, obstructionMask, QueryTriggerInteraction.Ignore))
            {
                float safe = Mathf.Max(minDistance, hit.distance - collisionOffset);
                desiredDistanceAfterCollision = Mathf.Min(desiredDistanceAfterCollision, safe);
            }
        }

        // smooth distance
        currentDistance = Mathf.SmoothDamp(currentDistance, Mathf.Clamp(desiredDistanceAfterCollision, minDistance, maxDistance),
            ref distanceVelocity, distanceSmoothTime, Mathf.Infinity, dt);

        // final pos/rot smoothing
        Vector3 finalOffset = rot * new Vector3(0f, offset.y, -currentDistance);
        Vector3 finalPos = targetPos + finalOffset;

        float posSmooth = positionSmoothTime;
        if (inputMag > inputResponsivenessThreshold) posSmooth *= 0.28f;

        Vector3 newPos = Vector3.SmoothDamp(transform.position, finalPos, ref positionVelocity, Mathf.Max(0.0001f, posSmooth), Mathf.Infinity, dt);

        if ((newPos - transform.position).sqrMagnitude > positionEpsilon * positionEpsilon)
            transform.position = newPos;
        else
            transform.position = finalPos;

        // look at slightly above the target center
        Vector3 lookTarget = targetPos + Vector3.up * (offset.y * 0.5f);
        Quaternion lookRot = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);

        // smooth rotation using expo lerp (frame-rate independent)
        float t = 1f - Mathf.Exp(-18f * dt);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, t);
    }
}
