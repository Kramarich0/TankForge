using UnityEngine;
using UnityEngine.AI;

public class SimpleTankAI : MonoBehaviour
{
    [Header("Health")]
    private TankHealth tankHealth;

    [Header("UI")]
    public HealthAiDisplay enemyHealthDisplay;

    [Header("Transfroms")]
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
    public float fireRate = 9f;
    public float minGunAngle = -5f;
    public float maxGunAngle = 20f;

    public AudioClip idleSound;
    public AudioClip driveSound;

    private AudioSource idleSource;
    private AudioSource driveSource;

    public GameObject muzzleSmoke;
    public AudioSource shootSource;
    public AudioClip shootSound;

    [Header("Debug / Fixes")]
    public bool debugGizmos = true;
    public bool debugLogs = false;
    [Tooltip("Если модель корпуса в сцене смотрит 'назад' относительно forward (Z), включи это")]
    public bool invertBodyForward = false;
    [Tooltip("Если башня у модели смотрит в -Z (назад), включи это")]
    public bool invertTurretForward = false;
    [Tooltip("Если ствол вверх/вниз использует локальную ось X вместо Z, включи это")]
    public bool gunUsesLocalXForPitch = true;

    private NavMeshAgent agent;
    private float nextFireTime = 0f;
    private bool navAvailable = false;

    void Awake()
    {
        tankHealth = GetComponent<TankHealth>();
        agent = GetComponent<NavMeshAgent>();

        idleSource = gameObject.AddComponent<AudioSource>();
        driveSource = gameObject.AddComponent<AudioSource>();

        SetupAudioSource(idleSource, idleSound, 0.5f, true);
        SetupAudioSource(driveSource, driveSound, 0.1f, true);
    }

    void UpdateAudio()
    {
        if (!navAvailable || agent == null) return;

        bool isMoving = agent.velocity.sqrMagnitude > 0.01f;

        float targetIdleVol = isMoving ? 0f : 1f;
        float targetDriveVol = isMoving ? 1f : 0f;

        idleSource.volume = Mathf.MoveTowards(idleSource.volume, targetIdleVol, Time.deltaTime * 2f);
        driveSource.volume = Mathf.MoveTowards(driveSource.volume, targetDriveVol, Time.deltaTime * 2f);

        if (!idleSource.isPlaying) idleSource.Play();
        if (!driveSource.isPlaying) driveSource.Play();
    }

    void SetupAudioSource(AudioSource source, AudioClip clip, float volume, bool loop)
    {
        if (clip != null)
        {
            source.clip = clip;
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1f;
            source.maxDistance = 100f;
            source.volume = volume;
            source.pitch = 1f;
        }
    }


    void Start()
    {
        tankHealth = GetComponent<TankHealth>();
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.angularSpeed = 120f;
        agent.acceleration = 8f;
        agent.updateRotation = false;
        agent.updatePosition = true;
        agent.stoppingDistance = shootRange * 0.9f;

        shootSource = gameObject.AddComponent<AudioSource>();
        if (shootSource != null && shootSound != null)
        {
            SetupAudioSource(shootSource, shootSound, 0.4f, false);
        }

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 2f, NavMesh.AllAreas))
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
    }

    void OnEnable()
    {
        if (idleSound != null) idleSource.Play();
        if (driveSound != null) driveSource.Play();
    }

    void OnDisable()
    {
        if (idleSource != null) idleSource.Stop();
        if (driveSource != null) driveSource.Stop();
    }


    void Update()
    {
        if (GamePauseManager.Instance != null && GamePauseManager.Instance.IsPaused) return;
        if (player == null) return;

        agent.speed = moveSpeed;
        UpdateAudio();

        float dist = Vector3.Distance(transform.position, player.position);
        if (debugLogs) Debug.Log("[AI] Distance = " + dist.ToString("F2"));


        if (enemyHealthDisplay != null)
        {
            enemyHealthDisplay.SetHealth(tankHealth.currentHealth);
        }

        if (dist < shootRange)
        {
            if (navAvailable && agent.isOnNavMesh) agent.isStopped = true;

            if (turret != null)
            {
                Vector3 dir = player.position - turret.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir.normalized * (invertTurretForward ? -1f : 1f));
                    float y = targetRot.eulerAngles.y;
                    Quaternion onlyY = Quaternion.Euler(0f, y, 0f);
                    turret.rotation = Quaternion.RotateTowards(turret.rotation, onlyY, rotationSpeed * Time.deltaTime);
                }
            }

            if (gun != null)
            {
                Vector3 dirToPlayer = player.position - gun.position;
                Vector3 adjustedTarget = new(player.position.x, gun.position.y, player.position.z);
                Vector3 localDir = turret.InverseTransformDirection(adjustedTarget - gun.position);

                float pitch;
                if (gunUsesLocalXForPitch)
                {
                    pitch = Mathf.Atan2(localDir.y, localDir.z) * Mathf.Rad2Deg;
                }
                else
                {
                    pitch = Mathf.Atan2(localDir.y, localDir.x) * Mathf.Rad2Deg;
                }

                pitch = Mathf.Clamp(pitch, minGunAngle, maxGunAngle);

                Vector3 currentEuler = gun.localEulerAngles;
                float newPitch = Mathf.MoveTowardsAngle(currentEuler.x, pitch, rotationSpeed * Time.deltaTime);
                gun.localEulerAngles = new Vector3(newPitch, currentEuler.y, currentEuler.z);
            }


            if (Time.time >= nextFireTime)
            {
                if (IsLineOfSightClear())
                {
                    Shoot();
                    nextFireTime = Time.time + (1f / Mathf.Max(0.0001f, fireRate));
                }
            }
        }
        else
        {
            if (navAvailable && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);

                Vector3 desired = agent.desiredVelocity;
                Vector3 actualVel = agent.velocity;

                Vector3 heading = desired.sqrMagnitude > 0.01f ? desired : actualVel;
                if (heading.sqrMagnitude > 0.01f && body != null)
                {
                    Vector3 forwardDir = heading.normalized * (invertBodyForward ? -1f : 1f);
                    Quaternion target = Quaternion.LookRotation(forwardDir);
                    body.rotation = Quaternion.RotateTowards(body.rotation, target, rotationSpeed * Time.deltaTime);
                }
            }
            else
            {
                // fallback
                Vector3 toTarget = player.position - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.001f)
                {
                    Vector3 move = toTarget.normalized * moveSpeed * Time.deltaTime;
                    transform.position += move;

                    if (body != null)
                    {
                        Vector3 forwardDir = toTarget.normalized * (invertBodyForward ? -1f : 1f);
                        Quaternion target = Quaternion.LookRotation(forwardDir);
                        body.rotation = Quaternion.RotateTowards(body.rotation, target, rotationSpeed * Time.deltaTime);
                    }
                }
            }
        }
    }


    bool IsLineOfSightClear()
    {
        RaycastHit hit;
        if (Physics.Raycast(gunEnd.position, player.position - gunEnd.position, out hit, shootRange))
        {
            if (hit.collider.CompareTag("Player"))
            {
                return true;
            }
        }
        return false;
    }

    void Shoot()
    {
        if (gunEnd == null || bulletPrefab == null) return;

        GameObject b = Instantiate(bulletPrefab, gunEnd.position, gunEnd.rotation);

        Rigidbody rb = b.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = gunEnd.forward * 100f;
        }

        if (shootSound != null && shootSource != null)
        {
            shootSource.PlayOneShot(shootSound);
        }

        if (muzzleSmoke != null)
        {
            muzzleSmoke.SetActive(true);
            Invoke(nameof(HideMuzzleSmoke), 0.8f);
        }

        if (debugLogs) Debug.DrawRay(gunEnd.position, gunEnd.forward * 10f, Color.red, 1f);
        Destroy(b, 8f);
    }

    void HideMuzzleSmoke()
    {
        if (muzzleSmoke != null) muzzleSmoke.SetActive(false);
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} уничтожен!");
        Destroy(gameObject);
    }


    void OnDrawGizmos()
    {
        if (!debugGizmos) return;
        if (player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, player.position + Vector3.up * 0.5f);
        }

        if (body != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(body.position + Vector3.up * 0.5f, body.position + (body.forward * 2f) + Vector3.up * 0.5f);
            Gizmos.DrawLine(body.position + Vector3.up * 0.5f, body.position + (body.right * 0.5f) + Vector3.up * 0.5f);
        }

        if (turret != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(turret.position + Vector3.up * 0.6f, turret.position + (turret.forward * 2f) + Vector3.up * 0.6f);
        }

        if (gun != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(gun.position + Vector3.up * 0.7f, gun.position + (gun.forward * 2f) + Vector3.up * 0.7f);
        }

        if (gunEnd != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(gunEnd.position + Vector3.up * 0.8f, gunEnd.position + (gunEnd.forward * 3f) + Vector3.up * 0.8f);
        }

        if (agent != null && agent.isOnNavMesh)
        {
            Gizmos.color = Color.magenta;
            Vector3 steer = agent.steeringTarget;
            Gizmos.DrawLine(transform.position + Vector3.up * 0.2f, steer + Vector3.up * 0.2f);
            Gizmos.DrawSphere(steer + Vector3.up * 0.2f, 0.1f);
            Gizmos.color = Color.white;
            Vector3 des = agent.desiredVelocity;
            Gizmos.DrawLine(transform.position + Vector3.up * 0.3f, transform.position + (des.normalized * 1.5f) + Vector3.up * 0.3f);
        }
    }
}
