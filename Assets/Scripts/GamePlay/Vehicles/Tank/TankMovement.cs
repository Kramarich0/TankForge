using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class TankMovement : MonoBehaviour
{
    [Header("Input")]
    public InputActionAsset actionsAsset;
    private InputAction moveAction;
    [Range(0f, 1f)] public float inputDeadzone = 0.02f;

    [Header("Tracks")]
    public TankTrack leftTrack;
    public TankTrack rightTrack;

    [Header("Speed")]
    public SpeedDisplay speedDisplay;
    public float maxForwardSpeed = 8f;
    public float maxBackwardSpeed = 4f;
    [Tooltip("How strongly turn input affects track mixing (0..1)")]
    [Range(0f, 1f)] public float turnSharpness = 0.7f;
    [Tooltip("How quickly inputs are followed (higher = more responsive)")]
    public float moveResponse = 2.5f;
    public float turnResponse = 3.0f;

    [Header("Torque / Brakes")]
    public float maxMotorTorque = 4500f;
    public float maxBrakeTorque = 5000f;
    [Header("Rigidbody / Safety")]
    public Rigidbody rb;
    public bool enforceMinimumMass = true;
    public float minRecommendedMass = 2000f;
    [Tooltip("Минимальная продольная скорость (м/с) при которой считаем, что танк движется (для логики торможения)")]
    public float movingThreshold = 0.15f;

    [Header("Engine Sounds")]
    public AudioClip idleSound;
    public AudioClip driveSound;
    [Range(0f, 1f)] public float minVolume = 0.2f;
    [Range(0f, 1f)] public float maxVolume = 1f;
    [Range(0.5f, 2f)] public float minPitch = 0.7f;
    [Range(0.5f, 2f)] public float maxPitch = 1.3f;

    // --- INTERNAL STATE ---
    private float currentBlend = 0f;
    private float blendVelocity = 0f;
    private float rawMoveInput = 0f;
    private float rawTurnInput = 0f;
    private float smoothedMove = 0f;
    private float smoothedTurn = 0f;
    private float enginePower = 0f;
    private float enginePowerVelocity = 0f;
    private float rawMoveSmoothed = 0f;
    private float rawTurnSmoothed = 0f;
    private float moveVelocity = 0f;
    private float turnVelocity = 0f;

    private AudioSource idleSource;
    private AudioSource driveSource;
    private float reverseLockTimer = 0f;
    [Tooltip("Время блокировки мощности при смене направления (сек)")]
    public float reverseLockDuration = 0.18f;
    [Header("Turning")]
    [Tooltip("Дополнительное усиление поворота при скорости ниже 0.5 м/с")]
    public float stationaryTurnBoost = 1.5f;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("TankMovement: Rigidbody не найден!");
            enabled = false;
            return;
        }

        if (enforceMinimumMass && rb.mass < minRecommendedMass)
        {
            Debug.LogWarning($"TankMovement: масса rb ({rb.mass}) ниже рекомендуемой {minRecommendedMass}. Устанавливаю рекомендуемую.");
            rb.mass = minRecommendedMass;
        }

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (rb.angularDamping < 0.5f) rb.angularDamping = 0.5f;

        idleSource = gameObject.AddComponent<AudioSource>();
        driveSource = gameObject.AddComponent<AudioSource>();
        AudioManager.AssignToMaster(idleSource);
        AudioManager.AssignToMaster(driveSource);
        SetupAudioSource(idleSource, idleSound, true);
        SetupAudioSource(driveSource, driveSound, true);

        TankWheelSetup.ApplyToAllWheels(leftTrack.wheels, rightTrack.wheels, rb.mass);
    }

    void SetupAudioSource(AudioSource source, AudioClip clip, bool loop)
    {
        if (source == null) return;
        if (clip != null)
        {
            source.clip = clip;
            source.loop = loop;
            source.playOnAwake = false;
            source.volume = 0f;
            source.pitch = 1f;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 5f;
            source.maxDistance = 500f;
        }
    }

    void OnEnable()
    {
        if (actionsAsset == null)
        {
            Debug.LogError("TankMovement: actionsAsset не задан!");
            return;
        }
        moveAction = actionsAsset.FindAction("Gameplay/Move", true);
        if (moveAction == null)
        {
            Debug.LogError("TankMovement: Move action не найден по пути 'Gameplay/Move'!");
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
        Vector2 v = moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        float rMove = Mathf.Clamp(v.y, -1f, 1f);
        float rTurn = Mathf.Clamp(v.x, -1f, 1f);

        rawMoveInput = Mathf.Abs(rMove) < inputDeadzone ? 0f : rMove;
        rawTurnInput = Mathf.Abs(rTurn) < inputDeadzone ? 0f : rTurn;

        rawMoveSmoothed = Mathf.Lerp(rawMoveSmoothed, rMove, 5f * Time.deltaTime);
        rawTurnSmoothed = Mathf.Lerp(rawTurnSmoothed, rTurn, 5f * Time.deltaTime);

        float absMove = Mathf.Abs(rawMoveSmoothed);
        float absTurn = Mathf.Abs(rawTurnSmoothed) * 0.5f;
        float targetBlend = Mathf.Clamp01(absMove + absTurn);
        currentBlend = Mathf.SmoothDamp(currentBlend, targetBlend, ref blendVelocity, 0.2f);

        float idleVolume = Mathf.Lerp(maxVolume, minVolume, currentBlend);
        float driveVolume = Mathf.Lerp(0f, maxVolume, currentBlend);
        float idlePitch = Mathf.Lerp(maxPitch, minPitch, currentBlend);
        float drivePitch = Mathf.Lerp(minPitch, maxPitch, currentBlend);

        if (idleSource != null) { idleSource.volume = idleVolume; idleSource.pitch = idlePitch; }
        if (driveSource != null) { driveSource.volume = driveVolume; driveSource.pitch = drivePitch; }

        float currentSpeedMS = Vector3.Dot(rb.linearVelocity, transform.forward);
        float currentSpeedKmh = currentSpeedMS * 3.6f;
        speedDisplay.SetSpeed((int)currentSpeedKmh);

        if (reverseLockTimer > 0f) reverseLockTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        if (Mathf.Abs(smoothedTurn) < 0.05f) smoothedTurn = 0f;

        smoothedMove = Mathf.SmoothDamp(smoothedMove, rawMoveInput, ref moveVelocity, 1f / Mathf.Max(moveResponse, 0.1f));
        smoothedTurn = Mathf.SmoothDamp(smoothedTurn, rawTurnInput, ref turnVelocity, 1f / Mathf.Max(turnResponse, 0.1f));

        float inputMagnitude = Mathf.Max(Mathf.Abs(smoothedMove), Mathf.Abs(smoothedTurn));
        float targetEnginePower = inputMagnitude > 0.01f ? 1f : 0f;
        enginePower = Mathf.MoveTowards(enginePower, targetEnginePower, Time.deltaTime * 0.8f);

        HandleMovementPhysics(smoothedMove, smoothedTurn);
    }

    void HandleMovementPhysics(float moveInput, float turnInput)
    {
        if (rb == null) return;

        float currentForwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        float absForwardSpeed = Mathf.Abs(currentForwardSpeed);

        if (absForwardSpeed > movingThreshold && moveInput != 0f && Mathf.Sign(currentForwardSpeed) != Mathf.Sign(moveInput) && reverseLockTimer <= 0f)
        {
            reverseLockTimer = reverseLockDuration;
        }

        float desiredBrake = 0f;
        if (reverseLockTimer > 0f)
        {
            desiredBrake = maxBrakeTorque;
            leftTrack.ApplyTorque(0f, desiredBrake);
            rightTrack.ApplyTorque(0f, desiredBrake);
            return;
        }

        float speedFactor = Mathf.Clamp01(absForwardSpeed / maxForwardSpeed);
        float lowSpeedBoost = 1f + (1f - speedFactor) * 2.0f;
        float effectiveTurnSharpness = turnSharpness * lowSpeedBoost;


        float leftPower = Mathf.Clamp(moveInput + turnInput * effectiveTurnSharpness, -1f, 1f);
        float rightPower = Mathf.Clamp(moveInput - turnInput * effectiveTurnSharpness, -1f, 1f);

        bool wantsReverse = Mathf.Sign(moveInput) != Mathf.Sign(currentForwardSpeed);
        if (absForwardSpeed > 0.5f && wantsReverse)
        {
            float speedRatio = Mathf.InverseLerp(0.5f, maxForwardSpeed, absForwardSpeed);
            desiredBrake = Mathf.Lerp(maxBrakeTorque * 0.2f, maxBrakeTorque, speedRatio);
            leftPower *= 0.2f;
            rightPower *= 0.2f;
        }

        float currentMaxSpeed = currentForwardSpeed > 0f ? maxForwardSpeed : maxBackwardSpeed;
        float speedLimitFactor = 1f;
        if (absForwardSpeed > currentMaxSpeed * 0.8f)
        {
            speedLimitFactor = Mathf.InverseLerp(currentMaxSpeed, currentMaxSpeed * 0.8f, absForwardSpeed);
            speedLimitFactor = Mathf.Clamp01(speedLimitFactor);
        }

        float reverseFactor = 0.6f;
        float leftMotor = leftPower * maxMotorTorque * speedLimitFactor * enginePower * (leftPower < 0f ? reverseFactor : 1f);
        float rightMotor = rightPower * maxMotorTorque * speedLimitFactor * enginePower * (rightPower < 0f ? reverseFactor : 1f);


        leftTrack.ApplyTorque(leftMotor, desiredBrake);
        rightTrack.ApplyTorque(rightMotor, desiredBrake);
    }

}
