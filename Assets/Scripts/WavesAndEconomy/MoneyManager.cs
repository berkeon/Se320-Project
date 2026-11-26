using System;
using UnityEngine;

// Keeps track of the player's money. Killing an enemy gives that enemy's reward,
// and building a tower costs money.
public class MoneyManager : MonoBehaviour
{
    [SerializeField] int startMoney = 150;

    public int Money { get; private set; }

    public event Action<int> MoneyChanged;

    void Awake()
    {
        Money = startMoney;
    }

    void OnEnable()
    {
        Enemy.Died += OnEnemyDied;
    }

    void OnDisable()
    {
        Enemy.Died -= OnEnemyDied;
    }

    void OnEnemyDied(Enemy enemy)
    {
        Add(enemy.Reward);
    }

    public void Add(int amount)
    {
        Money += amount;
        MoneyChanged?.Invoke(Money);
    }

    // Returns false (and spends nothing) if there isn't enough money.
    public bool TrySpend(int amount)
    {
        if (Money < amount) return false;

        Money -= amount;
        MoneyChanged?.Invoke(Money);
        return true;
    }
}
