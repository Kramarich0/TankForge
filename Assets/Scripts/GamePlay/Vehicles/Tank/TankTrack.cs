using UnityEngine;

[System.Serializable]
public class TankTrack
{
    public WheelCollider[] wheels;
    [HideInInspector] public float currentTorque;

    public void ApplyTorque(float torque, float brake)
    {
        if (wheels == null || wheels.Length == 0) return;

        int count = wheels.Length;
        for (int i = 0; i < count; i++)
        {
            if (wheels[i] == null) continue;

            float weight = Mathf.Lerp(0.3f, 1f, (float)i / (count - 1)); 
            wheels[i].motorTorque = torque * weight;
            wheels[i].brakeTorque = brake;
        }
    }

}
