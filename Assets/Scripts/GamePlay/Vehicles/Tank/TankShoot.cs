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
    public float recoilBack = 0.15f;
    public float recoilUp = 0.05f;
    public float recoilSpeed = 20f;
    public float recoilReturnSpeed = 7f;
    public int bulletDamage = 20;

    private Vector3 recoilVelocity;

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
        if (GameUIManager.Instance != null && GameUIManager.Instance.IsPaused) return;
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
        else if (reloadSoundPlaying)
        {
            audioSource.Stop();
            audioSource.loop = false;
            reloadSoundPlaying = false;
        }


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
        onShotFired?.Invoke();
    }

    void HideMuzzleSmoke()
    {
        if (muzzleSmoke != null) muzzleSmoke.SetActive(false);
    }
}
