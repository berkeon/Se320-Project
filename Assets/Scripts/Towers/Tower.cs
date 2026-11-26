using System.Collections.Generic;
using UnityEngine;

// Shared code for every tower: finding enemies in range, turning the head and shooting.
// The range is a trigger SphereCollider on the tower itself. A trigger only fires when one of the
// two objects has a Rigidbody, so the enemy prefabs have a kinematic Rigidbody on them.
[RequireComponent(typeof(SphereCollider))]
public abstract class Tower : MonoBehaviour
{
    [Header("Stats (set per prefab)")]
    [SerializeField] protected int cost = 50;
    [SerializeField] protected float range = 5f;
    [SerializeField] protected float fireRate = 2f;   // shots per second
    [SerializeField] protected int damage = 1;

    [Header("Parts")]
    [SerializeField] Transform head;                  // the part that rotates
    [SerializeField] Transform firePoint;             // end of the barrel
    [SerializeField] Projectile projectilePrefab;
    [SerializeField] float turnSpeed = 8f;

    [Header("Sound")]
    [SerializeField] AudioClip placeSound;            // played once when the tower is built
    [SerializeField, Range(0f, 1f)] float placeVolume = 0.3f;

    readonly List<Enemy> enemiesInRange = new List<Enemy>();
    Enemy target;
    float fireCooldown;

    public int Cost => cost;

    void Awake()
    {
        SphereCollider rangeTrigger = GetComponent<SphereCollider>();
        rangeTrigger.isTrigger = true;
        rangeTrigger.radius = range;
    }

    void OnTriggerEnter(Collider other)
    {
        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null && !enemiesInRange.Contains(enemy))
            enemiesInRange.Add(enemy);
    }

    void OnTriggerExit(Collider other)
    {
        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null)
            enemiesInRange.Remove(enemy);
    }

    void Start()
    {
        SoundEffects.Play(placeSound, placeVolume);
    }

    void Update()
    {
        // Enemies that die inside the range never call OnTriggerExit, so remove them here.
        enemiesInRange.RemoveAll(e => e == null || e.IsDead);

        fireCooldown -= Time.deltaTime;

        target = FindClosestEnemy();
        if (target == null) return;

        TurnHeadTowards(target.transform.position);

        // Fire rate delay, so the tower doesn't shoot every frame.
        if (fireCooldown <= 0f)
        {
            Shoot();
            fireCooldown = 1f / fireRate;
        }
    }

    Enemy FindClosestEnemy()
    {
        Enemy closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (Enemy enemy in enemiesInRange)
        {
            float distance = (enemy.transform.position - transform.position).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = enemy;
            }
        }
        return closest;
    }

    void TurnHeadTowards(Vector3 point)
    {
        Vector3 direction = point - head.position;
        direction.y = 0f; // only turn left and right
        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        head.rotation = Quaternion.Slerp(head.rotation, lookRotation, turnSpeed * Time.deltaTime);
    }

    protected virtual void Shoot()
    {
        Projectile projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        projectile.Launch(target, damage);
    }

    // Shows the range in the Scene view when the tower is selected.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
