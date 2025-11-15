using UnityEngine;

public class AIAudio
{
    readonly TankAI owner;
    public AIAudio(TankAI owner) { this.owner = owner; }

    public void UpdateEngineAudio()
    {
        if (owner.agent == null) return;

        float speed = owner.agent.velocity.magnitude;
        float blend = Mathf.Clamp01(speed / owner.moveSpeed);

        if (owner.idleSource != null)
        {
            owner.idleSource.volume = Mathf.Lerp(owner.maxIdleVolume, owner.minIdleVolume, blend);
            owner.idleSource.pitch = Mathf.Lerp(owner.maxIdlePitch, owner.minIdlePitch, blend);
        }

        if (owner.driveSource != null)
        {
            owner.driveSource.volume = Mathf.Lerp(owner.minDriveVolume, owner.maxDriveVolume, blend);
            owner.driveSource.pitch = Mathf.Lerp(owner.minDrivePitch, owner.maxDrivePitch, blend);
        }
    }
}
