using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class TankShoot : MonoBehaviour
{
    [Header("Shooting")]
    public InputActionReference shootAction;
    public Transform gunEnd;
    public GameObject bulletPrefab;
    public float bulletSpeed = 800f;
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
    public float returnSpeed = 7f;

    private float nextFireTime = 0f;
    private bool isRecoiling = false;
    private Vector3 originalLocalPos;

    [Header("Recoil")]
    public float recoilBack = 0.15f;   // сила отката назад
    public float recoilUp = 0.05f;     // небольшой подъем ствола вверх
    public float recoilSpeed = 20f;    // скорость движения к точке отдачи
    public float recoilReturnSpeed = 7f; // скорость возврата в исходное положение

    private Vector3 recoilVelocity;      // для SmoothDamp


    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        originalLocalPos = transform.localPosition;
    }

    void OnEnable() { shootAction?.action?.Enable(); }
    void OnDisable() { shootAction?.action?.Disable(); }

    void Update()
    {
        if (GamePauseManager.Instance != null && GamePauseManager.Instance.IsPaused) return;
        if (shootAction?.action == null) return;

        // Обновляем UI перезарядки
        if (reloadDisplay != null)
        {
            float remainingTime = Mathf.Max(0f, nextFireTime - Time.time);
            reloadDisplay.SetReload(remainingTime, 1f / fireRate);
        }

        // Стрельба
        if (shootAction.action.WasPressedThisFrame() && Time.time >= nextFireTime)
        {
            Shoot();
            reloadSoundPlaying = false;
        }

        // Звуки перезарядки
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
        else if (reloadSoundPlaying)
        {
            audioSource.Stop();
            audioSource.loop = false;
            reloadSoundPlaying = false;
        }

        // Вычисляем цель отдачи
        Vector3 targetPos = isRecoiling ?
         originalLocalPos - transform.forward * recoilBack + transform.up * recoilUp :
         originalLocalPos;

        transform.localPosition = Vector3.SmoothDamp(transform.localPosition, targetPos, ref recoilVelocity, 1f / (isRecoiling ? recoilSpeed : recoilReturnSpeed));

        if (isRecoiling && Vector3.Distance(transform.localPosition, targetPos) < 0.001f)
        {
            isRecoiling = false;
        }

    }

    void Shoot()
    {
        nextFireTime = Time.time + (1f / fireRate);
        isRecoiling = true;

        if (shootSound != null)
            audioSource.PlayOneShot(shootSound);

        if (muzzleSmoke != null)
        {
            muzzleSmoke.SetActive(true);
            CancelInvoke(nameof(HideMuzzleSmoke));
            Invoke(nameof(HideMuzzleSmoke), 1.2f);
        }

        if (bulletPrefab != null && gunEnd != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, gunEnd.position, gunEnd.rotation);

            // Передаём стартовые данные через Initialize()
            if (bullet.TryGetComponent<Bullet>(out var bulletScript))
            {
                TeamComponent teamComp = GetComponentInParent<TeamComponent>();
                Team team = teamComp ? teamComp.team : Team.Neutral;
                bulletScript.Initialize(gunEnd.forward * bulletSpeed, team);
            }
            else if (bullet.TryGetComponent<Rigidbody>(out var rb))
            {
                // fallback, если Bullet отсутствует
                rb.linearVelocity = gunEnd.forward * bulletSpeed;
            }
        }
    }

    void HideMuzzleSmoke()
    {
        if (muzzleSmoke != null) muzzleSmoke.SetActive(false);
    }
}
