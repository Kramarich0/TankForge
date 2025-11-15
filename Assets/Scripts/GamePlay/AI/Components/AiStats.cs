public class AIStats
{
    readonly TankAI owner;
    public AIStats(TankAI owner) { this.owner = owner; }

    public void ApplyStatsFromClass()
    {
        TankAI.TankStats stats = owner.mediumStats;
        switch (owner.tankClass)
        {
            case TankAI.TankClass.Light:
                stats = owner.lightStats; break;
            case TankAI.TankClass.Medium:
                stats = owner.mediumStats; break;
            case TankAI.TankClass.Heavy:
                stats = owner.heavyStats; break;
        }

        owner.moveSpeed = stats.moveSpeed;
        owner.rotationSpeed = stats.rotationSpeed;
        owner.fireRate = stats.fireRate;
        owner.shootRange = stats.shootRange;
        owner.bulletDamage = stats.bulletDamage;

        if (owner.tankHealth != null)
        {
            owner.tankHealth.maxHealth = stats.health;
            owner.tankHealth.currentHealth = stats.health;
        }
    }
}
