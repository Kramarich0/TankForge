using UnityEngine;

[DisallowMultipleComponent]
public class TurretAiming : MonoBehaviour
{
    [Header("References")]
    public Transform turretPivot;   
    public Transform gunPivot;      
    public Transform cameraTransform; 
    public Transform gunEnd;        

    [Header("Yaw (turret)")]
    public float yawSpeed = 120f;         
    public float yawSnapAngle = 1f;       
    public float yawSmoothTime = 0.06f;   

    [Header("Pitch (gun)")]
    public float pitchSpeed = 90f;        
    public float pitchMin = -10f;         
    public float pitchMax = 25f;          
    public float pitchSnapAngle = 0.8f;

    [Header("Misc")]
    public float aimDistance = 200f;      
    public bool invertPitch = false;      

    
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

        
        currentYaw = turretPivot.localEulerAngles.y;
        if (currentYaw > 180f) currentYaw -= 360f;

        currentPitch = gunPivot.localEulerAngles.x;
        if (currentPitch > 180f) currentPitch -= 360f;
    }

    void LateUpdate()
    {
        if (!enabled) return;

        
        Vector3 aimPoint = cameraTransform.position + cameraTransform.forward * aimDistance;

        
        Vector3 worldDir = aimPoint - (gunPivot != null ? gunPivot.position : turretPivot.position);
        if (worldDir.sqrMagnitude < 0.0001f) return;

        
        Transform yawBase = turretPivot.parent != null ? turretPivot.parent : turretPivot;
        
        Vector3 localDirForYaw = yawBase.InverseTransformDirection(worldDir);
        localDirForYaw.y = 0f;
        if (localDirForYaw.sqrMagnitude > 0.000001f)
        {
            localDirForYaw.Normalize();
            float targetYaw = Mathf.Atan2(localDirForYaw.x, localDirForYaw.z) * Mathf.Rad2Deg;
            float curYaw = turretPivot.localEulerAngles.y;
            if (curYaw > 180f) curYaw -= 360f;

            
            float angleDiff = Mathf.DeltaAngle(curYaw, targetYaw);
            if (Mathf.Abs(angleDiff) <= yawSnapAngle)
            {
                currentYaw = targetYaw;
            }
            else
            {
                
                currentYaw = Mathf.MoveTowardsAngle(curYaw, targetYaw, yawSpeed * Time.deltaTime);
            }

            turretPivot.localEulerAngles = new Vector3(0f, currentYaw, 0f);
        }

        
        
        Vector3 localDirForPitch = turretPivot.InverseTransformDirection(worldDir);
        
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
        
        yawSpeed = Mathf.Max(1f, yawSpeed);
        pitchSpeed = Mathf.Max(1f, pitchSpeed);
        pitchMin = Mathf.Clamp(pitchMin, -89f, 89f);
        pitchMax = Mathf.Clamp(pitchMax, -89f, 89f);
        if (pitchMin > pitchMax) { float t = pitchMin; pitchMin = pitchMax; pitchMax = t; }
    }
#endif
}
