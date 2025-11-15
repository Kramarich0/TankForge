using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class AIInit
{
    readonly TankAI owner;
    public AIInit(TankAI owner) { this.owner = owner; }


    public void Awake()
    {
        owner.tankHealth = owner.GetComponent<TankHealth>();
        owner.agent = owner.GetComponent<NavMeshAgent>();
        owner.teamComp = owner.GetComponent<TeamComponent>();


    }


    public void Start()
    {

        new AIStats(owner).ApplyStatsFromClass();

        if (owner.agent != null)
        {
            owner.agent.speed = owner.moveSpeed;
            owner.agent.angularSpeed = 120f;
            owner.agent.acceleration = 8f;
            owner.agent.updateRotation = false;
            owner.agent.updatePosition = true;
            owner.agent.stoppingDistance = owner.shootRange * 0.85f;
            owner.agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

            if (NavMesh.SamplePosition(owner.transform.position, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                owner.agent.Warp(hit.position);
                owner.navAvailable = true;
                if (owner.debugLogs) Debug.Log("[AI] NavMesh found and agent warped.");
            }
            else
            {
                owner.navAvailable = false;
                if (owner.debugLogs) Debug.LogWarning("[AI] NavMesh not found nearby. Using fallback movement.");
            }
        }

        if (owner.enemyHealthDisplayPrefab != null && owner.tankHealth != null)
        {
            var d = Object.Instantiate(owner.enemyHealthDisplayPrefab);
            d.target = owner.tankHealth;
            d.targetTeam = owner.teamComp;
        }


        owner.StartCoroutine(DelayedSetupAudio());
    }

    IEnumerator DelayedSetupAudio()
    {

        const int maxFrames = 10;
        int frames = 0;
        while (AudioManager.Instance == null && frames < maxFrames)
        {
            frames++;
            yield return null;
        }


        SetupEngineAudio();
    }

    void SetupEngineAudio()
    {
        if (owner.idleSound != null)
        {
            owner.idleSource = owner.gameObject.AddComponent<AudioSource>();
            AudioManager.AssignToMaster(owner.idleSource);
            owner.idleSource.clip = owner.idleSound;
            owner.idleSource.loop = true;
            owner.idleSource.spatialBlend = 1f;
            owner.idleSource.minDistance = 3f;
            owner.idleSource.maxDistance = 50f;
            owner.idleSource.rolloffMode = AudioRolloffMode.Logarithmic;
            owner.idleSource.volume = owner.minIdleVolume;
            owner.idleSource.Play();
        }

        if (owner.driveSound != null)
        {
            owner.driveSource = owner.gameObject.AddComponent<AudioSource>();
            AudioManager.AssignToMaster(owner.driveSource);
            owner.driveSource.clip = owner.driveSound;
            owner.driveSource.loop = true;
            owner.driveSource.spatialBlend = 1f;
            owner.driveSource.minDistance = 3f;
            owner.driveSource.maxDistance = 50f;
            owner.driveSource.rolloffMode = AudioRolloffMode.Logarithmic;
            owner.driveSource.volume = owner.minDriveVolume;
            owner.driveSource.Play();
        }

        if (owner.shootSound != null)
        {
            owner.shootSource = owner.gameObject.AddComponent<AudioSource>();
            AudioManager.AssignToMaster(owner.shootSource);
            owner.shootSource.clip = owner.shootSound;
            owner.shootSource.loop = false;
            owner.shootSource.spatialBlend = 1f;
            owner.shootSource.minDistance = 3f;
            owner.shootSource.maxDistance = 50f;
            owner.shootSource.rolloffMode = AudioRolloffMode.Logarithmic;
            owner.shootSource.volume = 0.6f;
        }
    }
}
