using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Shows the game over panel when the base health reaches zero, and pauses the game.
// Restart reloads the game scene.
public class GameOverScreen : MonoBehaviour
{
    [SerializeField] PlayerBase playerBase;
    [SerializeField] GameObject panel;
    [SerializeField] Button restartButton;

    void Awake()
    {
        panel.SetActive(false);
        restartButton.onClick.AddListener(Restart);
    }

    void OnEnable()
    {
        if (playerBase != null) playerBase.BaseDestroyed += Show;
    }

    void OnDisable()
    {
        if (playerBase != null) playerBase.BaseDestroyed -= Show;
    }

    void Show()
    {
        panel.SetActive(true);
        Time.timeScale = 0f;
    }

    void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
