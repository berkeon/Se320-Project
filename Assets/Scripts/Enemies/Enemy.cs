using System;
using UnityEngine;

// Shared code for every enemy type: walking along the path, health and dying.
// The enemy types only differ in the numbers below, which are set per prefab in the Inspector.
public abstract class Enemy : MonoBehaviour
{
    // Other systems listen to these, so the enemy doesn't need to know about money or the base.
    public static event Action<Enemy> Died;          // killed by a tower
    public static event Action<Enemy> ReachedBase;   // walked all the way to the end of the path

    [Header("Stats (set per prefab)")]
    [SerializeField] protected float speed = 2.5f;
    [SerializeField] protected int maxHealth = 6;
    [SerializeField] protected int reward = 10;      // money the player gets for a kill
    [SerializeField] protected int baseDamage = 1;   // base health lost when this enemy gets through

    [Header("Movement")]
    [SerializeField] float turnSpeed = 10f;

    int health;
    bool isDead;
    EnemyPath path;
    int waypointIndex;

    public bool IsDead => isDead;
    public int Health => health;
    public int Reward => reward;
    public int BaseDamage => baseDamage;

    protected virtual void Awake()
    {
        health = maxHealth;
    }

    // Called by the wave manager right after the enemy is spawned.
    public void SetPath(EnemyPath newPath)
    {
        path = newPath;
        waypointIndex = 0;
    }

    void Update()
    {
        if (isDead || path == null || path.Count == 0) return;
        MoveAlongPath();
    }

    void MoveAlongPath()
    {
        Vector3 target = path.GetWaypoint(waypointIndex).position;
        target.y = transform.position.y; // only move on the ground, keep our own height

        Vector3 direction = target - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, turnSpeed * Time.deltaTime);
        }

        // MoveTowards never goes past the target point. Before this we moved by direction * speed and
        // checked "distance < 0.1", and the enemy kept stepping over the waypoint and jittering around it.
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (transform.position == target)
        {
            waypointIndex++;
            if (waypointIndex >= path.Count)
                ReachEnd();
        }
    }

    void ReachEnd()
    {
        if (isDead) return;
        isDead = true; // towers and projectiles ignore it from now on
        ReachedBase?.Invoke(this);
        Destroy(gameObject);
    }

    public void TakeDamage(int amount)
    {
        // Two projectiles can hit the enemy in the same frame. Without this check the second one
        // killed it again and it was counted twice.
        if (isDead) return;

        health -= amount;
        if (health <= 0)
            Die();
    }

    void Die()
    {
        isDead = true;
        Died?.Invoke(this);
        Destroy(gameObject);
    }
}
