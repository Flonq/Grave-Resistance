using UnityEngine;
using System.Collections;

public class WaveManager : MonoBehaviour
{
    [Header("Wave Settings")]
    public int currentWave = 1;
    public int zombiesPerWave = 5;
    public int waveMultiplier = 2;
    public float timeBetweenWaves = 10f;
    
    [Header("Spawn Settings")]
    public GameObject zombiePrefab;
    public Transform[] spawnPoints;
    public float spawnDelay = 2f;
    
    [Header("UI")]
    public TMPro.TextMeshProUGUI waveText;
    public TMPro.TextMeshProUGUI countdownText;
    
    // Wave tracking
    private int zombiesSpawned = 0;
    private int zombiesAlive = 0;
    private bool isWaveActive = false;
    
    // Singleton
    public static WaveManager Instance;
    
    // Events
    public System.Action<int> OnWaveChanged;
    public System.Action<int> OnZombieCountChanged;
    
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    void Start()
    {
        StartCoroutine(StartFirstWave());
    }
    
    System.Collections.IEnumerator StartFirstWave()
    {
        yield return new WaitForSeconds(3f); // Game start delay
        StartWave();
    }
    
    public void StartWave()
    {
        isWaveActive = true;
        zombiesSpawned = 0;
        
        int zombiesToSpawn = zombiesPerWave + (currentWave - 1) * waveMultiplier;
        
        UpdateWaveUI();
        Debug.Log($"Wave {currentWave} started! Spawning {zombiesToSpawn} zombies");
        
        // Fire events
        OnWaveChanged?.Invoke(currentWave);
        OnZombieCountChanged?.Invoke(zombiesToSpawn);
        
        StartCoroutine(SpawnZombies(zombiesToSpawn));
    }
    
    System.Collections.IEnumerator SpawnZombies(int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnZombie();
            zombiesSpawned++;
            zombiesAlive++;
            
            yield return new WaitForSeconds(spawnDelay);
        }
    }
    
    void SpawnZombie()
    {
        if (spawnPoints.Length == 0) return;
        
        // Random spawn point seç
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        
        // Zombie spawn et
        GameObject zombie = Instantiate(zombiePrefab, spawnPoint.position, spawnPoint.rotation);
        
        Debug.Log($"Zombie spawned at {spawnPoint.name}");
    }
    
    public void OnZombieDeath()
    {
        zombiesAlive--;
        Debug.Log($"Zombie died! Remaining: {zombiesAlive}");
        
        // YENİ: UI güncelle
        OnZombieCountChanged?.Invoke(zombiesAlive);
        
        // Wave complete check
        if (zombiesAlive <= 0 && isWaveActive)
        {
            CompleteWave();
        }
    }
    
    void CompleteWave()
    {
        isWaveActive = false;
        currentWave++;
        
        Debug.Log($"Wave {currentWave - 1} completed!");
        
        // Start break between waves
        StartCoroutine(WaveBreak());
    }
    
    System.Collections.IEnumerator WaveBreak()
    {
        float breakTimer = timeBetweenWaves;
        
        while (breakTimer > 0)
        {
            UpdateCountdownUI(breakTimer);
            yield return new WaitForSeconds(1f);
            breakTimer--;
        }
        
        StartWave(); // Start next wave
    }
    
    void UpdateWaveUI()
    {
        if (waveText != null)
            waveText.text = $"Wave {currentWave}";
    }
    
    void UpdateCountdownUI(float time)
    {
        if (countdownText != null)
            countdownText.text = $"Next Wave: {Mathf.Ceil(time)}";
    }
} 