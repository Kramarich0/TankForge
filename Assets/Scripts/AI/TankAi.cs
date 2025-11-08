using System;
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(TankHealth))]
public class TankAI : MonoBehaviour
{
    public enum Team { Player = 0, Friendly = 1, Enemy = 2 }
    [Header("Team")]
    public Team team = Team.Enemy;

    [Serializable]
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
    public TankStats lightStats = new() { health = 75f, moveSpeed = 6f, rotationSpeed = 120f, fireRate = 1.5f, shootRange = 80f, bulletDamage = 60 };
    public TankStats mediumStats = new() { health = 150f, moveSpeed = 4f, rotationSpeed = 70f, fireRate = 1f, shootRange = 100f, bulletDamage = 40 };
    public TankStats heavyStats = new() { health = 300f, moveSpeed = 2f, rotationSpeed = 45f, fireRate = 0.6f, shootRange = 120f, bulletDamage = 80 };
    public enum TankClass { Light, Medium, Heavy }
    public TankClass tankClass = TankClass.Medium;

    [Header("References")]
    public Transform turret;
    public Transform gun;
    public Transform gunEnd;
    public Transform body;
    public GameObject bulletPrefab;

    [Header("Detection & Teams")]
    public float detectionRadius = 120f;
    [Tooltip("Если оставить пустым (нулевым), будет искать по всем слоям — удобно для дебага")]
    public LayerMask targetLayer = 0;
    public float preferredAttackDistance = 30f;
    public float chaseDistance = 150f;
    public float loseTargetTime = 3f;

    [Header("Shooting")]
    public float bulletSpeed = 80f;
    [Range(0f, 10f)] public float spreadAngle = 1.5f;
    public float fireRate = 1f;
    private float nextFireTime = 0f;
    private int bulletDamage = 10;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 45f;          // deg/s for body
    public float turretRotationSpeed = 60f;    // deg/s for turret (separate, smoother)
    public float bodyRotationSmoothing = 8f;
    private Rigidbody rb;

    [Header("Combat movement (strafe)")]
    [Tooltip("Амплитуда бокового маневра (м)")]
    public float strafeAmplitude = 1.2f;
    [Tooltip("Частота страйфа")]
    public float strafeFrequency = 0.6f;
    public float approachSpeedFactor = 1f;
    public float minApproachDistance = 10f;

    [Header("Pitch smoothing")]
    public float gunPitchSmoothing = 30f; // deg/s smoothing for pitch

    [Header("Debug")]
    public bool debugGizmos = true;
    public bool debugLogs = false;

    private TankHealth tankHealth;
    private Transform currentTarget;
    private float lastSeenTargetTime = -999f;
    private Vector3 lastTargetVelocity = Vector3.zero;
    private Vector3 velocitySmooth = Vector3.zero;
    private Vector3 patrolPoint;
    private float patrolTimer = 0f;
    private float nextPatrolPick = 0f;
    private float seedOffset;
    private System.Random rnd = new System.Random();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; // контролируем Y-вращение вручную
        tankHealth = GetComponent<TankHealth>();
        seedOffset = (GetInstanceID() % 1000) * 0.1f;
    }

    void Start()
    {
        ApplyStatsFromClass();
        nextPatrolPick = Time.time + UnityEngine.Random.Range(0f, 2f);
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
        bulletDamage = stats.bulletDamage;

        detectionRadius = Mathf.Max(detectionRadius, stats.shootRange + 20f);
    }

    void Update()
    {
        if (GamePauseManager.Instance != null && GamePauseManager.Instance.IsPaused) return;

        FindOrUpdateTarget();

        if (currentTarget != null)
        {
            AimAndMaybeShoot();
        }
    }

    void FixedUpdate()
    {
        if (GamePauseManager.Instance != null && GamePauseManager.Instance.IsPaused) return;

        if (currentTarget != null)
            HandleCombatMovement();
        else
            HandlePatrolMovement();
    }

    // === FIXED/ADDED ===: более надёжный поиск цели с фолбеками
    void FindOrUpdateTarget()
    {
        // Если есть цель - проверяем жив ли target и видимость
        if (currentTarget != null)
        {
            TankHealth tgtHealth = currentTarget.GetComponent<TankHealth>();
            if (tgtHealth == null || tgtHealth.currentHealth <= 0f)
            {
                if (debugLogs) Debug.Log($"{name}: target died - clearing");
                currentTarget = null;
                return;
            }

            float distNow = Vector3.Distance(transform.position, currentTarget.position);
            if (distNow > chaseDistance * 1.5f)
            {
                currentTarget = null;
                return;
            }

            if (IsLineOfSightClear(currentTarget))
            {
                lastSeenTargetTime = Time.time;
                Rigidbody trgRb = currentTarget.GetComponent<Rigidbody>();
                if (trgRb != null) lastTargetVelocity = trgRb.linearVelocity;
                return;
            }
            else
            {
                if (Time.time - lastSeenTargetTime > loseTargetTime)
                {
                    if (debugLogs) Debug.Log($"{name}: lost sight of target - clearing");
                    currentTarget = null;
                }
                return;
            }
        }

        // Если нет цели — ищем кандидатов
        Collider[] cols;
        if (targetLayer.value == 0) // === FIXED ===: если маска не задана — ищем по всем слоям (частая причина)
        {
            cols = Physics.OverlapSphere(transform.position, detectionRadius, ~0, QueryTriggerInteraction.Ignore);
        }
        else
        {
            cols = Physics.OverlapSphere(transform.position, detectionRadius, targetLayer, QueryTriggerInteraction.Ignore);
        }

        Transform best = null;
        float bestScore = float.MaxValue;

        foreach (var c in cols)
        {
            if (c.transform == transform) continue;

            // пытаемся получить TankAI (чтобы знать команду)
            TankAI otherAI = c.GetComponent<TankAI>();
            TankHealth otherHealth = c.GetComponent<TankHealth>();
            Rigidbody otherRb = c.GetComponent<Rigidbody>();

            // фильтруем: без health — обычно не танк (фолбек — если нужен, можно расширить)
            if (otherHealth == null) continue;
            if (otherHealth.currentHealth <= 0f) continue;

            bool isCandidate = false;

            if (otherAI != null)
            {
                // если у цели есть TankAI — сравниваем команды
                if (otherAI.team != this.team) isCandidate = true;
            }
            else
            {
                // === FIXED/ADDED ===: фолбек — если у объекта нет TankAI, но есть TankHealth, считаем его враждебным
                // только если у цели не установлен явно та же команда (мы не знаем) — здесь выбор за тобой
                // удобный вариант: если цель имеет тег "Player" и наша команда != Player => считаем вражеской
                if (c.CompareTag("Player") && this.team != Team.Player) isCandidate = true;
                else if (!c.CompareTag("Player"))
                {
                    // Если у цели нет TankAI — принимаем её как потенциальную цель (например, игрок)
                    isCandidate = true;
                }
            }

            if (!isCandidate) continue;

            float d = Vector3.Distance(transform.position, c.transform.position);
            if (d < bestScore)
            {
                bestScore = d;
                best = c.transform;
            }
        }

        if (best != null)
        {
            currentTarget = best;
            lastSeenTargetTime = Time.time;
            Rigidbody trgRb = currentTarget.GetComponent<Rigidbody>();
            if (trgRb != null) lastTargetVelocity = trgRb.linearVelocity;
            if (debugLogs) Debug.Log($"{name} acquired target {currentTarget.name}");
        }
    }

    bool IsLineOfSightClear(Transform target)
    {
        if (gunEnd == null || target == null) return false;
        Vector3 dir = (GetTargetAimPoint(target) - gunEnd.position);
        float dist = dir.magnitude;

        if (dist < 0.01f) return true;

        // используем Raycast, игнорируем триггеры
        if (Physics.Raycast(gunEnd.position, dir.normalized, out RaycastHit hit, Mathf.Min(dist, detectionRadius)))
        {
            if (hit.collider.transform == target || hit.collider.transform.IsChildOf(target))
                return true;
            return false;
        }
        return true;
    }

    Vector3 GetTargetAimPoint(Transform target)
    {
        // цель по центру + немного вверх — обычно подходит для танка
        return target.position + Vector3.up * 1.0f;
    }

    // === FIXED/ADDED ===: сглаживание башни и безопасная стрельба
    private float pitchVelocity = 0f;
    void AimAndMaybeShoot()
    {
        if (turret == null || gun == null || gunEnd == null || currentTarget == null) return;

        Vector3 shooterPos = gunEnd.position;
        Vector3 targetPos = GetTargetAimPoint(currentTarget);
        Vector3 targetVel = Vector3.zero;
        Rigidbody trgRb = currentTarget.GetComponent<Rigidbody>();
        if (trgRb != null) targetVel = trgRb.linearVelocity;

        Vector3 predicted;
        bool success = FirstOrderIntercept(shooterPos, Vector3.zero, bulletSpeed, targetPos, targetVel, out predicted);
        if (!success) predicted = targetPos;

        // Поворот башни (Y) — ограничиваем скорость поворота turretRotationSpeed
        Vector3 flatDir = predicted - turret.position;
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(flatDir.normalized);
            turret.rotation = Quaternion.RotateTowards(turret.rotation, targetRot, turretRotationSpeed * Time.deltaTime);
        }

        // Pitch: локальное направление относительно башни
        Vector3 localDir = turret.InverseTransformDirection(predicted - gun.position);
        float desiredPitch = Mathf.Atan2(localDir.y, localDir.z) * Mathf.Rad2Deg;

        // Smooth pitch to avoid snapping (gunPitchSmoothing deg/sec)
        Vector3 curEuler = gun.localEulerAngles;
        float curPitch = curEuler.x;
        // Convert to signed angle
        curPitch = NormalizeAngle(curPitch);
        float newPitch = Mathf.MoveTowards(curPitch, desiredPitch, gunPitchSmoothing * Time.deltaTime);
        gun.localEulerAngles = new Vector3(newPitch, curEuler.y, curEuler.z);

        // Стрельба (проверки)
        float distToTarget = Vector3.Distance(transform.position, currentTarget.position);
        if (distToTarget <= detectionRadius && Time.time >= nextFireTime)
        {
            if (IsLineOfSightClear(currentTarget))
            {
                Vector3 aimDir = (predicted - gunEnd.position).normalized;

                // spread в конусе (не резкий)
                float halfAngle = spreadAngle * 0.5f;
                Quaternion spreadRot = Quaternion.Euler(UnityEngine.Random.Range(-halfAngle, halfAngle), UnityEngine.Random.Range(-halfAngle, halfAngle), 0f);
                Vector3 finalDir = spreadRot * aimDir;

                Shoot(finalDir);
                nextFireTime = Time.time + 1f / Mathf.Max(0.0001f, fireRate);
            }
        }
    }

    void Shoot(Vector3 direction)
    {
        if (bulletPrefab == null || gunEnd == null) return;

        GameObject b = Instantiate(bulletPrefab, gunEnd.position, Quaternion.LookRotation(direction));
        Rigidbody brb = b.GetComponent<Rigidbody>();
        if (brb != null)
        {
            brb.linearVelocity = direction * bulletSpeed;
        }

        var bulletComp = b.GetComponent<Bullet>();
        if (bulletComp != null)
        {
            bulletComp.damage = bulletDamage;
        }

        Destroy(b, 8f);
        if (debugLogs) Debug.DrawRay(gunEnd.position, direction * 10f, Color.red, 1f);
    }

    // === FIXED/ADDED ===: плавное движение в бою с PerlinNoise для страйфа (устраняет резкие фазы)
    void HandleCombatMovement()
    {
        if (currentTarget == null) return;

        Vector3 toTarget = currentTarget.position - transform.position;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        float targetForwardSpeed = 0f;
        if (dist > preferredAttackDistance + 2f)
            targetForwardSpeed = moveSpeed * approachSpeedFactor;
        else if (dist < Mathf.Max(minApproachDistance, preferredAttackDistance * 0.5f))
            targetForwardSpeed = -moveSpeed * 0.5f;
        else
            targetForwardSpeed = 0f;

        // === FIXED ===: PerlinNoise для плавности, затем map to [-1,1]
        float p = Mathf.PerlinNoise(seedOffset, Time.time * strafeFrequency);
        float lateral = (p * 2f - 1f) * strafeAmplitude;

        // Для снижения "вздрагивания" при смене направления — линейный Lerp (плавный переход)
        lateral = Mathf.Lerp(0f, lateral, 0.9f);

        Vector3 desiredVelocity = (forward * targetForwardSpeed) + (right * lateral);

        Vector3 newVel = Vector3.SmoothDamp(rb.linearVelocity, desiredVelocity, ref velocitySmooth, 0.12f, moveSpeed * 2f, Time.fixedDeltaTime);
        newVel.y = rb.linearVelocity.y; // сохраняем гравитацию
        rb.linearVelocity = newVel;

        // Поворот корпуса — используем RotateTowards с лимитом градусов/с, чтобы танк не "крутился" резко
        Vector3 lookDir = toTarget.normalized;
        if (newVel.sqrMagnitude > 0.1f) lookDir = newVel.normalized;

        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion desiredRot = Quaternion.LookRotation(lookDir);
            Quaternion limited = Quaternion.RotateTowards(rb.rotation, desiredRot, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(limited);

            if (body != null)
            {
                body.rotation = Quaternion.RotateTowards(body.rotation, limited, rotationSpeed * Time.fixedDeltaTime);
            }
        }
    }

    // === FIXED/ADDED ===: плавный патруль
    void HandlePatrolMovement()
    {
        if (Time.time >= nextPatrolPick)
        {
            Vector2 r = UnityEngine.Random.insideUnitCircle * Mathf.Clamp(detectionRadius * 0.4f, 10f, 40f);
            patrolPoint = transform.position + new Vector3(r.x, 0f, r.y);
            nextPatrolPick = Time.time + UnityEngine.Random.Range(3f, 6f);
        }

        Vector3 toPatrol = patrolPoint - transform.position;
        toPatrol.y = 0f;
        float d = toPatrol.magnitude;
        Vector3 desired = (d > 0.5f) ? toPatrol.normalized * moveSpeed * 0.6f : Vector3.zero;

        Vector3 newVel = Vector3.SmoothDamp(rb.linearVelocity, desired, ref velocitySmooth, 0.5f, moveSpeed * 2f, Time.fixedDeltaTime);
        newVel.y = rb.linearVelocity.y;
        rb.linearVelocity = newVel;

        if (toPatrol.sqrMagnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(toPatrol.normalized);
            Quaternion limited = Quaternion.RotateTowards(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(limited);
            if (body != null) body.rotation = Quaternion.RotateTowards(body.rotation, limited, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    // intercept (как раньше)
    bool FirstOrderIntercept(Vector3 shooterPos, Vector3 shooterVel, float shotSpeed, Vector3 targetPos, Vector3 targetVel, out Vector3 predicted)
    {
        predicted = targetPos;
        Vector3 r = targetPos - shooterPos;
        Vector3 v = targetVel - shooterVel;

        float a = Vector3.Dot(v, v) - shotSpeed * shotSpeed;
        float b = 2f * Vector3.Dot(v, r);
        float c = Vector3.Dot(r, r);

        float disc = b * b - 4f * a * c;
        if (disc < 0f)
            return false;

        float sqrtDisc = Mathf.Sqrt(disc);
        float t1 = (-b + sqrtDisc) / (2f * a);
        float t2 = (-b - sqrtDisc) / (2f * a);
        float t = Mathf.Min(t1, t2);
        if (t < 0f) t = Mathf.Max(t1, t2);
        if (t < 0f) return false;

        predicted = targetPos + targetVel * t;
        return true;
    }

    void OnDrawGizmos()
    {
        if (!debugGizmos) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        if (currentTarget != null && gunEnd != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(gunEnd.position, currentTarget.position + Vector3.up * 1f);
        }
    }

    float NormalizeAngle(float a)
    {
        while (a > 180f) a -= 360f;
        while (a <= -180f) a += 360f;
        return a;
    }
}
