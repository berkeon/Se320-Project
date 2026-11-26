using System;
using UnityEngine;

// The player's base at the end of the path. Every enemy that reaches it takes away some health.
public class PlayerBase : MonoBehaviour
{
    [SerializeField] int maxHealth = 20;

    public int Health { get; private set; }
    public bool IsDestroyed => Health <= 0;

    public event Action<int> HealthChanged;
    public event Action BaseDestroyed;

    void Awake()
    {
        Health = maxHealth;
    }

    void OnEnable()
    {
        Enemy.ReachedBase += OnEnemyReachedBase;
    }

    void OnDisable()
    {
        Enemy.ReachedBase -= OnEnemyReachedBase;
    }

    void OnEnemyReachedBase(Enemy enemy)
    {
        TakeDamage(enemy.BaseDamage);
    }

    public void TakeDamage(int amount)
    {
        if (IsDestroyed) return;

        Health = Mathf.Max(0, Health - amount);
        HealthChanged?.Invoke(Health);

        if (Health == 0)
            BaseDestroyed?.Invoke();
    }
}
