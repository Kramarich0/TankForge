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
    public float maxTurnTorque = 8000f;
    public float maxBrakeTorque = 5000f;
    [Header("Pivot turn")]
    public float pivotTurnForce = 6000f;
    public float pivotAddRbTorque = 8000f;

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
    public float blendSpeed = 5f;
    private AudioSource idleSource;
    private AudioSource driveSource;
    private float currentBlend = 0f;
    private float targetBlend = 0f;


    private float rawMoveInput = 0f;
    private float rawTurnInput = 0f;
    private float smoothedMove = 0f;
    private float smoothedTurn = 0f;

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
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;


        if (rb.angularDamping < 0.5f) rb.angularDamping = 0.5f;

        idleSource = gameObject.AddComponent<AudioSource>();
        driveSource = gameObject.AddComponent<AudioSource>();
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
        Vector2 v = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        float rMove = Mathf.Clamp(v.y, -1f, 1f);
        float rTurn = Mathf.Clamp(v.x, -1f, 1f);

        rawMoveInput = Mathf.Abs(rMove) < inputDeadzone ? 0f : rMove;
        rawTurnInput = Mathf.Abs(rTurn) < inputDeadzone ? 0f : rTurn;

        float absMove = Mathf.Abs(rawMoveInput);
        targetBlend = Mathf.Clamp01(absMove);
        currentBlend = Mathf.MoveTowards(currentBlend, targetBlend, blendSpeed * Time.deltaTime);
        float idleVolume = Mathf.Lerp(maxVolume, minVolume, currentBlend);
        float driveVolume = Mathf.Lerp(0f, maxVolume, currentBlend);
        float idlePitch = Mathf.Lerp(maxPitch, minPitch, currentBlend);
        float drivePitch = Mathf.Lerp(minPitch, maxPitch, currentBlend);

        if (idleSource != null) { idleSource.volume = idleVolume; idleSource.pitch = idlePitch; }
        if (driveSource != null) { driveSource.volume = driveVolume; driveSource.pitch = drivePitch; }


        float currentSpeedMS = Vector3.Dot(rb.linearVelocity, transform.forward);
        float currentSpeedKmh = currentSpeedMS * 3.6f;
        if (speedDisplay != null) speedDisplay.SetSpeed((int)currentSpeedKmh);
    }

    void FixedUpdate()
    {
        smoothedMove = Mathf.MoveTowards(smoothedMove, rawMoveInput, moveResponse * Time.fixedDeltaTime);
        smoothedTurn = Mathf.MoveTowards(smoothedTurn, rawTurnInput, turnResponse * Time.fixedDeltaTime);


        HandleMovementPhysics(smoothedMove, smoothedTurn);
        LimitSpeed();
    }

    void HandleMovementPhysics(float moveInput, float turnInput)
    {
        if (rb == null) return;

        float absMove = Mathf.Abs(moveInput);
        float absTurn = Mathf.Abs(turnInput);

        float currentForwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        if (absMove < 0.05f && absTurn > 0.05f)
        {

            float left = turnInput * pivotTurnForce;
            float right = -turnInput * pivotTurnForce;

            leftTrack.ApplyTorque(left, 0f);
            rightTrack.ApplyTorque(right, 0f);


            rb.AddRelativeTorque(pivotAddRbTorque * Time.fixedDeltaTime * turnInput * Vector3.up, ForceMode.Acceleration);
            return;
        }

        float leftPower = Mathf.Clamp(moveInput + turnInput * turnSharpness, -1f, 1f);
        float rightPower = Mathf.Clamp(moveInput - turnInput * turnSharpness, -1f, 1f);

        float reverseFactor = 0.6f;
        float leftMotor = leftPower * maxMotorTorque * (leftPower < 0f ? reverseFactor : 1f);
        float rightMotor = rightPower * maxMotorTorque * (rightPower < 0f ? reverseFactor : 1f);

        float desiredBrake = 0f;
        if (Mathf.Abs(currentForwardSpeed) > movingThreshold && Mathf.Sign(currentForwardSpeed) != Mathf.Sign(moveInput) && moveInput != 0f)
        {
            float speedRatio = Mathf.InverseLerp(0.5f, maxForwardSpeed, Mathf.Abs(currentForwardSpeed));
            desiredBrake = Mathf.Lerp(maxBrakeTorque * 0.2f, maxBrakeTorque, speedRatio);
            leftMotor *= 0.2f;
            rightMotor *= 0.2f;
        }
        leftTrack.ApplyTorque(leftMotor, desiredBrake);
        rightTrack.ApplyTorque(rightMotor, desiredBrake);

    }

    void LimitSpeed()
    {
        if (rb == null) return;

        Vector3 forwardVel = Vector3.Project(rb.linearVelocity, transform.forward);
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        float max = forwardSpeed > 0f ? maxForwardSpeed : maxBackwardSpeed;

        if (Mathf.Abs(forwardSpeed) > max + 0.001f)
        {
            Vector3 newForward = transform.forward * Mathf.Sign(forwardSpeed) * max;
            Vector3 lateral = rb.linearVelocity - forwardVel;
            rb.linearVelocity = newForward + lateral;
        }
    }
}
