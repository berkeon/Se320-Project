// Slow enemy with a lot of health.
public class TankEnemy : Enemy
{
    void Reset()
    {
        speed = 1.4f;
        maxHealth = 20;
        reward = 25;
        baseDamage = 1;
    }
}
