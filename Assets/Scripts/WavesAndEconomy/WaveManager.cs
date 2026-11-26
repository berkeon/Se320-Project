using System.Collections;
using UnityEngine;

// Sits at the start of the path and spawns the waves.
// Every wave has a few more enemies than the one before. When a wave has finished spawning,
// a countdown starts and the next wave begins automatically.
public class WaveManager : MonoBehaviour
{
    [SerializeField] EnemyPath path;

    [Tooltip("Wave 1 uses the first type, wave 2 the second, and so on. After the last one it starts over.")]
    [SerializeField] Enemy[] enemyTypes;

    [Header("Waves (rough numbers, need playtesting)")]
    [SerializeField] int enemiesInFirstWave = 5;
    [SerializeField] int extraEnemiesPerWave = 2;
    [SerializeField] float timeBetweenSpawns = 1.2f;
    [SerializeField] float timeBeforeFirstWave = 5f;
    [SerializeField] float timeBetweenWaves = 10f;

    float countdown;
    bool spawning;

    public int CurrentWave { get; private set; }
    public float Countdown => countdown;

    void Start()
    {
        countdown = timeBeforeFirstWave;
    }

    void Update()
    {
        if (spawning || enemyTypes == null || enemyTypes.Length == 0) return;

        countdown -= Time.deltaTime;
        if (countdown <= 0f)
            StartCoroutine(SpawnWave());
    }

    IEnumerator SpawnWave()
    {
        spawning = true;
        CurrentWave++;

        int enemyCount = enemiesInFirstWave + (CurrentWave - 1) * extraEnemiesPerWave;
        Enemy enemyType = enemyTypes[(CurrentWave - 1) % enemyTypes.Length];
        Debug.Log("Wave " + CurrentWave + ": " + enemyCount + " x " + enemyType.name);

        for (int i = 0; i < enemyCount; i++)
        {
            SpawnEnemy(enemyType);
            yield return new WaitForSeconds(timeBetweenSpawns);
        }

        countdown = timeBetweenWaves;
        spawning = false;
    }

    void SpawnEnemy(Enemy prefab)
    {
        Enemy enemy = Instantiate(prefab, transform.position, transform.rotation);
        enemy.SetPath(path);
    }
}
