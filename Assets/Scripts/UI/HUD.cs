using TMPro;
using UnityEngine;

// In-game HUD: shows the base health and the player's money.
public class HUD : MonoBehaviour
{
    [SerializeField] PlayerBase playerBase;
    [SerializeField] MoneyManager money;
    [SerializeField] TMP_Text healthText;
    [SerializeField] TMP_Text moneyText;

    void OnEnable()
    {
        if (playerBase != null) playerBase.HealthChanged += ShowHealth;
        if (money != null) money.MoneyChanged += ShowMoney;
    }

    void OnDisable()
    {
        if (playerBase != null) playerBase.HealthChanged -= ShowHealth;
        if (money != null) money.MoneyChanged -= ShowMoney;
    }

    void Start()
    {
        ShowHealth(playerBase.Health);
        ShowMoney(money.Money);
    }

    void ShowHealth(int health)
    {
        healthText.text = "Base Health: " + health;
    }

    void ShowMoney(int amount)
    {
        moneyText.text = "Money: $" + amount;
    }
}
