using UnityEngine;
using UnityEngine.AI;

public class AINavigation
{
    readonly TankAI owner;

    // Добавляем контекст как у игрока
    float smoothedMove = 0f;
    float smoothedTurn = 0f;
    float moveVelocity = 0f;
    float turnVelocity = 0f;
    float enginePower = 0f;
    float reverseLockTimer = 0f;

    public AINavigation(TankAI owner) { this.owner = owner; }

    public void UpdateNavigation()
    {
        // Обновляем таймер обратного хода как у игрока
        if (reverseLockTimer > 0f)
            reverseLockTimer -= Time.deltaTime;

        // Сглаживаем вводы как у игрока
        if (Mathf.Abs(smoothedTurn) < 0.05f) smoothedTurn = 0f;

        float targetMove = GetTargetMoveInput();
        float targetTurn = GetTargetTurnInput();

        smoothedMove = Mathf.SmoothDamp(smoothedMove, targetMove, ref moveVelocity, 1f / Mathf.Max(owner.moveResponse, 0.1f));
        smoothedTurn = Mathf.SmoothDamp(smoothedTurn, targetTurn, ref turnVelocity, 1f / Mathf.Max(owner.turnResponse, 0.1f));

        // Управление мощностью двигателя как у игрока
        float inputMagnitude = Mathf.Max(Mathf.Abs(smoothedMove), Mathf.Abs(smoothedTurn));
        float targetEnginePower = inputMagnitude > 0.01f ? 1f : 0f;
        enginePower = Mathf.MoveTowards(enginePower, targetEnginePower, Time.deltaTime * 0.8f);
    }

    public void MoveTo(Vector3 position)
    {
        if (owner.agent == null || !owner.navAvailable || !owner.agent.isOnNavMesh) return;

        UpdateNavigation();

        // Обновляем цель агента
        if (!owner.agent.hasPath || Vector3.Distance(owner.agent.destination, position) > 0.5f)
            owner.agent.SetDestination(position);

        var rb = owner.GetComponent<Rigidbody>();
        bool hasTracks = owner.leftTrack != null && owner.rightTrack != null && rb != null;

        if (!hasTracks)
        {
            // Fallback для объектов без гусениц
            owner.agent.updatePosition = true;
            owner.agent.isStopped = false;
            AlignBodyToVelocity();
            return;
        }

        owner.agent.updatePosition = false;
        owner.agent.isStopped = false;

        // Останавливаемся если близко к цели
        float stopDist = Mathf.Max(0.25f, owner.agent.stoppingDistance);
        Vector3 toTarget = position - owner.transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude < stopDist)
        {
            ApplyTankPhysics(0f, 0f, rb);
            owner.agent.isStopped = true;
            return;
        }

        // Используем сглаженные значения как у игрока
        ApplyTankPhysics(smoothedMove, smoothedTurn, rb);

        // Плавно ориентируем корпус в направлении движения
        AlignBodyToMovementDirection(smoothedMove, smoothedTurn);
    }

    private float GetTargetMoveInput()
    {
        if (owner.agent == null || !owner.agent.hasPath) return 0f;

        Vector3 toNext = owner.agent.steeringTarget - owner.transform.position;
        toNext.y = 0f;

        if (toNext.magnitude < 0.1f) return 0f;

        Vector3 dir = toNext.normalized;
        float angle = Vector3.SignedAngle(owner.transform.forward, dir, Vector3.up);

        // Определяем ввод движения на основе направления
        float moveInput = Vector3.Dot(owner.transform.forward, dir);

        // При больших углах сначала поворачиваем на месте
        if (Mathf.Abs(angle) > 60f)
            moveInput = Mathf.Clamp(moveInput, -0.3f, 0.3f);

        return Mathf.Clamp(moveInput, -1f, 1f);
    }

    private float GetTargetTurnInput()
    {
        if (owner.agent == null || !owner.agent.hasPath) return 0f;

        Vector3 toNext = owner.agent.steeringTarget - owner.transform.position;
        toNext.y = 0f;

        if (toNext.magnitude < 0.1f) return 0f;

        Vector3 dir = toNext.normalized;
        float angle = Vector3.SignedAngle(owner.transform.forward, dir, Vector3.up);

        // Нормализуем угол для поворота
        float turnInput = Mathf.Clamp(angle / 45f, -1f, 1f);

        return turnInput;
    }

    private void ApplyTankPhysics(float moveInput, float turnInput, Rigidbody rb)
    {
        if (rb == null) return;

        // Копируем физику из MovementPhysics игрока
        float currentForwardSpeed = Vector3.Dot(rb.linearVelocity, owner.transform.forward);
        float absForwardSpeed = Mathf.Abs(currentForwardSpeed);


        // Reverse lock как у игрока
        if (absForwardSpeed > owner.movingThreshold && moveInput != 0f &&
            Mathf.Sign(currentForwardSpeed) != Mathf.Sign(moveInput) && reverseLockTimer <= 0f)
        {
            reverseLockTimer = owner.reverseLockDuration;
        }

        float desiredBrake = 0f;
        if (reverseLockTimer > 0f)
        {
            desiredBrake = owner.maxBrakeTorque;
            if (owner.leftTrack != null) owner.leftTrack.ApplyTorque(0f, desiredBrake);
            if (owner.rightTrack != null) owner.rightTrack.ApplyTorque(0f, desiredBrake);
            return;
        }

        // Расчет мощности поворота как у игрока
        float speedFactor = Mathf.Clamp01(absForwardSpeed / owner.maxForwardSpeed);
        float lowSpeedBoost = 1f + (1f - speedFactor) * 2.0f;
        float effectiveTurnSharpness = owner.turnSharpness * lowSpeedBoost;

        float leftPower = Mathf.Clamp(moveInput + turnInput * effectiveTurnSharpness, -1f, 1f);
        float rightPower = Mathf.Clamp(moveInput - turnInput * effectiveTurnSharpness, -1f, 1f);

        // Обработка реверса как у игрока
        bool wantsReverse = Mathf.Sign(moveInput) != Mathf.Sign(currentForwardSpeed);
        if (absForwardSpeed > 0.5f && wantsReverse)
        {
            float speedRatio = Mathf.InverseLerp(0.5f, owner.maxForwardSpeed, absForwardSpeed);
            desiredBrake = Mathf.Lerp(owner.maxBrakeTorque * 0.2f, owner.maxBrakeTorque, speedRatio);
            leftPower *= 0.2f;
            rightPower *= 0.2f;
        }

        // Ограничение скорости как у игрока
        float currentMaxSpeed = currentForwardSpeed > 0f ? owner.maxForwardSpeed : owner.maxBackwardSpeed;
        float speedLimitFactor = 1f;
        if (absForwardSpeed > currentMaxSpeed * 0.8f)
        {
            speedLimitFactor = Mathf.InverseLerp(currentMaxSpeed, currentMaxSpeed * 0.8f, absForwardSpeed);
            speedLimitFactor = Mathf.Clamp01(speedLimitFactor);
        }

        float reverseFactor = 0.6f;
        float leftMotor = leftPower * owner.maxMotorTorque * speedLimitFactor * enginePower * (leftPower < 0f ? reverseFactor : 1f);
        float rightMotor = rightPower * owner.maxMotorTorque * speedLimitFactor * enginePower * (rightPower < 0f ? reverseFactor : 1f);

        if (owner.leftTrack != null) owner.leftTrack.ApplyTorque(leftMotor, desiredBrake);
        if (owner.rightTrack != null) owner.rightTrack.ApplyTorque(rightMotor, desiredBrake);

        // Боковая стабилизация для уменьшения скольжения
        Vector3 lateralVel = Vector3.Project(rb.linearVelocity, owner.transform.right);
        if (lateralVel.sqrMagnitude > 0.01f)
        {
            rb.AddForce(-lateralVel * 8f, ForceMode.Acceleration);
        }
    }

    private void AlignBodyToMovementDirection(float moveInput, float turnInput)
    {
        if (owner.body == null) return;

        // Определяем желаемое направление на основе вводов
        Vector3 desiredForward = owner.transform.forward;

        if (Mathf.Abs(moveInput) > 0.1f || Mathf.Abs(turnInput) > 0.1f)
        {
            // При движении ориентируемся по направлению движения
            Quaternion targetRotation = Quaternion.LookRotation(desiredForward);
            owner.body.rotation = Quaternion.RotateTowards(owner.body.rotation, targetRotation, owner.rotationSpeed * Time.deltaTime);
        }
    }

    public void AlignBodyToVelocity()
    {
        if (owner.body != null && owner.agent != null)
        {
            Vector3 vel = owner.agent.velocity;
            if (vel.sqrMagnitude > 0.01f)
            {
                Vector3 forwardDir = vel.normalized * (owner.invertBodyForward ? -1f : 1f);
                Quaternion target = Quaternion.LookRotation(forwardDir);
                owner.body.rotation = Quaternion.RotateTowards(owner.body.rotation, target, owner.rotationSpeed * Time.deltaTime);
            }
        }
    }

    public void PatrolRandom()
    {
        if (owner.agent != null && owner.navAvailable && owner.agent.isOnNavMesh && !owner.agent.hasPath)
        {
            Vector3 rand = owner.transform.position + Random.insideUnitSphere * 8f;
            if (NavMesh.SamplePosition(rand, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                owner.agent.SetDestination(hit.position);
        }
    }

    public void DrawGizmos()
    {
        if (!owner.debugGizmos) return;
        Gizmos.color = Color.green;
        if (owner.agent != null)
            Gizmos.DrawLine(owner.transform.position, owner.transform.position + owner.agent.velocity);
    }
}