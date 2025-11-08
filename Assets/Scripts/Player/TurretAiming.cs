using UnityEngine;

[DisallowMultipleComponent]
public class TurretAiming : MonoBehaviour
{
    [Header("References")]
    public Transform turretPivot;   // Y-поворот (локально вокруг Y)
    public Transform gunPivot;      // X-поворот (локально вокруг X)
    public Transform cameraTransform; // камера/точка, по которой целимся (обычно Camera.main)
    public Transform gunEnd;        // конец ствола (для crosshair/prediction)

    [Header("Yaw (turret)")]
    public float yawSpeed = 120f;         // град/с при ограничении скорости
    public float yawSnapAngle = 1f;       // угол для "прищёлкивания"
    public float yawSmoothTime = 0.06f;   // для SmoothDampAngle (альтернатива)

    [Header("Pitch (gun)")]
    public float pitchSpeed = 90f;        // град/с
    public float pitchMin = -10f;         // минимальный угол по X (в локальных градусах)
    public float pitchMax = 25f;          // максимальный угол по X
    public float pitchSnapAngle = 0.8f;

    [Header("Misc")]
    public float aimDistance = 200f;      // куда проецируем направление камеры (точка цели)
    public bool invertPitch = false;      // если модель имеет инвертированную ось

    // внутренние
    float yawVelocity;
    float currentYaw;
    float currentPitch;

    void Start()
    {
        if (turretPivot == null || gunPivot == null || cameraTransform == null)
        {
            Debug.LogError("[TurretAiming] Не все ссылки заданы!");
            enabled = false;
            return;
        }

        // привязываемся к текущим локальным углам
        currentYaw = turretPivot.localEulerAngles.y;
        if (currentYaw > 180f) currentYaw -= 360f;

        currentPitch = gunPivot.localEulerAngles.x;
        if (currentPitch > 180f) currentPitch -= 360f;
    }

    void LateUpdate()
    {
        if (!enabled) return;

        // формируем мировую точку, куда смотрит камера
        Vector3 aimPoint = cameraTransform.position + cameraTransform.forward * aimDistance;

        // направление от pivot'а ствола к этой точке (важно: брать позицию gunPivot или turretPivot по желанию)
        Vector3 worldDir = aimPoint - (gunPivot != null ? gunPivot.position : turretPivot.position);
        if (worldDir.sqrMagnitude < 0.0001f) return;

        // --- YAW (на turretPivot.parent'е) ---
        Transform yawBase = turretPivot.parent != null ? turretPivot.parent : turretPivot;
        // переводим направление в локальные координаты базы, чтобы убрать наклон корпуса
        Vector3 localDirForYaw = yawBase.InverseTransformDirection(worldDir);
        localDirForYaw.y = 0f;
        if (localDirForYaw.sqrMagnitude > 0.000001f)
        {
            localDirForYaw.Normalize();
            float targetYaw = Mathf.Atan2(localDirForYaw.x, localDirForYaw.z) * Mathf.Rad2Deg;
            float curYaw = turretPivot.localEulerAngles.y;
            if (curYaw > 180f) curYaw -= 360f;

            // если угол маленький — "прищёлкиваем", иначе плавно движемся
            float angleDiff = Mathf.DeltaAngle(curYaw, targetYaw);
            if (Mathf.Abs(angleDiff) <= yawSnapAngle)
            {
                currentYaw = targetYaw;
            }
            else
            {
                // MoveTowardsAngle даёт предсказуемую скорость; можно заменить на SmoothDampAngle при желании
                currentYaw = Mathf.MoveTowardsAngle(curYaw, targetYaw, yawSpeed * Time.deltaTime);
            }

            turretPivot.localEulerAngles = new Vector3(0f, currentYaw, 0f);
        }

        // --- PITCH (в локале turretPivot) ---
        // переводим мировое направление в локал turretPivot (чтобы учитывать поворот башни)
        Vector3 localDirForPitch = turretPivot.InverseTransformDirection(worldDir);
        // угол по X: atan2( y , z )
        float targetPitch = Mathf.Atan2(localDirForPitch.y, localDirForPitch.z) * Mathf.Rad2Deg;
        if (invertPitch) targetPitch = -targetPitch;
        targetPitch = Mathf.Clamp(targetPitch, pitchMin, pitchMax);

        float curPitch = gunPivot.localEulerAngles.x;
        if (curPitch > 180f) curPitch -= 360f;
        float pitchDiff = Mathf.DeltaAngle(curPitch, targetPitch);

        if (Mathf.Abs(pitchDiff) <= pitchSnapAngle)
        {
            currentPitch = targetPitch;
        }
        else
        {
            currentPitch = Mathf.MoveTowardsAngle(curPitch, targetPitch, pitchSpeed * Time.deltaTime);
        }

        gunPivot.localEulerAngles = new Vector3(currentPitch, 0f, 0f);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // минимальная валидация в инспекторе
        yawSpeed = Mathf.Max(1f, yawSpeed);
        pitchSpeed = Mathf.Max(1f, pitchSpeed);
        pitchMin = Mathf.Clamp(pitchMin, -89f, 89f);
        pitchMax = Mathf.Clamp(pitchMax, -89f, 89f);
        if (pitchMin > pitchMax) { float t = pitchMin; pitchMin = pitchMax; pitchMax = t; }
    }
#endif
}
