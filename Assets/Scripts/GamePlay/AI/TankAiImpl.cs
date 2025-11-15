using UnityEngine;

public class TankAIImpl
{
    readonly TankAI owner;
    readonly AIInit init;
    readonly AIAudio audio;
    readonly AIStats stats;
    readonly AIPerception perception;
    readonly AINavigation navigation;
    readonly AICombat combat;
    readonly AIWeapons weapons;

    public TankAIImpl(TankAI owner)
    {
        this.owner = owner;
        init = new AIInit(owner);
        audio = new AIAudio(owner);
        stats = new AIStats(owner);
        perception = new AIPerception(owner);
        navigation = new AINavigation(owner);
        combat = new AICombat(owner);
        weapons = new AIWeapons(owner);
    }

    public void Awake() => init.Awake();
    public void Start() => init.Start();
    public void Update()
    {
        // Pause check + audio update + scanning + main state machine
        if (GameUIManager.Instance != null && GameUIManager.Instance.IsPaused) return;
        audio.UpdateEngineAudio();

        owner.scanTimer -= Time.deltaTime;
        if (owner.scanTimer <= 0f)
        {
            owner.scanTimer = owner.scanInterval;
            perception.FindNearestEnemy();
            perception.FindNearestCapturePoint();
        }

        owner.agent.speed = owner.moveSpeed;

        Transform effectiveTarget = null;
        TankAI.AIState nextState = TankAI.AIState.Patrolling;

        if (owner.currentCapturePointTarget != null && Vector3.Distance(owner.transform.position, owner.currentCapturePointTarget.transform.position) < 3f)
        {
            if (owner.navAvailable && owner.agent.isOnNavMesh) owner.agent.isStopped = true;
            owner.isCapturing = true;
            nextState = TankAI.AIState.Capturing;

            if (owner.debugLogs) Debug.Log($"[AI] Capturing point at {owner.currentCapturePointTarget.gameObject.name}");
        }
        else if (owner.currentCapturePointTarget != null)
        {
            float distToCapturePoint = Vector3.Distance(owner.transform.position, owner.currentCapturePointTarget.transform.position);
            float distToEnemy = owner.currentTarget != null ? Vector3.Distance(owner.transform.position, owner.currentTarget.position) : float.MaxValue;
            owner.agent.stoppingDistance = 0f;
            if (distToCapturePoint < distToEnemy || distToEnemy > owner.shootRange * 1.5f)
            {
                effectiveTarget = owner.currentCapturePointTarget.transform;
                owner.isCapturing = false;
                nextState = TankAI.AIState.Moving;
            }
            else if (distToEnemy < owner.shootRange)
            {
                effectiveTarget = owner.currentTarget;
                owner.agent.stoppingDistance = owner.shootRange * 0.85f;
                owner.currentCapturePointTarget = null;
                owner.isCapturing = false;
                nextState = TankAI.AIState.Fighting;
            }
        }
        else if (owner.player != null)
        {
            owner.agent.stoppingDistance = owner.shootRange * 0.85f;
            effectiveTarget = owner.player;
            owner.isCapturing = false;
            nextState = TankAI.AIState.Fighting;
        }
        else if (owner.currentTarget != null)
        {
            effectiveTarget = owner.currentTarget;
            float distToEnemy = Vector3.Distance(owner.transform.position, owner.currentTarget.position);

            owner.agent.stoppingDistance = owner.shootRange * 0.85f;
            owner.isCapturing = false;

            nextState = (distToEnemy <= owner.shootRange) ? TankAI.AIState.Fighting : TankAI.AIState.Moving;
        }

        owner.currentState = nextState;

        if (owner.isCapturing)
        {
            navigation.AlignBodyToVector((owner.currentCapturePointTarget.transform.position - owner.transform.position).normalized);
        }
        else if (effectiveTarget != null)
        {
            float dist = Vector3.Distance(owner.transform.position, effectiveTarget.position);

            if (dist < owner.shootRange && nextState == TankAI.AIState.Fighting)
            {
                if (owner.navAvailable && owner.agent.isOnNavMesh) owner.agent.isStopped = true;
                combat.AimAt(effectiveTarget);

                if (Time.time >= owner.nextFireTime && perception.HasLineOfSight(effectiveTarget))
                {
                    Vector3 aimDir = (effectiveTarget.position - owner.gunEnd.position).normalized;
                    float angle = Vector3.Angle(owner.gunEnd.forward, aimDir);
                    if (angle < 5f)
                    {
                        weapons.ShootAt(effectiveTarget);
                        owner.nextFireTime = Time.time + 1f / Mathf.Max(0.0001f, owner.fireRate);
                    }
                }
            }
            else
            {
                if (owner.navAvailable && owner.agent.isOnNavMesh)
                {
                    owner.agent.isStopped = false;
                    owner.agent.SetDestination(effectiveTarget.position);
                    navigation.AlignBodyToVelocity();
                }
                else
                {
                    Vector3 toTarget = effectiveTarget.position - owner.transform.position;
                    toTarget.y = 0f;
                    if (toTarget.sqrMagnitude > 0.001f)
                    {
                        Vector3 move = toTarget.normalized * owner.moveSpeed * Time.deltaTime;
                        owner.transform.position += move;
                        navigation.AlignBodyToVector(toTarget.normalized);
                    }
                }
            }
        }
        else
        {
            if (owner.navAvailable && owner.agent.isOnNavMesh && !owner.agent.hasPath)
            {
                Vector3 rand = owner.transform.position + Random.insideUnitSphere * 8f;
                if (UnityEngine.AI.NavMesh.SamplePosition(rand, out UnityEngine.AI.NavMeshHit hit, 3f, UnityEngine.AI.NavMesh.AllAreas))
                    owner.agent.SetDestination(hit.position);
            }
            navigation.AlignBodyToVelocity();
        }
    }

    public void OnDrawGizmos()
    {
        if (!owner.debugGizmos) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(owner.transform.position, owner.detectionRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(owner.transform.position, owner.capturePointDetectionRadius);

        if (owner.gunEnd != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(owner.gunEnd.position, owner.gunEnd.forward * 5f);
        }

        if (owner.currentCapturePointTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(owner.transform.position, owner.currentCapturePointTarget.transform.position);
        }
    }
}
