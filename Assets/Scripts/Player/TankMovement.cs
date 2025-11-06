using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Более реалистичное управление танком на базе Rigidbody.
/// - Все физические действия в FixedUpdate.
/// - Плавная акселерация через MoveTowards для скорости.
/// - Разделение forward/backward speeds и поворот в зависимости от скорости.
/// - Подходит для arcade-ish танка; для настоящих гусеничных треков потребуется симуляция двух гусениц.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class TankMovement : MonoBehaviour
{
    [Header("Input")]
    public InputActionAsset actionsAsset;
    private InputAction moveAction;

    [Header("Speeds")]
    public float maxForwardSpeed = 6f;
    public float maxBackwardSpeed = 2.5f;
    public float acceleration = 8f; // units/sec^2
    public float deceleration = 10f; // braking
    public float turnSpeedStationary = 90f; // deg/sec when stopped
    public float turnSpeedMoving = 45f; // deg/sec when moving

    [Header("Physics")]
    public float mass = 1500f;

    private Rigidbody rb;
    private float targetSpeed = 0f;
    private float currentSpeed = 0f;
    private Vector2 inputMove;

    [Header("Audio (optional)")]
    public AudioSource idleSource;
    public AudioSource driveSource;
    [Range(0f,1f)] public float minVolume = 0.2f;
    [Range(0f,1f)] public float maxVolume = 1f;
    [Range(0.5f,2f)] public float minPitch = 0.8f;
    [Range(0.5f,2f)] public float maxPitch = 1.3f;
    public float audioBlendSpeed = 3f;
    private float audioBlend = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = mass;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void OnEnable()
    {
        if (actionsAsset == null) Debug.LogError("TankMovementImproved: actionsAsset не назначен");
        moveAction = actionsAsset?.FindAction("Gameplay/Move", true);
        moveAction?.Enable();
    }

    void OnDisable() => moveAction?.Disable();

    void Update()
    {
        inputMove = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        // расчёт целевой скорости (в единицах в секунду)
        if (inputMove.y > 0.01f) targetSpeed = inputMove.y * maxForwardSpeed;
        else if (inputMove.y < -0.01f) targetSpeed = inputMove.y * maxBackwardSpeed; // будет отрицательной
        else targetSpeed = 0f;

        // аудио бленд
        float absInput = Mathf.Abs(inputMove.y);
        float targetBlend = absInput;
        audioBlend = Mathf.MoveTowards(audioBlend, targetBlend, audioBlendSpeed * Time.deltaTime);
        if (idleSource) { idleSource.volume = Mathf.Lerp(maxVolume, minVolume, audioBlend); idleSource.pitch = Mathf.Lerp(maxPitch, minPitch, audioBlend); }
        if (driveSource) { driveSource.volume = Mathf.Lerp(0f, maxVolume, audioBlend); driveSource.pitch = Mathf.Lerp(minPitch, maxPitch, audioBlend); }
    }

    void FixedUpdate()
    {
        // Плавная аппроксимация текущей скорости
        float accel = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * Time.fixedDeltaTime);

        // Двигаем Rigidbody корректно через MovePosition (совместимо с interpolation)
        Vector3 move = transform.forward * (currentSpeed * Time.fixedDeltaTime);
        rb.MovePosition(rb.position + move);

        // Поворот: зависит от того, движемся ли мы
        float turnInput = inputMove.x;
        float currentTurnSpeed = Mathf.Lerp(turnSpeedStationary, turnSpeedMoving, Mathf.InverseLerp(0f, maxForwardSpeed, Mathf.Abs(currentSpeed)));
        float yaw = turnInput * currentTurnSpeed * Time.fixedDeltaTime;

        // Когда едем назад, инвертируем поворот (опционально)
        if (currentSpeed < -0.01f) yaw = -yaw;

        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        rb.MoveRotation(rb.rotation * rot);
    }
}
