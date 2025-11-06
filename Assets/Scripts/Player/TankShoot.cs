using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class TankShoot : MonoBehaviour
{
    [Header("Shooting")]
    public InputActionReference shootAction;
    public Transform gunEnd;
    public GameObject bulletPrefab;
    public float fireRate = 0.5f;

    [Header("Effects")]
    public GameObject muzzleSmoke;
    public AudioSource audioSource;
    public AudioClip shootSound;
    public AudioClip reloadSound;

    [Header("Reload UI")]
    public ReloadDisplay reloadDisplay;

    [Header("Recoil")]
    public float recoilAmount = 0.1f;
    public float recoilSpeed = 10f;
    public float returnSpeed = 5f;

    private float nextFireTime = 0f;
    private bool isRecoiling = false;
    private Vector3 originalLocalPos;

    private bool canShoot = false;
    private bool reloadSoundPlaying = false;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        originalLocalPos = transform.localPosition;
        nextFireTime = 0f;


        StartCoroutine(EnableShootingNextFrame());
    }

    IEnumerator EnableShootingNextFrame()
    {
        yield return null;
        canShoot = true;

        if (shootAction?.action != null)
        {
            shootAction.action.Enable();
            shootAction.action.Reset();
        }
    }

    void OnDisable()
    {
        if (shootAction?.action != null)
        {
            shootAction.action.Disable();
        }
    }

    void Update()
    {
        if (!canShoot) return;
        if (GamePauseManager.Instance != null && GamePauseManager.Instance.IsPaused) return;
        if (shootAction?.action == null) return;


        bool pressed = false;
        try
        {
            pressed = shootAction.action.ReadValue<float>() > 0.5f;
        }
        catch
        {
            pressed = shootAction.action.IsPressed();
        }

        if (pressed && Time.time >= nextFireTime)
        {
            Shoot();
        }


        if (reloadDisplay != null)
        {
            float remainingTime = Mathf.Max(0f, nextFireTime - Time.time);
            reloadDisplay.SetReload(remainingTime, 1f / fireRate);
        }


        if (Time.time < nextFireTime && nextFireTime > 0f)
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
            audioSource.clip = null;
            reloadSoundPlaying = false;
        }


        if (isRecoiling)
        {
            Vector3 targetPos = originalLocalPos - transform.forward * recoilAmount;
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPos, recoilSpeed * Time.deltaTime);
            isRecoiling = Vector3.Distance(transform.localPosition, targetPos) > 0.01f;
        }
        else
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, originalLocalPos, returnSpeed * Time.deltaTime);
        }
    }

    void Shoot()
    {
        nextFireTime = Time.time + (1f / fireRate);

        if (shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        if (muzzleSmoke != null)
        {
            muzzleSmoke.SetActive(true);
            Invoke(nameof(HideMuzzleSmoke), 2f);
        }

        isRecoiling = true;

        if (bulletPrefab != null && gunEnd != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, gunEnd.position, gunEnd.rotation);
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = gunEnd.forward * 20f;
            }
        }
    }

    void HideMuzzleSmoke()
    {
        if (muzzleSmoke != null) muzzleSmoke.SetActive(false);
    }
}
