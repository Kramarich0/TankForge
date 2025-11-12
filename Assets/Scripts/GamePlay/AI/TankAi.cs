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
    private TankHealth tankHealth;

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
    private int bulletDamage;

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
    private CapturePoint currentCapturePointTarget = null;
    private bool isCapturing = false;

    private NavMeshAgent agent;
    private float nextFireTime = 0f;
    private bool navAvailable = false;

    TeamComponent teamComp;
    Transform currentTarget;
    NavMeshAgent targetAgent;
    private AIState currentState = AIState.Idle;
    public BulletPool bulletPool;


    float scanTimer = 0f;
    readonly float scanInterval = 0.4f;

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

    private AudioSource idleSource;
    private AudioSource driveSource;
    private AudioSource shootSource;


    void Awake()
    {
        tankHealth = GetComponent<TankHealth>();
        agent = GetComponent<NavMeshAgent>();
        teamComp = GetComponent<TeamComponent>();

        SetupEngineAudio();
    }

    void SetupEngineAudio()
    {
        if (idleSound != null)
        {
            idleSource = gameObject.AddComponent<AudioSource>();
            idleSource.clip = idleSound;
            idleSource.loop = true;
            idleSource.spatialBlend = 1f;
            idleSource.minDistance = 3f;
            idleSource.maxDistance = 50f;
            idleSource.rolloffMode = AudioRolloffMode.Logarithmic;
            idleSource.volume = minIdleVolume;
            idleSource.Play();
        }

        if (driveSound != null)
        {
            driveSource = gameObject.AddComponent<AudioSource>();
            driveSource.clip = driveSound;
            driveSource.loop = true;
            driveSource.spatialBlend = 1f;
            driveSource.minDistance = 3f;
            driveSource.maxDistance = 50f;
            driveSource.rolloffMode = AudioRolloffMode.Logarithmic;
            driveSource.volume = minDriveVolume;
            driveSource.Play();
        }

        if (shootSound != null)
        {
            shootSource = gameObject.AddComponent<AudioSource>();
            shootSource.clip = shootSound;
            shootSource.loop = false;
            shootSource.spatialBlend = 1f;
            shootSource.minDistance = 3f;
            shootSource.maxDistance = 50f;
            shootSource.rolloffMode = AudioRolloffMode.Logarithmic;
            shootSource.volume = 0.6f; // Можно подстроить громкость
        }
    }

    void Start()
    {
        ApplyStatsFromClass();

        agent.speed = moveSpeed;
        agent.angularSpeed = 120f;
        agent.acceleration = 8f;
        agent.updateRotation = false;
        agent.updatePosition = true;
        agent.stoppingDistance = shootRange * 0.85f;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 10f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            navAvailable = true;
            if (debugLogs) Debug.Log("[AI] NavMesh found and agent warped.");
        }
        else
        {
            navAvailable = false;
            if (debugLogs) Debug.LogWarning("[AI] NavMesh not found nearby. Using fallback movement.");
        }

        if (enemyHealthDisplayPrefab != null && tankHealth != null)
        {
            var d = Instantiate(enemyHealthDisplayPrefab);
            d.target = tankHealth;
            d.targetTeam = teamComp;
        }
    }

    void ApplyStatsFromClass()
    {
        TankStats stats = mediumStats;
        switch (tankClass)
        {
            case TankClass.Light:
                stats = lightStats; break;
            case TankClass.Medium:
                stats = mediumStats; break;
            case TankClass.Heavy:
                stats = heavyStats; break;
        }

        moveSpeed = stats.moveSpeed;
        rotationSpeed = stats.rotationSpeed;
        fireRate = stats.fireRate;
        shootRange = stats.shootRange;
        bulletDamage = stats.bulletDamage;

        if (tankHealth != null)
        {
            tankHealth.maxHealth = stats.health;
            tankHealth.currentHealth = stats.health;
        }
    }

    void Update()
    {
        if (GameUIManager.Instance != null && GameUIManager.Instance.IsPaused) return;
        UpdateEngineAudio();

        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0f)
        {
            scanTimer = scanInterval;
            FindNearestEnemy();
            FindNearestCapturePoint();
        }

        agent.speed = moveSpeed;

        Transform effectiveTarget = null;
        AIState nextState = AIState.Patrolling;

        if (currentCapturePointTarget != null && Vector3.Distance(transform.position, currentCapturePointTarget.transform.position) < 3f)
        {
            if (navAvailable && agent.isOnNavMesh) agent.isStopped = true;
            isCapturing = true;
            nextState = AIState.Capturing;

            if (debugLogs) Debug.Log($"[AI] Capturing point at {currentCapturePointTarget.gameObject.name}");
        }
        else if (currentCapturePointTarget != null)
        {
            float distToCapturePoint = Vector3.Distance(transform.position, currentCapturePointTarget.transform.position);
            float distToEnemy = currentTarget != null ? Vector3.Distance(transform.position, currentTarget.position) : float.MaxValue;
            agent.stoppingDistance = 0f;
            if (distToCapturePoint < distToEnemy || distToEnemy > shootRange * 1.5f)
            {
                effectiveTarget = currentCapturePointTarget.transform;
                isCapturing = false;
                nextState = AIState.Moving;
            }
            else if (distToEnemy < shootRange)
            {
                effectiveTarget = currentTarget;
                agent.stoppingDistance = shootRange * 0.85f;
                currentCapturePointTarget = null;
                isCapturing = false;
                nextState = AIState.Fighting;
            }
        }
        else if (player != null)
        {
            agent.stoppingDistance = shootRange * 0.85f;
            effectiveTarget = player;
            isCapturing = false;
            nextState = AIState.Fighting;
        }
        else if (currentTarget != null)
        {
            effectiveTarget = currentTarget;
            agent.stoppingDistance = shootRange * 0.85f;
            isCapturing = false;
            nextState = AIState.Moving;
        }

        currentState = nextState;

        if (isCapturing)
        {
            AlignBodyToVector((currentCapturePointTarget.transform.position - transform.position).normalized);
        }
        else if (effectiveTarget != null)
        {
            float dist = Vector3.Distance(transform.position, effectiveTarget.position);

            if (dist < shootRange && nextState == AIState.Fighting)
            {
                if (navAvailable && agent.isOnNavMesh) agent.isStopped = true;
                AimAt(effectiveTarget);

                if (Time.time >= nextFireTime && HasLineOfSight(effectiveTarget))
                {
                    Vector3 aimDir = (effectiveTarget.position - gunEnd.position).normalized;
                    float angle = Vector3.Angle(gunEnd.forward, aimDir);
                    if (angle < 5f)
                    {
                        ShootAt(effectiveTarget);
                        nextFireTime = Time.time + 1f / Mathf.Max(0.0001f, fireRate);
                    }
                }
            }
            else
            {
                if (navAvailable && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.SetDestination(effectiveTarget.position);
                    AlignBodyToVelocity();
                }
                else
                {
                    Vector3 toTarget = effectiveTarget.position - transform.position;
                    toTarget.y = 0f;
                    if (toTarget.sqrMagnitude > 0.001f)
                    {
                        Vector3 move = toTarget.normalized * moveSpeed * Time.deltaTime;
                        transform.position += move;
                        AlignBodyToVector(toTarget.normalized);
                    }
                }
            }
        }
        else
        {
            if (navAvailable && agent.isOnNavMesh && !agent.hasPath)
            {
                Vector3 rand = transform.position + Random.insideUnitSphere * 8f;
                if (NavMesh.SamplePosition(rand, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);
            }
            AlignBodyToVelocity();
        }
    }

    void UpdateEngineAudio()
    {
        if (agent == null) return;

        float speed = agent.velocity.magnitude;
        float blend = Mathf.Clamp01(speed / moveSpeed);

        if (idleSource != null)
        {
            idleSource.volume = Mathf.Lerp(maxIdleVolume, minIdleVolume, blend);
            idleSource.pitch = Mathf.Lerp(maxIdlePitch, minIdlePitch, blend);
        }

        if (driveSource != null)
        {
            driveSource.volume = Mathf.Lerp(minDriveVolume, maxDriveVolume, blend);
            driveSource.pitch = Mathf.Lerp(minDrivePitch, maxDrivePitch, blend);
        }
    }

    void AlignBodyToVelocity()
    {
        if (body != null && agent != null)
        {
            Vector3 vel = agent.velocity;
            if (vel.sqrMagnitude > 0.01f)
            {
                Vector3 forwardDir = vel.normalized * (invertBodyForward ? -1f : 1f);
                Quaternion target = Quaternion.LookRotation(forwardDir);
                body.rotation = Quaternion.RotateTowards(body.rotation, target, rotationSpeed * Time.deltaTime);
            }
        }
    }

    void AlignBodyToVector(Vector3 vec)
    {
        if (body != null)
        {
            Vector3 forwardDir = vec * (invertBodyForward ? -1f : 1f);
            Quaternion target = Quaternion.LookRotation(forwardDir);
            body.rotation = Quaternion.RotateTowards(body.rotation, target, rotationSpeed * Time.deltaTime);
        }
    }

    void FindNearestEnemy()
    {
        TeamComponent[] all = FindObjectsByType<TeamComponent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        float bestDist = float.MaxValue;
        Transform best = null;
        foreach (var tc in all)
        {
            if (tc == null || tc == teamComp) continue;
            if (tc.team == teamComp.team) continue;
            float d = Vector3.SqrMagnitude(tc.transform.position - transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = tc.transform;
            }
        }
        currentTarget = best;
        if (currentTarget != null) targetAgent = currentTarget.GetComponent<NavMeshAgent>();
    }

    void FindNearestCapturePoint()
    {
        Collider[] captureColliders = Physics.OverlapSphere(transform.position, capturePointDetectionRadius, capturePointsLayer);
        float bestDist = float.MaxValue;
        CapturePoint best = null;

        foreach (var col in captureColliders)
        {
            CapturePoint cp = col.GetComponent<CapturePoint>();
            if (cp == null) continue;

            if (cp.GetControllingTeam() == teamComp.team) continue;

            float d = Vector3.SqrMagnitude(cp.transform.position - transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = cp;
            }
        }

        currentCapturePointTarget = best;
        if (debugLogs && best != null) Debug.Log($"[AI] Found capture point: {best.gameObject.name} at distance {Mathf.Sqrt(bestDist):F1}");
    }

    bool HasLineOfSight(Transform t)
    {
        if (t == null || gunEnd == null) return false;
        Vector3 from = gunEnd.position;
        Vector3 to = t.position + Vector3.up * 1.2f;
        Vector3 dir = to - from;

        if (Physics.Raycast(from, dir.normalized, out RaycastHit hit, shootRange))
        {
            if (hit.collider.transform.IsChildOf(transform))
                return false;

            if (hit.collider.transform == t || hit.collider.transform.IsChildOf(t))
                return true;

            return false;
        }

        return true;
    }


    void AimAt(Transform t)
    {
        if (turret != null)
        {
            Vector3 dir = t.position - turret.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
                turret.rotation = Quaternion.RotateTowards(turret.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }

        if (gun != null && gunEnd != null)
        {
            Vector3 predicted = PredictTargetPosition(t);
            Vector3 adjustedTarget = new(predicted.x, gun.position.y, predicted.z);
            Vector3 localDir = turret.InverseTransformDirection(adjustedTarget - gun.position);

            float pitch;
            if (gunUsesLocalXForPitch)
                pitch = Mathf.Atan2(localDir.y, localDir.z) * Mathf.Rad2Deg;
            else
                pitch = Mathf.Atan2(localDir.y, localDir.x) * Mathf.Rad2Deg;

            pitch = Mathf.Clamp(pitch, minGunAngle, maxGunAngle);
            Vector3 currentEuler = gun.localEulerAngles;
            float newPitch = Mathf.MoveTowardsAngle(currentEuler.x, pitch, rotationSpeed * Time.deltaTime);
            gun.localEulerAngles = new Vector3(newPitch, currentEuler.y, currentEuler.z);
        }
    }

    Vector3 PredictTargetPosition(Transform t)
    {
        Vector3 targetPos = t.position;
        Vector3 targetVel = Vector3.zero;
        if (targetAgent != null) targetVel = targetAgent.velocity;
        else
        {
            if (t.TryGetComponent<Rigidbody>(out var rb)) targetVel = rb.linearVelocity;
        }

        Vector3 dir = targetPos - gunEnd.position;
        float dist = dir.magnitude;
        float time = dist / Mathf.Max(0.001f, projectileSpeed);

        for (int i = 0; i < 3; i++)
        {
            Vector3 predicted = targetPos + targetVel * time;
            Vector3 toPred = predicted - gunEnd.position;
            float d = toPred.magnitude;
            time = d / Mathf.Max(0.001f, projectileSpeed);
        }

        return targetPos + targetVel * time;
    }

    void ShootAt(Transform t)
    {
        if (bulletPool == null || gunEnd == null) return;

        Vector3 predicted = PredictTargetPosition(t);
        Vector3 aim = predicted - gunEnd.position;
        Vector3 horizontal = new Vector3(aim.x, 0f, aim.z);
        float distance = horizontal.magnitude;
        float dy = aim.y;

        Vector3 initVelocity;

        if (bulletUseGravity)
        {
            float v = projectileSpeed;
            float g = Mathf.Abs(Physics.gravity.y);
            float d = distance;
            float v4 = v * v * v * v;
            float under = v4 - g * (g * d * d + 2f * dy * v * v);
            if (under >= 0f && d > 0.001f)
            {
                float sq = Mathf.Sqrt(under);
                float lowAngle = Mathf.Atan2(v * v - sq, g * d);
                float highAngle = Mathf.Atan2(v * v + sq, g * d);
                float chosen = Mathf.Min(lowAngle, highAngle);

                Vector3 flatDir = horizontal.normalized;
                Vector3 launchDir = Quaternion.AngleAxis(chosen * Mathf.Rad2Deg, Vector3.Cross(flatDir, Vector3.up)) * flatDir;
                initVelocity = launchDir * v;
            }
            else
            {
                initVelocity = aim.normalized * projectileSpeed;
            }
        }
        else
        {
            initVelocity = aim.normalized * projectileSpeed;
        }

        string shooterDisplay = (teamComp != null && !string.IsNullOrEmpty(teamComp.displayName))
            ? teamComp.displayName
            : gameObject.name;

        Collider[] shooterColliders = GetComponentsInParent<Collider>();

        bulletPool.SpawnBullet(
            gunEnd.position,
            initVelocity,
            teamComp != null ? teamComp.team : TeamEnum.Neutral,
            shooterDisplay,
            bulletDamage,
            shooterColliders
        );

        shootSource?.Play();
    }

    void OnDrawGizmos()
    {
        if (!debugGizmos) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, capturePointDetectionRadius);

        if (gunEnd != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(gunEnd.position, gunEnd.forward * 5f);
        }

        if (currentCapturePointTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, currentCapturePointTarget.transform.position);
        }
    }
}
