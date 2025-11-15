using UnityEngine;

public class AIStateHandler
{
    readonly TankAI owner;
    readonly AIPerception perception;
    readonly AINavigation navigation;
    readonly AICombat combat;
    readonly AIWeapons weapons;

    public AIStateHandler(TankAI owner, AIPerception perception, AINavigation navigation, AICombat combat, AIWeapons weapons)
    {
        this.owner = owner;
        this.perception = perception;
        this.navigation = navigation;
        this.combat = combat;
        this.weapons = weapons;
    }

    public void UpdateState()
    {
        // 1. Обновление таймера сканирования
        owner.scanTimer -= Time.deltaTime;
        if (owner.scanTimer <= 0f)
        {
            owner.scanTimer = owner.scanInterval;
            perception.FindNearestEnemy();
            perception.FindNearestCapturePoint();
        }

        // 2. Решение состояния
        Transform target = DetermineTarget(out TankAI.AIState nextState);
        owner.currentState = nextState;

        // 3. Действия по состоянию
        if (nextState == TankAI.AIState.Fighting && target != null)
        {
            combat.AimAt(target);
            weapons.ShootAt(target);
        }
        else if (target != null)
        {
            navigation.MoveTo(target.position);
        }
        else
        {
            navigation.PatrolRandom();
        }
    }

    Transform DetermineTarget(out TankAI.AIState nextState)
    {
        // логика выбора target и состояния
        nextState = TankAI.AIState.Patrolling;
        // возвращает Transform эффективной цели
        return owner.currentTarget ?? owner.player ?? owner.currentCapturePointTarget?.transform;
    }

    public void OnDrawGizmos()
    {
        if (!owner.debugGizmos) return;
        perception.DrawGizmos();
        navigation.DrawGizmos();
    }
}
