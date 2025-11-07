using UnityEngine;
using UnityEngine.InputSystem;

public class TankShoot : MonoBehaviour
{
    [Header("Shooting")]
    public InputActionReference shootAction;
    public Transform gunEnd;
    public GameObject bulletPrefab;
    public float bulletSpeed = 280f;
    public float fireRate = 0.5f;

    [Header("Effects")]
    public GameObject muzzleSmoke;
    public AudioSource audioSource;
    public AudioClip shootSound;
    public AudioClip reloadSound;
    private bool reloadSoundPlaying = false;

    [Header("Reload UI")]
    public ReloadDisplay reloadDisplay;

    [Header("Recoil")]
    public float recoilAmount = 0.12f;
    public float recoilSpeed = 20f;
    public float returnSpeed = 7f;

    private float nextFireTime = 0f;
    private bool isRecoiling = false;
    private Vector3 originalLocalPos;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        originalLocalPos = transform.localPosition;
    }

    void OnEnable() { if (shootAction?.action != null) shootAction.action.Enable(); }
    void OnDisable() { if (shootAction?.action != null) shootAction.action.Disable(); }

    void Update()
    {
        if (GamePauseManager.Instance != null && GamePauseManager.Instance.IsPaused) return;
        if (shootAction?.action == null) return;

        if (reloadDisplay != null)
        {
            float remainingTime = Mathf.Max(0f, nextFireTime - Time.time);
            reloadDisplay.SetReload(remainingTime, 1f / fireRate);
        }

        if (shootAction.action.WasPressedThisFrame() && Time.time >= nextFireTime)
        {
            Shoot();
            reloadSoundPlaying = false;
        }

        if (Time.time < nextFireTime)
        {
            if (!reloadSoundPlaying && reloadSound != null)
            {
                audioSource.clip = reloadSound;
                audioSource.loop = true;
                audioSource.Play();
                reloadSoundPlaying = true;
            }
        }
        else
        {
            if (reloadSoundPlaying)
            {
                audioSource.Stop();
                audioSource.loop = false;
                reloadSoundPlaying = false;
            }
        }

        // Отдача
        if (isRecoiling)
        {
            Vector3 targetPos = originalLocalPos - transform.forward * recoilAmount;
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPos, recoilSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.localPosition, targetPos) < 0.01f)
                isRecoiling = false;
        }
        else
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, originalLocalPos, returnSpeed * Time.deltaTime);
        }
    }

    void Shoot()
    {
        nextFireTime = Time.time + (1f / fireRate);

        if (shootSound != null && audioSource != null) audioSource.PlayOneShot(shootSound);

        if (muzzleSmoke != null)
        {
            muzzleSmoke.SetActive(true);
            CancelInvoke(nameof(HideMuzzleSmoke));
            Invoke(nameof(HideMuzzleSmoke), 1.2f);
        }

        isRecoiling = true;

        if (bulletPrefab != null && gunEnd != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, gunEnd.position, gunEnd.rotation);
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = gunEnd.forward * bulletSpeed;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        }
    }

    void HideMuzzleSmoke()
    {
        if (muzzleSmoke != null) muzzleSmoke.SetActive(false);
    }
}
