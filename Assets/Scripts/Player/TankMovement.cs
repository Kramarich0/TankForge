using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class TankMovement : MonoBehaviour
{
    [Header("Input (Input System)")]
    public InputActionAsset actionsAsset;
    [Tooltip("Пример: 'Gameplay/Move' — действие должно отдавать Vector2 (x=turn, y=move).")]
    public string moveActionPath = "Gameplay/Move";

    InputAction moveAction;
    bool hasInputAction = false;

    [Header("Movement")]
    public float forwardSpeed = 8f;
    public float backwardSpeed = 4f;
    public float acceleration = 10f;        
    public float deceleration = 12f;        
    public float brakeDeceleration = 30f;   
    public float turnSpeed = 90f;
    public float turnWhileReverseFactor = 0.6f;

    [Header("Rigidbody / Pivot")]
    [Tooltip("Если true — используем Rigidbody.MovePosition/MoveRotation (физика).")]
    public bool useRigidbody = true;
    public Transform rotationPivot;
    public bool adjustRigidbodyCenterOfMass = true;

    [Header("Noise / Deadzones")]
    [Range(0f, 0.1f)] public float inputDeadzone = 0.02f;
    public float movementEpsilon = 0.001f;
    public float rotationEpsilon = 0.01f;

    [Header("Smoothing")]
    [Tooltip("Время (в секундах) для экспоненциального сглаживания поворота (меньше — быстрее).")]
    public float rotationSmoothTime = 0.06f;

    [Header("Audio")]
    public AudioClip idleSound;
    public AudioClip driveSound;
    [Range(0f, 1f)] public float minVolume = 0.2f;
    [Range(0f, 1f)] public float maxVolume = 1f;
    [Range(0.5f, 2f)] public float minPitch = 0.7f;
    [Range(0.5f, 2f)] public float maxPitch = 1.3f;
    public float blendSpeed = 5f;

    [Header("Debug")]
    public bool debugLogs = false;

    
    Rigidbody rb;
    AudioSource idleSource, driveSource;
    float currentBlend = 0f, targetBlend = 0f;

    
    float currentSpeed = 0f;      
    float targetSpeed = 0f;

    
    float rawMoveInput = 0f;
    float rawTurnInput = 0f;
    float moveInput = 0f;
    float turnInput = 0f;

    
    float yawVelocity = 0f;

    public SpeedDisplay speedDisplay; 

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (useRigidbody)
        {
            if (rb == null)
            {
                Debug.LogWarning("[TankMovement] useRigidbody=true, но Rigidbody не найден. Переключаюсь на Transform-mode.");
                useRigidbody = false;
            }
            else
            {
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                if (rotationPivot != null && adjustRigidbodyCenterOfMass)
                {
                    rb.centerOfMass = rb.transform.InverseTransformPoint(rotationPivot.position);
                    if (debugLogs) Debug.Log("[TankMovement] rb.centerOfMass set.");
                }
            }
        }
        else
        {
            if (rb != null)
            {
                rb.isKinematic = true;
                if (debugLogs) Debug.Log("[TankMovement] Transform-mode: rb.isKinematic = true");
            }
        }

        if (idleSound != null)
        {
            idleSource = gameObject.AddComponent<AudioSource>();
            SetupAudioSource(idleSource, idleSound);
        }
        if (driveSound != null)
        {
            driveSource = gameObject.AddComponent<AudioSource>();
            SetupAudioSource(driveSource, driveSound);
        }
    }

    void SetupAudioSource(AudioSource source, AudioClip clip)
    {
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;
        source.pitch = 1f;
        source.spatialBlend = 1f;
        source.Play();
    }

    void OnEnable()
    {
        hasInputAction = false;
        if (actionsAsset != null && !string.IsNullOrEmpty(moveActionPath))
        {
            try
            {
                moveAction = actionsAsset.FindAction(moveActionPath, true);
                if (moveAction != null)
                {
                    moveAction.Enable();
                    hasInputAction = true;
                    if (debugLogs) Debug.Log("[TankMovement] Using InputAction: " + moveActionPath);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[TankMovement] Ошибка получения действия: " + e.Message);
            }
        }
    }

    void OnDisable()
    {
        if (hasInputAction && moveAction != null) moveAction.Disable();
    }

    void Update()
    {
        
        Vector2 moveVec = Vector2.zero;
        if (hasInputAction && moveAction != null)
            moveVec = moveAction.ReadValue<Vector2>();
        else
            moveVec = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        rawMoveInput = Mathf.Clamp(moveVec.y, -1f, 1f);
        rawTurnInput = Mathf.Clamp(moveVec.x, -1f, 1f);

        moveInput = Mathf.Abs(rawMoveInput) < inputDeadzone ? 0f : rawMoveInput;
        turnInput = Mathf.Abs(rawTurnInput) < inputDeadzone ? 0f : rawTurnInput;

        
        if (moveInput > 0f) targetSpeed = moveInput * forwardSpeed;
        else if (moveInput < 0f) targetSpeed = moveInput * backwardSpeed; 
        else targetSpeed = 0f;

        
        float absMove = Mathf.Abs(moveInput);
        targetBlend = Mathf.Clamp01(absMove);
        currentBlend = Mathf.MoveTowards(currentBlend, targetBlend, blendSpeed * Time.deltaTime);
        float idleVolume = Mathf.Lerp(maxVolume, minVolume, currentBlend);
        float driveVolume = Mathf.Lerp(0f, maxVolume, currentBlend);
        float idlePitch = Mathf.Lerp(maxPitch, minPitch, currentBlend);
        float drivePitch = Mathf.Lerp(minPitch, maxPitch, currentBlend);
        if (idleSource != null) { idleSource.volume = idleVolume; idleSource.pitch = idlePitch; }
        if (driveSource != null) { driveSource.volume = driveVolume; driveSource.pitch = drivePitch; }
    }

    void FixedUpdate()
    {
        float tiltAngle = Vector3.Angle(transform.up, Vector3.up);
        if (tiltAngle > 89f) 
        {
            currentSpeed = 0f; 
            return;            
        }
        
        if (useRigidbody && rb != null)
        {
            
            bool braking = false;
            float targetSpeedForPhysics = targetSpeed;

            if (rawMoveInput < -inputDeadzone && currentSpeed > 0.001f)
            {
                
                braking = true;
                targetSpeedForPhysics = 0f;
            }
            else
            {
                
                targetSpeedForPhysics = targetSpeed;
            }

            
            float rate;
            if (braking) rate = brakeDeceleration;
            else rate = (Mathf.Abs(targetSpeedForPhysics) > Mathf.Epsilon) ? acceleration : deceleration;

            
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeedForPhysics, rate * Time.fixedDeltaTime);

            
            

            if (Mathf.Abs(currentSpeed) < movementEpsilon) currentSpeed = 0f;

            
            Vector3 forward = rb.rotation * Vector3.forward;
            Vector3 moveDelta = forward * currentSpeed * Time.fixedDeltaTime;
            if (moveDelta.sqrMagnitude > (movementEpsilon * movementEpsilon))
            {
                rb.MovePosition(rb.position + moveDelta);
            }

            
            float effectiveTurnSpeed = turnSpeed * (currentSpeed < 0 ? turnWhileReverseFactor : 1f);
            float desiredAngular = turnInput * effectiveTurnSpeed; 
            float currentY = rb.rotation.eulerAngles.y;
            float desiredY = currentY + desiredAngular * Time.fixedDeltaTime;

            
            float t = 1f - Mathf.Exp(-Time.fixedDeltaTime / Mathf.Max(0.0001f, rotationSmoothTime));
            Quaternion targetRot = Quaternion.Euler(0f, desiredY, 0f);
            Quaternion newRot = Quaternion.Slerp(rb.rotation, targetRot, t);

            
            float deltaAngle = Mathf.DeltaAngle(rb.rotation.eulerAngles.y, newRot.eulerAngles.y);
            if (Mathf.Abs(deltaAngle) > rotationEpsilon)
            {
                rb.MoveRotation(newRot);
            }

            
            if (speedDisplay != null) speedDisplay.SetSpeed(Mathf.Abs(currentSpeed));
        }
        else
        {
            
            
            float physTargetSpeed = targetSpeed;

            
            bool braking = false;
            if (rawMoveInput < -inputDeadzone && currentSpeed > 0.001f)
            {
                braking = true;
                physTargetSpeed = 0f;
            }

            float rate = braking ? brakeDeceleration : ((Mathf.Abs(physTargetSpeed) > Mathf.Epsilon) ? acceleration : deceleration);
            currentSpeed = Mathf.MoveTowards(currentSpeed, physTargetSpeed, rate * Time.fixedDeltaTime);
            if (Mathf.Abs(currentSpeed) < movementEpsilon) currentSpeed = 0f;

            Vector3 move = transform.forward * currentSpeed * Time.fixedDeltaTime;
            if (move.sqrMagnitude > (movementEpsilon * movementEpsilon))
                transform.Translate(move, Space.World);

            float effectiveTurnSpeed = turnSpeed * (currentSpeed < 0 ? turnWhileReverseFactor : 1f);
            float turnAmount = turnInput * effectiveTurnSpeed * Time.fixedDeltaTime;
            if (Mathf.Abs(turnAmount) > rotationEpsilon)
            {
                if (rotationPivot != null)
                    transform.RotateAround(rotationPivot.position, Vector3.up, turnAmount);
                else
                    transform.Rotate(0f, turnAmount, 0f);
            }

            if (speedDisplay != null) speedDisplay.SetSpeed(Mathf.Abs(currentSpeed));
        }
    }
}
