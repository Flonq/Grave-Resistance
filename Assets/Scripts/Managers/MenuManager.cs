using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MenuManager : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource buttonClickSound;
    
    [Header("UI Panels")]
    public GameObject settingsPanel;
    
    [Header("Volume Settings")]
    public Slider masterVolumeSlider;
    public TextMeshProUGUI masterVolumeValue;
    
    [Header("Graphics Settings")]
    public TMP_Dropdown graphicsDropdown;
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;
    
    [Header("Fade Transition")]
    public Animator fadeAnimator;
    
    [Header("Button References")]
    public Animator playButtonAnimator;
    public Animator settingsButtonAnimator;
    public Animator quitButtonAnimator;
    public Animator backButtonAnimator;
    
    // CACHE EDILEN WAIT OBJECTS
    private WaitForSeconds fadeWait = new WaitForSeconds(0.5f);
    private WaitForSeconds shortWait = new WaitForSeconds(0.2f);
    
    private void Start()
    {
        // FADEPANEL'I KORU
        if (fadeAnimator != null)
        {
            DontDestroyOnLoad(fadeAnimator.gameObject);
            Debug.Log("FadePanel marked as DontDestroyOnLoad");
        }
            
        // Settings panel başlangıçta kapalı
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
            
        // Volume slider event'i bağla
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            
        // Graphics dropdown event'i bağla
        if (graphicsDropdown != null)
            graphicsDropdown.onValueChanged.AddListener(OnGraphicsQualityChanged);
            
        // Resolution dropdown event'i bağla
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            
        // Fullscreen toggle event'i bağla
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            
        // FADEPANEL OTOMATIK BUL
        if (fadeAnimator == null)
        {
            GameObject fadePanel = GameObject.Find("FadePanel");
            if (fadePanel != null)
            {
                fadeAnimator = fadePanel.GetComponent<Animator>();
                DontDestroyOnLoad(fadeAnimator.gameObject);
                Debug.Log("FadeAnimator automatically found and preserved!");
            }
            else
            {
                Debug.LogError("FadePanel GameObject not found in scene!");
            }
        }
            
        // Başlangıç değerlerini ayarla
        LoadSettings();
    }
    
    // PLAY Button Function
    public void PlayGame()
    {
        PlayButtonSound();
        FadeToScene("GraveResistance");
    }
    
    // SETTINGS Button Function  
    public void OpenSettings()
    {
        PlayButtonSound();
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            // Animation trigger et
            Animator animator = settingsPanel.GetComponent<Animator>();
            if (animator != null)
                animator.SetBool("IsOpen", true);
            Debug.Log("Settings Opened!");
        }
    }
    
    // BACK Button Function (Settings'ten çık)
    public void CloseSettings()
    {
        PlayButtonSound();
        // Animation trigger et
        if (settingsPanel != null)
        {
            Animator animator = settingsPanel.GetComponent<Animator>();
            if (animator != null)
                animator.SetBool("IsOpen", false);
            Debug.Log("Settings Closing...");
            
            // 0.5 saniye sonra panel'i kapat
            StartCoroutine(CloseSettingsAfterAnimation());
        }
    }
    
    // COROUTINE - Animation bitince panel kapat
    private System.Collections.IEnumerator CloseSettingsAfterAnimation()
    {
        yield return new WaitForSeconds(0.5f); // Animation süresi
        
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            SaveSettings();
            Debug.Log("Settings Closed!");
        }
    }
    
    // QUIT Button Function
    public void QuitGame()
    {
        PlayButtonSound();
        Debug.Log("QUIT Game!");
        
        #if UNITY_EDITOR
        // Unity Editor'da
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        // Build'de
        Application.Quit();
        #endif
    }
    
    // VOLUME değiştiğinde
    public void OnMasterVolumeChanged(float value)
    {
        // Eski: AudioListener.volume = value;
        // Yeni: AudioManager kullan
        if (AudioManager.Instance != null)
            AudioManager.Instance.OnMasterVolumeChanged(value);
        
        if (masterVolumeValue != null)
            masterVolumeValue.text = Mathf.RoundToInt(value * 100) + "%";
    }
    
    // GRAPHICS QUALITY değiştiğinde
    public void OnGraphicsQualityChanged(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }
    
    // RESOLUTION değiştiğinde
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
            Debug.Log("Resolution: " + newRes.width + "x" + newRes.height);
        }
    }
    
    // FULLSCREEN değiştiğinde
    public void OnFullscreenChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
    
    // AYARLARI KAYDET
    private void SaveSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", AudioListener.volume);
        PlayerPrefs.SetInt("GraphicsQuality", QualitySettings.GetQualityLevel());
        PlayerPrefs.SetInt("ResolutionIndex", resolutionDropdown.value);
        PlayerPrefs.SetInt("Fullscreen", Screen.fullScreen ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    // AYARLARI YÜKLEsaveLE
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
    
    private void PlayButtonSound()
    {
        // Eski kod: buttonClickSound.Play()
        // Yeni kod: AudioManager kullan
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();
    }

    // BUTTON HOVER Sound
    public void PlayButtonHover()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonHover();
    }

    // BUTTON ANIMATION CONTROL
    public void StartButtonHover()
    {
        // PLAY Button'un Animator'ını bul ve trigger et
        Animator animator = GameObject.Find("PlayButton").GetComponent<Animator>();
        if (animator != null)
            animator.SetBool("IsHovering", true);
    }

    public void EndButtonHover()
    {
        // PLAY Button'un Animator'ını bul ve trigger et
        Animator animator = GameObject.Find("PlayButton").GetComponent<Animator>();
        if (animator != null)
            animator.SetBool("IsHovering", false);
    }

    // SETTINGS BUTTON ANIMATION
    public void StartSettingsHover()
    {
        Animator animator = GameObject.Find("SettingsButton").GetComponent<Animator>();
        if (animator != null)
            animator.SetBool("IsHovering", true);
    }

    public void EndSettingsHover()
    {
        Animator animator = GameObject.Find("SettingsButton").GetComponent<Animator>();
        if (animator != null)
            animator.SetBool("IsHovering", false);
    }

    // QUIT BUTTON ANIMATION
    public void StartQuitHover()
    {
        Animator animator = GameObject.Find("QuitButton").GetComponent<Animator>();
        if (animator != null)
            animator.SetBool("IsHovering", true);
    }

    public void EndQuitHover()
    {
        Animator animator = GameObject.Find("QuitButton").GetComponent<Animator>();
        if (animator != null)
            animator.SetBool("IsHovering", false);
    }

    // BACK BUTTON ANIMATION
    public void StartBackHover()
    {
        if (backButtonAnimator != null)
            backButtonAnimator.SetBool("IsHovering", true);
    }

    public void EndBackHover()
    {
        Animator animator = GameObject.Find("BackButton").GetComponent<Animator>();
        if (animator != null)
            animator.SetBool("IsHovering", false);
    }

    // FADE TRANSITION METHODS
    public void FadeToScene(string sceneName)
    {
        StartCoroutine(FadeAndLoadScene(sceneName));
    }

    private System.Collections.IEnumerator FadeAndLoadScene(string sceneName)
    {
        if (fadeAnimator != null)
            fadeAnimator.SetTrigger("FadeIn");
            
        yield return fadeWait;
        SceneManager.LoadScene(sceneName);
        
        yield return shortWait;
        if (fadeAnimator != null)
            fadeAnimator.SetTrigger("FadeOut");
    }
} 