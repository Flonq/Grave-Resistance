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
            
            // 🔧 YENİ INPUT SYSTEM KULLAN
            if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Debug.Log("⌨️ ESC tuşuna basıldı!");
                TogglePause();
            }
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
        Debug.Log("PauseGame called!");
        Debug.Log($"pauseMenu is null: {pauseMenu == null}");
        
        SetGameState(GameState.Paused);
        if (pauseMenu) 
        {
            pauseMenu.SetActive(true);
            Debug.Log("Pause menu activated!");
        }
        else
        {
            Debug.LogError("Pause menu reference is missing!");
        }
        
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
        
        // Geçici olarak devre dışı
        // StartCoroutine(UpdateStatsAfterFrame());
        
        Debug.Log($"Game Over Stats: Kills: {killCount}, Score: {playerScore}, Time: {GetFormattedTime()}");
    }

    System.Collections.IEnumerator UpdateStatsAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        
        // SAFE search with timeout
        float timeout = 1f; // 1 saniye timeout
        float elapsed = 0f;
        GameObject statsText = null;
        
        while (elapsed < timeout && statsText == null)
        {
            statsText = GameObject.Find("StatsText");
            if (statsText != null) break;
            
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
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
            Debug.LogWarning("StatsText not found - Game Over UI may be missing");
            // Infinite error'u engelle
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
        // Find and hide specific menus in new scene
        GameObject gameOverCanvas = GameObject.Find("GameOverCanvas");
        if (gameOverCanvas) gameOverCanvas.SetActive(false);
        
        // Ensure game UI is active
        GameObject gameUI = GameObject.Find("GameUI");
        if (gameUI) gameUI.SetActive(true);
        
        // PausePanel'i başlangıçta kapalı tut
        if (pauseMenu) pauseMenu.SetActive(false);
    }
}
