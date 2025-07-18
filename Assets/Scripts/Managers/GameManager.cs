using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public bool isPaused = false;
    public int playerScore = 0;
    public int killCount = 0;
    public float gameTime = 0f;
    
    [Header("UI References")]
    public GameObject pauseMenu;
    public GameObject gameOverScreen;
    public GameObject gameUI;
    public TMPro.TextMeshProUGUI gameOverStatsText; // YENİ
    
    // Singleton pattern
    public static GameManager Instance { get; private set; }
    
    // Game States
    public enum GameState { Playing, Paused, GameOver }
    public GameState currentState = GameState.Playing;
    
    // Events
    public System.Action<int> OnScoreChanged;
    public System.Action<int> OnKillCountChanged;
    public System.Action OnGamePaused;
    public System.Action OnGameResumed;
    public System.Action OnGameOver;
    
    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Initialize game
        SetGameState(GameState.Playing);
        Time.timeScale = 1f;
        
        // Hide menus with delay - after scene fully loaded
        Invoke(nameof(HideAllMenus), 0.1f);
    }
    
    void Update()
    {
        if (currentState == GameState.Playing)
        {
            gameTime += Time.deltaTime;
            // ESC input kaldırıldı - PlayerController handle ediyor
        }
    }
    
    public void SetGameState(GameState newState)
    {
        currentState = newState;
        
        switch (newState)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;
                
            case GameState.Paused:
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                OnGamePaused?.Invoke();
                break;
                
            case GameState.GameOver:
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                OnGameOver?.Invoke();
                break;
        }
    }
    
    public void TogglePause()
    {
        if (currentState == GameState.Playing)
        {
            PauseGame();
        }
        else if (currentState == GameState.Paused)
        {
            ResumeGame();
        }
    }
    
    public void PauseGame()
    {
        SetGameState(GameState.Paused);
        if (pauseMenu) pauseMenu.SetActive(true);
        if (gameUI) gameUI.SetActive(false);
    }
    
    public void ResumeGame()
    {
        SetGameState(GameState.Playing);
        if (pauseMenu) pauseMenu.SetActive(false);
        if (gameUI) gameUI.SetActive(true);
        OnGameResumed?.Invoke();
    }
    
    public void GameOver()
    {
        SetGameState(GameState.GameOver);
        
        // Update Game Over UI with final stats
        UpdateGameOverStats();
        
        if (gameOverScreen) gameOverScreen.SetActive(true);
        if (gameUI) gameUI.SetActive(false);
    }

    void UpdateGameOverStats()
    {
        Debug.Log("=== UPDATING GAME OVER STATS ===");
        
        // Wait a frame for GameOverCanvas to become active
        StartCoroutine(UpdateStatsAfterFrame());
    }

    System.Collections.IEnumerator UpdateStatsAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        
        // Now find StatsText after GameOverCanvas is active
        GameObject statsText = GameObject.Find("StatsText");
        Debug.Log($"Found StatsText after frame: {statsText}");
        
        if (statsText != null)
        {
            TMPro.TextMeshProUGUI statsComponent = statsText.GetComponent<TMPro.TextMeshProUGUI>();
            if (statsComponent != null)
            {
                string newText = $"Kills: {killCount}\nScore: {playerScore}\nTime: {GetFormattedTime()}";
                statsComponent.text = newText;
                Debug.Log($"Game Over Stats Updated: {newText}");
            }
        }
        else
        {
            Debug.LogError("StatsText still not found after frame!");
        }
    }
    
    public void AddScore(int points)
    {
        playerScore += points;
        Debug.Log($"=== SCORE ADDED: +{points}, Total: {playerScore} ===");
        OnScoreChanged?.Invoke(playerScore);
        Debug.Log($"OnScoreChanged event fired: {playerScore}");
    }
    
    public void AddKill()
    {
        killCount++;
        Debug.Log($"=== KILL ADDED: {killCount} ===");
        AddScore(100); // 100 points per kill
        OnKillCountChanged?.Invoke(killCount);
        Debug.Log($"OnKillCountChanged event fired: {killCount}");
    }
    
    public void RestartGame()
    {
        // Complete GameManager reset
        Time.timeScale = 1f;
        
        // Destroy this GameManager instance
        if (Instance == this)
        {
            Instance = null;
        }
        
        // Destroy the GameObject completely
        Destroy(gameObject);
        
        // Load scene - new GameManager will be created
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    // Public getters
    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(gameTime / 60);
        int seconds = Mathf.FloorToInt(gameTime % 60);
        return $"{minutes:00}:{seconds:00}";
    }

    // Yeni fonksiyon ekle
    void HideAllMenus()
    {
        // Unity 6.0 API - FindObjectsByType kullan
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        
        foreach (Canvas canvas in canvases)
        {
            // Hide all pause-related canvases
            if (canvas.name.Contains("Pause"))
            {
                canvas.gameObject.SetActive(false);
            }
        }
        
        // Find and hide menus in new scene
        GameObject gameOverCanvas = GameObject.Find("GameOverCanvas");
        if (gameOverCanvas) gameOverCanvas.SetActive(false);
        
        GameObject gameUI = GameObject.Find("GameUI");
        if (gameUI) gameUI.SetActive(true);
    }
}
