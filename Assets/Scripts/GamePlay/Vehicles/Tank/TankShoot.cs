using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class TankShoot : MonoBehaviour
{
    public System.Action onShotFired;

    [Header("Shooting")]
    public InputActionReference shootAction;
    public Transform gunEnd;
    public BulletPool bulletPool;
    public float bulletSpeed = 800f;
    public float shotsPerSecond = 8f;

    [Header("Effects")]
    public GameObject muzzleSmoke;
    public AudioSource audioSource;
    public AudioClip shootSound;
    public AudioClip reloadSound;

    [Header("Reload UI")]
    public ReloadDisplay reloadDisplay;

    [Header("Recoil")]
    public float recoilBack = 0.12f;
    public float recoilDecay = 8f;
    public float recoilJitter = 0.01f;
    public float recoilPhysicalImpulse = 150f;
    public int bulletDamage = 20;

    private float nextFireTime = 0f;
    private bool reloadClipPlayed = false;
    private Vector3 originalLocalPos;
    private Vector3 recoilOffset = Vector3.zero;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        // AudioManager.AssignToMaster(audioSource);

        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 10f;
        audioSource.maxDistance = 500f;

        originalLocalPos = transform.localPosition;

        originalLocalPos = transform.localPosition;
    }

    void OnEnable() { shootAction.action?.Enable(); }
    void OnDisable() { shootAction.action?.Disable(); }

    void Update()
    {
        if (GameUIManager.Instance != null && GameUIManager.Instance.IsPaused) return;
        if (shootAction.action == null) return;

        if (reloadDisplay != null)
        {
            float remainingTime = Mathf.Max(0f, nextFireTime - Time.time);
            reloadDisplay.SetReload(remainingTime, 1f / shotsPerSecond);
        }

        if (shootAction.action.WasPressedThisFrame() && Time.time >= nextFireTime)
        {
            Shoot();
        }

        if (Time.time < nextFireTime)
        {
            float remainingTime = nextFireTime - Time.time;

            if (!reloadClipPlayed && remainingTime <= 2.5f && reloadSound != null)
            {
                audioSource.PlayOneShot(reloadSound);
                reloadClipPlayed = true;
            }
        }
        else
        {
            reloadClipPlayed = false;
        }

        recoilOffset = Vector3.Lerp(recoilOffset, Vector3.zero, recoilDecay * Time.deltaTime);

        transform.localPosition = originalLocalPos + recoilOffset;
    }

    void Shoot()
    {
        float fireInterval = 1f / Mathf.Max(0.0001f, shotsPerSecond);
        nextFireTime = Time.time + fireInterval;

        recoilOffset += -transform.forward * recoilBack;

        if (recoilJitter > 0f)
        {
            Vector3 jitter = Random.insideUnitSphere * recoilJitter;
            jitter.y = 0f;
            recoilOffset += jitter;
        }

        if (shootSound != null)
        {
            var tempSource = gameObject.AddComponent<AudioSource>();
            tempSource.clip = shootSound;
            tempSource.volume = Random.Range(0.9f, 1.1f);
            tempSource.pitch = Random.Range(0.95f, 1.05f);
            tempSource.spatialBlend = 1f;

            tempSource.minDistance = 10f;
            tempSource.maxDistance = 500f;
            tempSource.rolloffMode = AudioRolloffMode.Linear;
            AudioManager.AssignToMaster(tempSource);

            tempSource.Play();
            Destroy(tempSource, shootSound.length + 0.1f);
        }

        if (muzzleSmoke != null)
        {
            muzzleSmoke.SetActive(true);
            CancelInvoke(nameof(HideMuzzleSmoke));
            Invoke(nameof(HideMuzzleSmoke), 1.2f);
        }

        if (bulletPool != null && gunEnd != null)
        {
            TeamComponent teamComp = GetComponentInParent<TeamComponent>();
            TeamEnum team = teamComp ? teamComp.team : TeamEnum.Neutral;
            string shooterDisplay = teamComp != null && !string.IsNullOrEmpty(teamComp.displayName)
             ? teamComp.displayName
             : gameObject.name;

            Collider[] shooterColliders = GetComponentsInParent<Collider>();

            bulletPool.SpawnBullet(
                gunEnd.position,
                gunEnd.forward * bulletSpeed,
                team,
                shooterDisplay,
                bulletDamage,
                shooterColliders
            );
        }

        Rigidbody parentRb = GetComponentInParent<Rigidbody>();
        if (parentRb != null && recoilPhysicalImpulse > 0f)
        {
            parentRb.AddForceAtPosition(-gunEnd.forward * recoilPhysicalImpulse, gunEnd.position, ForceMode.Impulse);
        }
        
        onShotFired?.Invoke();
    }

    void HideMuzzleSmoke()
    {
        if (muzzleSmoke != null) muzzleSmoke.SetActive(false);
    }
}
