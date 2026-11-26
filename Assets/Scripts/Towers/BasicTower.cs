// Cheap tower that shoots fast but does little damage.
// All the behaviour is in Tower, this class only gives different default numbers.
public class BasicTower : Tower
{
    void Reset()
    {
        cost = 50;
        range = 5f;
        fireRate = 2f;
        damage = 1;
    }
}
