// Fast enemy with low health.
public class FastEnemy : Enemy
{
    void Reset()
    {
        speed = 4.5f;
        maxHealth = 3;
        reward = 8;
        baseDamage = 1;
    }
}
