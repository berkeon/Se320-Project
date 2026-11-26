using UnityEngine;

// Follows its target every frame (so it doesn't miss fast enemies) and damages
// the first enemy it touches. Needs a trigger collider and a kinematic Rigidbody.
public class Projectile : MonoBehaviour
{
    [SerializeField] float speed = 18f;
    [SerializeField] AudioClip hitSound;
    [SerializeField, Range(0f, 1f)] float hitVolume = 0.15f; // quiet on purpose, it plays a lot

    Enemy target;
    int damage;
    bool hasHit;

    public void Launch(Enemy newTarget, int newDamage)
    {
        target = newTarget;
        damage = newDamage;
    }

    void Update()
    {
        // The target can die (or reach the base) before the projectile gets there.
        // Then there is nothing to follow, so the projectile just disappears.
        if (target == null || target.IsDead)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 aimPoint = target.transform.position + Vector3.up * 0.4f;
        transform.position = Vector3.MoveTowards(transform.position, aimPoint, speed * Time.deltaTime);

        Vector3 direction = aimPoint - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return; // could touch two enemies in the same physics step

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null || enemy.IsDead) return;

        hasHit = true;
        enemy.TakeDamage(damage);
        SoundEffects.Play(hitSound, hitVolume);
        Destroy(gameObject);
    }
}
