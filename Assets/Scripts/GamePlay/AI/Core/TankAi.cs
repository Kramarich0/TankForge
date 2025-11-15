using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(TankHealth))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(TeamComponent))]
public class TankAI : MonoBehaviour
{
    public enum TankClass { Light, Medium, Heavy }
    public enum AIState { Idle, Patrolling, Moving, Capturing, Fighting }

    [System.Serializable]
    public struct TankStats
    {
        public float health;
        public float moveSpeed;
        public float rotationSpeed;
        public float fireRate;
        public float shootRange;
        public int bulletDamage;
    }

    [Header("Class")]
    public TankClass tankClass = TankClass.Medium;
    public TankStats lightStats = new() { health = 75f, moveSpeed = 7f, rotationSpeed = 60f, fireRate = 1f, shootRange = 80f, bulletDamage = 10 };
    public TankStats mediumStats = new() { health = 150f, moveSpeed = 4.3f, rotationSpeed = 40f, fireRate = .5f, shootRange = 100f, bulletDamage = 100 };
    public TankStats heavyStats = new() { health = 300f, moveSpeed = 2f, rotationSpeed = 30f, fireRate = .2f, shootRange = 120f, bulletDamage = 200 };

    [Header("Health")]
    internal TankHealth tankHealth;

    [Header("UI")]
    public HealthAiDisplay enemyHealthDisplayPrefab;

    [Header("Transforms")]
    public Transform player;
    public Transform turret;
    public Transform gun;
    public Transform gunEnd;
    public Transform body;

    [Header("Prefabs")]
    public GameObject bulletPrefab;

    [Header("Movement / Combat")]
    public float shootRange = 100f;
    public float moveSpeed = 3f;
    public float rotationSpeed = 40f;
    public float fireRate = 1f;
    public float minGunAngle = -5f;
    public float maxGunAngle = 20f;
    internal int bulletDamage;

    [Header("Projectile")]
    public float projectileSpeed = 80f;
    public bool bulletUseGravity = true;

    [Header("Debug / Fixes")]
    public bool debugGizmos = true;
    public bool debugLogs = false;
    [Tooltip("Если модель корпуса в сцене смотрит 'назад' относительно forward (Z), включи это")]
    public bool invertBodyForward = false;
    [Tooltip("Если башня у модели смотрит в -Z (назад), включи это")]
    public bool invertTurretForward = false;
    [Tooltip("Если ствол вверх/вниз использует локальную ось X вместо Z, включи это")]
    public bool gunUsesLocalXForPitch = true;

    [Header("Perception")]
    public float detectionRadius = 50f;
    public float fieldOfView = 140f;
    public LayerMask detectionMask = -1;

    [Header("Capture Points")]
    public LayerMask capturePointsLayer = -1;
    public float capturePointDetectionRadius = 60f;
    internal CapturePoint currentCapturePointTarget = null;
    internal bool isCapturing = false;

    internal NavMeshAgent agent;
    internal float nextFireTime = 0f;
    internal bool navAvailable = false;

    internal TeamComponent teamComp;
    internal Transform currentTarget;
    internal NavMeshAgent targetAgent;
    internal AIState currentState = AIState.Idle;
    public BulletPool bulletPool;

    internal float scanTimer = 0f;
    internal readonly float scanInterval = 0.4f;

    [Header("Audio")]
    public AudioClip idleSound;
    public AudioClip driveSound;
    public AudioClip shootSound;

    [Header("Engine Audio Settings")]
    [Range(0f, 1f)] public float minIdleVolume = 0.2f;
    [Range(0f, 1f)] public float maxIdleVolume = 0.5f;
    [Range(0f, 1f)] public float minDriveVolume = 0f;
    [Range(0f, 1f)] public float maxDriveVolume = 0.5f;
    [Range(0.5f, 2f)] public float minIdlePitch = 0.8f;
    [Range(0.5f, 2f)] public float maxIdlePitch = 1.2f;
    [Range(0.5f, 2f)] public float minDrivePitch = 0.8f;
    [Range(0.5f, 2f)] public float maxDrivePitch = 1.3f;

    internal AudioSource idleSource;
    internal AudioSource driveSource;
    internal AudioSource shootSource;

    TankAIImpl impl;

    void Awake()
    {
        impl = new TankAIImpl(this);
        impl.Awake();
    }

    void Start()
    {
        impl.Start();
    }

    void Update()
    {
        impl.Update();
    }

    void OnDrawGizmos()
    {
        impl?.OnDrawGizmos();
    }
}
