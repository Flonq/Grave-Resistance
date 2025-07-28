using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // YENİ EKLEME

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pausePanel;
    public Button resumeButton;
    public Button settingsButton;
    public Button mainMenuButton;
    
    [Header("Settings Panel")]
    public GameObject settingsPanel;
    
    // YENİ SETTINGS REFERANSLARI EKLE:
    [Header("Settings UI")]
    public Slider masterVolumeSlider;
    public TextMeshProUGUI masterVolumeValue;
    public TMP_Dropdown graphicsDropdown;
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;
    public Button backButton;
    
    [Header("Audio")]
    public AudioSource buttonClickSound;
    
    // Singleton pattern
    public static PauseManager Instance { get; private set; }
    
    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("PauseManager Instance created successfully!");
        }
        else
        {
            Debug.LogWarning("Multiple PauseManager instances detected!");
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        Debug.Log("PauseManager Start() method called!"); // 👈 YENİ DEBUG
        
        // Button event'lerini bağla
        if (resumeButton) 
        {
            resumeButton.onClick.AddListener(ResumeGame);
            Debug.Log("Resume button connected");
        }
        if (settingsButton) 
        {
            settingsButton.onClick.AddListener(OpenSettings);
            Debug.Log("Settings button connected");
        }
        if (mainMenuButton) 
        {
            mainMenuButton.onClick.AddListener(GoToMainMenu);
            Debug.Log("Main menu button connected");
        }
        
        // Settings UI events
        if (backButton) 
        {
            backButton.onClick.AddListener(CloseSettings);
            Debug.Log("Back button event connected");
        }
        
        if (masterVolumeSlider) 
        {
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            Debug.Log("Volume slider event connected");
        }
        
        // Panel'leri başlangıçta kapat
        if (pausePanel) pausePanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        
        LoadSettings();
        
        Debug.Log("All PauseManager setup completed!");
    }
    
    // Resume Game
    public void ResumeGame()
    {
        PlayButtonSound();
        
        // Settings panel'i kapat
        if (settingsPanel && settingsPanel.activeInHierarchy)
        {
            settingsPanel.SetActive(false);
        }
        
        // 🔧 OYUN NESNELERİNİ TEKRAR AKTİF ET
        ResumeGameObjects();
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
        }
    }
    
    // Settings Menu
    public void OpenSettings()
    {
        PlayButtonSound();
        
        if (settingsPanel)
        {
            // 🔧 PAUSE PANEL'İ GİZLE
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
                Debug.Log("PausePanel hidden");
            }
            
            // 🔧 SETTINGS PANEL'İ GÖSTER
            settingsPanel.SetActive(true);
            Debug.Log("SettingsPanel activated");
            
            // 🔧 ANIMATOR'I KAPAT (Alpha sorunu için)
            Animator animator = settingsPanel.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
                Debug.Log("Settings Animator disabled");
            }
            
            // 🔧 CANVAS GROUP'U AYARLA
            CanvasGroup canvasGroup = settingsPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                Debug.Log("CanvasGroup configured: Alpha=1, Interactable=True");
            }
            
            // 🔧 TEMPORARY TIME SCALE FIX'İ KALDIR!
            // StartCoroutine(TemporaryTimeScaleFix()); ← BU SATIRI SİL!
            
            Debug.Log("Settings opened successfully");
        }
    }

    // Close Settings
    public void CloseSettings()
    {
        PlayButtonSound();
        
        if (settingsPanel != null)
        {
            Debug.Log("Closing Settings Panel");
            
            // 🔧 SETTINGS PANEL'İ GİZLE
            settingsPanel.SetActive(false);
            
            // 🔧 PAUSE PANEL'İ GÖSTER
            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
                Debug.Log("PausePanel restored");
            }
            
            SaveSettings();
        }
    }
    
    private System.Collections.IEnumerator CloseSettingsAfterAnimation()
    {
        yield return new WaitForSeconds(0.5f); // Animation süresi
        
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            SaveSettings();
            Debug.Log("Settings Panel closed");
        }
    }
    
    // YENİ: Volume Control
    public void OnMasterVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.OnMasterVolumeChanged(value);
        
        if (masterVolumeValue != null)
            masterVolumeValue.text = Mathf.RoundToInt(value * 100) + "%";
    }
    
    // YENİ: Graphics Quality
    public void OnGraphicsQualityChanged(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }
    
    // YENİ: Resolution
    public void OnResolutionChanged(int resolutionIndex)
    {
        Resolution[] resolutions = {
            new Resolution { width = 1280, height = 720 },
            new Resolution { width = 1366, height = 768 },
            new Resolution { width = 1920, height = 1080 },
            new Resolution { width = 2560, height = 1440 },
            new Resolution { width = 3840, height = 2160 }
        };
        
        if (resolutionIndex < resolutions.Length)
        {
            Resolution newRes = resolutions[resolutionIndex];
            Screen.SetResolution(newRes.width, newRes.height, Screen.fullScreen);
        }
    }
    
    // YENİ: Fullscreen
    public void OnFullscreenChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
    
    // YENİ: Save Settings
    private void SaveSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", AudioListener.volume);
        PlayerPrefs.SetInt("GraphicsQuality", QualitySettings.GetQualityLevel());
        if (resolutionDropdown) PlayerPrefs.SetInt("ResolutionIndex", resolutionDropdown.value);
        PlayerPrefs.SetInt("Fullscreen", Screen.fullScreen ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    // YENİ: Load Settings
    private void LoadSettings()
    {
        // Volume
        float volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        AudioListener.volume = volume;
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = volume;
        OnMasterVolumeChanged(volume);
        
        // Graphics Quality
        int graphics = PlayerPrefs.GetInt("GraphicsQuality", 3);
        QualitySettings.SetQualityLevel(graphics);
        if (graphicsDropdown != null)
            graphicsDropdown.value = graphics;
        
        // Resolution
        int resolution = PlayerPrefs.GetInt("ResolutionIndex", 2);
        if (resolutionDropdown != null)
            resolutionDropdown.value = resolution;
        
        // Fullscreen
        bool fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        Screen.fullScreen = fullscreen;
        if (fullscreenToggle != null)
            fullscreenToggle.isOn = fullscreen;
    }
    
    // Main Menu'ye dön
    public void GoToMainMenu()
    {
        PlayButtonSound();
        
        // Time scale'i düzelt
        Time.timeScale = 1f;
        
        // GameManager'ı temizle
        if (GameManager.Instance != null)
        {
            Destroy(GameManager.Instance.gameObject);
        }
        
        // MainMenu sahnesine git
        SceneManager.LoadScene("MainMenu");
    }
    
    // Button sound
    void PlayButtonSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }
    }

    // 🔧 YENİ: Oyun nesnelerini manuel pause et
    private void PauseGameObjects()
    {
        // Player'ı durdur
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var playerController = player.GetComponent<MonoBehaviour>();
            if (playerController != null)
            {
                playerController.enabled = false;
            }
        }
        
        // Zombileri durdur
        GameObject[] zombies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject zombie in zombies)
        {
            var zombieController = zombie.GetComponent<MonoBehaviour>();
            if (zombieController != null)
            {
                zombieController.enabled = false;
            }
        }
        
        // Wave Manager'ı durdur
        if (FindFirstObjectByType<WaveManager>() != null)
        {
            FindFirstObjectByType<WaveManager>().enabled = false;
        }
        
        Debug.Log("Game objects manually paused");
    }

    // 🔧 YENİ: Oyun nesnelerini tekrar aktif et  
    private void ResumeGameObjects()
    {
        // Player'ı aktif et
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var playerController = player.GetComponent<MonoBehaviour>();
            if (playerController != null)
            {
                playerController.enabled = true;
            }
        }
        
        // Zombileri aktif et
        GameObject[] zombies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject zombie in zombies)
        {
            var zombieController = zombie.GetComponent<MonoBehaviour>();
            if (zombieController != null)
            {
                zombieController.enabled = true;
            }
        }
        
        // Wave Manager'ı aktif et
        if (FindFirstObjectByType<WaveManager>() != null)
        {
            FindFirstObjectByType<WaveManager>().enabled = true;
        }
        
        Debug.Log("Game objects resumed");
    }

    // 🔧 MANUEL UI CLICK HANDLING
    void Update()
    {
        // Sadece Settings panel açıkken çalış
        if (settingsPanel != null && settingsPanel.activeInHierarchy)
        {
            HandleManualUIInput();
        }
    }

    // 🔧 YENİ: Manuel UI Input
    private void HandleManualUIInput()
    {
        // 🔧 YENİ INPUT SYSTEM KULLAN
        if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            
            // Volume Slider kontrolü
            if (masterVolumeSlider != null && IsMouseOverUI(masterVolumeSlider.gameObject, mousePos))
            {
                Debug.Log("Manual Volume Slider click detected");
                HandleVolumeSliderClick(mousePos);
            }
            
            // Back Button kontrolü
            if (backButton != null && IsMouseOverUI(backButton.gameObject, mousePos))
            {
                Debug.Log("Manual Back Button click detected");
                CloseSettings();
            }
            
            // Graphics Dropdown kontrolü
            if (graphicsDropdown != null && IsMouseOverUI(graphicsDropdown.gameObject, mousePos))
            {
                Debug.Log("Manual Graphics Dropdown click detected");
            }
        }
    }

    // 🔧 YENİ: Mouse UI üzerinde mi kontrol et
    private bool IsMouseOverUI(GameObject uiElement, Vector2 mousePos)
    {
        RectTransform rectTransform = uiElement.GetComponent<RectTransform>();
        if (rectTransform == null) return false;
        
        // Screen pozisyonunu RectTransform'a çevir
        Camera camera = null; // UI Camera yoksa null
        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePos, camera);
    }

    // 🔧 YENİ: Volume Slider manuel kontrolü
    private void HandleVolumeSliderClick(Vector2 mousePos)
    {
        if (masterVolumeSlider == null) return;
        
        RectTransform sliderRect = masterVolumeSlider.GetComponent<RectTransform>();
        if (sliderRect == null) return;
        
        // Mouse pozisyonunu slider'ın local pozisyonuna çevir
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(sliderRect, mousePos, null, out localPoint))
        {
            // Slider'ın genişliği üzerinde pozisyonu hesapla
            float sliderWidth = sliderRect.rect.width;
            float normalizedValue = (localPoint.x + sliderWidth / 2) / sliderWidth;
            normalizedValue = Mathf.Clamp01(normalizedValue);
            
            // Slider değerini güncelle
            masterVolumeSlider.value = normalizedValue;
            
            // Volume'u değiştir
            OnMasterVolumeChanged(normalizedValue);
            
            Debug.Log($"Volume manually set to: {normalizedValue:F2}");
        }
    }

    // 🔧 YENİ: Geçici Time Scale Fix
    private System.Collections.IEnumerator TemporaryTimeScaleFix()
    {
        // Sadece UI'nin çalışması için çok kısa süre
        Time.timeScale = 0.001f; // Neredeyse durmuş ama UI çalışır
        yield return new WaitForSecondsRealtime(0.001f);
        Time.timeScale = 0f; // Tekrar durdur
    }
}
