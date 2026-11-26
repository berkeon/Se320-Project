// Shoots slowly but every shot does a lot of damage.
public class HeavyTower : Tower
{
    void Reset()
    {
        cost = 120;
        range = 6f;
        fireRate = 0.5f;
        damage = 5;
    }
}
