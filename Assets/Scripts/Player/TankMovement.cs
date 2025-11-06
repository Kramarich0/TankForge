using UnityEngine;
using UnityEngine.InputSystem;

public class TankMovement : MonoBehaviour
{
    public InputActionAsset actionsAsset;
    private InputAction moveAction;

    [Header("Movement")]
    public float forwardSpeed = 4f;
    public float backwardSpeed = 1f;
    public float acceleration = .0002f;
    public float deceleration = 8f;
    public float turnSpeed = 30f;

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

    void Awake()
    {
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
        else
        {
            Debug.LogWarning("AudioClip не назначен для одного из звуков!");
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
        float moveInput = v.y;
        float turnInput = v.x;

        if (moveInput > 0)
            targetSpeed = moveInput * forwardSpeed;
        else if (moveInput < 0)
            targetSpeed = moveInput * backwardSpeed;
        else
            targetSpeed = 0f;

        float accelerationRate = (targetSpeed != 0) ? acceleration : deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelerationRate * Time.deltaTime);

        float move = currentSpeed * Time.deltaTime;
        transform.Translate(Vector3.forward * move, Space.Self);
        transform.Rotate(0f, turnInput * turnSpeed * Time.deltaTime, 0f);

        if (speedDisplay != null)
        {
            //            Debug.Log("Speed: " + currentSpeed);
            speedDisplay.SetSpeed(currentSpeed);
        }
        else
        {
            Debug.Log("SpeedDisplay is null!");
        }

        float absMove = Mathf.Abs(moveInput);
        targetBlend = Mathf.Clamp01(absMove);

        currentBlend = Mathf.MoveTowards(currentBlend, targetBlend, blendSpeed * Time.deltaTime);

        float idleVolume = Mathf.Lerp(maxVolume, minVolume, currentBlend);
        float driveVolume = Mathf.Lerp(0f, maxVolume, currentBlend);

        float idlePitch = Mathf.Lerp(maxPitch, minPitch, currentBlend);
        float drivePitch = Mathf.Lerp(minPitch, maxPitch, currentBlend);

        idleSource.volume = idleVolume;
        idleSource.pitch = idlePitch;

        driveSource.volume = driveVolume;
        driveSource.pitch = drivePitch;
    }
}