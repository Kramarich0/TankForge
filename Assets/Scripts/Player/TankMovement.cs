using UnityEngine;
using UnityEngine.InputSystem;

public class TankMovement : MonoBehaviour
{
    public InputActionAsset actionsAsset;
    private InputAction moveAction;

    [Header("Movement")]
    public float forwardSpeed = 8f;
    public float backwardSpeed = 4f;
    public float acceleration = 10f;    // units/sec^2
    public float deceleration = 12f;    // units/sec^2 when no input
    public float turnSpeed = 90f;       // degrees per second (at full steer)
    public float turnWhileReverseFactor = 0.6f; // поворот медленнее при движении назад

    [Header("Pivot / Rigidbody")]
    [Tooltip("Если у модели pivot не в центре, укажите Transform, расположенный в центре машины (пустой GameObject).")]
    public Transform rotationPivot; // optional pivot in world space
    [Tooltip("Если true — изменю centerOfMass на rb, чтобы вращение происходило вокруг rotationPivot (если задано).")]
    public bool adjustRigidbodyCenterOfMass = true;
    [Header("Optional Rigidbody")]
    public bool useRigidbody = true;

    [Header("Smoothing / Responsiveness")]
    [Range(0f, 1f)] public float inputDeadzone = 0.02f;
    [Tooltip("Чем меньше значение, тем быстрее остановится 'плавание' при отсутствии ввода.")]
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

    public SpeedDisplay speedDisplay;

    private Rigidbody rb;
    private float yawVelocity = 0f; 
    private Vector3 fixedDesiredVelocity = Vector3.zero;
    private float fixedDeltaY = 0f;

    void Awake()
    {
        if (useRigidbody)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogWarning("Rigidbody не найден на танке, переключаюсь на movement через Transform.");
                useRigidbody = false;
            }
            else
            {
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                // Если указан pivot и разрешено — смещаем центр массы
                if (rotationPivot != null && adjustRigidbodyCenterOfMass)
                {
                    Vector3 com = rb.transform.InverseTransformPoint(rotationPivot.position);
                    rb.centerOfMass = com;
                    // небольшая логика: логируем один раз, чтобы знать что изменили
                    Debug.Log($"TankMovement: rb.centerOfMass установлен в {com} (Local) по rotationPivot.");
                }
            }
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

        // цель скорости
        if (moveInput > 0)
            targetSpeed = moveInput * forwardSpeed;
        else if (moveInput < 0)
            targetSpeed = moveInput * backwardSpeed;
        else
            targetSpeed = 0f;

        // ускорение/торможение
        float rate = (Mathf.Abs(targetSpeed) > Mathf.Epsilon) ? acceleration : deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.deltaTime);

        // desired velocity (передаем в FixedUpdate)
        fixedDesiredVelocity = transform.forward * currentSpeed;

        // поворот — рассчитываем желаемый угловой шаг для текущего кадра
        float effectiveTurnSpeed = turnSpeed * (currentSpeed < 0 ? turnWhileReverseFactor : 1f);
        float desiredAngular = turnInput * effectiveTurnSpeed;

        // сглаживаем угол — получаем deltaY (degrees) для применения в этом кадре
        float currentY = transform.eulerAngles.y;
        float targetY = currentY + desiredAngular * Time.deltaTime;
        float smoothTime = rotationSmoothTime;
        if (Mathf.Abs(turnInput) > 0.05f) smoothTime *= 0.4f;

        float newY = Mathf.SmoothDampAngle(currentY, targetY, ref yawVelocity, smoothTime);
        float deltaY = Mathf.DeltaAngle(currentY, newY);
        fixedDeltaY = deltaY; // передаем в FixedUpdate

        // UI
        if (speedDisplay != null) speedDisplay.SetSpeed(currentSpeed);

        // Audio blend
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
    }

    void FixedUpdate()
    {
        if (useRigidbody && rb != null)
        {
            rb.MovePosition(rb.position + fixedDesiredVelocity * Time.fixedDeltaTime);
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, fixedDeltaY, 0f));
        }
        else
        {
            // Transform-based fallback
            transform.Translate(fixedDesiredVelocity * Time.fixedDeltaTime, Space.World);

            if (rotationPivot != null && !useRigidbody)
            {
                transform.RotateAround(rotationPivot.position, Vector3.up, fixedDeltaY);
            }
            else
            {
                transform.Rotate(0f, fixedDeltaY, 0f);
            }
        }
    }
}
