using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class TankMovement : MonoBehaviour
{
    public InputActionAsset actionsAsset;
    private InputAction moveAction;

    [Header("Movement")]
    public float forwardSpeed = 8f;
    public float backwardSpeed = 4f;
    public float acceleration = 10f;
    public float deceleration = 12f;
    public float turnSpeed = 90f;
    public float turnWhileReverseFactor = 0.6f;

    [Header("Pivot / Rigidbody")]
    public Transform rotationPivot;
    public bool adjustRigidbodyCenterOfMass = true;
    public bool useRigidbody = true;

    [Header("Smoothing / Responsiveness")]
    [Range(0f, 1f)] public float inputDeadzone = 0.02f;
    public float rotationSmoothTime = 0.06f;

    [Header("Engine Sounds")]
    public AudioClip idleSound;
    public AudioClip driveSound;
    [Range(0f, 1f)] public float minVolume = 0.2f;
    [Range(0f, 1f)] public float maxVolume = 1f;
    [Range(0.5f, 2f)] public float minPitch = 0.7f;
    [Range(0.5f, 2f)] public float maxPitch = 1.3f;

    [Header("Smoothing")]
    public float blendSpeed = 5f;

    private AudioSource idleSource;
    private AudioSource driveSource;

    private float currentBlend = 0f;
    private float targetBlend = 0f;

    
    private float currentSpeed = 0f;
    private float targetSpeed = 0f;
    private float targetTurnInput = 0f;
    private float currentYaw = 0f;

    public SpeedDisplay speedDisplay;

    private Rigidbody rb;
    private float yawVelocity = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("TankMovement: Rigidbody не найден!");
            useRigidbody = false;
            return;
        }

        
        rb.isKinematic = false;
        rb.useGravity = true; 
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        
        rb.linearDamping = 0.5f;  
        rb.angularDamping = 5f;

        if (rotationPivot != null && adjustRigidbodyCenterOfMass)
        {
            Vector3 com = rb.transform.InverseTransformPoint(rotationPivot.position);
            rb.centerOfMass = com;
        }

        idleSource = gameObject.AddComponent<AudioSource>();
        driveSource = gameObject.AddComponent<AudioSource>();
        SetupAudioSource(idleSource, idleSound, true);
        SetupAudioSource(driveSource, driveSound, true);
    }

    void SetupAudioSource(AudioSource source, AudioClip clip, bool loop)
    {
        if (clip != null)
        {
            source.clip = clip;
            source.loop = loop;
            source.playOnAwake = false;
            source.volume = 0f;
            source.pitch = 1f;
        }
    }

    void OnEnable()
    {
        if (actionsAsset == null)
        {
            Debug.LogError("actionsAsset не задан в TankMovement!");
            return;
        }
        moveAction = actionsAsset.FindAction("Gameplay/Move", true);
        if (moveAction == null)
        {
            Debug.LogError("Move action не найден!");
            return;
        }
        moveAction.Enable();

        if (idleSound != null) idleSource.Play();
        if (driveSound != null) driveSource.Play();
    }

    void OnDisable()
    {
        if (moveAction != null) moveAction.Disable();
        if (idleSource != null) idleSource.Stop();
        if (driveSource != null) driveSource.Stop();
    }

    void Update()
    {
        Vector2 v = moveAction.ReadValue<Vector2>();
        float rawMoveInput = Mathf.Clamp(v.y, -1f, 1f);
        float rawTurnInput = Mathf.Clamp(v.x, -1f, 1f);

        float moveInput = Mathf.Abs(rawMoveInput) < inputDeadzone ? 0f : rawMoveInput;
        float turnInput = Mathf.Abs(rawTurnInput) < inputDeadzone ? 0f : rawTurnInput;

        
        if (moveInput > 0)
            targetSpeed = moveInput * forwardSpeed;
        else if (moveInput < 0)
            targetSpeed = moveInput * backwardSpeed;
        else
            targetSpeed = 0f;

        targetTurnInput = turnInput;

        
        float absMove = Mathf.Abs(moveInput);
        targetBlend = Mathf.Clamp01(absMove);
        currentBlend = Mathf.MoveTowards(currentBlend, targetBlend, blendSpeed * Time.deltaTime);

        float idleVolume = Mathf.Lerp(maxVolume, minVolume, currentBlend);
        float driveVolume = Mathf.Lerp(0f, maxVolume, currentBlend);

        float idlePitch = Mathf.Lerp(maxPitch, minPitch, currentBlend);
        float drivePitch = Mathf.Lerp(minPitch, maxPitch, currentBlend);

        if (idleSource != null)
        {
            idleSource.volume = idleVolume;
            idleSource.pitch = idlePitch;
        }
        if (driveSource != null)
        {
            driveSource.volume = driveVolume;
            driveSource.pitch = drivePitch;
        }

        if (speedDisplay != null)
            speedDisplay.SetSpeed(currentSpeed);
    }

    void FixedUpdate()
    {
        if (!useRigidbody || rb == null) return;

        
        float rate = (Mathf.Abs(targetSpeed) > Mathf.Epsilon) ? acceleration : deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.fixedDeltaTime);

        
        
        
        Vector3 desiredVelocity = transform.forward * currentSpeed;

        
        desiredVelocity.y = rb.linearVelocity.y;

        rb.linearVelocity = desiredVelocity;

        
        float effectiveTurnSpeed = turnSpeed * (currentSpeed < 0 ? turnWhileReverseFactor : 1f);
        float desiredAngular = targetTurnInput * effectiveTurnSpeed;

        
        float smoothTime = rotationSmoothTime;
        if (Mathf.Abs(targetTurnInput) > 0.05f)
            smoothTime = rotationSmoothTime * 0.4f;

        currentYaw = Mathf.SmoothDampAngle(currentYaw, currentYaw + desiredAngular * Time.fixedDeltaTime,
            ref yawVelocity, smoothTime, Mathf.Infinity, Time.fixedDeltaTime);

        rb.rotation = Quaternion.Euler(0f, currentYaw, 0f);
    }
}
