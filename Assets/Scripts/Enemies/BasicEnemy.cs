// The normal enemy: medium speed, medium health.
// All the behaviour is in Enemy, this class only gives different default numbers.
public class BasicEnemy : Enemy
{
    void Reset()
    {
        speed = 2.5f;
        maxHealth = 6;
        reward = 10;
        baseDamage = 1;
    }
}
