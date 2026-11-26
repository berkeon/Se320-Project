using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Main menu: Start loads the game scene, Quit closes the game. Both buttons play a click sound.
public class MainMenu : MonoBehaviour
{
    [SerializeField] string gameSceneName = "Game";
    [SerializeField] Button startButton;
    [SerializeField] Button quitButton;
    [SerializeField] AudioSource clickSound;

    bool busy;

    void Start()
    {
        startButton.onClick.AddListener(() => StartCoroutine(ClickThen(StartGame)));
        quitButton.onClick.AddListener(() => StartCoroutine(ClickThen(QuitGame)));
    }

    // Plays the click and waits for it to end. Loading the scene right away cut the sound off.
    IEnumerator ClickThen(Action action)
    {
        if (busy) yield break;
        busy = true;

        float wait = 0f;
        if (clickSound != null && clickSound.clip != null)
        {
            clickSound.Play();
            wait = clickSound.clip.length;
        }
        yield return new WaitForSecondsRealtime(wait);

        action();
    }

    void StartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
